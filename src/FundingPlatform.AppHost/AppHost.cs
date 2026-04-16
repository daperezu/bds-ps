var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
                       .WithDataVolume("fundingplatform-sqldata")
                       .AddDatabase("fundingdb");

builder.AddProject<Projects.FundingPlatform_Web>("webapp")
    .WithExternalHttpEndpoints()
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

builder.Build().Run();
