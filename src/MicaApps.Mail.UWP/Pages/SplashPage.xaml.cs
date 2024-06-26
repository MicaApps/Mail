﻿using Mail.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Mail.Pages
{
    public sealed partial class SplashPage : Page
    {
        private readonly SplashScreen Splash; // Variable to hold the splash screen object.

        public SplashPage(SplashScreen Splash, bool loadState) : this(Splash)
        {
            if (loadState)
            {
                RestoreState();
            }
        }

        private SplashPage(SplashScreen Splash)
        {
            InitializeComponent();
            ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
            TitleBar.ButtonBackgroundColor = Colors.Transparent;
            TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            Window.Current.SetTitleBar(AppTitleBar);

            this.Splash = Splash ?? throw new ArgumentNullException(nameof(Splash), "Parameter could not be null");
            this.Splash.Dismissed += Splash_Dismissed;

            Loaded += SplashPage_Loaded;
            Unloaded += SplashPage_Unloaded;
        }

        private void SplashPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        private void SplashPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetUIPosition(Splash.ImageLocation);
            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            if (Splash != null)
            {
                SetUIPosition(Splash.ImageLocation);
            }
        }

        private async void Splash_Dismissed(SplashScreen sender, object args)
        {
            bool IsSignIn = await App.Services.GetService<OutlookService>().SignInSilentAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Window.Current.Content = new Frame
                {
                    Content = new MainPage(IsSignIn)
                };
            });
        }

        private void RestoreState()
        {
            // TODO: write code to load state
        }

        private void SetUIPosition(Rect NewRect)
        {
            PositionImage(NewRect);
            PositionRing(NewRect);
        }

        private void PositionImage(Rect NewRect)
        {
            extendedSplashImage.SetValue(Canvas.LeftProperty, NewRect.X);
            extendedSplashImage.SetValue(Canvas.TopProperty, NewRect.Y);
            extendedSplashImage.Height = NewRect.Height;
            extendedSplashImage.Width = NewRect.Width;

        }

        private void PositionRing(Rect NewRect)
        {
            splashProgressRing.SetValue(Canvas.LeftProperty, NewRect.X + (NewRect.Width * 0.5) - (splashProgressRing.Width * 0.5));
            splashProgressRing.SetValue(Canvas.TopProperty, NewRect.Y + NewRect.Height + NewRect.Height * 0.1);
        }
    }
}
