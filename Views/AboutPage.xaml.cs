using System;
using FeatureTool.Services;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FeatureTool.Views
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            VersionText.Text = "v" + AppInfo.Version;
        }

        private async void RepoButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/CuzTeam/FeatureTool"));
        }
    }
}
