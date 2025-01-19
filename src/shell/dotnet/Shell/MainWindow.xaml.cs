// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Ribbon;
using CommunityToolkit.Mvvm.ComponentModel;
using MorganStanley.ComposeUI.LayoutPersistence.Abstractions;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.ImageSource;
using MorganStanley.ComposeUI.Shell.Layout;
using MorganStanley.ComposeUI.Shell.Utilities;

namespace MorganStanley.ComposeUI.Shell;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : RibbonWindow
{
    private readonly IModuleLoader _moduleLoader;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly ImageSourceProvider _iconProvider;
    private readonly ILayoutPersistence<string> _layoutPersistence;
    private readonly LayoutManager _layoutManager;

    public MainWindow(
        IModuleCatalog moduleCatalog,
        IModuleLoader moduleLoader,
        ILayoutPersistence<string> layoutPersistence,
        IImageSourcePolicy? imageSourcePolicy = null)
    {
        _moduleCatalog = moduleCatalog;
        _moduleLoader = moduleLoader;
        _layoutPersistence = layoutPersistence;
        _iconProvider = new ImageSourceProvider(imageSourcePolicy ?? new DefaultImageSourcePolicy());

        InitializeComponent();
        SelectModulesTab();

        _layoutManager = new LayoutManager(_xamDockManager, _moduleLoader, _layoutPersistence);
    }

    private async void RibbonWindow_Initialized(object sender, System.EventArgs e)
    {
        var moduleIds = await _moduleCatalog.GetModuleIds();

        var modules = new List<ModuleViewModel>();
        foreach (var moduleId in moduleIds)
        {
            var manifest = await _moduleCatalog.GetManifest(moduleId);
            modules.Add(new ModuleViewModel(manifest, _iconProvider));
        }

        ViewModel = new MainWindowViewModel
        {
            Modules = new ObservableCollection<ModuleViewModel>(modules)
        };
    }

    public void AddDockableFloatingContent(WebContent webContent)
    {
        if (_layoutManager.IsLayoutLoading)
        {
            _layoutManager.AddAndSetModuleContentState(webContent);
        }
        else
        {
            _verticalSplit.Panes.Add(new WebContentPane(webContent, _moduleLoader));
        }
    }

    internal MainWindowViewModel ViewModel
    {
        get => (MainWindowViewModel) DataContext;
        private set => DataContext = value;
    }

    private async void StartModule_Click(object sender, RoutedEventArgs e)
    {
        // I ❤️ C#
        if (sender is FrameworkElement
            {
                DataContext: ModuleViewModel module
            })
        {
            await _moduleLoader.StartModule(new StartRequest(module.Manifest.Id));
        }
    }

    internal sealed class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<ModuleViewModel> Modules
        {
            get => _modules;
            set => SetProperty(ref _modules, value);
        }

        private ObservableCollection<ModuleViewModel> _modules = new();
    }

    internal sealed class ModuleViewModel
    {
        public ModuleViewModel(IModuleManifest manifest, ImageSourceProvider imageSourceProvider)
        {
            Manifest = manifest;

            if (manifest.TryGetDetails<WebManifestDetails>(out var webManifestDetails))
            {
                if (webManifestDetails.IconUrl != null)
                {
                    ImageSource = imageSourceProvider.GetImageSource(
                        webManifestDetails.IconUrl,
                        webManifestDetails.Url);
                }
            }
            else if (manifest.TryGetDetails<NativeManifestDetails>(out var nativeManifestDetails))
            {
                var path = nativeManifestDetails.Path.IsAbsoluteUri ? nativeManifestDetails.Path.AbsolutePath : nativeManifestDetails.Path.ToString();
                if (File.Exists(path))
                {
                    using var icon =
                        System.Drawing.Icon.ExtractAssociatedIcon(path);

                    if (icon != null)
                    {
                        using var bitmap = icon.ToBitmap();

                        ImageSource = bitmap.ToImageSource();
                    }
                }
            }
        }

        public IModuleManifest Manifest { get; }

        public System.Windows.Media.ImageSource? ImageSource { get; }
    }

    private async void LoadLayout_Click(object sender, RoutedEventArgs e)
    {
        await _layoutManager.LoadLayoutAsync();
        SelectModulesTab();
    }

    private async void SaveLayout_Click(object sender, RoutedEventArgs e)
    {
        await _layoutManager.SaveLayoutAsync();
        SelectModulesTab();
    }

    private void XamDockManager_InitializePaneContent(object sender, Infragistics.Windows.DockManager.Events.InitializePaneContentEventArgs e)
    {
        if (_layoutManager.WebContentPanes != null && _layoutManager.WebContentPanes.TryGetValue(e.NewPane.SerializationId, out var webContentPane))
        {
            e.NewPane = webContentPane;
        }
    }

    private void SelectModulesTab()
    {
        Ribbon.SelectedIndex = 1;
    }
}