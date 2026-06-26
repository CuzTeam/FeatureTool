using FeatureTool.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FeatureTool.Views
{
    public sealed partial class ConfigPage : Page
    {
        private FeaturePage? _featurePage;

        public ConfigPage()
        {
            InitializeComponent();
            ShowReadOnlyToggle.IsOn = AppSettings.Instance.ShowReadOnlyFeatures;
        }

        public void SetFeaturePage(FeaturePage featurePage)
        {
            _featurePage = featurePage;
        }

        private void ShowReadOnlyToggle_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.Instance.ShowReadOnlyFeatures = ShowReadOnlyToggle.IsOn;
            _featurePage?.RefreshFilter();
        }
    }
}
