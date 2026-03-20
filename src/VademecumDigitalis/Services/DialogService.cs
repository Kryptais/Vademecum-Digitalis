using Microsoft.Maui.Controls;

namespace VademecumDigitalis.Services
{
    public class DialogService : IDialogService
    {
        private Page MainPage => Application.Current?.MainPage
            ?? throw new InvalidOperationException("No main page available.");

        public Task DisplayAlert(string title, string message, string cancel) =>
            MainPage.DisplayAlert(title, message, cancel);

        public Task<bool> DisplayAlert(string title, string message, string accept, string cancel) =>
            MainPage.DisplayAlert(title, message, accept, cancel);

        public Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel",
            string? placeholder = null, Keyboard? keyboard = null, string? initialValue = null) =>
            MainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder,
                keyboard: keyboard ?? Keyboard.Default, initialValue: initialValue ?? string.Empty);

        public Task<string?> DisplayActionSheet(string title, string cancel, string? destruction, params string[] buttons) =>
            MainPage.DisplayActionSheet(title, cancel, destruction, buttons);

        public Task PushModalAsync(Page page) =>
            MainPage.Navigation.PushModalAsync(page);
    }
}
