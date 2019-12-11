using EasyLocalization.Localization;
using EasyLocalization.Readers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WargameTournamentManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LocalizationManager.Instance.AddCulture(
                CultureInfo.GetCultureInfo("en-US"),
                new CharSeperatedFileReader("Resources/en-US.txt"),
                true);
            LocalizationManager.Instance.AddCulture(
                CultureInfo.GetCultureInfo("es-ES"),
                new CharSeperatedFileReader("Resources/es-ES.txt"));
        }
    }
}
