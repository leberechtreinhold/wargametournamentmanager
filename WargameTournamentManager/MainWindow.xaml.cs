using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace WargameTournamentManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public Tournament currentTournament { get; set; }
        public Tournament creationTournament { get; set; }

        public MainWindow()
        {
            creationTournament = new Tournament();
            currentTournament = null;
            InitializeComponent();
            this.DataContext = this;
        }

        private void CreateNewTournament_Click(object sender, RoutedEventArgs e)
        {
            createTournamentWindow.IsOpen = true; 
        }

        private void SaveTournament_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog savefile = new SaveFileDialog();
            // Tournament file
            savefile.FileName = "tournament" + currentTournament.Name + ".tour";
            savefile.Filter = "Tournament files (*.tour)|*.tour|All files (*.*)|*.*";

            bool? saved = savefile.ShowDialog();
            if (saved == true)
            {
                string json = JsonConvert.SerializeObject(currentTournament);
                using (StreamWriter sw = new StreamWriter(savefile.FileName))
                {
                    sw.Write(json);
                }
                createTournamentWindow.IsOpen = false;
            }
        }

        private void LoadTournament_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Tournament files (*.tour)|*.tour|All files (*.*)|*.*";

            bool? loaded = openFileDialog.ShowDialog();

            if (loaded == true)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                currentTournament = JsonConvert.DeserializeObject<Tournament>(json);
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

}
