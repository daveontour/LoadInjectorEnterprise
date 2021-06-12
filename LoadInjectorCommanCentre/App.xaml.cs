using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LoadInjectorCommandCentre {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private void Application_Startup(object sender, StartupEventArgs e) {
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif

            MainWindow wnd = e.Args.Length >= 1 ? new MainWindow(e.Args) : new MainWindow();
            wnd.Show();
        }
    }
}