using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
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
