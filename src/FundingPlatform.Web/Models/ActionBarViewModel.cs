namespace FundingPlatform.Web.Models;

public enum ActionBarAlignment
{
    Start = 0,
    End = 1,
    SpaceBetween = 2
}

public sealed record ActionBarViewModel(
    IReadOnlyList<ActionItem> Actions,
    ActionBarAlignment Alignment = ActionBarAlignment.End);
