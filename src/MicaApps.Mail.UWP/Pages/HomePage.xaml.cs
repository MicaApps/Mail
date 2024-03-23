using CommunityToolkit.Authentication;
using Mail.Extensions;
using Mail.Models;
using Mail.Models.Enums;
using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Mail.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly ObservableCollection<AccountModel> _accountSource = new();
        private readonly ObservableCollection<object> _mailFolderSource = new();
        private readonly OutlookService _mailService;

        public HomePage()
        {
            InitializeComponent();

            SetupTitleBar();
            SetupPaneToggleButton();

            //TODO: Test only and should remove this later
            _mailService = App.Services.GetRequiredService<OutlookService>();

            if (_mailService is OutlookService outlookService)
            {
                var provider = outlookService.Provider as MsalProvider;
                var model = new AccountModel(
                    provider.Account.GetTenantProfiles().First().ClaimsPrincipal.FindFirst("name").Value,
                    provider.Account.Username,
                    outlookService.MailType);
                _accountSource.Add(model);

                outlookService.CurrentAccount = model;
            }

            Loaded += HomePage_Loaded;
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
            try
            {
                _mailFolderSource.Add(new NavigationViewItemSeparator());

                try
                {
                    IReadOnlyList<Models.MailFolder> MailFolders = await _mailService.GetMailSuperFoldersAsync()
                        .OrderBy(folder => folder.Type)
                        .ToArrayAsync();

                    if (MailFolders.Count > 0)
                    {
                        _mailFolderSource.AddRange(MailFolders.TakeWhile((Data) => Data.Type != MailFolderType.Other));
                        _mailFolderSource.Add(new NavigationViewItemSeparator());
                        _mailFolderSource.AddRange(MailFolders.Skip(_mailFolderSource.Count));
                    }
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception);
                }

                NavView.SelectedItem = _mailFolderSource.OfType<Models.MailFolder>().FirstOrDefault();
                _mailService.MailFoldersTree.CollectionChanged += (Sender, Args) =>
                {
                    //Trace.WriteLine($"Tree Changed: {Enum.GetName(typeof(NotifyCollectionChangedAction),Args.Action)} : {JsonConvert.SerializeObject(Args.NewItems)}");
                    foreach (var item in Args.NewItems)
                    {
                        if (Args.Action == NotifyCollectionChangedAction.Add)
                            _mailFolderSource.Add(item);
                        else if (Args.Action == NotifyCollectionChangedAction.Remove)
                            _mailFolderSource.Remove(item);
                    }
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
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
        }

        private void NavView_SelectionChanged(NavigationView sender,
            Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                if (args.IsSettingsSelected)
                {
                    NavigationContent.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
                }
                else if (args.SelectedItem is Models.MailFolder data)
                {
                    NavigationContent.Navigate(typeof(MailFolderDetailsPage), data, new DrillInNavigationTransitionInfo());
                }
            }
            catch (Exception ex) { string a = ex.Message; }
        }

        private void PaneToggleButton_Click(object sender, RoutedEventArgs e)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }
    }
}