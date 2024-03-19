using LiteDB;
using Mail.Extensions;
using Mail.Models;
using Mail.Pages;
using Mail.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Mail
{
    sealed partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        public App()
        {
            InitializeComponent();
            Services = BuildServiceProvider();
        }

        private IServiceProvider BuildServiceProvider()
        {
            Windows.Storage.StorageFolder storageFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder;
            var fileStream = storageFolder.OpenStreamForWriteAsync("storage.db", CreationCollisionOption.OpenIfExists).Result;

            LiteDatabase liteDatabase = new LiteDatabase(fileStream);

            return new ServiceCollection()
                .AddSingleton<OutlookService, OutlookService>()
                .AddSingleton<LocalCacheService>()
                .AddSingleton<LiteDatabaseService>(_ => new LiteDatabaseService(liteDatabase))
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()))
                .BuildServiceProvider();
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await HandleLaunchedOrActivatedAsync(e);
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await HandleLaunchedOrActivatedAsync(args);
        }

        private async Task HandleLaunchedOrActivatedAsync(IActivatedEventArgs args)
        {
            SystemInformation.Instance.TrackAppUse(args);

            Window.Current.Content = new SplashPage(args.SplashScreen, args.PreviousExecutionState == ApplicationExecutionState.Terminated);
            Window.Current.Activate();
        }
    }
}