# Xamarin-to-MAUI-AI-Skills

AI-assisted development skills and guides for migrating Xamarin Forms to .NET MAUI with best practices, MVVM patterns, and performance optimization.

---

## 📁 Repository Contents

```
.
├── .github/
│   └── copilot-instructions.md         # GitHub Copilot context file
├── skills/
│   ├── xamarin-forms-development.md    # Xamarin Forms day-to-day development
│   ├── dotnet-maui-development.md      # .NET MAUI day-to-day development
│   └── maui-migration-guide.md          # Complete migration guide
├── examples/
│   ├── xamarin/
│   │   ├── ViewModel-Example.cs         # Prism ViewModel with Rx
│   │   ├── Renderer-Example.cs          # Android custom renderer
│   │   └── Page-Example.xaml            # XAML page with bindings
│   └── maui/
│       ├── ViewModel-Example.cs         # ObservableObject with source generators
│       ├── Handler-Example.cs           # Android custom handler
│       └── Page-Example.xaml            # XAML page with compiled bindings
├── LICENSE                               # MIT License
└── README.md                             # This file
```

---

## 🚀 Quick Start

### For Individual Developers

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/Xamarin-to-MAUI-AI-Skills.git
   ```

2. **Copy to your project**
   ```bash
   cp Xamarin-to-MAUI-AI-Skills/.github/copilot-instructions.md your-project/.github/
   ```

3. **Use with GitHub Copilot**
   - Copilot automatically detects the instructions
   - Reference skill files in chat: `@workspace using xamarin-forms-development.md`

---

## 📚 What's Included

### 1. **xamarin-forms-development.md**
Complete guide for Xamarin Forms development with:
- Prism MVVM patterns
- Reactive Extensions (Rx)
- Custom Renderers (Android/iOS)
- Realm database integration
- Navigation patterns
- Memory management

### 2. **dotnet-maui-development.md**
Comprehensive .NET MAUI guide covering:
- CommunityToolkit.Mvvm source generators
- Custom Handlers (replaces Renderers)
- Shell navigation
- Compiled bindings
- Performance optimization
- WeakReferenceMessenger

### 3. **maui-migration-guide.md**
Step-by-step migration guide with:
- Architecture transformation
- Renderers → Handlers conversion
- Prism → Shell navigation
- Project structure changes
- Complete code examples

---

## 🔧 IDE Setup

### Visual Studio (Windows/Mac)

1. **Install GitHub Copilot**
   - Extensions → Manage Extensions → Search "GitHub Copilot"
   - Install and restart

2. **Configure**
   - Copy `.github/copilot-instructions.md` to your project
   - Copilot automatically uses these instructions

3. **Usage**
   - Open Copilot Chat (`Ctrl+Alt+C`)
   - Type: `Create ViewModel per xamarin-forms-development.md`

### Visual Studio Code

1. **Install Extensions**
   - GitHub Copilot
   - GitHub Copilot Chat

2. **Configure**
   - Copy files to your project root

3. **Usage**
   - Open Chat: `Ctrl+Shift+I`
   - Reference skills: `@workspace using dotnet-maui-development.md`

### JetBrains Rider

1. **Install Plugin**
   - Settings → Plugins → GitHub Copilot

2. **Configure**
   - Copy instructions file to `.github/`

3. **Usage**
   - Chat: `Ctrl+Shift+G`
   - Inline suggestions work automatically

---

## 💡 Usage Examples

### Creating a Xamarin ViewModel

**Reference:** `xamarin-forms-development.md`

**Prompt:**
```
@workspace Create a Prism ViewModel for a list page with:
- INavigationService dependency
- ObservableCollection for items
- Rx-based search with throttle
- Proper disposal pattern

Follow xamarin-forms-development.md patterns.
```

**Expected Output:**
```csharp
public class ItemListViewModel : BindableBase, IDestructible
{
    private readonly INavigationService _navigationService;
    private readonly CompositeDisposable _disposables = new();

    public ItemListViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        this.WhenPropertyChanged(nameof(SearchText))
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Subscribe(_ => PerformSearch())
            .DisposeWith(_disposables);
    }

    private ObservableCollection<Item> _items;
    public ObservableCollection<Item> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public void Destroy() => _disposables.Dispose();
}
```

### Creating a MAUI Handler

**Reference:** `dotnet-maui-development.md`

**Prompt:**
```
@workspace Create a custom Entry handler for Android with:
- Transparent background
- Border color support
- Proper event cleanup in DisconnectHandler

Follow dotnet-maui-development.md handler patterns.
```

**Expected Output:**
```csharp
public partial class CustomEntryHandler : EntryHandler
{
    public static IPropertyMapper<IEntry, CustomEntryHandler> Mapper = 
        new PropertyMapper<IEntry, CustomEntryHandler>(EntryHandler.Mapper)
        {
            [nameof(CustomEntry.BorderColor)] = MapBorderColor
        };

    protected override AppCompatEditText CreatePlatformView()
    {
        var view = base.CreatePlatformView();
        view.SetBackgroundColor(Android.Graphics.Color.Transparent);
        return view;
    }

    protected override void ConnectHandler(AppCompatEditText platformView)
    {
        base.ConnectHandler(platformView);
        platformView.TextChanged += OnTextChanged;
    }

    protected override void DisconnectHandler(AppCompatEditText platformView)
    {
        platformView.TextChanged -= OnTextChanged; // CRITICAL: Prevent memory leak
        base.DisconnectHandler(platformView);
    }
}
```

### Migrating Code

**Reference:** `maui-migration-guide.md`

**Prompt:**
```
@workspace Convert this Xamarin renderer to MAUI handler:
[paste code here]

Follow maui-migration-guide.md section on Renderers to Handlers.
```

---

## 🎯 Key Features

### For Xamarin Development
- ✅ Prism MVVM patterns
- ✅ Rx subscription management
- ✅ Custom Renderers (Android & iOS)
- ✅ Realm database best practices
- ✅ Memory leak prevention
- ✅ Thread safety patterns

### For MAUI Development
- ✅ CommunityToolkit.Mvvm source generators
- ✅ Custom Handlers with proper cleanup
- ✅ Shell navigation patterns
- ✅ Compiled bindings (x:DataType)
- ✅ Performance optimization (AOT, trimming)
- ✅ WeakReferenceMessenger patterns

### For Migration
- ✅ Step-by-step conversion guide
- ✅ Architecture transformation
- ✅ Before/after code examples
- ✅ Common pitfalls and solutions
- ✅ Testing checklist

---

## 📊 Comparison: Xamarin vs MAUI

| Feature | Xamarin | MAUI |
|---------|---------|------|
| MVVM Framework | Prism | CommunityToolkit.Mvvm |
| Navigation | Prism NavigationService | Shell Navigation |
| Custom UI | Renderers | Handlers |
| Dependency Injection | Prism DryIoc | Microsoft.Extensions.DI |
| Messaging | Prism EventAggregator | WeakReferenceMessenger |
| Collections | ListView | CollectionView |
| Bindings | Runtime | Compiled (x:DataType) |

---

## 🏆 Best Practices Included

### Performance
- [x] AOT compilation settings
- [x] IL trimming configuration
- [x] CollectionView virtualization
- [x] Layout hierarchy flattening
- [x] Compiled bindings
- [x] Proper disposal patterns

### Architecture
- [x] MVVM with source generators
- [x] Weak reference messaging
- [x] Async/await patterns
- [x] Cancellation token usage
- [x] Dependency injection

### Memory Management
- [x] Event handler cleanup
- [x] Handler disconnection
- [x] Weak references
- [x] Disposable patterns
- [x] Realm thread safety

---

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Ways to Contribute
- Report issues
- Suggest improvements
- Add code examples
- Translate to other languages
- Share your migration experience

---

## 📄 License

This project is licensed under the MIT License - see [LICENSE](LICENSE) file for details.

You are free to:
- ✅ Use commercially
- ✅ Modify
- ✅ Distribute
- ✅ Use privately

Requirements:
- Include copyright notice
- Include license text

---

## 🙏 Acknowledgments

- [Microsoft .NET MAUI Team](https://github.com/dotnet/maui)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Prism Library](https://github.com/PrismLibrary/Prism)
- [Realm](https://github.com/realm/realm-dotnet)
- All contributors

---

## 🔗 Related Resources

### Official Documentation
- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [Xamarin.Forms Documentation](https://docs.microsoft.com/xamarin/xamarin-forms/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [Prism Documentation](https://prismlibrary.com/)

### Migration Tools
- [.NET Upgrade Assistant](https://dotnet.microsoft.com/platform/upgrade-assistant)
- [Xamarin to MAUI Migration Docs](https://docs.microsoft.com/dotnet/maui/get-started/migrate/)

### Community
- [.NET MAUI GitHub](https://github.com/dotnet/maui)
- [Xamarin Forums](https://forums.xamarin.com/)
- [Stack Overflow - MAUI](https://stackoverflow.com/questions/tagged/maui)

---

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/yourusername/Xamarin-to-MAUI-AI-Skills/issues)
- **Discussions:** [GitHub Discussions](https://github.com/yourusername/Xamarin-to-MAUI-AI-Skills/discussions)

---

## ⭐ Star History

If you find this helpful, please ⭐ star the repository!

---

**Made with ❤️ for the .NET community**
