using FundingPlatform.Application.DTOs;
using FundingPlatform.Domain.Entities;
using AppEntity = FundingPlatform.Domain.Entities.Application;

namespace FundingPlatform.Application.Services;

/// <summary>
/// Spec 011 (FR-043) — top-level service that turns Application aggregates into
/// JourneyViewModel objects (Full / Mini / Micro variants). Pure projection — no
/// state mutation. Bulk path avoids N+1 over VersionHistory.
/// </summary>
public interface IJourneyProjector
{
    JourneyViewModel Project(AppEntity application, JourneyVariant variant);
    IReadOnlyDictionary<int, JourneyViewModel> ProjectMany(
        IReadOnlyCollection<AppEntity> applications,
        JourneyVariant variant);
    int DaysInCurrentState(AppEntity application, DateTimeOffset asOfUtc);
}

public sealed class JourneyProjector : IJourneyProjector
{
    private readonly IJourneyStageResolver _stageResolver;
    private readonly IStageMappingProvider _stageMappings;

    public JourneyProjector(IJourneyStageResolver stageResolver, IStageMappingProvider stageMappings)
    {
        _stageResolver = stageResolver;
        _stageMappings = stageMappings;
    }

    public JourneyViewModel Project(AppEntity application, JourneyVariant variant)
    {
        var mainline = _stageResolver.BuildMainline(application);
        var branches = _stageResolver.BuildBranches(application);
        var current = _stageResolver.ResolveCurrent(application);
        // application number falls back to "APP-{id}" when no other surface owns it.
        var number = $"APP-{application.Id:D5}";

        // Mini / Micro keep the same mainline; the partial drops branches/labels.
        return new JourneyViewModel(
            ApplicationId: Guid.Empty, // domain Application uses int Id; we don't fabricate Guids here
            ApplicationNumber: number,
            CurrentMainlineStage: current,
            Mainline: mainline,
            Branches: variant == JourneyVariant.Full ? branches : Array.Empty<JourneyBranch>(),
            Variant: variant);
    }

    public IReadOnlyDictionary<int, JourneyViewModel> ProjectMany(
        IReadOnlyCollection<AppEntity> applications,
        JourneyVariant variant)
    {
        var dict = new Dictionary<int, JourneyViewModel>(applications.Count);
        foreach (var app in applications)
        {
            dict[app.Id] = Project(app, variant);
        }
        return dict;
    }

    public int DaysInCurrentState(AppEntity application, DateTimeOffset asOfUtc)
    {
        // The latest stage-transition VersionHistory entry sets the timestamp.
        DateTime latest = application.CreatedAt;
        foreach (var v in application.VersionHistory)
        {
            if (_stageMappings.IsStageTransition(v.Action) && v.Timestamp > latest)
            {
                latest = v.Timestamp;
            }
        }
        var span = asOfUtc - new DateTimeOffset(latest, TimeSpan.Zero);
        var days = (int)Math.Floor(span.TotalDays);
        return Math.Max(0, days);
    }
}
