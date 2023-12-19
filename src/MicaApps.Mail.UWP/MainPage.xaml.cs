
using Mail.Pages;
using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Authentication;

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

            //每当登录状态变动时所执行的动作
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
