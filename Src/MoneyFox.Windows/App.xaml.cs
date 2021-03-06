﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Metadata;
using Windows.Globalization;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Cheesebaron.MvxPlugins.Settings.WindowsUWP;
using Microsoft.Toolkit.Uwp.Helpers;
#if !DEBUG
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
#endif
using MoneyFox.Business.Manager;
using MoneyFox.DataAccess;
using MoneyFox.Foundation;
using MoneyFox.Foundation.Constants;
using MoneyFox.Foundation.Resources;
using MoneyFox.Windows.Tasks;
using MoneyFox.Windows.Views;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using UniversalRateReminder;

namespace MoneyFox.Windows
{
    /// <summary>
    ///     Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App
    {
        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            SetTheme();
            Suspending += OnSuspending;

            ApplicationContext.DbPath = DatabaseConstants.DB_NAME;
            ApplicationContextOld.DbPath = DatabaseConstants.DB_NAME_OLD;
        }

        private void SetTheme()
        {
            switch (new WindowsUwpSettings().GetValue(SettingsManager.THEME_KEYNAME, AppTheme.System))
            {
                case AppTheme.Dark:
                RequestedTheme = ApplicationTheme.Dark;
                break;

                case AppTheme.Light:
                RequestedTheme = ApplicationTheme.Light;
                break;

                case AppTheme.System:
                var uiSettings = new UISettings();
                var color = uiSettings.GetColorValue(UIColorType.Background);

                RequestedTheme = color == Color.FromArgb(255, 255, 255, 255)
                    ? ApplicationTheme.Light
                    : ApplicationTheme.Dark;
                break;
            }
        }

        /// <summary>
        ///     Invoked when the application is launched normally by the end user.  Other entry points
        ///     will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if !DEBUG
            MobileCenter.Start("1fba816a-eea6-42a8-bf46-0c0fcc1589db", typeof(Analytics), typeof(Crashes));
#endif
            if (e.PreviousExecutionState != ApplicationExecutionState.Running)
            {
                Window.Current.Content = new ShellPage { Language = ApplicationLanguages.Languages[0] };
                ApplicationLanguages.PrimaryLanguageOverride = GlobalizationPreferences.Languages[0];

                var shell = (ShellPage)Window.Current.Content;

                // When the navigation stack isn't restored, navigate to the first page
                // suppressing the initial entrance animation.
                var setup = new Setup(shell.Frame);
                setup.Initialize();

                var start = Mvx.Resolve<IMvxAppStart>();
                start.Start();

                BackgroundTaskHelper.Register(typeof(ClearPaymentsTask), new TimeTrigger(15, false));
                BackgroundTaskHelper.Register(typeof(RecurringPaymentTask), new TimeTrigger(15, false));

                shell.ViewModel = Mvx.Resolve<ShellViewModel>();

                //If Jump Lists are supported, add them
                if (ApiInformation.IsTypePresent("Windows.UI.StartScreen.JumpList"))
                {
                    await SetJumplist();
                }

                await CallRateReminder();
            }

            OverrideTitleBarColor();
            
            // Ensure the current window is active
            Window.Current.Activate();
        }

        private async Task SetJumplist()
        {
            var jumpList = await JumpList.LoadCurrentAsync();
            jumpList.Items.Clear();
            jumpList.SystemGroupKind = JumpListSystemGroupKind.None;

            var listItemAddIncome = JumpListItem.CreateWithArguments(Constants.ADD_INCOME_TILE_ID,
                                                                     Strings.AddIncomeLabel);
            listItemAddIncome.Logo = new Uri("ms-appx:///Assets/IncomeTileIcon.png");
            jumpList.Items.Add(listItemAddIncome);

            var listItemAddSpending = JumpListItem.CreateWithArguments(Constants.ADD_EXPENSE_TILE_ID,
                                                                       Strings.AddExpenseLabel);
            listItemAddSpending.Logo = new Uri("ms-appx:///Assets/SpendingTileIcon.png");
            jumpList.Items.Add(listItemAddSpending);

            var listItemAddTransfer = JumpListItem.CreateWithArguments(Constants.ADD_TRANSFER_TILE_ID,
                                                                       Strings.AddTransferLabel);
            listItemAddTransfer.Logo = new Uri("ms-appx:///Assets/TransferTileIcon.png");
            jumpList.Items.Add(listItemAddTransfer);

            await jumpList.SaveAsync();
        }

        private async Task CallRateReminder()
        {
            RatePopup.RateButtonText = Strings.YesLabel;
            RatePopup.CancelButtonText = Strings.NotNowLabel;
            RatePopup.Title = Strings.RateReminderTitle;
            RatePopup.Content = Strings.RateReminderText;

            await RatePopup.CheckRateReminderAsync();
        }

        private void OverrideTitleBarColor()
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            // set up our brushes
            var bkgColor = Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush;
            var backgroundColor = Current.Resources["AppBarBrush"] as SolidColorBrush;
            var appForegroundColor = Current.Resources["AppForegroundPrimaryBrush"] as SolidColorBrush;

            // override colors!
            if (bkgColor != null && appForegroundColor != null)
            {
                // If on a mobile device set the status bar
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    StatusBar.GetForCurrentView().BackgroundColor = backgroundColor?.Color;
                    StatusBar.GetForCurrentView().BackgroundOpacity = 0.6;
                    StatusBar.GetForCurrentView().ForegroundColor = appForegroundColor.Color;
                }

                titleBar.BackgroundColor = backgroundColor?.Color;
                titleBar.ButtonBackgroundColor = backgroundColor?.Color;

                titleBar.ForegroundColor = Colors.White;
                titleBar.ButtonForegroundColor = Colors.White;
            }
        }

        /// <summary>
        /// Event fired when a Background Task is activated (in Single Process Model)
        /// </summary>
        /// <param name="args">Arguments that describe the BackgroundTask activated</param>
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            var deferral = args.TaskInstance.GetDeferral();

            new SyncBackupTask().Run(args.TaskInstance);

            deferral.Complete();
        }

        /// <summary>
        ///     Invoked when application execution is being suspended.  Application state is saved
        ///     without knowing whether the application will be terminated or resumed with the contents
        ///     of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            new SettingsManager(new WindowsUwpSettings()).SessionTimestamp = DateTime.Now.AddMinutes(-15).ToString(CultureInfo.CurrentCulture);

            deferral.Complete();
        }
    }
}