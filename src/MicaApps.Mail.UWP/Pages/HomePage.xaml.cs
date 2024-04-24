using CommunityToolkit.Authentication;
using Mail.Extensions;
using Mail.Services;
using Mail.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Mail.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly ObservableCollection<AccountModel> AccountSource = new();

        private readonly ObservableCollection<object> MailFolderSource = new();
        private readonly IMailService Service;

        public HomePage()
        {
            InitializeComponent();

            SetupTitleBar();

            SetupPaneToggleButton();

            //TODO: Test only and should remove this later
            Service = App.Services.GetService<OutlookService>()!;
            GetUserProfileAsync();

            Loaded += HomePage_Loaded;
        }

        private async void GetUserProfileAsync()
        {
            try
            { 
                var service = Service as OutlookService;
                var Provider = service.Provider as MsalProvider;

                var avatarBytes = await service.GetUserAvatarAsync();
               
                var bitmap = new BitmapImage();
                MemoryStream ms = new MemoryStream(avatarBytes);
                bitmap.SetSourceAsync(ms.AsRandomAccessStream());

                var model = new AccountModel(
                    Provider.Account.GetTenantProfiles().First().ClaimsPrincipal.FindFirst("name").Value,
                    Provider.Account.Username,
                    service.MailType,
                    bitmap);
                AccountSource.Add(model);

                service.CurrentAccount = model;
            }
            catch
            {
                throw;
            }
        }

        private void SetupTitleBar()
        {
            CoreApplicationViewTitleBar SystemBar = CoreApplication.GetCurrentView().TitleBar;
            SystemBar.LayoutMetricsChanged += SystemBar_LayoutMetricsChanged;
            SystemBar.IsVisibleChanged += SystemBar_IsVisibleChanged;
            Window.Current.SetTitleBar(AppTitleBar);
        }

        private void SetupPaneToggleButton()
        {
            PaneToggleButton.ApplyTemplate();
            var content = PaneToggleButton.FindChildOfName<ContentPresenter>("ContentPresenter");
            content.Visibility = Visibility.Collapsed;
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
            MailFolderSource.Add(new NavigationViewItemSeparator());

            try
            {
                IReadOnlyList<MailFolderData> MailFolders = await Service.GetMailSuperFoldersAsync().OrderBy((Data) => Data.Type).ToArrayAsync();

                if (MailFolders.Count > 0)
                {
                    MailFolderSource.AddRange(MailFolders.TakeWhile((Data) => Data.Type != MailFolderType.Other));
                    MailFolderSource.Add(new NavigationViewItemSeparator());
                    MailFolderSource.AddRange(MailFolders.Skip(MailFolderSource.Count));
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
            }

            NavView.SelectedItem = MailFolderSource.OfType<MailFolderData>().FirstOrDefault();

            Service.MailFoldersTree.CollectionChanged += (Sender, Args) =>
            {
                //Trace.WriteLine($"Tree Changed: {Enum.GetName(typeof(NotifyCollectionChangedAction),Args.Action)} : {JsonConvert.SerializeObject(Args.NewItems)}");
                foreach (var item in Args.NewItems)
                {
                    if (Args.Action == NotifyCollectionChangedAction.Add)
                        MailFolderSource.Add(item);
                    else if (Args.Action == NotifyCollectionChangedAction.Remove)
                        MailFolderSource.Remove(item);
                }
            };
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

        private void PaneToggleButton_Click(object sender, RoutedEventArgs e)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }
    }
}