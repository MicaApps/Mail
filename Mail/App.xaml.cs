﻿using System;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Mail.Extensions;
using Mail.Pages;
using Mail.Services;
using Mail.Services.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using SqlSugar;
using ICacheService = Mail.Services.ICacheService;
using System.Threading.Tasks;

namespace Mail
{
    sealed partial class App : Application
    {
        public static IServiceProvider Services = null!;

        public App()
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            var services = new ServiceCollection();
            RegisterServices(services);

            Services = services.BuildServiceProvider();
            Suspending += OnSuspending;
        }

        private async void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            await LogExceptionAsync(e.ExceptionObject as Exception);
        }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Handle exception whne app is release buid.
#if !DEBUG
           e.Handled = true;
#endif
           await LogExceptionAsync(e.Exception);
        }

        private async Task LogExceptionAsync(Exception e)
        {
            Trace.WriteLine("log exception");
            Trace.WriteLine(e.Message);
            Trace.WriteLine(e.StackTrace);
            //Save log file when app is release build.
#if !DEBUG
            try
            {
                var cacheFolder = ApplicationData.Current.LocalCacheFolder;
                var logs = await cacheFolder.CreateFolderAsync("logs", CreationCollisionOption.OpenIfExists);
                var timestamp = DateTime.Now.ToString("yyyyMMddhhmmfff");
                var logFile = await logs.CreateFileAsync($"unhandled_error_{timestamp}.log");
                await FileIO.WriteTextAsync(logFile, $"{e.Message}\r\n{e.StackTrace}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("error on save log");
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
            }
#endif
        }

        private void RegisterServices(IServiceCollection services)
        {
            var connectionString = $"DataSource={ApplicationData.Current.LocalFolder.Path}\\mail.db";
            Trace.WriteLine($"{nameof(connectionString)}Path: {connectionString}");
            
            services.AddSingleton<OutlookService, OutlookService>()
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()))
                .AddSingleton<ISqlSugarClient>(factory =>
                {
                    var client = new SqlSugarScope(new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        DbType = DbType.Sqlite,
                        IsAutoCloseConnection = true,
                        ConfigureExternalServices = new ConfigureExternalServices
                        {
                            EntityService = (c, p) =>
                            {
                                /***低版本C#写法***/
                                // int?  decimal? 这种 isNullable=true 不支持string
                                if (p.IsPrimarykey == false && c.PropertyType.IsGenericType &&
                                    c.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    p.IsNullable = true;
                                }
                            }
                        },
                    }, db =>
                    {
                        var cache = factory.GetRequiredService<IMemoryCache>();
                        db.Aop.DataExecuting = (_, entityInfo) =>
                        {
                            var value = entityInfo.EntityValue;
                            var key = entityInfo.OperationType + value.GetHashCode();
                            if (cache.Get(key) is not null) return;

                            cache.Set(key, 1, TimeSpan.FromSeconds(2));
                            db.GetDbOperationEvent().OnExecEvent(value, entityInfo.OperationType);
                        };
                        //db.Aop.DataExecuted = (entity, entityInfo) => { Trace.WriteLine($"DataExecuted: {entity}"); };

#if DEBUG
                        //调式代码 用来打印SQL 
                        db.Aop.OnLogExecuting = (sql, pars) =>
                        {
                            Trace.WriteLine(
                                $"ExecSql: {sql}\r\n{db.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value))}\n");
                        };
#endif
                    });

                    client.DbMaintenance.CreateDatabase();
                    client.CodeFirst.InitTables<MailFolderData>();
                    client.CodeFirst.InitTables<MailMessageData>();
                    client.CodeFirst.InitTables<MailMessageContentData>();
                    client.CodeFirst.InitTables<MailMessageRecipientData>();
                    return client;
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