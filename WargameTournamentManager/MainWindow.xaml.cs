using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public Tournament currentTournament { get; set; }
        public Tournament creationTournament { get; set; }

        public MainWindow()
        {
            creationTournament = new Tournament();
            currentTournament = null;
            InitializeComponent();
            this.DataContext = this;
        }

        void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void CreateNewTournament_Click(object sender, RoutedEventArgs e)
        {
            createTournamentWindow.DataContext = this.creationTournament;
            createTournamentWindow.IsOpen = true;
        }

        private void SaveTournament_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(creationTournament.Name))
            {
                this.ShowMessageAsync("Error", "Por favor, inserta un nombre para el torneo");
                return;
            }
            if (string.IsNullOrWhiteSpace(creationTournament.Game))
            {
                this.ShowMessageAsync("Error", "Por favor, inserta el nombre del juego");
                return;
            }
            if (creationTournament.Config.NumberRounds < 1 || creationTournament.Config.NumberRounds > 10)
            {
                this.ShowMessageAsync("Error", "El número de rondas es inválido, debe estar entre 1 y 10.");
                return;
            }
            SaveFileDialog savefile = new SaveFileDialog();
            // Tournament file
            savefile.FileName = "tournament" + creationTournament.Name + ".tour";
            savefile.Filter = "Tournament files (*.tour)|*.tour|All files (*.*)|*.*";

            bool? saved = savefile.ShowDialog();
            if (saved == true)
            {
                string json = JsonConvert.SerializeObject(creationTournament);
                using (StreamWriter sw = new StreamWriter(savefile.FileName))
                {
                    sw.Write(json);
                }
                createTournamentWindow.IsOpen = false;

                currentTournament = creationTournament.Clone();
                creationTournament = new Tournament();
                CreateNewTournamentButton.IsEnabled = false;
                LoadTournamentButton.IsEnabled = false;
                MainTab.SelectedIndex = MainTab.SelectedIndex + 1;
                OnPropertyChanged("currentTournament");
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

            CreateNewTournamentButton.IsEnabled = false;
            LoadTournamentButton.IsEnabled = false;
            MainTab.SelectedIndex = MainTab.SelectedIndex + 1;
            OnPropertyChanged("currentTournament");
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
