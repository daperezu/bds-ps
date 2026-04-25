var builder = DistributedApplication.CreateBuilder(args);

// Tests pass --EphemeralStorage=true to opt out of dev-convenience features that
// don't survive a fresh-container test run: the persistent SQL data volume and
// the auto-deploy SQL project. The E2E fixture deploys the dacpac itself via
// sqlpackage so it can wait synchronously for completion before tests start.
var ephemeralStorage = string.Equals(
    builder.Configuration["EphemeralStorage"], "true", StringComparison.OrdinalIgnoreCase);

var sqlBuilder = builder.AddSqlServer("sqlserver");
if (!ephemeralStorage)
{
    sqlBuilder = sqlBuilder.WithDataVolume("fundingplatform-sqldata");
}

var sqlServer = sqlBuilder.AddDatabase("fundingdb");

if (!ephemeralStorage)
{
    builder.AddSqlProject<Projects.FundingPlatform_Database>("database-schema")
           .WithReference(sqlServer);
}

var syncfusionLicense = builder.Configuration["Syncfusion:LicenseKey"] ?? "Ngo9BigBOggjHTQxAR8/V1JHaF1cXmhMYVJpR2NbeU5xdF9DZVZURGY/P1ZhSXxVdkFhXX1cdXFQRmJVU019XEE=";
var localeCode = builder.Configuration["FundingAgreement:LocaleCode"] ?? "es-CO";
var currencyIsoCode = builder.Configuration["FundingAgreement:CurrencyIsoCode"] ?? "COP";
var funderLegalName = builder.Configuration["FundingAgreement:Funder:LegalName"] ?? "";
var funderTaxId = builder.Configuration["FundingAgreement:Funder:TaxId"] ?? "";
var funderAddress = builder.Configuration["FundingAgreement:Funder:Address"] ?? "";
var funderContactEmail = builder.Configuration["FundingAgreement:Funder:ContactEmail"] ?? "";
var funderContactPhone = builder.Configuration["FundingAgreement:Funder:ContactPhone"] ?? "";
var signedUploadMaxSizeBytes = builder.Configuration["SignedUpload:MaxSizeBytes"] ?? "20971520";

builder.AddProject<Projects.FundingPlatform_Web>("webapp")
    .WithExternalHttpEndpoints()
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithEnvironment("Syncfusion__LicenseKey", syncfusionLicense)
    .WithEnvironment("FundingAgreement__LocaleCode", localeCode)
    .WithEnvironment("FundingAgreement__CurrencyIsoCode", currencyIsoCode)
    .WithEnvironment("FundingAgreement__Funder__LegalName", funderLegalName)
    .WithEnvironment("FundingAgreement__Funder__TaxId", funderTaxId)
    .WithEnvironment("FundingAgreement__Funder__Address", funderAddress)
    .WithEnvironment("FundingAgreement__Funder__ContactEmail", funderContactEmail)
    .WithEnvironment("FundingAgreement__Funder__ContactPhone", funderContactPhone)
    .WithEnvironment("SignedUpload__MaxSizeBytes", signedUploadMaxSizeBytes);

builder.Build().Run();
