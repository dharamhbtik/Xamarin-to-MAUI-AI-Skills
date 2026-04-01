# Xamarin Forms Development Skill

Comprehensive guide for Xamarin Forms development with Prism, Reactive Extensions, and best practices.

---

## Table of Contents

1. [ViewModel Patterns](#viewmodel-patterns)
2. [Navigation](#navigation)
3. [Custom Renderers](#custom-renderers)
4. [Realm Database](#realm-database)
5. [Service Registration](#service-registration)
6. [XAML Patterns](#xaml-patterns)
7. [Platform-Specific Code](#platform-specific-code)
8. [Best Practices](#best-practices)

---

## ViewModel Patterns

### Base ViewModel Structure

```csharp
public class ViewModelName : BindableBase, IDestructible
{
    private readonly INavigationService _navigationService;
    private readonly CompositeDisposable _disposables = new();

    public ViewModelName(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        // Initialize commands
        SaveCommand = new DelegateCommand(async () => await SaveAsync())
            .ObservesCanExecute(() => CanSave);
    }

    // Observable properties with change notification
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, OnNameChanged);
    }

    // Computed property
    public bool CanSave => !string.IsNullOrWhiteSpace(Name);

    // Commands
    public DelegateCommand SaveCommand { get; private set; }

    private async Task SaveAsync()
    {
        await _navigationService.GoBackAsync();
    }

    // Rx-based property dependencies
    protected void PropertyIsDependentOn(string sourceProperty, string propertyToNotify)
    {
        this.WhenPropertyChanged(sourceProperty)
            .Select(_ => propertyToNotify)
            .ObserveOn(SynchronizationContext.Current)
            .SubscribeWeakly(this, RaisePropertyChanged)
            .DisposeWith(_disposables);
    }

    // Cleanup
    public virtual void Destroy()
    {
        _disposables?.Dispose();
    }
}
```

### Observable Property with Rx

```csharp
public class SearchViewModel : BindableBase, IDestructible
{
    private readonly CompositeDisposable _disposables = new();

    public SearchViewModel()
    {
        // Debounced search
        this.WhenPropertyChanged(nameof(SearchText))
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => PerformSearch())
            .DisposeWith(_disposables);
    }

    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private void PerformSearch()
    {
        // Search logic after debounce
    }

    public void Destroy()
    {
        _disposables?.Dispose();
    }
}
```

---

## Navigation

### Prism Navigation

```csharp
// Navigate to page
await _navigationService.NavigateAsync("PageName");

// With parameters
var parameters = new NavigationParameters();
parameters.Add("Key", value);
await _navigationService.NavigateAsync("PageName", parameters);

// Modal navigation
await _navigationService.NavigateAsync("PageName", useModalNavigation: true);

// Go back
await _navigationService.GoBackAsync();

// Clear stack and navigate
await _navigationService.NavigateAsync("/MainPage");

// Deep linking
await _navigationService.NavigateAsync("/NavigationPage/MainPage/DetailPage");
```

### Receiving Navigation Parameters

```csharp
public class DetailViewModel : BindableBase, INavigationAware
{
    public void OnNavigatedTo(INavigationParameters parameters)
    {
        if (parameters.ContainsKey("Key"))
        {
            var value = parameters.GetValue<T>("Key");
        }
    }

    public void OnNavigatedFrom(INavigationParameters parameters)
    {
        // Pass data back
        parameters.Add("Result", result);
    }
}
```

---

## Custom Renderers

### Android Renderer

```csharp
[assembly: ExportRenderer(typeof(CustomView), typeof(CustomViewRenderer))]
namespace YourApp.Droid.Renderers
{
    public class CustomViewRenderer : ViewRenderer<CustomView, Android.Views.View>
    {
        public CustomViewRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<CustomView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                // Cleanup old element
            }

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    SetNativeControl(new Android.Views.View(Context));
                }
                
                UpdateProperties();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(CustomView.PropertyName):
                    UpdateProperties();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup resources
            }
            base.Dispose(disposing);
        }
    }
}
```

### iOS Renderer

```csharp
[assembly: ExportRenderer(typeof(CustomView), typeof(CustomViewRenderer))]
namespace YourApp.iOS.Renderers
{
    public class CustomViewRenderer : ViewRenderer<CustomView, UIView>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<CustomView> e)
        {
            base.OnElementChanged(e);

            if (Control == null && e.NewElement != null)
            {
                SetNativeControl(new UIView());
            }

            if (e.NewElement != null)
            {
                UpdateProperties();
            }
        }
    }
}
```

---

## Realm Database

### Model Definition

```csharp
public class Task : RealmObject
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsCompleted { get; set; }

    // Relationships
    public IList<Item> Items { get; }

    // Backlink
    [Backlink(nameof(Item.Task))]
    public IQueryable<Item> RelatedItems { get; }
}
```

### Realm Operations

```csharp
public class RealmService
{
    private readonly Realm _realm;

    public RealmService()
    {
        var config = new RealmConfiguration("app.realm")
        {
            SchemaVersion = 1,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
                // Migration logic
            }
        };
        _realm = Realm.GetInstance(config);
    }

    public void AddTask(Task task)
    {
        _realm.Write(() =>
        {
            _realm.Add(task);
        });
    }

    public void UpdateTask(Action updateAction)
    {
        _realm.Write(updateAction);
    }

    public IQueryable<Task> GetTasks()
    {
        return _realm.All<Task>().Where(t => !t.IsCompleted);
    }

    public Task GetTask(string id)
    {
        return _realm.Find<Task>(id);
    }
}
```

---

## Service Registration

### App.xaml.cs

```csharp
public partial class App : PrismApplication
{
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Services
        containerRegistry.RegisterSingleton<IRealmService, RealmService>();
        containerRegistry.RegisterSingleton<IApiService, ApiService>();
        containerRegistry.RegisterInstance(typeof(ILogger), new Logger());

        // Navigation
        containerRegistry.RegisterForNavigation<NavigationPage>();
        containerRegistry.RegisterForNavigation<MainPage, MainViewModel>();
        containerRegistry.RegisterForNavigation<DetailPage, DetailViewModel>();
    }

    protected override async void OnInitialized()
    {
        InitializeComponent();
        await NavigationService.NavigateAsync("/NavigationPage/MainPage");
    }
}
```

---

## XAML Patterns

### Page Structure

```xml
<ContentPage x:Class="YourApp.Views.MainPage"
             xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:prism="clr-namespace:Prism.Mvvm;assembly=Prism.Forms"
             prism:ViewModelLocator.AutowireViewModel="True"
             Title="{Binding Title}">
    
    <ContentPage.ToolbarItems>
        <ToolbarItem Text="Add" Command="{Binding AddCommand}" />
    </ContentPage.ToolbarItems>
    
    <ContentPage.Content>
        <Grid RowDefinitions="Auto,*">
            <SearchBar Grid.Row="0" Text="{Binding SearchText}" />
            
            <CollectionView Grid.Row="1"
                          ItemsSource="{Binding Items}"
                          SelectedItem="{Binding SelectedItem}"
                          SelectionMode="Single">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <SwipeView>
                            <SwipeView.RightItems>
                                <SwipeItems Mode="Execute">
                                    <SwipeItem Text="Delete"
                                              BackgroundColor="Red"
                                              Command="{Binding DeleteCommand}"
                                              CommandParameter="{Binding .}" />
                                </SwipeItems>
                            </SwipeView.RightItems>
                            
                            <Grid Padding="10">
                                <Label Text="{Binding Name}" />
                            </Grid>
                        </SwipeView>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </Grid>
    </ContentPage.Content>
</ContentPage>
```

### Converters

```csharp
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }
}
```

```xml
<ContentPage.Resources>
    <ResourceDictionary>
        <local:InverseBoolConverter x:Key="InverseBoolConverter" />
    </ResourceDictionary>
</ContentPage.Resources>

<ActivityIndicator IsVisible="{Binding IsBusy}"
                   IsRunning="{Binding IsBusy}" />
<Button IsVisible="{Binding IsBusy, Converter={StaticResource InverseBoolConverter}}" />
```

---

## Platform-Specific Code

### Device Checks

```csharp
// Platform check
if (Device.RuntimePlatform == Device.Android)
{
    // Android-specific
}
else if (Device.RuntimePlatform == Device.iOS)
{
    // iOS-specific
}

// Using Xamarin.Essentials
if (DeviceInfo.Platform == DevicePlatform.Android)
{
    // Android-specific
}
```

### OnPlatform in XAML

```xml
<Label FontFamily="{OnPlatform iOS='Helvetica', Android='Roboto'}" />

<OnPlatform x:TypeArguments="x:Double"
             x:Key="IconSize">
    <On Platform="iOS" Value="20" />
    <On Platform="Android" Value="24" />
</OnPlatform>
```

---

## Best Practices

### Do's

- ✅ Use `SetProperty()` for all property changes
- ✅ Dispose `CompositeDisposable` in `Destroy()`
- ✅ Use `ObservableCollection` for lists
- ✅ Throttle search input with Rx
- ✅ Use `WeakReference` for event subscriptions on long-lived objects
- ✅ Handle null checks in renderers
- ✅ Use `CachingStrategy="RecycleElement"` on lists

### Don'ts

- ❌ Use `async void` (except event handlers)
- ❌ Create new realms on every operation (use singleton)
- ❌ Forget to unregister Prism event subscriptions
- ❌ Use `Device.BeginInvokeOnMainThread` unnecessarily
- ❌ Block the UI thread with Realm operations
- ❌ Forget to dispose renderers properly

### Performance Tips

1. **Use CollectionView over ListView**
2. **Set CachingStrategy to RecycleElement**
3. **Use ObservableRangeCollection for bulk updates**
4. **Avoid nested StackLayout, use Grid instead**
5. **Use OnBindingContextChanged for heavy setup, not constructor**

### Memory Leak Prevention

```csharp
public class ViewModel : BindableBase, IDestructible
{
    public ViewModel()
    {
        // Subscribe
        MessagingCenter.Subscribe<Message>(this, "Key", OnMessage);
    }

    public void Destroy()
    {
        // IMPORTANT: Unsubscribe when navigating away
        MessagingCenter.Unsubscribe<Message>(this, "Key");
    }
}
```

### Thread Safety

```csharp
// BAD - Realm access from wrong thread
var tasks = _realm.All<Task>().ToList();
await Task.Run(() =>
{
    // This will throw - realm is thread-confined
    var task = _realm.Find<Task>(id);
});

// GOOD - Pass data, not realm references
var tasks = _realm.All<Task>().ToList();
var taskData = tasks.Select(t => new TaskDto(t)).ToList();
await Task.Run(() =>
{
    // Process DTOs
});
```

---

## Resources

- [Prism Documentation](https://prismlibrary.com/)
- [Xamarin.Forms Documentation](https://docs.microsoft.com/xamarin/xamarin-forms/)
- [Reactive Extensions](https://github.com/dotnet/reactive)
- [Realm .NET](https://www.mongodb.com/docs/realm/sdk/dotnet/)
