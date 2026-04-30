using System.Security.Claims;
using FundingPlatform.Application.Applications.Commands;
using FundingPlatform.Application.Services;
using FundingPlatform.Domain.Interfaces;
using FundingPlatform.Infrastructure.Persistence;
using FundingPlatform.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Web.Controllers;

[Authorize(Roles = "Applicant")]
[Route("Application/{appId}/Item")]
public class ItemController : Controller
{
    private readonly ApplicationService _applicationService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly AppDbContext _dbContext;

    public ItemController(
        ApplicationService applicationService,
        ICategoryRepository categoryRepository,
        AppDbContext dbContext)
    {
        _applicationService = applicationService;
        _categoryRepository = categoryRepository;
        _dbContext = dbContext;
    }

    [HttpGet("Add")]
    public async Task<IActionResult> Add(int appId)
    {
        await VerifyOwnershipAsync(appId);

        var categories = await _categoryRepository.GetAllActiveAsync();
        var viewModel = new AddItemViewModel
        {
            ApplicationId = appId,
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost("Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int appId, AddItemViewModel model)
    {
        await VerifyOwnershipAsync(appId);

        if (!ModelState.IsValid)
        {
            var categories = await _categoryRepository.GetAllActiveAsync();
            model.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
            model.ApplicationId = appId;
            return View(model);
        }

        var command = new AddItemCommand(
            appId,
            model.ProductName,
            model.CategoryId,
            model.TechnicalSpecifications);

        await _applicationService.AddItemAsync(command);

        TempData["SuccessMessage"] = "Ítem agregado con éxito.";
        return RedirectToAction("Details", "Application", new { id = appId });
    }

    [HttpGet("{itemId}/Edit")]
    public async Task<IActionResult> Edit(int appId, int itemId)
    {
        await VerifyOwnershipAsync(appId);

        var application = await _applicationService.GetApplicationAsync(appId);
        if (application is null)
        {
            return NotFound();
        }

        var item = application.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return NotFound();
        }

        var categories = await _categoryRepository.GetAllActiveAsync();
        var viewModel = new EditItemViewModel
        {
            Id = item.Id,
            ApplicationId = appId,
            ProductName = item.ProductName,
            CategoryId = item.CategoryId,
            TechnicalSpecifications = item.TechnicalSpecifications,
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost("{itemId}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int appId, int itemId, EditItemViewModel model)
    {
        await VerifyOwnershipAsync(appId);

        if (!ModelState.IsValid)
        {
            var categories = await _categoryRepository.GetAllActiveAsync();
            model.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
            model.ApplicationId = appId;
            model.Id = itemId;
            return View(model);
        }

        var command = new UpdateItemCommand(
            itemId,
            appId,
            model.ProductName,
            model.CategoryId,
            model.TechnicalSpecifications);

        await _applicationService.UpdateItemAsync(command);

        TempData["SuccessMessage"] = "Ítem actualizado con éxito.";
        return RedirectToAction("Details", "Application", new { id = appId });
    }

    [HttpPost("{itemId}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int appId, int itemId)
    {
        await VerifyOwnershipAsync(appId);

        var command = new RemoveItemCommand(itemId, appId);
        await _applicationService.RemoveItemAsync(command);

        TempData["SuccessMessage"] = "Ítem eliminado con éxito.";
        return RedirectToAction("Details", "Application", new { id = appId });
    }

    [HttpGet("{id}/Impact")]
    public async Task<IActionResult> Impact(int appId, int id)
    {
        await VerifyOwnershipAsync(appId);

        var application = await _applicationService.GetApplicationAsync(appId);
        if (application is null)
        {
            return NotFound();
        }

        var item = application.Items.FirstOrDefault(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        var templates = await _applicationService.GetImpactTemplatesAsync();

        var viewModel = new ImpactViewModel
        {
            ApplicationId = appId,
            ItemId = id,
            ItemProductName = item.ProductName,
            SelectedTemplateId = item.Impact?.ImpactTemplateId,
            Templates = templates.Select(t => new ImpactTemplateOptionViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description
            }).ToList()
        };

        // If there's an existing impact, populate parameter values
        if (item.Impact is not null)
        {
            var selectedTemplate = templates.FirstOrDefault(t => t.Id == item.Impact.ImpactTemplateId);
            if (selectedTemplate is not null)
            {
                viewModel.Parameters = selectedTemplate.Parameters.Select(p => new ImpactParameterInputViewModel
                {
                    ParameterId = p.Id,
                    Name = p.Name,
                    DisplayLabel = p.DisplayLabel,
                    DataType = p.DataType,
                    IsRequired = p.IsRequired,
                    Value = item.Impact.ParameterValues
                        .FirstOrDefault(pv => pv.ParameterId == p.Id)?.Value
                }).ToList();
            }
        }

        return View(viewModel);
    }

    [HttpPost("{id}/Impact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Impact(int appId, int id, ImpactViewModel model)
    {
        await VerifyOwnershipAsync(appId);

        if (!model.SelectedTemplateId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SelectedTemplateId), "Seleccione una plantilla de impacto.");
        }

        if (!ModelState.IsValid || !model.SelectedTemplateId.HasValue)
        {
            var templates = await _applicationService.GetImpactTemplatesAsync();
            model.Templates = templates.Select(t => new ImpactTemplateOptionViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description
            }).ToList();
            model.ApplicationId = appId;
            model.ItemId = id;
            return View(model);
        }

        var parameterValues = new Dictionary<int, string?>();
        if (model.Parameters is not null)
        {
            foreach (var param in model.Parameters)
            {
                parameterValues[param.ParameterId] = param.Value;
            }
        }

        var command = new SetItemImpactCommand(
            appId,
            id,
            model.SelectedTemplateId.Value,
            parameterValues);

        await _applicationService.SetItemImpactAsync(command);

        TempData["SuccessMessage"] = "Evaluación de impacto guardada con éxito.";
        return RedirectToAction("Details", "Application", new { id = appId });
    }

    [HttpGet("TemplateParameters/{templateId}")]
    public async Task<IActionResult> GetTemplateParameters(int appId, int templateId)
    {
        var templates = await _applicationService.GetImpactTemplatesAsync();
        var template = templates.FirstOrDefault(t => t.Id == templateId);

        if (template is null)
        {
            return NotFound();
        }

        var parameters = template.Parameters.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            displayLabel = p.DisplayLabel,
            dataType = p.DataType,
            isRequired = p.IsRequired
        });

        return Json(parameters);
    }

    private async Task<int> GetCurrentApplicantIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var applicant = await _dbContext.Applicants
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return applicant?.Id ?? throw new InvalidOperationException("Applicant not found for current user.");
    }

    private async Task VerifyOwnershipAsync(int appId)
    {
        var applicantId = await GetCurrentApplicantIdAsync();
        var application = await _applicationService.GetApplicationAsync(appId);

        if (application is null || application.ApplicantId != applicantId)
        {
            throw new UnauthorizedAccessException("You do not own this application.");
        }
    }
}
