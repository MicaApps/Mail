using Mail.Servives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Mail.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        //数据绑定
        private static ObservableCollection<Email> data = new ObservableCollection<Email>();
        public SettingsPage()
        {
            this.InitializeComponent();
            list.ItemsSource = data;
        }
        public void SignOut(object sender, RoutedEventArgs e)
        {
            _ = App.Current.Services.GetService<OutlookService>().SignOut(null);
        }
        public async void GetEmail(object sender, RoutedEventArgs e)
        {
            var emails =await App.Current.Services.GetService<OutlookService>().GetEmail();

            data.Clear();
            for (int i = 0; i < emails.Count; i++)
            {
                data.Add(emails[i]);
            }
        }


    }
}
