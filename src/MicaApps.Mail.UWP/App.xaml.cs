using Chloe;
using Chloe.Infrastructure;
using Mail.Extensions;
using Mail.Pages;
using Mail.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.IO;
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
            Services = RegisterServices();
        }

        private async Task InitalizeDatabaseAsync()
        {
            var path = Package.Current.InstalledLocation.Path;
            await Services.GetService<IDbContext>().Session.ExecuteReaderAsync(await PathIO.ReadTextAsync(Path.Combine(path, "Assets", "sql.sql")));
        }


        /// <summary>
        /// 获取要注入的依赖
        /// </summary>
        /// <returns>返回要注入的依赖</returns>
        private IServiceProvider RegisterServices()
        {
            return new ServiceCollection().AddSingleton<OutlookService, OutlookService>()
                                          .AddSingleton<ICacheService, CacheService>()
                                          .AddSingleton<LocalCacheService>()
                                          .AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()))
                                          .AddTransient<IDbContext>(_ => new CustomDbContext(new DbConnectionFactory(() => new SqliteConnection($"DataSource={ApplicationData.Current.LocalFolder.Path}\\mail.db"))))
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

            await InitalizeDatabaseAsync();

            Window.Current.Content = new SplashPage(args.SplashScreen, args.PreviousExecutionState == ApplicationExecutionState.Terminated);
            Window.Current.Activate();
        }
    }
}