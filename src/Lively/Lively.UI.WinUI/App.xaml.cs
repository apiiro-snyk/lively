﻿using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Grpc.Client;
using Lively.UI.WinUI.Factories;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using static Lively.Common.Constants;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance for the current application instance.
        /// </summary>
        public static IServiceProvider Services
        {
            get
            {
                IServiceProvider serviceProvider = ((App)Current)._serviceProvider;
                return serviceProvider ?? throw new InvalidOperationException("The service provider is not initialized");
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            if (!SingleInstanceUtil.IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                _ = NativeMethods.MessageBox(IntPtr.Zero, "Wallpaper core is not running, exiting..", "Lively UI", 16);
                //Sad dev noises.. this.Exit() does not work without Window: https://github.com/microsoft/microsoft-ui-xaml/issues/5931
                Process.GetCurrentProcess().Kill();
            }

            this.InitializeComponent();
            _serviceProvider = ConfigureServices();
            SetAppTheme(Services.GetRequiredService<IUserSettingsClient>().Settings.ApplicationTheme);
            //Services.GetRequiredService<SettingsViewModel>().AppThemeChanged += (s, e) => SetAppTheme(e);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var m_window = Services.GetRequiredService<MainWindow>();
            m_window.Activate();
            m_window.SetWindowSizeEx(875, 875);
        }

        private IServiceProvider ConfigureServices()
        {
            //TODO: make nlogger write only to console.
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<IDesktopCoreClient, WinDesktopCoreClient>()
                .AddSingleton<IUserSettingsClient, UserSettingsClient>()
                .AddSingleton<IDisplayManagerClient, DisplayManagerClient>()
                .AddSingleton<ICommandsClient, CommandsClient>()
                .AddSingleton<IAppUpdaterClient, AppUpdaterClient>()
                .AddSingleton<MainWindow>()
                .AddSingleton<LibraryViewModel>() //Library items are stored..
                .AddSingleton<SettingsViewModel>() //Some events..
                //transient
                //.AddTransient<HelpViewModel>()
                .AddTransient<AboutViewModel>()
                .AddTransient<AddWallpaperViewModel>()
                .AddTransient<LibraryUtil>()
                .AddTransient<ScreenLayoutViewModel>()
                .AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .BuildServiceProvider();

            return provider;
        }

        //Cannot change runtime.
        //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/4474
        public void SetAppTheme(Common.AppTheme theme)
        {
            switch (theme)
            {
                case Common.AppTheme.Auto:
                    //Nothing
                    break;
                case Common.AppTheme.Light:
                    this.RequestedTheme = ApplicationTheme.Light;
                    break;
                case Common.AppTheme.Dark:
                    this.RequestedTheme = ApplicationTheme.Dark;
                    break;
            }
        }

        //Cannot set custom language on unpackaged, issues:
        //https://github.com/microsoft/microsoft-ui-xaml/issues/5940
        //https://github.com/microsoft/WindowsAppSDK/issues/1687
        //https://github.com/microsoft/WindowsAppSDK-Samples/issues/138
        void SetAppLanguage(string cult = "en-US")
        {
            ApplicationLanguages.PrimaryLanguageOverride = cult;
            CultureInfo culture = new CultureInfo(cult);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            //ResourceContext.GetForCurrentView().Reset();
            ResourceContext.GetForViewIndependentUse().Reset();
        }

        public static void ShutDown()
        {
            try
            {
                ((ServiceProvider)App.Services)?.Dispose();
            }
            catch (InvalidOperationException) { /* not initialised */ }

            //Stackoverflow exception :L
            //Note: Exit() does not work without Window: https://github.com/microsoft/microsoft-ui-xaml/issues/5931
            //((App)Current).Exit();
        }
    }
}
