using Microsoft.Maui.Controls;

namespace VademecumDigitalis.Services
{
    public interface IDialogService
    {
        Task DisplayAlert(string title, string message, string cancel);
        Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
        Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null, Keyboard? keyboard = null, string? initialValue = null);
        Task<string?> DisplayActionSheet(string title, string cancel, string? destruction, params string[] buttons);
        Task PushModalAsync(Page page);
    }
}
