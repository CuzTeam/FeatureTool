using System.Security.Principal;
using FeatureTool.Services;
using FeatureTool.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;

namespace FeatureTool
{
    public sealed partial class MainWindow : Window
    {
        private readonly FeaturePage _featurePage = new();
        private readonly ConfigPage _configPage = new();
        private readonly AboutPage _aboutPage = new();

        public string AppVersion => AppInfo.Version;

        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            _configPage.SetFeaturePage(_featurePage);

            ContentFrame.Content = _featurePage;

            NavView.SelectedItem = NavView.MenuItems[0];

            Activated += OnFirstActivated;
        }

        private bool _firstActivated = true;
        private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
        {
            if (!_firstActivated) return;
            _firstActivated = false;
            Resize(new SizeInt32(1000, 660));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item) return;
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "Feature":
                    ContentFrame.Content = _featurePage;
                    break;
                case "Config":
                    ContentFrame.Content = _configPage;
                    break;
                case "About":
                    ContentFrame.Content = _aboutPage;
                    break;
            }
        }

        private void Resize(SizeInt32 size)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId).Resize(size);
        }
    }
}
