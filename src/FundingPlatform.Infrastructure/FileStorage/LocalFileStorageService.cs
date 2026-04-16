using FundingPlatform.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FundingPlatform.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _storagePath = configuration.GetValue<string>("FileStorage:Path") ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        using var outputStream = File.Create(filePath);
        await fileStream.CopyToAsync(outputStream);

        return filePath;
    }

    public Task DeleteFileAsync(string storagePath)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }
        return Task.CompletedTask;
    }

    public Task<Stream> GetFileAsync(string storagePath)
    {
        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException("File not found.", storagePath);
        }
        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }
}
