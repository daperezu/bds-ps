var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
                       .WithDataVolume("fundingplatform-sqldata")
                       .AddDatabase("fundingdb");

var syncfusionLicense = builder.Configuration["Syncfusion:LicenseKey"] ?? "";
var localeCode = builder.Configuration["FundingAgreement:LocaleCode"] ?? "es-CO";
var currencyIsoCode = builder.Configuration["FundingAgreement:CurrencyIsoCode"] ?? "COP";
var funderLegalName = builder.Configuration["FundingAgreement:Funder:LegalName"] ?? "";
var funderTaxId = builder.Configuration["FundingAgreement:Funder:TaxId"] ?? "";
var funderAddress = builder.Configuration["FundingAgreement:Funder:Address"] ?? "";
var funderContactEmail = builder.Configuration["FundingAgreement:Funder:ContactEmail"] ?? "";
var funderContactPhone = builder.Configuration["FundingAgreement:Funder:ContactPhone"] ?? "";

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
    .WithEnvironment("FundingAgreement__Funder__ContactPhone", funderContactPhone);

builder.Build().Run();
