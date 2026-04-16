namespace FundingPlatform.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteFileAsync(string storagePath);
    Task<Stream> GetFileAsync(string storagePath);
}
