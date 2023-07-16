using System;
using System.Data;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Chloe;
using Chloe.Infrastructure;
using Mail.Extensions;
using Mail.Pages;
using Mail.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Mail
{
    sealed partial class App : Application
    {
        public static IServiceProvider Services = null!;

        public App()
        {
            InitializeComponent();
            var services = new ServiceCollection();
            RegisterServices(services);
            Services = services.BuildServiceProvider();

            Suspending += OnSuspending;
        }

        private void RegisterServices(IServiceCollection services)
        {
            var connectionString = $"DataSource={ApplicationData.Current.LocalFolder.Path}\\mail.db";
#if DEBUG
            Trace.WriteLine($"{nameof(connectionString)}Path: {connectionString}");
#endif
            services.AddSingleton<OutlookService, OutlookService>()
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton<LocalCacheService>()
                .AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()))
                .AddTransient<IDbContext>(_ =>
                {
                    var db = new CustomDbContext(new SQLiteConnectionFactory(connectionString));
                    return db;
                })
                .BuildServiceProvider();
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            OnLaunchedOrActivated(e);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            OnLaunchedOrActivated(args);
        }

        private void OnLaunchedOrActivated(IActivatedEventArgs args)
        {
            SystemInformation.Instance.TrackAppUse(args);

            if (Window.Current.Content is Frame RootFrame)
            {
                //Do something on activate
            }
            else
            {
                Window.Current.Content = new SplashPage(args.SplashScreen,
                    args.PreviousExecutionState == ApplicationExecutionState.Terminated);
            }

            Window.Current.Activate();
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var Deferral = e.SuspendingOperation.GetDeferral();

            try
            {
                //TODO: 保存应用程序状态并停止任何后台活动
            }
            catch (Exception)
            {
            }
            finally
            {
                Deferral.Complete();
            }
        }
    }
}

public class SQLiteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connString;

    public SQLiteConnectionFactory(string connString)
    {
        _connString = connString;
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connString);
        return conn;
    }
}