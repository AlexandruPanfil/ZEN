using MudBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ZEN.Shared;

namespace ZEN.Web.Components.Dialogs;

public partial class ConfirmationDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Parameter] public string Text { get; set; } = "Are you sure?";

    protected override void OnInitialized()
    {
        if (Text == "Are you sure?")
            Text = L["Confirmation"];
    }

    private void Yes() => MudDialog.Close(DialogResult.Ok(true));
    private void No() => MudDialog.Cancel();
}