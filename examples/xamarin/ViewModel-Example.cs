// Xamarin Forms Prism ViewModel Example
// Reference: skills/xamarin-forms-development.md

using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;

namespace YourApp.ViewModels
{
    /// <summary>
    /// Example ViewModel for a list page with search functionality
    /// Demonstrates Prism MVVM patterns with Reactive Extensions
    /// </summary>
    public class ItemListViewModel : BindableBase, IDestructible
    {
        private readonly INavigationService _navigationService;
        private readonly CompositeDisposable _disposables = new();

        public ItemListViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            // Initialize commands
            NavigateToDetailCommand = new DelegateCommand<Item>(async item => await NavigateToDetailAsync(item));
            RefreshCommand = new DelegateCommand(async () => await LoadItemsAsync());

            // Rx-based search with debounce
            this.WhenPropertyChanged(nameof(SearchText))
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(_ => PerformSearch())
                .DisposeWith(_disposables);
        }

        #region Observable Properties

        private ObservableCollection<Item> _items = new();
        public ObservableCollection<Item> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private Item _selectedItem;
        public Item SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value, OnItemSelected);
        }

        #endregion

        #region Commands

        public DelegateCommand<Item> NavigateToDetailCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }

        #endregion

        #region Methods

        private async Task LoadItemsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                // Load items from service
                var items = await Task.Delay(1000); // Simulate API call
                // Items = new ObservableCollection<Item>(result);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void PerformSearch()
        {
            // Search logic with debounced input
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Reset to full list
                return;
            }

            // Filter items based on SearchText
        }

        private async void OnItemSelected(Item item)
        {
            if (item == null) return;

            var parameters = new NavigationParameters();
            parameters.Add("SelectedItem", item);
            await _navigationService.NavigateAsync("ItemDetailPage", parameters);
        }

        private async Task NavigateToDetailAsync(Item item)
        {
            var parameters = new NavigationParameters
            {
                { "Item", item }
            };
            await _navigationService.NavigateAsync("ItemDetailPage", parameters);
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Called when navigating away from this ViewModel
        /// Clean up all Rx subscriptions to prevent memory leaks
        /// </summary>
        public void Destroy()
        {
            _disposables?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Example model class
    /// </summary>
    public class Item
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
