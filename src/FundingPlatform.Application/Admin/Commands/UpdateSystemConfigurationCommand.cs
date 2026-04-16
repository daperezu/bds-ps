namespace FundingPlatform.Application.Admin.Commands;

public record UpdateSystemConfigurationCommand(
    List<ConfigurationUpdate> Configurations);

public record ConfigurationUpdate(int Id, string Value);
