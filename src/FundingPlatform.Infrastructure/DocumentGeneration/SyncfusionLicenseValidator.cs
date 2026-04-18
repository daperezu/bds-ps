using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;

namespace FundingPlatform.Infrastructure.DocumentGeneration;

public class SyncfusionLicenseValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncfusionLicenseValidator> _logger;

    public SyncfusionLicenseValidator(
        IConfiguration configuration,
        ILogger<SyncfusionLicenseValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void ValidateOrThrow()
    {
        var licenseKey = _configuration["Syncfusion:LicenseKey"];

        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            _logger.LogWarning(
                "Syncfusion license key is missing (configure 'Syncfusion:LicenseKey' for production). Generated PDFs will carry the Syncfusion evaluation watermark until a license is provided.");
            return;
        }

        SyncfusionLicenseProvider.RegisterLicense(licenseKey);

        try
        {
            var probe = new Syncfusion.HtmlConverter.HtmlToPdfConverter(
                Syncfusion.HtmlConverter.HtmlRenderingEngine.Blink);
            _ = probe;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Syncfusion HTML-to-PDF converter failed to initialize.");
            throw new InvalidOperationException(
                "Syncfusion HTML-to-PDF converter failed to initialize. Verify license key and runtime dependencies.",
                ex);
        }

        _logger.LogInformation("Syncfusion license registered and converter probe succeeded.");
    }
}
