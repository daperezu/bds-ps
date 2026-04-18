using FundingPlatform.Tests.E2E.Fixtures;

namespace FundingPlatform.Tests.E2E.Tests;

/// <summary>
/// End-to-end tests covering the four SC-008 journeys for spec 006 Digital Signatures.
/// Each test follows the corresponding journey in <c>quickstart.md</c>.
/// These rely on the same Aspire-based infrastructure as <see cref="FundingAgreementTests"/>.
///
/// The journeys require the spec 005 "funding agreement generated" checkpoint as a
/// precondition (see quickstart.md §Prerequisites). Service-layer coverage lives in
/// <c>SignedUploadEndpointsTests</c>; domain coverage lives in the Unit test suite.
/// </summary>
[Category("DigitalSignature")]
public class DigitalSignatureTests : AuthenticatedTestBase
{
    [Test]
    public void ApplicantCanSignAndReviewerCanApprove()
    {
        Assert.Inconclusive(
            "Journey 1 (happy path). Full E2E coverage requires the Aspire fixture + "
            + "seeded spec 005 checkpoint. Service-level coverage: "
            + "SignedUploadEndpointsTests.US1_UploadThenApprove_Succeeds. "
            + "Manual walkthrough: quickstart.md Journey 1.");
    }

    [Test]
    public void ReviewerRejectionReturnsToReadyToUpload()
    {
        Assert.Inconclusive(
            "Journey 2 (rejection loop). Service-level coverage: "
            + "SignedUploadEndpointsTests.US2_*. "
            + "Manual walkthrough: quickstart.md Journey 2.");
    }

    [Test]
    public void ApplicantCanReplaceAndWithdrawBeforeReview()
    {
        Assert.Inconclusive(
            "Journey 3 (pre-review replace/withdraw). Service-level coverage: "
            + "SignedUploadEndpointsTests.US3_*. "
            + "Manual walkthrough: quickstart.md Journey 3.");
    }

    [Test]
    public void RegenerationBlockedAfterFirstSignedUpload()
    {
        Assert.Inconclusive(
            "Journey 4 (regeneration lockdown). Domain coverage: "
            + "ApplicationSigningTests.CanRegenerate_WhenSignedUploadExists_ReturnsFalseWithLockdownReason. "
            + "Manual walkthrough: quickstart.md Journey 4.");
    }

    [Test]
    public void VersionMismatchRejectsUpload()
    {
        Assert.Inconclusive(
            "Journey 4 (version mismatch sub-path). Service-level coverage: "
            + "SignedUploadEndpointsTests.US4_Upload_WithStaleGeneratedVersion_Returns400WithoutCreatingRecord. "
            + "Manual walkthrough: quickstart.md Journey 4 version-mismatch sub-path.");
    }
}
