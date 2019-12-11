using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EasyLocalization.Localization;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace WargameTournamentManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        internal static MainWindow gMainWindow;

        public Tournament currentTournament { get; set; }

        public MainWindow()
        {
            currentTournament = null;
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += OnLoad;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            gMainWindow = this;
        }

        private void ExportRanking_Click(object sender, RoutedEventArgs e)
        {
            currentTournament.ExportRankingAskingUser();
        }

        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnChangeLanguageClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            if (btn.Name == "btn_lang_sp")
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("es-ES");
                LocalizationManager.Instance.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
            }
            else if (btn.Name == "btn_lang_en")
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                LocalizationManager.Instance.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
            }
        }
    }

    public class TournamentActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tournament = value as Tournament;
            return tournament != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TournamentInactiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tournament = value as Tournament;
            return tournament == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
