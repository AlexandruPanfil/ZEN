using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace ZEN.Web.Components.Dialogs;

public partial class ConfirmationDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string Text { get; set; } = "Are you sure?";

    private void Yes() => MudDialog.Close(DialogResult.Ok(true));
    private void No() => MudDialog.Cancel();
}