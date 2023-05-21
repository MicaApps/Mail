using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using CommunityToolkit.Authentication;
using Mail.Services;
using Mail.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Mail.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly ObservableCollection<AccountModel> AccountSource = new ObservableCollection<AccountModel>();

        private readonly ObservableCollection<object> MailFolderSource = new ObservableCollection<object>();

        public HomePage()
        {
            InitializeComponent();

            SetupTitleBar();

            //TODO: Test only and should remove this later
            try
            {
                MsalProvider Provider = App.Services.GetService<OutlookService>().Provider as MsalProvider;
                AccountSource.Add(new AccountModel(
                    Provider.Account.GetTenantProfiles().First().ClaimsPrincipal.FindFirst("name").Value,
                    Provider.Account.Username));
            }
            catch
            {
                throw;
            }


            Loaded += HomePage_Loaded;
        }

        private void SetupTitleBar()
        {
            CoreApplicationViewTitleBar SystemBar = CoreApplication.GetCurrentView().TitleBar;
            SystemBar.LayoutMetricsChanged += SystemBar_LayoutMetricsChanged;
            SystemBar.IsVisibleChanged += SystemBar_IsVisibleChanged;
            AppTitleBar.Margin = new Thickness(SystemBar.SystemOverlayLeftInset, AppTitleBar.Margin.Top,
                SystemBar.SystemOverlayRightInset, AppTitleBar.Margin.Bottom);
            Window.Current.SetTitleBar(AppTitleBar);
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavView.SettingsItem is NavigationViewItem SettingItem)
            {
                SettingItem.SelectsOnInvoked = false;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New)
            {
                MailFolderSource.Add(0);
                var isFirstOther = true;
                try
                {
                    // TODO 新账号会引发异常
                    // the specified folder could not be found in the store
                    await foreach (var item in App.Services.GetService<OutlookService>().GetMailFoldersAsync()
                                       .OrderBy(item => item.Type))
                    {
                        if (item.Type == MailFolderType.Other && isFirstOther)
                        {
                            MailFolderSource.Add(1);
                            isFirstOther = false;
                        }

                        MailFolderSource.Add(item);
                    }
                }
                catch (Exception Exception)
                {
                    //Console.WriteLine(Exception);
                }

                NavView.SelectedItem = MailFolderSource.FirstOrDefault(item => item is MailFolderData);
            }
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
            AppTitleBar.Margin = new Thickness(sender.SystemOverlayLeftInset, AppTitleBar.Margin.Top,
                sender.SystemOverlayRightInset, AppTitleBar.Margin.Bottom);
        }

        private void NavView_SelectionChanged(NavigationView sender,
            Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigationContent.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
            }
            else if (args.SelectedItem is MailFolderData data)
            {
                NavigationContent.Navigate(typeof(MailFolderDetailsPage), data, new DrillInNavigationTransitionInfo());
            }
        }
    }

    class FolderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MailFolderType folderType)
            {
                return folderType switch
                {
                    MailFolderType.Inbox => "\uE10F",
                    MailFolderType.Deleted => "\uE107",
                    MailFolderType.Drafts => "\uEC87",
                    MailFolderType.SentItems => "\uE122",
                    MailFolderType.Junk => "\uE107",
                    MailFolderType.Archive => "\uE7B8",
                    MailFolderType.Other => "\uE8B7",
                    _ => "\uE8B7"
                };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    class MailFolderNavigationDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Divider { get; set; }

        public DataTemplate Content { get; set; }

        public DataTemplate ContentWithChild { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is MailFolderData folder)
            {
                if (folder.ChildFolders != null) return ContentWithChild;

                return Content;
            }
            else
            {
                return Divider;
            }
        }
    }
}