
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
        public MainPage(bool isLogin)
        {
            InitializeComponent();
            ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
            TitleBar.ButtonBackgroundColor = Colors.Transparent;
            TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            App.Services.GetService<OutlookService>().Provider.StateChanged += Provider_StateChanged;

            if (isLogin)
            {
                HandleLogin();
            }
            else
            {
                MainNavigation.Navigate(typeof(LoginPage));
            }
        }

        private void HandleLogin()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Load Contacts to Cache
                    var contacts = await App.Services.GetService<OutlookService>().GetContactsAsync();
                    App.Services.GetService<ICacheService>()!.AddOrReplaceCache<IReadOnlyList<ContactModel>>(contacts);
                }
                catch (Exception e)
                {
                    // ignore
                }
            });
            MainNavigation.Navigate(typeof(HomePage));
        }

        private void Provider_StateChanged(object sender, ProviderStateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case ProviderState.SignedIn:
                    {
                        HandleLogin();
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
