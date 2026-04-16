using FundingPlatform.Application.Admin.Commands;
using FundingPlatform.Application.DTOs;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;
using FundingPlatform.Domain.Interfaces;

namespace FundingPlatform.Application.Services;

public class AdminService
{
    private readonly IImpactTemplateRepository _impactTemplateRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;

    public AdminService(
        IImpactTemplateRepository impactTemplateRepository,
        ISystemConfigurationRepository systemConfigurationRepository)
    {
        _impactTemplateRepository = impactTemplateRepository;
        _systemConfigurationRepository = systemConfigurationRepository;
    }

    public async Task<List<ImpactTemplateDto>> GetAllImpactTemplatesAsync()
    {
        var templates = await _impactTemplateRepository.GetAllAsync();

        return templates.Select(t => new ImpactTemplateDto(
            t.Id,
            t.Name,
            t.Description,
            t.IsActive,
            t.Parameters.Select(p => new ImpactTemplateParameterDto(
                p.Id,
                p.Name,
                p.DisplayLabel,
                p.DataType.ToString(),
                p.IsRequired,
                p.ValidationRules,
                p.SortOrder)).ToList())).ToList();
    }

    public async Task<ImpactTemplateDto?> GetImpactTemplateByIdAsync(int id)
    {
        var template = await _impactTemplateRepository.GetByIdWithParametersAsync(id);
        if (template is null)
        {
            return null;
        }

        return new ImpactTemplateDto(
            template.Id,
            template.Name,
            template.Description,
            template.IsActive,
            template.Parameters.Select(p => new ImpactTemplateParameterDto(
                p.Id,
                p.Name,
                p.DisplayLabel,
                p.DataType.ToString(),
                p.IsRequired,
                p.ValidationRules,
                p.SortOrder)).ToList());
    }

    public async Task<int> CreateImpactTemplateAsync(CreateImpactTemplateCommand command)
    {
        var template = new ImpactTemplate(command.Name, command.Description, isActive: true);

        foreach (var paramDef in command.Parameters)
        {
            if (!Enum.TryParse<ParameterDataType>(paramDef.DataType, ignoreCase: true, out var dataType))
            {
                dataType = ParameterDataType.Text;
            }

            var parameter = new ImpactTemplateParameter(
                paramDef.Name,
                paramDef.DisplayLabel,
                dataType,
                paramDef.IsRequired,
                paramDef.ValidationRules,
                paramDef.SortOrder);

            template.AddParameter(parameter);
        }

        await _impactTemplateRepository.AddAsync(template);
        await _impactTemplateRepository.SaveChangesAsync();

        return template.Id;
    }

    public async Task UpdateImpactTemplateAsync(UpdateImpactTemplateCommand command)
    {
        var template = await _impactTemplateRepository.GetByIdWithParametersAsync(command.Id)
            ?? throw new InvalidOperationException($"Impact template {command.Id} not found.");

        template.Update(command.Name, command.Description);

        if (command.IsActive)
        {
            template.Activate();
        }
        else
        {
            template.Deactivate();
        }

        template.ClearParameters();

        foreach (var paramDef in command.Parameters)
        {
            if (!Enum.TryParse<ParameterDataType>(paramDef.DataType, ignoreCase: true, out var dataType))
            {
                dataType = ParameterDataType.Text;
            }

            var parameter = new ImpactTemplateParameter(
                paramDef.Name,
                paramDef.DisplayLabel,
                dataType,
                paramDef.IsRequired,
                paramDef.ValidationRules,
                paramDef.SortOrder);

            template.AddParameter(parameter);
        }

        await _impactTemplateRepository.UpdateAsync(template);
        await _impactTemplateRepository.SaveChangesAsync();
    }

    public async Task<List<SystemConfigurationDto>> GetAllSystemConfigurationsAsync()
    {
        var configs = await _systemConfigurationRepository.GetAllAsync();

        return configs.Select(c => new SystemConfigurationDto(
            c.Id,
            c.Key,
            c.Value,
            c.Description)).ToList();
    }

    public async Task UpdateSystemConfigurationAsync(UpdateSystemConfigurationCommand command)
    {
        foreach (var configUpdate in command.Configurations)
        {
            var config = await _systemConfigurationRepository.GetByIdAsync(configUpdate.Id)
                ?? throw new InvalidOperationException($"System configuration {configUpdate.Id} not found.");

            config.UpdateValue(configUpdate.Value);
            await _systemConfigurationRepository.UpdateAsync(config);
        }

        await _systemConfigurationRepository.SaveChangesAsync();
    }
}
