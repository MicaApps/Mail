using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mail.Services;
using Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Core;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace MicaApps.Mail.PageModels
{
    internal class SplashPageModel : ObservableObject
    {
        public ICommand DismissedCommand {  get; set; }

        public SplashPageModel() 
        {
            DismissedCommand = new AsyncRelayCommand(DismissedAsync);
        }

        private async Task DismissedAsync()
        {
            bool IsSignIn = await App.Services.GetService<OutlookService>().SignInSilentAsync();
                Window.Current.Content = new Frame
                {
                    Content = new MainPage(IsSignIn)
                };
        }
    }
}
