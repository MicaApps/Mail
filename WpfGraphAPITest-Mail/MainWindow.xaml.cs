using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

namespace WpfGraphAPITest_Mail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> list = new ObservableCollection<string>();
        public MainWindow()
        {
            InitializeComponent();
            mylist.ItemsSource = list;
        }

        string[] scopes = new string[] { "Mail.ReadWrite", "offline_access" };

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;

            IAccount firstAccount;

            int howToSignIn = 1;
            switch (howToSignIn)
            {
                // 0: Use account used to signed-in in Windows (WAM)
                case 0:
                    // WAM will always get an account in the cache. So if we want
                    // to have a chance to select the accounts interactively, we need to
                    // force the non-account
                    firstAccount = Microsoft.Identity.Client.PublicClientApplication.OperatingSystemAccount;
                    break;

                //  1: Use one of the Accounts known by Windows(WAM)
                case 1:
                    // We force WAM to display the dialog with the accounts
                    firstAccount = null;
                    break;

                //  Use any account(Azure AD). It's not using WAM
                default:
                    var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                    firstAccount = accounts.FirstOrDefault();
                    break;
            }

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(firstAccount)
                        .WithParentActivityOrWindow(new WindowInteropHelper(this).Handle) // optional, used to center the browser on the window
                        .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        list.Add($"Error Acquiring Token:{System.Environment.NewLine}{msalex}");
                    });
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    list.Add($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                });
                return;
            }

            if (authResult != null)
            {
                GetHttpContentWithToken(authResult.AccessToken);
                //DisplayBasicTokenInfo(authResult);
            }
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        public void GetHttpContentWithToken(string token)
        {
            var scopes = new[] { "Mail.ReadWrite" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = "common";

            // Value from app registration
            var clientId = "0b3dac55-dc21-442b-ace7-ccefbb5a9f80";

            var pca = PublicClientApplicationBuilder
                        .Create(clientId)
                        .WithTenantId(tenantId)
                        .Build();

            // DelegateAuthenticationProvider is a simple auth provider implementation
            // that allows you to define an async function to retrieve a token
            // Alternatively, you can create a class that implements IAuthenticationProvider
            // for more complex scenarios
            var authProvider = new DelegateAuthenticationProvider(async (request) => {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            });

            var graphClient = new GraphServiceClient(authProvider);

            List<Message> res = new List<Message>();
            var page = graphClient.Me.Messages.Request();
            while (page != null)
            {
                var pageres = page.GetAsync().Result;
                foreach (var item in pageres)
                    res.Add(item);
                page = pageres.NextPageRequest;
                break;//这只是一个示例，这次我们先只获取一页数据
            }

            foreach(var item in res)
                this.Dispatcher.Invoke(() =>
                {
                    list.Add($"{item.Subject}\n{item.BodyPreview}");
                });
        }
    }
}
