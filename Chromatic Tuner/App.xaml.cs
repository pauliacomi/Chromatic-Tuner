using System;
using Windows.UI.Xaml;
using System.Threading.Tasks;
using Chromatic_Tuner.Services.SettingsServices;
using Windows.ApplicationModel.Activation;

namespace Chromatic_Tuner
{

    sealed partial class App : Template10.Common.BootStrapper
    {

        ISettingsService _settings;
        
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync();
            InitializeComponent(); 
            

            SplashFactory = (e) => new Views.Splash(e);

            _settings = SettingsService.Instance;
            RequestedTheme = _settings.AppTheme;
            CacheMaxDuration = _settings.CacheMaxDuration;
            ShowShellBackButton = _settings.UseShellBackButton;

            //Suspending += OnSuspending;
        }

        // runs even if restored from state
        public override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            // setup hamburger shell
            var nav = NavigationServiceFactory(BackButton.Attach, ExistingContent.Include);
            Window.Current.Content = new Views.Shell(nav);
            return Task.FromResult<object>(null);
        }

        // runs only when not restored from state
        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            await Task.Delay(50);
            NavigationService.Navigate(typeof(Views.MainPage));
        }

        ///// <summary>
        ///// Invoked when the application is launched normally by the end user.  Other entry points
        ///// will be used such as when the application is launched to open a specific file.
        ///// </summary>
        ///// <param name="e">Details about the launch request and process.</param>
        //protected override void OnLaunched(LaunchActivatedEventArgs e)
        //{

        //    Frame rootFrame = Window.Current.Content as Frame;

        //    // Do not repeat app initialization when the Window already has content,
        //    // just ensure that the window is active
        //    if (rootFrame == null)
        //    {
        //        // Create a Frame to act as the navigation context and navigate to the first page
        //        rootFrame = new Frame();
        //        // Set the default language
        //        rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

        //        rootFrame.NavigationFailed += OnNavigationFailed;

        //        if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
        //        {
        //            //TODO: Load state from previously suspended application
        //        }

        //        // Place the frame in the current Window
        //        Window.Current.Content = rootFrame;
        //    }

        //    if (rootFrame.Content == null)
        //    {
        //        // When the navigation stack isn't restored navigate to the first page,
        //        // configuring the new page by passing required information as a navigation
        //        // parameter
        //        rootFrame.Navigate(typeof(MainPage), e.Arguments);
        //    }
        //    // Ensure the current window is active
        //    Window.Current.Activate();
        //}

        ///// <summary>
        ///// Invoked when Navigation to a certain page fails
        ///// </summary>
        ///// <param name="sender">The Frame which failed navigation</param>
        ///// <param name="e">Details about the navigation failure</param>
        //void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        //{
        //    throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        //}

        ///// <summary>
        ///// Invoked when application execution is being suspended.  Application state is saved
        ///// without knowing whether the application will be terminated or resumed with the contents
        ///// of memory still intact.
        ///// </summary>
        ///// <param name="sender">The source of the suspend request.</param>
        ///// <param name="e">Details about the suspend request.</param>
        //private void OnSuspending(object sender, SuspendingEventArgs e)
        //{
        //    var deferral = e.SuspendingOperation.GetDeferral();
        //    //TODO: Save application state and stop any background activity
        //    deferral.Complete();
        //}
    }
}
