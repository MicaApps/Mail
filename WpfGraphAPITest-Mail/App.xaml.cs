using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Identity.Client.Broker;

namespace WpfGraphAPITest_Mail
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            CreateApplication();
        }


        public static void CreateApplication()
        {
            //new
            _clientApp = PublicClientApplicationBuilder.Create(ClientId)
                                                    .WithAuthority($"{Instance}{Tenant}")
                                                    .WithDefaultRedirectUri()
                                                    .WithBrokerPreview(true)
                                                    //.WithRedirectUri("http://localhost")
                                                    .Build();
            //new

            var storageProperties =
                 new StorageCreationPropertiesBuilder(CacheSettings.CacheFileName, CacheSettings.CacheDir)
                 //.WithUnprotectedFile()
                 .Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).Result;
            cacheHelper.RegisterCache(_clientApp.UserTokenCache);
        }

        // Below are the clientId (Application Id) of your app registration and the tenant information. 
        // You have to replace:
        // - the content of ClientID with the Application Id for your app registration
        // - The content of Tenant by the information about the accounts allowed to sign-in in your application:
        //   - For Work or School account in your org, use your tenant ID, or domain
        //   - for any Work or School accounts, use organizations
        //   - for any Work or School accounts, or Microsoft personal account, use common
        //   - for Microsoft Personal account, use consumers
        private static string ClientId = "0b3dac55-dc21-442b-ace7-ccefbb5a9f80";

        // Note: Tenant is important for the quickstart.
        private static string Tenant = "common";
        private static string Instance = "https://login.microsoftonline.com/";
        private static IPublicClientApplication _clientApp;

        public static IPublicClientApplication PublicClientApp { get { return _clientApp; } }
    }
}
