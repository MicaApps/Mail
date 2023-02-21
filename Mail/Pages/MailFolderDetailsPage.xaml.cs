using Mail.Class;
using Mail.Class.Data;
using Mail.Class.Models;
using Mail.Enum;
using Mail.Servives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Nito.AsyncEx;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Mail.Pages
{
    public sealed partial class MailFolderDetailsPage : Page
    {
        private readonly AsyncLock SelectionChangeLocker = new AsyncLock();
        private readonly ObservableCollection<MailMessageListDetailViewModel> PreviewSource = new ObservableCollection<MailMessageListDetailViewModel>();

        public MailFolderDetailsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MailFolderType Type)
            {
                MailFolderData Data = await App.Services.GetService<OutlookService>().GetMailFolderAsync(Type, 0, 20);

                foreach (MailMessageData MessageData in Data.MessageCollection)
                {
                    PreviewSource.Add(new MailMessageListDetailViewModel(MessageData));
                }
            }
        }

        private async void ListDetailsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.SingleOrDefault() is MailMessageListDetailViewModel Model)
            {
                if (sender is ListDetailsView View)
                {
                    using (await SelectionChangeLocker.LockAsync())
                    {
                        for (int Retry = 0; Retry < 10; Retry++)
                        {
                            if (View.FindChildOfType<WebView2>() is WebView2 Browser)
                            {
                                await Browser.EnsureCoreWebView2Async().AsTask().ContinueWith((_) => Browser.NavigateToString(Model.Content), TaskScheduler.FromCurrentSynchronizationContext());
                                break;
                            }

                            await Task.Delay(300);
                        }
                    }
                }
            }
        }
    }
}
