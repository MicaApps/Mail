using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FreeSql;
using FreeSql.Sqlite;
using Mail.Extensions;
using Mail.Pages;
using Mail.Services;
using Mail.Services.Data;
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
                .AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()))
                .AddSingleton<IFreeSql>(_ =>
                {
                    var db = new FreeSqlBuilder().UseConnectionFactory(DataType.Sqlite,
                            () => new SqliteConnection(connectionString), typeof(SqliteProvider<>))
#if DEBUG
                        .UseMonitorCommand(cmd => Trace.WriteLine($"执行的sql: {cmd.CommandText}\n"))
#endif
                        .UseAutoSyncStructure(true)
                        .UseNoneCommandParameter(true)
                        .Build();

                    db.CodeFirst.SyncStructure<MailFolderData>();
                    db.CodeFirst.SyncStructure<MailMessageData>();
                    db.CodeFirst.SyncStructure<MailMessageContentData>();
                    db.CodeFirst.SyncStructure<MailMessageRecipientData>();

                    db.Aop.CurdAfter += (s, e) =>
                    {
                        var operationEvent = db.GetDbOperationEvent();
                        operationEvent.OnExecEvent(s, e.CurdType);
                    };

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
            SuspendingDeferral Deferral = e.SuspendingOperation.GetDeferral();

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