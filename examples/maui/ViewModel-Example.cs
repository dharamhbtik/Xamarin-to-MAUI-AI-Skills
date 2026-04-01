// .NET MAUI ViewModel Example with CommunityToolkit.Mvvm
// Reference: skills/dotnet-maui-development.md

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace YourApp.ViewModels
{
    /// <summary>
    /// Example ViewModel for MAUI with source generators
    /// Demonstrates CommunityToolkit.Mvvm patterns
    /// </summary>
    public partial class ItemListViewModel : ObservableObject, IDisposable
    {
        /// <summary>
        /// Observable collection of items
        /// Generated property: public ObservableCollection<Item> Items { get; set; }
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Item> _items = new();

        /// <summary>
        /// Search text with change notification
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSearchActive))]
        private string _searchText = string.Empty;

        /// <summary>
        /// Busy state indicator
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// Selected item
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
        private Item? _selectedItem;

        /// <summary>
        /// Computed property indicating if search is active
        /// </summary>
        public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);

        /// <summary>
        /// Computed property indicating if an item is selected
        /// </summary>
        public bool HasSelectedItem => SelectedItem != null;

        /// <summary>
        /// Constructor
        /// </summary>
        public ItemListViewModel()
        {
            // Register for weak reference messages
            WeakReferenceMessenger.Default.Register<ItemUpdatedMessage>(
                this,
                (r, m) => OnItemUpdated(m));
        }

        #region Commands

        /// <summary>
        /// Load items command (auto-generated)
        /// Command property: public IAsyncRelayCommand LoadItemsCommand { get; }
        /// </summary>
        [RelayCommand]
        private async Task LoadItemsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // Simulate loading from service
                await Task.Delay(1000);

                var items = new List<Item>
                {
                    new() { Id = "1", Name = "Item 1", Description = "Description 1" },
                    new() { Id = "2", Name = "Item 2", Description = "Description 2" },
                    new() { Id = "3", Name = "Item 3", Description = "Description 3" }
                };

                Items = new ObservableCollection<Item>(items);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Navigate to detail command with parameter
        /// </summary>
        [RelayCommand]
        private async Task NavigateToDetailAsync(Item? item)
        {
            if (item == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Item", item }
            };

            await Shell.Current.GoToAsync("ItemDetailPage", parameters);
        }

        /// <summary>
        /// Search command with cancellation support
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadItemsAsync();
                return;
            }

            try
            {
                IsBusy = true;

                // Simulate search with cancellation support
                await Task.Delay(500, cancellationToken);

                // Filter items based on search text
                var filtered = Items.Where(i =>
                    i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Items = new ObservableCollection<Item>(filtered);
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled - ignore
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Delete item command with CanExecute
        /// Command property: public IRelayCommand<Item> DeleteCommand { get; }
        /// CanExecute: public bool CanDelete => SelectedItem != null;
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void Delete(Item item)
        {
            Items.Remove(item);
        }

        private bool CanDelete => SelectedItem != null;

        #endregion

        #region Private Methods

        private void OnItemUpdated(ItemUpdatedMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existingItem = Items.FirstOrDefault(i => i.Id == message.Item.Id);
                if (existingItem != null)
                {
                    // Update existing item
                    var index = Items.IndexOf(existingItem);
                    Items[index] = message.Item;
                }
            });
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Clean up resources and unregister messages
        /// </summary>
        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
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

    /// <summary>
    /// Message for item updates
    /// </summary>
    public record ItemUpdatedMessage(Item Item);
}
