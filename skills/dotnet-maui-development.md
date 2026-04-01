# .NET MAUI Development Skill

Comprehensive guide for .NET 10 MAUI development with CommunityToolkit.Mvvm, best practices, and performance optimization.

---

## Table of Contents

1. [ViewModel Patterns](#viewmodel-patterns)
2. [Shell Navigation](#shell-navigation)
3. [Custom Handlers](#custom-handlers)
4. [Realm Database](#realm-database)
5. [XAML with Compiled Bindings](#xaml-with-compiled-bindings)
6. [Performance Best Practices](#performance-best-practices)
7. [Platform-Specific Implementation](#platform-specific-implementation)
8. [Do's and Don'ts](#dos-and-donts)

---

## ViewModel Patterns

### Basic ViewModel with Source Generators

```csharp
public partial class ViewModelName : ObservableObject
{
    // Generates: public string Name { get; set; } with INotifyPropertyChanged
    [ObservableProperty]
    private string _name;

    // ObservableProperty with notification callback
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _firstName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _lastName;

    // Computed property
    public string FullName => $"{FirstName} {LastName}";

    // Command generated automatically
    [RelayCommand]
    private async Task SaveAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    // Command with CanExecute
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SubmitAsync()
    {
        // Submit logic
    }

    // Computed property for CanExecute
    private bool CanSave =>
        !string.IsNullOrWhiteSpace(FirstName) &&
        !string.IsNullOrWhiteSpace(LastName);
}
```

### Advanced ViewModel with Validation

```csharp
public partial class FormViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    [ObservableProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _email;

    [ObservableProperty]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    private string _password;

    public bool HasErrors => _errors.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        return propertyName != null && _errors.ContainsKey(propertyName)
            ? _errors[propertyName]
            : Enumerable.Empty<string>();
    }

    partial void OnEmailChanged(string value)
    {
        ValidateProperty(value, nameof(Email));
    }

    private void ValidateProperty(string value, string propertyName)
    {
        _errors.Remove(propertyName);

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(this) { MemberName = propertyName };
        Validator.TryValidateProperty(value, context, validationResults);

        if (validationResults.Any())
        {
            _errors[propertyName] = validationResults.Select(r => r.ErrorMessage!).ToList();
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        SubmitCommand.NotifyCanExecuteChanged();
    }
}
```

### ViewModel with Messenger

```csharp
public partial class ListViewModel : ObservableObject
{
    public ListViewModel()
    {
        // Register for messages (weak reference)
        WeakReferenceMessenger.Default.Register<ItemUpdatedMessage>(
            this,
            (r, m) => OnItemUpdated(m));
    }

    [ObservableProperty]
    private ObservableCollection<Item> _items = new();

    private void OnItemUpdated(ItemUpdatedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var item = Items.FirstOrDefault(i => i.Id == message.Item.Id);
            if (item != null)
            {
                // Update item
            }
        });
    }

    // Cleanup
    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}

// Message definition
public record ItemUpdatedMessage(Item Item);
```

---

## Shell Navigation

### Registration

```csharp
// MauiProgram.cs
builder.Services.AddTransient<ListViewModel>();
builder.Services.AddTransient<ListPage>();
builder.Services.AddTransient<DetailViewModel>();
builder.Services.AddTransient<DetailPage>();

// AppShell.xaml.cs
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes
        Routing.RegisterRoute("list", typeof(ListPage));
        Routing.RegisterRoute("detail", typeof(DetailPage));
        Routing.RegisterRoute("edit", typeof(EditPage));
    }
}
```

### Navigation Commands

```csharp
// Basic navigation
await Shell.Current.GoToAsync("detail");

// With parameters
await Shell.Current.GoToAsync($"detail?id={itemId}");

// Complex object via dictionary
var parameters = new Dictionary<string, object>
{
    { "Item", selectedItem },
    { "Mode", EditMode.Update }
};
await Shell.Current.GoToAsync("edit", parameters);

// Modal presentation
await Shell.Current.GoToAsync("detail",
    new ShellNavigationQueryParameters { { "modal", true } });

// Navigate back
await Shell.Current.GoToAsync("..");

// Navigate to root
await Shell.Current.GoToAsync("//root");
```

### Receiving Navigation Data

```csharp
[QueryProperty(nameof(ItemId), "id")]
[QueryProperty(nameof(Item), "Item")]
public partial class DetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemId;

    [ObservableProperty]
    private Item _item;

    partial void OnItemIdChanged(string value)
    {
        LoadItem(value);
    }
}
```

---

## Custom Handlers

### Android Handler

```csharp
namespace YourApp.Platforms.Android.Handlers;

public partial class CustomEntryHandler : EntryHandler
{
    public static IPropertyMapper<IEntry, CustomEntryHandler> Mapper =
        new PropertyMapper<IEntry, CustomEntryHandler>(EntryHandler.Mapper)
        {
            [nameof(IEntry.Background)] = MapBackground,
            [nameof(CustomEntry.BorderColor)] = MapBorderColor
        };

    public CustomEntryHandler() : base(Mapper) { }

    protected override AppCompatEditText CreatePlatformView()
    {
        var view = base.CreatePlatformView();

        // Configure native view
        view.SetBackgroundColor(Android.Graphics.Color.Transparent);
        view.SetPadding(20, 10, 20, 10);

        return view;
    }

    protected override void ConnectHandler(AppCompatEditText platformView)
    {
        base.ConnectHandler(platformView);

        // Subscribe to events
        platformView.TextChanged += OnTextChanged;
    }

    protected override void DisconnectHandler(AppCompatEditText platformView)
    {
        // CRITICAL: Unsubscribe all events to prevent memory leaks
        platformView.TextChanged -= OnTextChanged;

        base.DisconnectHandler(platformView);
    }

    private static void MapBorderColor(CustomEntryHandler handler, IEntry entry)
    {
        if (handler.PlatformView is AppCompatEditText editText && entry is CustomEntry custom)
        {
            // Apply border color
        }
    }

    private void OnTextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        // Handle text change
    }
}
```

### iOS Handler

```csharp
namespace YourApp.Platforms.iOS.Handlers;

public partial class CustomEntryHandler : EntryHandler
{
    public static IPropertyMapper<IEntry, CustomEntryHandler> Mapper =
        new PropertyMapper<IEntry, CustomEntryHandler>(EntryHandler.Mapper)
        {
            [nameof(CustomEntry.BorderColor)] = MapBorderColor
        };

    protected override MauiTextField CreatePlatformView()
    {
        var view = base.CreatePlatformView();

        view.BorderStyle = UITextBorderStyle.RoundedRect;
        view.Layer.CornerRadius = 8;

        return view;
    }

    protected override void ConnectHandler(MauiTextField platformView)
    {
        base.ConnectHandler(platformView);
        platformView.EditingDidBegin += OnEditingBegin;
    }

    protected override void DisconnectHandler(MauiTextField platformView)
    {
        platformView.EditingDidBegin -= OnEditingBegin;
        base.DisconnectHandler(platformView);
    }

    private static void MapBorderColor(CustomEntryHandler handler, IEntry entry)
    {
        if (handler.PlatformView.Layer is CALayer layer && entry is CustomEntry custom)
        {
            layer.BorderColor = custom.BorderColor.ToCGColor();
        }
    }
}
```

### Handler Registration

```csharp
// MauiProgram.cs
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler<CustomEntry, CustomEntryHandler>();
    handlers.AddHandler<CustomButton, CustomButtonHandler>();
    handlers.AddHandler<CustomMap, CustomMapHandler>();
});
```

---

## Realm Database

### Model with Fluent API

```csharp
public class Task : IRealmObject
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsCompleted { get; set; }

    // Backlink
    [Backlink(nameof(Item.Task))]
    public IQueryable<Item> Items { get; }
}
```

### Realm Service with Async

```csharp
public interface IRealmService
{
    Task<Task> GetTaskAsync(string id);
    Task<IEnumerable<Task>> GetTasksAsync();
    Task AddTaskAsync(Task task);
    Task UpdateTaskAsync(Action<Realm> update);
}

public class RealmService : IRealmService
{
    private readonly Realm _realm;

    public RealmService()
    {
        var config = new FlexibleSyncConfiguration(App.Current.User)
        {
            SchemaVersion = 1
        };
        _realm = Realm.GetInstance(config);
    }

    public async Task<Task> GetTaskAsync(string id)
    {
        return await _realm.FindAsync<Task>(id);
    }

    public async Task<IEnumerable<Task>> GetTasksAsync()
    {
        return await _realm.All<Task>()
            .Filter("IsCompleted == false")
            .ToArrayAsync();
    }

    public async Task AddTaskAsync(Task task)
    {
        await _realm.WriteAsync(() =>
        {
            _realm.Add(task);
        });
    }

    public async Task UpdateTaskAsync(Action<Realm> update)
    {
        await _realm.WriteAsync(update);
    }
}
```

---

## XAML with Compiled Bindings

### Basic Page with DataType

```xml
<ContentPage x:Class="YourApp.Views.TasksPage"
             xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:YourApp.ViewModels"
             xmlns:model="clr-namespace:YourApp.Models"
             x:DataType="vm:TasksViewModel">

    <ContentPage.Resources>
        <DataTemplate x:Key="TaskTemplate"
                      x:DataType="model:Task">
            <SwipeView>
                <SwipeView.LeftItems>
                    <SwipeItems>
                        <SwipeItem Text="Complete"
                                   BackgroundColor="Green"
                                   Command="{Binding Source={RelativeSource AncestorType={x:Type vm:TasksViewModel}}, Path=CompleteCommand}"
                                   CommandParameter="{Binding .}" />
                    </SwipeItems>
                </SwipeView.LeftItems>

                <Border Padding="10">
                    <Grid ColumnDefinitions="*,Auto">
                        <VerticalStackLayout>
                            <Label Text="{Binding Name}"
                                   FontSize="16" />
                            <Label Text="{Binding CreatedAt, StringFormat='{0:g}'}"
                                   FontSize="12"
                                   TextColor="Gray" />
                        </VerticalStackLayout>

                        <CheckBox Grid.Column="1"
                                  IsChecked="{Binding IsCompleted}" />
                    </Grid>
                </Border>
            </SwipeView>
        </DataTemplate>
    </ContentPage.Resources>

    <Grid RowDefinitions="Auto,*">
        <SearchBar Grid.Row="0"
                   Text="{Binding SearchText}"
                   SearchCommand="{Binding SearchCommand}" />

        <CollectionView Grid.Row="1"
                       ItemsSource="{Binding Tasks}"
                       ItemTemplate="{StaticResource TaskTemplate}"
                       VirtualizationMode="Recycling"
                       RemainingItemsThreshold="5"
                       RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}">
        </CollectionView>
    </Grid>
</ContentPage>
```

### Performance-Optimized Layouts

```xml
<!-- Good: Flattened Grid -->
<Grid RowDefinitions="Auto,Auto,Auto"
      ColumnDefinitions="*,Auto">
    <Label Grid.Row="0" Grid.Column="0" Text="{Binding Title}" />
    <Label Grid.Row="0" Grid.Column="1" Text="{Binding Date}" />

    <Label Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"
           Text="{Binding Description}" />

    <Button Grid.Row="2" Grid.Column="1"
            Command="{Binding ActionCommand}" />
</Grid>

<!-- Bad: Nested StackLayouts -->
<VerticalStackLayout>
    <HorizontalStackLayout>
        <Label Text="{Binding Title}" />
        <Label Text="{Binding Date}" />
    </HorizontalStackLayout>
    <Label Text="{Binding Description}" />
    <HorizontalStackLayout>
        <Button Command="{Binding ActionCommand}" />
    </HorizontalStackLayout>
</VerticalStackLayout>
```

### Platform-Specific in MAUI

```xml
<Label FontSize="{OnPlatform iOS=16, Android=14}" />

<Button>
    <Button.WidthRequest>
        <OnPlatform x:TypeArguments="x:Double">
            <On Platform="iOS" Value="200" />
            <On Platform="Android" Value="180" />
        </OnPlatform>
    </Button.WidthRequest>
</Button>
```

---

## Performance Best Practices

### Compilation Settings

```xml
<!-- In .csproj -->
<PropertyGroup>
    <!-- AOT for faster startup -->
    <RunAOTCompilation>true</RunAOTCompilation>

    <!-- IL Linking for smaller app size -->
    <PublishTrimmed>true</PublishTrimmed>

    <!-- Full trimming mode (be careful with reflection) -->
    <TrimMode>full</TrimMode>

    <!-- Single file publishing -->
    <PublishSingleFile>true</PublishSingleFile>
</PropertyGroup>
```

### Async Patterns

```csharp
// Good: Task with cancellation
[RelayCommand]
private async Task LoadDataAsync(CancellationToken cancellationToken)
{
    try
    {
        var data = await _service.GetDataAsync(cancellationToken);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Items = new ObservableCollection<Item>(data);
        });
    }
    catch (OperationCanceledException)
    {
        // Handle cancellation
    }
}

// Good: Offload to background thread
[RelayCommand]
private async Task ProcessDataAsync()
{
    await Task.Run(() =>
    {
        // CPU-intensive work
        ProcessLargeDataSet();
    });

    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        // Update UI
    });
}
```

### Memory Management

```csharp
public partial class DataViewModel : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly WeakReferenceMessenger _messenger;

    public DataViewModel()
    {
        _messenger = WeakReferenceMessenger.Default;
        _messenger.Register<DataUpdatedMessage>(this, OnDataUpdated);
    }

    private void OnDataUpdated(object recipient, DataUpdatedMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update on UI thread
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _messenger.UnregisterAll(this);
    }
}
```

---

## Platform-Specific Implementation

### Conditional Compilation

```csharp
public partial class DeviceService
{
    public partial string GetDeviceId();
    public partial string GetAppVersion();
}

#if ANDROID
public partial class DeviceService
{
    public partial string GetDeviceId()
    {
        var context = Android.App.Application.Context;
        return Android.Provider.Settings.Secure.GetString(
            context.ContentResolver,
            Android.Provider.Settings.Secure.AndroidId);
    }

    public partial string GetAppVersion()
    {
        var context = Android.App.Application.Context;
        var info = context.PackageManager.GetPackageInfo(
            context.PackageName, 0);
        return info.VersionName;
    }
}
#elif IOS
public partial class DeviceService
{
    public partial string GetDeviceId()
    {
        return UIKit.UIDevice.CurrentDevice.IdentifierForVendor?.ToString() ?? string.Empty;
    }

    public partial string GetAppVersion()
    {
        return NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleShortVersionString")?.ToString() ?? string.Empty;
    }
}
#endif
```

---

## Do's and Don'ts

### ✅ Do's

- ✅ Use `[ObservableProperty]` and `[RelayCommand]` for MVVM
- ✅ Add `x:DataType` to all XAML for compiled bindings
- ✅ Use `CollectionView` with `VirtualizationMode="Recycling"`
- ✅ Unsubscribe events in `DisconnectHandler` for custom handlers
- ✅ Use `WeakReferenceMessenger` for cross-VM communication
- ✅ Implement `IDisposable` and cleanup in `OnDisappearing`
- ✅ Use `MainThread.InvokeOnMainThreadAsync` for UI updates
- ✅ Use `Task.Run` for CPU-intensive operations
- ✅ Enable AOT compilation for release builds
- ✅ Set explicit sizes on images

### ❌ Don'ts

- ❌ Use `async void` (except event handlers)
- ❌ Use `ListView` (deprecated, use `CollectionView`)
- ❌ Nest `CollectionView` inside `ScrollView`
- ❌ Hold strong references to ViewModels in services
- ❌ Use `MessagingCenter` (deprecated, use `WeakReferenceMessenger`)
- ❌ Forget to unregister message handlers
- ❌ Create new handlers on every property change
- ❌ Use reflection in trimmed builds
- ❌ Block UI thread with synchronous Realm operations
- ❌ Use `Device.RuntimePlatform` (use `DeviceInfo.Platform`)

---

## Resources

- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [MAUI Handler Architecture](https://learn.microsoft.com/dotnet/maui/user-interface/handlers/)
- [Shell Navigation](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/navigation)
