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
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Mail
{
    sealed partial class App : Application
    {
        /// <summary>
        /// 注入依赖
        /// </summary>
        public static IServiceProvider Services { get; private set; }= new ServiceCollection()
            .AddSingleton<OutlookService, OutlookService>()
            .AddSingleton<ICacheService, CacheService>()
            .AddSingleton<LocalCacheService>()
            .AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()))
            .AddTransient<IDbContext>(_ => new CustomDbContext(new DbConnectionFactory(() => new SqliteConnection($"DataSource={ApplicationData.Current.LocalFolder.Path}\\mail.db"))))
            .BuildServiceProvider();

        /// <summary>
        /// 构选函数
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// UWApp类的重写方法OnWindowCreated中，用于配置应用程序窗口的标题栏行为
        /// </summary>
        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            //将ExtendViewIntoTitleBar设置为true，让应用程序的内容显示到标题栏的区域
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
        }

        /// <summary>
        /// 它在应用程序启动时被调用
        /// </summary>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await HandleLaunchedOrActivatedAsync(e);
        }

        /// <summary>
        /// 在应用程序窗口被激活时被调用。这可以发生在应用程序启动后，当应用程序从后台切换到前台时，或者在应用程序的某个窗口重新获得焦点时。
        /// </summary>
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await HandleLaunchedOrActivatedAsync(args);
        }

        private async Task HandleLaunchedOrActivatedAsync(IActivatedEventArgs args)
        {
            //通过 SystemInformation 类的 TrackAppUse 方法来跟踪应用程序的使用情况。
            //这可能是应用程序内部用于收集应用程序的使用统计信息或日志，以便开发人员可以了解用户的使用情况。
            SystemInformation.Instance.TrackAppUse(args);

            await InitalizeDatabaseAsync();

            //创建了一个新的 SplashPage 对象，并将其设置为当前窗口的内容。
            //SplashPage 通常是一个启动画面，用于在应用程序启动时展示一些初始信息或加载状态。
            Window.Current.Content = new SplashPage(args.SplashScreen, args.PreviousExecutionState == ApplicationExecutionState.Terminated);

            //最后，通过调用 Activate 方法激活当前窗口，使其显示在屏幕上。
            Window.Current.Activate();
        }

        private async Task InitalizeDatabaseAsync()
        {
            //Package.Current.InstalledLocation 提供了有关应用程序包安装位置的信息，而 .Path 则返回该位置的路径。
            var path = Package.Current.InstalledLocation.Path;

            //首先使用 Path.Combine 构建了一个路径，将 "Assets" 目录下的 "sql.sql" 文件加入路径中
            //然后，通过 PathIO.ReadTextAsync 异步地读取 SQL 脚本的内容
            //最后，使用数据库上下文的 ExecuteReaderAsync 方法执行 SQL 脚本
            await Services.GetService<IDbContext>().Session.ExecuteReaderAsync(await PathIO.ReadTextAsync(Path.Combine(path, @"Assets", @"sql.sql")));
        }




    }
}