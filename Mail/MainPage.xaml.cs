
using Mail.Pages;
using Mail.Servives;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Mail
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is bool value )
            {
                if (value)
                {
                    MainNavigation.Navigate(typeof(HomePage));
                }
                else
                {
                    MainNavigation.Navigate(typeof(LoginPage));
                    
                }
            }
            App.Current.Services.GetService<OutlookService>().Provider.StateChanged += Provider_StateChanged;
        }

        private void Provider_StateChanged(object sender, CommunityToolkit.Authentication.ProviderStateChangedEventArgs e)
        {
            if (e.OldState != e.NewState)
            {
                switch(e.NewState)
                {
                    case CommunityToolkit.Authentication.ProviderState.SignedIn:
                        MainNavigation.Navigate(typeof(HomePage));
                        break;
                    case CommunityToolkit.Authentication.ProviderState.SignedOut:
                        MainNavigation.Navigate(typeof(LoginPage));
                        break;
                }
            }
        }

        

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
            var coreApplicationViewTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreApplicationViewTitleBar.ExtendViewIntoTitleBar = true;

            var applicationViewTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            applicationViewTitleBar.ButtonBackgroundColor = Colors.Transparent;
            applicationViewTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

    }
}
