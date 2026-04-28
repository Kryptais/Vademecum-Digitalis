using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels
{
    public partial class GlobalItemSearchViewModel : ObservableObject
    {
        private readonly InventoryViewModel _mainVm;
        private readonly IInventoryNavigationService _navigationService;

        public ObservableCollection<GlobalSearchResult> SearchResults { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        public GlobalItemSearchViewModel(
            InventoryViewModel mainVm,
            IInventoryNavigationService navigationService)
        {
            _mainVm = mainVm;
            _navigationService = navigationService;
        }

        partial void OnSearchTextChanged(string value)
        {
            PerformSearch(value);
        }

        private void PerformSearch(string term)
        {
            SearchResults.Clear();
            if (string.IsNullOrWhiteSpace(term) || _mainVm == null) return;

            foreach (var container in _mainVm.Containers)
            {
                var matches = container.Items.Where(i => i.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || 
                                                       (i.Details != null && i.Details.Contains(term, StringComparison.OrdinalIgnoreCase)));
                
                foreach (var item in matches)
                {
                    SearchResults.Add(new GlobalSearchResult(item, container));
                }
            }
        }

        [RelayCommand]
        private async Task ShowItem(GlobalSearchResult result)
        {
            if (result == null) return;

            await _navigationService.NavigateToContainerAsync(result.Container);
        }

        [RelayCommand]
        private async Task NavigateBack()
        {
            await _navigationService.NavigateBackAsync();
        }
    }
}
