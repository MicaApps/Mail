﻿
using Mail.Pages;
using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace Mail
{
    public sealed partial class MainPage : Page
    {
        public MainPage(bool isLogin)
        {
            InitializeComponent();
            ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
            TitleBar.ButtonBackgroundColor = Colors.Transparent;
            TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            App.Services.GetService<OutlookService>().Provider.StateChanged += Provider_StateChanged;

            if (isLogin)
            {
                MainNavigation.Navigate(typeof(HomePage));
            }
            else
            {
                MainNavigation.Navigate(typeof(LoginPage));
            }
        }

        private void Provider_StateChanged(object sender, CommunityToolkit.Authentication.ProviderStateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case CommunityToolkit.Authentication.ProviderState.SignedIn:
                    {
                        MainNavigation.Navigate(typeof(HomePage));
                        break;
                    }
                case CommunityToolkit.Authentication.ProviderState.SignedOut:
                    {
                        MainNavigation.Navigate(typeof(LoginPage));
                        break;
                    }
            }
        }
    }
}
