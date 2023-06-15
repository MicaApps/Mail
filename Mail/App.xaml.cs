using System;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Mail.Pages;
using Mail.Services;
using Mail.Services.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using SqlSugar;
using ICacheService = Mail.Services.ICacheService;

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
            Trace.WriteLine($"{nameof(connectionString)}Path: {connectionString}");

            services.AddSingleton<OutlookService, OutlookService>()
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton<ISqlSugarClient>(_ =>
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
                    });
                    //调式代码 用来打印SQL 
                    client.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        Trace.WriteLine(
                            $"ExecSql: {sql}\r\n{client.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value))}");
                    };
                    client.DbMaintenance.CreateDatabase();
                    client.CodeFirst.InitTables<MailFolderData>();
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