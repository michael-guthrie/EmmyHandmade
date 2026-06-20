namespace AssetManager
{
    using System;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AssetManager.Properties.Settings.Default.SettingsLoaded += Default_SettingsLoaded;

            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<Exception>(this, ex =>
                MessageBox.Show("Program encountered some stupid error.  Details are:" +
                                Environment.NewLine +
                                Environment.NewLine +
                                ex.ToString()));
        }

        private void Default_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
        {
            if (AssetManager.Properties.Settings.Default.WindowSizes == null)
            {
                AssetManager.Properties.Settings.Default.WindowSizes = new System.Collections.Specialized.NameValueCollection();
                AssetManager.Properties.Settings.Default.Save();
            }
        }

        public static Data.TrackingStore CreateAppDataContext()
        {
            throw new NotImplementedException();
        }
    }
}
