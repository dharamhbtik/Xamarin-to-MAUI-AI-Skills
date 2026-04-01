# GitHub Copilot Instructions

This file provides context to GitHub Copilot when working with Xamarin Forms and .NET MAUI code.

---

## Project Context

This repository contains AI-assisted development skills for:
- **Xamarin Forms** development with Prism and Reactive Extensions
- **.NET 10 MAUI** development with CommunityToolkit.Mvvm
- **Migration** patterns from Xamarin to MAUI

---

## Technology Stack

### Xamarin Forms Stack
- Xamarin.Forms 5.0+
- Prism.DryIoc.Forms (MVVM + Navigation)
- Reactive Extensions (Rx)
- Realm for local database
- Refit for HTTP APIs
- Custom Renderers for platform UI

### .NET MAUI Stack
- .NET 10
- Microsoft.Maui.Controls
- CommunityToolkit.Maui (v9.0+)
- CommunityToolkit.Mvvm (v8.2+ with source generators)
- Realm v12+ for local database
- Custom Handlers (replaces Renderers)

---

## Code Generation Guidelines

### When Working with Xamarin Forms

#### ViewModels (Prism Pattern)
```csharp
public class ViewModelName : BindableBase, IDestructible
{
    private readonly INavigationService _navigationService;
    private readonly CompositeDisposable _disposables = new();

    public ViewModelName(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        // Rx-based property dependencies
        this.WhenPropertyChanged(nameof(SearchText))
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Subscribe(_ => PerformSearch())
            .DisposeWith(_disposables);
    }

    // Properties with change notification
    private string _propertyName;
    public string PropertyName
    {
        get => _propertyName;
        set => SetProperty(ref _propertyName, value);
    }

    // Cleanup
    public void Destroy() => _disposables?.Dispose();
}
```

#### Custom Renderers (Android)
```csharp
[assembly: ExportRenderer(typeof(CustomView), typeof(CustomViewRenderer))]
namespace YourApp.Droid.Renderers
{
    public class CustomViewRenderer : ViewRenderer<CustomView, Android.Views.View>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<CustomView> e)
        {
            base.OnElementChanged(e);
            // Implementation
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == CustomView.PropertyNameProperty.PropertyName)
            {
                UpdateProperty();
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Cleanup resources
            base.Dispose(disposing);
        }
    }
}
```

#### XAML Pages
```xml
<ContentPage x:Class="YourApp.Views.PageName"
             xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:prism="clr-namespace:Prism.Mvvm;assembly=Prism.Forms"
             prism:ViewModelLocator.AutowireViewModel="True">
</ContentPage>
```

### When Working with .NET MAUI

#### ViewModels (CommunityToolkit.Mvvm)
```csharp
public partial class ViewModelName : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComputedProperty))]
    private string _propertyName;

    public string ComputedProperty => $"Computed: {PropertyName}";

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        await Shell.Current.GoToAsync("routename");
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        // Save logic
    }

    private bool CanSave => !string.IsNullOrWhiteSpace(PropertyName);
}
```

#### Custom Handlers (Android)
```csharp
public partial class CustomViewHandler : ViewHandler<CustomView, PlatformView>
{
    public static IPropertyMapper<ICustomView, CustomViewHandler> Mapper = 
        new PropertyMapper<ICustomView, CustomViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ICustomView.PropertyName)] = MapPropertyName
        };

    protected override PlatformView CreatePlatformView()
    {
        return new PlatformView(Context);
    }

    protected override void ConnectHandler(PlatformView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.Event += OnEvent;
    }

    protected override void DisconnectHandler(PlatformView platformView)
    {
        // CRITICAL: Unsubscribe events to prevent memory leaks
        platformView.Event -= OnEvent;
        base.DisconnectHandler(platformView);
    }

    private static void MapPropertyName(CustomViewHandler handler, ICustomView view)
    {
        // Map property changes
    }
}
```

#### XAML Pages (with Compiled Bindings)
```xml
<ContentPage x:Class="YourApp.Views.PageName"
             xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:YourApp.ViewModels"
             x:DataType="vm:ViewModelName">
    
    <CollectionView ItemsSource="{Binding Items}"
                    VirtualizationMode="Recycling">
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="vm:ItemViewModel">
                <Label Text="{Binding Name}" />
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</ContentPage>
```

#### Dependency Injection
```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IService, Implementation>();
builder.Services.AddTransient<ViewModelName>();
builder.Services.AddTransient<PageName>();

// Register routes for Shell
Routing.RegisterRoute("routename", typeof(PageName));
```

---

## Migration Patterns

### Navigation Migration
| From (Xamarin) | To (MAUI) |
|----------------|-----------|
| `NavigationService.NavigateAsync("Page")` | `Shell.Current.GoToAsync("//page")` |
| `NavigationService.GoBackAsync()` | `Shell.Current.GoToAsync("..")` |
| `NavigationParameters` | `Dictionary<string, object>` |

### Custom UI Migration
| From (Xamarin) | To (MAUI) |
|----------------|-----------|
| `ExportRenderer` | `ConfigureMauiHandlers` |
| `ViewRenderer` | `ViewHandler` |
| `OnElementChanged` | `CreatePlatformView()` + `ConnectHandler()` |
| `Dispose` | `DisconnectHandler()` |

### MVVM Migration
| From (Xamarin) | To (MAUI) |
|----------------|-----------|
| `BindableBase` | `ObservableObject` |
| `SetProperty()` | `[ObservableProperty]` |
| `DelegateCommand` | `[RelayCommand]` |
| `EventAggregator` | `WeakReferenceMessenger` |

---

## Performance Guidelines

### Always Apply
1. **Compiled Bindings**: Add `x:DataType` to all XAML
2. **Virtualization**: Use `VirtualizationMode="Recycling"` for lists
3. **Layout**: Prefer `Grid` over nested `StackLayout`
4. **Async**: Use `Task` return types, avoid `async void`
5. **Memory**: Unsubscribe events in `DisconnectHandler`
6. **Images**: Set explicit `WidthRequest`/`HeightRequest`

### Never Do
1. Never leave event handlers unsubscribed
2. Never use `async void` (except event handlers)
3. Never register pages as singletons in DI
4. Never use `ListView` (use `CollectionView`)
5. Never nest `CollectionView` inside `ScrollView`
6. Never use `MessagingCenter` (deprecated)

---

## Naming Conventions

### Xamarin Forms
- Pages: `*Page.xaml`
- ViewModels: `*ViewModel.cs`
- Renderers: `*Renderer.cs`
- Services: `*Service.cs` with `I*` interface
- Base: `BindableBase`

### MAUI
- Pages: `*Page.xaml`
- ViewModels: `*ViewModel.cs`
- Handlers: `*Handler.cs`
- Services: `*Service.cs` with `I*` interface
- Base: `ObservableObject`

---

## File Structure

### Xamarin Forms
```
YourApp/
├── Views/           # XAML pages
├── ViewModels/      # Prism ViewModels
├── Services/        # Business logic
├── Models/          # Entities
├── Controls/        # Custom views
└── Platforms/
    ├── Android/     # Renderers
    └── iOS/         # Renderers
```

### MAUI
```
YourApp/
├── Views/                   # XAML pages
├── ViewModels/              # ObservableObject ViewModels
├── Services/                # Business logic
├── Models/                  # Entities
├── Platforms/
│   ├── Android/
│   │   └── Handlers/        # Custom handlers
│   └── iOS/
│       └── Handlers/
├── Resources/               # Images, fonts
└── MauiProgram.cs           # DI configuration
```

---

## Testing Checklist

### Before Committing Xamarin Code
- [ ] Rx subscriptions disposed in `Destroy()`
- [ ] Prism navigation uses correct parameters
- [ ] Renderers handle null checks
- [ ] Events unsubscribed in `Dispose`

### Before Committing MAUI Code
- [ ] Handlers unsubscribe events in `DisconnectHandler`
- [ ] XAML has `x:DataType` for compiled bindings
- [ ] ViewModels use `[ObservableProperty]` and `[RelayCommand]`
- [ ] Shell routes registered in `AppShell.xaml.cs`
- [ ] Services registered in `MauiProgram.cs`
- [ ] No `async void` methods (except event handlers)

---

## Resources

### Documentation
- [.NET MAUI Performance](https://docs.microsoft.com/dotnet/maui/deployment/performance)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [MAUI Handler Architecture](https://learn.microsoft.com/dotnet/maui/user-interface/handlers/)
- [Shell Navigation](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/navigation)

### Community
- [.NET MAUI GitHub](https://github.com/dotnet/maui)
- [CommunityToolkit GitHub](https://github.com/CommunityToolkit/dotnet)
- [Prism Library](https://github.com/PrismLibrary/Prism)
