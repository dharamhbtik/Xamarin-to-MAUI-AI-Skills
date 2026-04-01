# Xamarin Forms to .NET MAUI Migration Guide

A comprehensive guide for migrating Xamarin Forms applications to .NET 10 MAUI with best practices, patterns, and performance optimization.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure Migration](#project-structure-migration)
3. [Renderers to Handlers](#renderers-to-handlers)
4. [Prism to MAUI DI](#prism-to-maui-di)
5. [MVVM Patterns](#mvvm-patterns)
6. [Performance Optimization](#performance-optimization)
7. [Platform-Specific Code](#platform-specific-code)
8. [Memory Management](#memory-management)

---

## Architecture Overview

### Current Xamarin Architecture

```
YourApp/
├── YourApp/                   # Shared .NET Standard 2.1 project
│   ├── ViewModels/           # Prism view models with Rx subscriptions
│   ├── Views/                # XAML pages and controls
│   ├── Services/             # Prism-registered services
│   ├── Models/               # Realm database models
│   └── Controls/             # Custom Xamarin views
├── Android/                  # Android platform project
│   ├── Renderers/           # Custom renderers
│   └── Implementations/      # Platform-specific implementations
└── iOS/                      # iOS platform project
```

### Target MAUI Architecture

```
YourAppMaui/
├── YourApp.csproj            # Single project with multi-targeting
├── MauiProgram.cs            # DI registration and app configuration
├── App.xaml/.cs             # Application root
├── AppShell.xaml/.cs        # Shell navigation structure
├── Platforms/
│   ├── Android/
│   │   ├── MainActivity.cs  # MauiAppCompatActivity
│   │   └── Handlers/        # Custom handlers
│   └── iOS/
│       ├── AppDelegate.cs   # MauiUIApplicationDelegate
│       └── Handlers/        # iOS-specific handlers
├── ViewModels/               # Updated for CommunityToolkit.Mvvm
├── Views/                   # XAML pages with compiled bindings
├── Services/                # Registered in MauiProgram.cs
├── Models/                  # Realm models (unchanged)
└── Resources/               # Images, fonts, raw assets
```

---

## Project Structure Migration

### 1. Project File Conversion

**Xamarin (YourApp.csproj)**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Xamarin.Forms" Version="5.0.0" />
    <PackageReference Include="Prism.DryIoc.Forms" Version="8.1" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

**MAUI (YourApp.csproj)**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-android;net10.0-ios</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- AOT Compilation for performance -->
    <RunAOTCompilation>true</RunAOTCompilation>
    <PublishTrimmed>true</PublishTrimmed>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="CommunityToolkit.Maui" Version="9.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2" />
    <PackageReference Include="Realm" Version="12.0" />
  </ItemGroup>
</Project>
```

### 2. MauiProgram.cs Setup

```csharp
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace YourApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("FontAwesome.otf", "FA");
            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<CustomEntry, CustomEntryHandler>();
                handlers.AddHandler<CustomButton, CustomButtonHandler>();
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IRealmService, RealmService>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<DetailViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DetailPage>();

        return builder.Build();
    }
}
```

### 3. App.xaml Conversion

**Xamarin (with Prism)**
```xml
<prism:PrismApplication x:Class="YourApp.App"
    xmlns="http://xamarin.com/schemas/2014/forms"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:prism="clr-namespace:Prism.DryIoc;assembly=Prism.DryIoc.Forms">
    <prism:PrismApplication.Resources>
        <!-- Resources -->
    </prism:PrismApplication.Resources>
</prism:PrismApplication>
```

**MAUI**
```xml
<Application x:Class="YourApp.App"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    
    <Application.Resources>
        <ResourceDictionary>
            <Color x:Key="PrimaryColor">#00A0D6</Color>
            <converters:InverseBoolConverter x:Key="InverseBoolConverter" />
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 4. Shell Navigation

```xml
<Shell x:Class="YourApp.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:views="clr-namespace:YourApp.Views">
    
    <FlyoutItem Title="Main" Icon="icon.png">
        <ShellContent ContentTemplate="{DataTemplate views:MainPage}" 
                      Route="MainPage" />
    </FlyoutItem>
    
    <ShellContent Route="DetailPage" 
                  ContentTemplate="{DataTemplate views:DetailPage}"
                  FlyoutItemIsVisible="False" />
</Shell>
```

```csharp
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute("MainPage", typeof(MainPage));
        Routing.RegisterRoute("DetailPage", typeof(DetailPage));
    }
}
```

---

## Renderers to Handlers

### Pattern Overview

| Aspect | Xamarin Renderer | MAUI Handler |
|--------|-----------------|--------------|
| Base Class | `ViewRenderer<TView, TNative>` | `ViewHandler<TView, TNative>` |
| Create View | `OnElementChanged` | `CreatePlatformView()` |
| Connect | `OnElementChanged` | `ConnectHandler()` |
| Disconnect | `Dispose` | `DisconnectHandler()` |
| Property Changes | `OnElementPropertyChanged` | `Mapper.Add()` |

### Example: Renderer → Handler

**Xamarin (Android Renderer)**
```csharp
[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
namespace YourApp.Droid.Renderers;

public class CustomEntryRenderer : EntryRenderer
{
    protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
    {
        base.OnElementChanged(e);
        
        if (Control != null)
        {
            Control.SetBackgroundColor(Android.Graphics.Color.Transparent);
            Control.SetPadding(20, 10, 20, 10);
        }
    }

    protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        base.OnElementPropertyChanged(sender, e);
        
        if (e.PropertyName == CustomEntry.BorderColorProperty.PropertyName)
        {
            UpdateBorderColor();
        }
    }
}
```

**MAUI Handler**
```csharp
namespace YourApp.Platforms.Android.Handlers;

public partial class CustomEntryHandler : EntryHandler
{
    public static IPropertyMapper<IEntry, CustomEntryHandler> CustomMapper = 
        new PropertyMapper<IEntry, CustomEntryHandler>(EntryHandler.Mapper)
        {
            [nameof(IEntry.Background)] = MapBackground,
            [nameof(CustomEntry.BorderColor)] = MapBorderColor
        };

    public CustomEntryHandler() : base(CustomMapper) { }

    protected override AppCompatEditText CreatePlatformView()
    {
        var view = base.CreatePlatformView();
        view.SetBackgroundColor(Android.Graphics.Color.Transparent);
        view.SetPadding(20, 10, 20, 10);
        return view;
    }

    protected override void ConnectHandler(AppCompatEditText platformView)
    {
        base.ConnectHandler(platformView);
        platformView.TextChanged += OnTextChanged;
    }

    protected override void DisconnectHandler(AppCompatEditText platformView)
    {
        // CRITICAL: Unsubscribe events to prevent memory leaks
        platformView.TextChanged -= OnTextChanged;
        base.DisconnectHandler(platformView);
    }

    private static void MapBorderColor(CustomEntryHandler handler, IEntry entry)
    {
        // Apply border color logic
    }

    private void OnTextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        // Handle text change
    }
}
```

---

## Prism to MAUI DI

### Service Registration

**Xamarin (Prism)**
```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<INavigationService, NavigationService>();
    containerRegistry.RegisterSingleton<IDialogService, DialogService>();
    containerRegistry.RegisterForNavigation<MainPage, MainViewModel>();
    containerRegistry.RegisterForNavigation<DetailPage, DetailViewModel>();
}
```

**MAUI**
```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    builder.Services.AddSingleton<INavigationService, NavigationService>();
    builder.Services.AddSingleton<IDialogService, DialogService>();
    
    builder.Services.AddTransient<MainViewModel>();
    builder.Services.AddTransient<DetailViewModel>();
    
    builder.Services.AddTransient<MainPage>();
    builder.Services.AddTransient<DetailPage>();
    
    return builder.Build();
}
```

### Navigation Migration

**Xamarin (Prism)**
```csharp
public class MainViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    
    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }
    
    private async Task NavigateToDetail(Item item)
    {
        var parameters = new NavigationParameters();
        parameters.Add("Item", item);
        await _navigationService.NavigateAsync("DetailPage", parameters);
    }
}
```

**MAUI (Shell Navigation)**
```csharp
public partial class MainViewModel : ObservableObject
{
    [RelayCommand]
    private async Task NavigateToDetail(Item item)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Item", item }
        };
        await Shell.Current.GoToAsync("DetailPage", parameters);
    }
}
```

```csharp
[QueryProperty(nameof(Item), "Item")]
public partial class DetailPage : ContentPage
{
    public Item Item { get; set; }
    
    public DetailPage(DetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

---

## MVVM Patterns

### ViewModel Base Class Migration

**Xamarin (Prism + Reactive)**
```csharp
public abstract class BaseViewModel : BindableBase, IDisposable, IDestructible
{
    private readonly CompositeDisposable _disposables = new();
    
    public virtual void Destroy() => Dispose();
    
    protected void PropertyIsDependentOn(string source, string target)
    {
        this.WhenPropertyChanged(source)
            .ObserveOn(SynchronizationContext.Current)
            .Select(_ => target)
            .SubscribeWeakly(this, RaisePropertyChanged)
            .DisposeWith(_disposables);
    }
}
```

**MAUI (CommunityToolkit.Mvvm)**
```csharp
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;
}

public partial class MainViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private ObservableCollection<Item> _items = new();

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            // Load items
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### Compiled Bindings

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:YourApp.ViewModels"
             x:Class="YourApp.Views.MainPage"
             x:DataType="vm:MainViewModel">
    
    <CollectionView ItemsSource="{Binding Items}"
                    VirtualizationMode="Recycling">
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="vm:ItemViewModel">
                <Grid>
                    <Label Text="{Binding Name}" />
                    <Label Text="{Binding Description}" />
                </Grid>
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
    
    <Button Text="Load" 
            Command="{Binding LoadItemsCommand}" />
</ContentPage>
```

---

## Performance Optimization

### Compilation Settings
```xml
<PropertyGroup>
    <RunAOTCompilation>true</RunAOTCompilation>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>full</TrimMode>
</PropertyGroup>
```

### CollectionView Best Practices

```xml
<CollectionView ItemsSource="{Binding Items}"
                VirtualizationMode="Recycling"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <!-- Item content -->
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

### Layout Optimization

```xml
<!-- Good: Flattened Grid -->
<Grid RowDefinitions="Auto,Auto" ColumnDefinitions="*,Auto">
    <Label Grid.Row="0" Grid.Column="0" />
    <Label Grid.Row="0" Grid.Column="1" />
    <Label Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2" />
</Grid>

<!-- Bad: Nested StackLayouts -->
<VerticalStackLayout>
    <HorizontalStackLayout>
        <Label />
        <Label />
    </HorizontalStackLayout>
    <Label />
</VerticalStackLayout>
```

---

## Platform-Specific Code

### Conditional Compilation

```csharp
public partial class PlatformService
{
    public partial string GetDeviceId();
}

#if ANDROID
public partial class PlatformService
{
    public partial string GetDeviceId()
    {
        var context = Android.App.Application.Context;
        return Android.Provider.Settings.Secure.GetString(
            context.ContentResolver,
            Android.Provider.Settings.Secure.AndroidId);
    }
}
#elif IOS
public partial class PlatformService
{
    public partial string GetDeviceId()
    {
        return UIKit.UIDevice.CurrentDevice.IdentifierForVendor?.ToString() ?? string.Empty;
    }
}
#endif
```

---

## Memory Management

### Disposal Patterns

```csharp
public partial class ViewModelName : BaseViewModel
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Realm _realm;

    public ViewModelName(IRealmService realmService)
    {
        _realm = realmService.GetRealm();
    }

    public override void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _realm.Dispose();
        
        base.Dispose();
    }
}
```

### Event Handler Management

```csharp
public partial class CustomControl : ContentView
{
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        
        if (args.OldHandler != null)
        {
            CleanupEventHandlers();
        }
        
        if (args.NewHandler != null)
        {
            SetupEventHandlers();
        }
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        
        if (Parent == null)
        {
            CleanupEventHandlers();
        }
    }
}
```

---

## Migration Checklist

### Pre-Migration
- [ ] Audit Xamarin.Forms packages for MAUI equivalents
- [ ] Document custom renderers for handler conversion
- [ ] Identify platform-specific code needing updates
- [ ] Review third-party dependencies

### Project Structure
- [ ] Create new MAUI project with multi-targeting
- [ ] Migrate shared code files
- [ ] Update namespace references
- [ ] Configure MauiProgram.cs
- [ ] Set up AppShell with routes

### Code Migration
- [ ] Convert App.xaml to MAUI structure
- [ ] Update ViewModels to use CommunityToolkit.Mvvm
- [ ] Replace Prism navigation with Shell navigation
- [ ] Convert renderers to handlers
- [ ] Add compiled bindings (x:DataType)

### Performance
- [ ] Replace ListView with CollectionView
- [ ] Flatten layout hierarchies
- [ ] Enable AOT compilation and trimming
- [ ] Optimize image loading
- [ ] Add proper disposal patterns
- [ ] Use WeakReferenceMessenger for cross-VM communication

### Testing
- [ ] Test on Android devices
- [ ] Test on iOS devices
- [ ] Profile memory usage
- [ ] Test navigation flows
- [ ] Verify platform-specific features

---

## Resources

- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [Migration Guide](https://docs.microsoft.com/dotnet/maui/get-started/migrate/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [MAUI Handler Architecture](https://learn.microsoft.com/dotnet/maui/user-interface/handlers/)
