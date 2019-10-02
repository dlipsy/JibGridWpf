using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace Jib.WPF.Testbed
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // now set the Green accent and dark theme
            ThemeManager.ChangeTheme(Application.Current, "Light.Teal"); 

            base.OnStartup(e);
        }
    }
}
