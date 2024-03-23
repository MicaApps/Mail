
using Mail.Pages;
using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Authentication;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Mail.Models;

namespace Mail
{
    public sealed partial class MainPage : Page
    {
        public MainPage(bool isSignedIn)
        {
            InitializeComponent();
            ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
            TitleBar.ButtonBackgroundColor = Colors.Transparent;
            TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            App.Services.GetService<OutlookService>().Provider.StateChanged += Provider_StateChanged;

            if (isSignedIn)
                MainNavigation.Navigate(typeof(HomePage));
            else
                MainNavigation.Navigate(typeof(LoginPage));
        }

        private void Provider_StateChanged(object sender, ProviderStateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case ProviderState.SignedIn:
                {
                    MainNavigation.Navigate(typeof(HomePage));
                    break;
                }
                case ProviderState.SignedOut when MainNavigation.CurrentSourcePageType != typeof(LoginPage):
                {
                    MainNavigation.Navigate(typeof(LoginPage));
                    break;
                }
            }
        }
    }
}
