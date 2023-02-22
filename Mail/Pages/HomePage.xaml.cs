using Mail.Class.Models;
using Mail.Enum;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;

namespace Mail.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly ObservableCollection<AccountModel> AccountSource = new ObservableCollection<AccountModel>();

        public HomePage()
        {
            InitializeComponent();

            SetupTitleBar();
            //Test only and should remove this later
            AccountSource.Add(new AccountModel("TestName", "TestAddress"));

            Loaded += HomePage_Loaded;
        }

        private void SetupTitleBar()
        {
            CoreApplicationViewTitleBar SystemBar = CoreApplication.GetCurrentView().TitleBar;
            SystemBar.LayoutMetricsChanged += SystemBar_LayoutMetricsChanged;
            SystemBar.IsVisibleChanged += SystemBar_IsVisibleChanged;
            AppTitleBar.Margin = new Thickness(SystemBar.SystemOverlayLeftInset, AppTitleBar.Margin.Top, SystemBar.SystemOverlayRightInset, AppTitleBar.Margin.Bottom);
            Window.Current.SetTitleBar(AppTitleBar);
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavView.SettingsItem is NavigationViewItem SettingItem)
            {
                SettingItem.SelectsOnInvoked = false;
            }

            NavigationContent.Navigate(typeof(MailFolderDetailsPage), MailFolderType.Inbox, new DrillInNavigationTransitionInfo());
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
            AppTitleBar.Margin = new Thickness(sender.SystemOverlayLeftInset, AppTitleBar.Margin.Top, sender.SystemOverlayRightInset, AppTitleBar.Margin.Bottom);
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigationContent.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                NavigationContent.Navigate(typeof(MailFolderDetailsPage), args.InvokedItemContainer.Tag, new DrillInNavigationTransitionInfo());
            }
        }
    }
}
