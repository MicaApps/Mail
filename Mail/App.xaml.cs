using Mail.Pages;
using Mail.Servives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail
{
    sealed partial class App : Application
    {
        public static IServiceProvider Services { get; } = new ServiceCollection().AddSingleton<OutlookService, OutlookService>()
                                                                                  .BuildServiceProvider();

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
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
                Window.Current.Content = new SplashPage(args.SplashScreen, args.PreviousExecutionState == ApplicationExecutionState.Terminated);
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
