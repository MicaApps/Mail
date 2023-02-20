
using Mail.Servives;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Mail.Pages
{

    public class Email
    {
        public string address { get; set; }
        public string name { get; set; }
    }
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HomePage : Page
    {

        //数据绑定
        private static ObservableCollection<Email> data = new ObservableCollection<Email>();

        public HomePage()
        {
            this.InitializeComponent();
            listView.ItemsSource = data;
            List<Email> emails;
            emails = new List<Email>();
            for (int i = 0; i < 2; i++)
            {
                emails.Add(new Email { name = "name" + i, address = i + "@qq.com" });
            }
            LoadEmail( emails);
        }

        public void LoadEmail(List<Email> emails)
        {
            data.Clear();
            for (int i = 0; i < emails.Count; i++)
            {
                data.Add(emails[i]);
            }
        }


        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args != null)
            {
                if (args.IsSettingsSelected)
                {
                    NavigationContent.Navigate(typeof(SettingsPage));
                }
            }
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var coreApplicationViewTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreApplicationViewTitleBar.LayoutMetricsChanged += (s, args) => UpdateAppTitle(s);

            Window.Current.SetTitleBar(AppTitleBar);

        }

        private void UpdateAppTitle(CoreApplicationViewTitleBar coreTitleBar)
        {
            Thickness currMargin = AppTitleBar.Margin;
            AppTitleBar.Margin = new Thickness(currMargin.Left, currMargin.Top, coreTitleBar.SystemOverlayRightInset, currMargin.Bottom);
        }
    }
}
