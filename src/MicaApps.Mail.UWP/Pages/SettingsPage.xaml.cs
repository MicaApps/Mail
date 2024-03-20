using Chloe;
using Mail.Extensions;
using Mail.Models;
using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ObservableCollection<ContactModel> ContactSource = new ObservableCollection<ContactModel>();

        public SettingsPage()
        {
            InitializeComponent();
        }

        public async void SignOut(object sender, RoutedEventArgs e)
        {
            await App.Services.GetService<OutlookService>().SignOutAsync(null);
            App.Services.GetService<LocalCacheService>().ClearData();
        }

        public async void GetEmail(object sender, RoutedEventArgs e)
        {
            ContactSource.Clear();
            ContactSource.AddRange(await App.Services.GetService<OutlookService>().GetContactsAsync());
        }
    }
}
