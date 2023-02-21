using Mail.Class;
using Mail.Servives;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ObservableCollection<Email> EmailSource = new ObservableCollection<Email>();

        public SettingsPage()
        {
            InitializeComponent();
        }

        public async void SignOut(object sender, RoutedEventArgs e)
        {
            await App.Services.GetService<OutlookService>().SignOutAsync(null);
        }

        public async void GetEmail(object sender, RoutedEventArgs e)
        {
            EmailSource.Clear();
            EmailSource.AddRange(await App.Services.GetService<OutlookService>().GetEmailAsync());
        }
    }
}
