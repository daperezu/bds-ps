using Microsoft.Data.SqlClient;

namespace FundingPlatform.Tests.E2E.Fixtures;

/// <summary>
/// Seeds FundingAgreement / SignedUpload / SigningReviewDecision rows directly via SQL,
/// bypassing the Syncfusion-backed PDF generation path. Used by E2E tests that need a
/// ResponseFinalized-plus-agreement or AgreementExecuted starting state but cannot rely
/// on a Syncfusion license being present in the test environment.
///
/// The Web app runs in-process from the repo (not containerized), so a placeholder file
/// written to a process-accessible path (e.g. /tmp) is reachable by the file-storage
/// service via its <c>File.OpenRead(StoragePath)</c> path.
/// </summary>
public static class FundingAgreementSeeder
{
    private static readonly byte[] PlaceholderPdfBytes =
        System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\nseeded placeholder\n%%EOF\n");

    /// <summary>
    /// Inserts a FundingAgreement row for the given application (if none exists yet)
    /// and writes a placeholder PDF file at an OS temp path. Returns the storage path.
    /// </summary>
    public static async Task<string> SeedGeneratedAgreementAsync(
        string connectionString, int applicationId, string generatedByUserEmail)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // Idempotent: if an agreement already exists for this application, return its path.
        var existingPath = await TryGetAgreementStoragePathAsync(conn, applicationId);
        if (existingPath is not null) return existingPath;

        var pdfPath = Path.Combine(Path.GetTempPath(), $"fa-seed-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(pdfPath, PlaceholderPdfBytes);

        var userId = await GetUserIdByEmailAsync(conn, generatedByUserEmail);

        const string sql = @"
INSERT INTO dbo.FundingAgreements
    (ApplicationId, FileName, ContentType, Size, StoragePath, GeneratedAtUtc, GeneratedByUserId, GeneratedVersion)
VALUES
    (@appId, @fileName, @contentType, @size, @storagePath, @generatedAt, @userId, 1);
SELECT SCOPE_IDENTITY();";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@appId", applicationId);
        cmd.Parameters.AddWithValue("@fileName", $"FundingAgreement-{applicationId}.pdf");
        cmd.Parameters.AddWithValue("@contentType", "application/pdf");
        cmd.Parameters.AddWithValue("@size", PlaceholderPdfBytes.LongLength);
        cmd.Parameters.AddWithValue("@storagePath", pdfPath);
        cmd.Parameters.AddWithValue("@generatedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@userId", userId);

        await cmd.ExecuteScalarAsync();
        return pdfPath;
    }

    /// <summary>
    /// Extends <see cref="SeedGeneratedAgreementAsync"/> by inserting an Approved signed
    /// upload with a decision row and flipping the application state to AgreementExecuted (6).
    /// Returns the signed-upload storage path.
    /// </summary>
    public static async Task<string> SeedExecutedAgreementAsync(
        string connectionString,
        int applicationId,
        string generatedByUserEmail,
        string applicantUserEmail,
        string reviewerUserEmail)
    {
        await SeedGeneratedAgreementAsync(connectionString, applicationId, generatedByUserEmail);

        var signedPdfPath = Path.Combine(Path.GetTempPath(), $"fa-signed-seed-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(signedPdfPath, PlaceholderPdfBytes);

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var applicantUserId = await GetUserIdByEmailAsync(conn, applicantUserEmail);
        var reviewerUserId = await GetUserIdByEmailAsync(conn, reviewerUserEmail);
        var agreementId = await GetFundingAgreementIdAsync(conn, applicationId);

        // Status = 4 (Approved) per SignedUploadStatus enum.
        const string insertUploadSql = @"
INSERT INTO dbo.SignedUploads
    (FundingAgreementId, UploaderUserId, GeneratedVersionAtUpload, FileName, ContentType, Size, StoragePath, UploadedAtUtc, Status)
VALUES
    (@agreementId, @uploaderUserId, 1, @fileName, 'application/pdf', @size, @storagePath, @uploadedAt, 4);
SELECT SCOPE_IDENTITY();";

        int uploadId;
        using (var cmd = new SqlCommand(insertUploadSql, conn))
        {
            cmd.Parameters.AddWithValue("@agreementId", agreementId);
            cmd.Parameters.AddWithValue("@uploaderUserId", applicantUserId);
            cmd.Parameters.AddWithValue("@fileName", $"signed-{applicationId}.pdf");
            cmd.Parameters.AddWithValue("@size", PlaceholderPdfBytes.LongLength);
            cmd.Parameters.AddWithValue("@storagePath", signedPdfPath);
            cmd.Parameters.AddWithValue("@uploadedAt", DateTime.UtcNow);
            var scalar = await cmd.ExecuteScalarAsync();
            uploadId = Convert.ToInt32(scalar);
        }

        const string insertDecisionSql = @"
INSERT INTO dbo.SigningReviewDecisions
    (SignedUploadId, Outcome, ReviewerUserId, Comment, DecidedAtUtc)
VALUES
    (@uploadId, 0, @reviewerUserId, NULL, @decidedAt);";

        using (var cmd = new SqlCommand(insertDecisionSql, conn))
        {
            cmd.Parameters.AddWithValue("@uploadId", uploadId);
            cmd.Parameters.AddWithValue("@reviewerUserId", reviewerUserId);
            cmd.Parameters.AddWithValue("@decidedAt", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }

        const string updateStateSql = @"
UPDATE dbo.Applications SET State = 6, UpdatedAt = @now WHERE Id = @appId;";

        using (var cmd = new SqlCommand(updateStateSql, conn))
        {
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@appId", applicationId);
            await cmd.ExecuteNonQueryAsync();
        }

        return signedPdfPath;
    }

    private static async Task<string?> TryGetAgreementStoragePathAsync(SqlConnection conn, int applicationId)
    {
        using var cmd = new SqlCommand(
            "SELECT StoragePath FROM dbo.FundingAgreements WHERE ApplicationId = @appId", conn);
        cmd.Parameters.AddWithValue("@appId", applicationId);
        var result = await cmd.ExecuteScalarAsync();
        return result is null || result == DBNull.Value ? null : (string)result;
    }

    private static async Task<string> GetUserIdByEmailAsync(SqlConnection conn, string email)
    {
        using var cmd = new SqlCommand(
            "SELECT Id FROM dbo.AspNetUsers WHERE NormalizedEmail = @email", conn);
        cmd.Parameters.AddWithValue("@email", email.ToUpperInvariant());
        var result = await cmd.ExecuteScalarAsync();
        if (result is null || result == DBNull.Value)
            throw new InvalidOperationException($"User not found by email: {email}");
        return (string)result;
    }

    private static async Task<int> GetFundingAgreementIdAsync(SqlConnection conn, int applicationId)
    {
        using var cmd = new SqlCommand(
            "SELECT Id FROM dbo.FundingAgreements WHERE ApplicationId = @appId", conn);
        cmd.Parameters.AddWithValue("@appId", applicationId);
        var result = await cmd.ExecuteScalarAsync();
        if (result is null || result == DBNull.Value)
            throw new InvalidOperationException($"FundingAgreement not found for application {applicationId}");
        return Convert.ToInt32(result);
    }
}
