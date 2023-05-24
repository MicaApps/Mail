using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail.Pages
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            SetupTitleBar();
        }

        private void SetupTitleBar()
        {
            CoreApplicationViewTitleBar SystemBar = CoreApplication.GetCurrentView().TitleBar;
            SystemBar.LayoutMetricsChanged += SystemBar_LayoutMetricsChanged;
            SystemBar.IsVisibleChanged += SystemBar_IsVisibleChanged;
            Window.Current.SetTitleBar(AppTitleBar);
        }

        private void SystemBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (sender.IsVisible)
            {
                Window.Current.SetTitleBar(AppTitleBar);
                AppTitleBar.Visibility = Visibility.Visible;
            }
            else
            {
                Window.Current.SetTitleBar(null);
                AppTitleBar.Visibility = Visibility.Collapsed;
            }
        }

        private void SystemBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            AppTitleBar.Height = sender.Height;
        }

        private async void Outlook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.Services.GetService<OutlookService>().SignInAsync();
            }
            catch (Exception ex)
            {
                await CoreWindow.GetForCurrentThread().DispatcherQueue.EnqueueAsync(() =>
                {
                    ErrorMessageBar.IsOpen = true;
                    ErrorMessageBar.Message = ex.Message;
                });
                
            }
        }
    }
}
