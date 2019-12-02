using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WargameTournamentManager
{
    /// <summary>
    /// Interaction logic for CreateTournamentScreen.xaml
    /// </summary>
    public partial class CreateTournamentScreen : UserControl
    {
        public Tournament creationTournament { get; set; }

        public CreateTournamentScreen()
        {
            creationTournament = new Tournament();
            InitializeComponent();
            this.DataContext = creationTournament;
        }

        private void CreateNewTournament_Click(object sender, RoutedEventArgs e)
        {
            createTournamentWindow.DataContext = this.creationTournament;
            createTournamentWindow.IsOpen = true;
        }

        private void SaveTournament_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(creationTournament.Name))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Por favor, inserta un nombre para el torneo");
                return;
            }
            if (string.IsNullOrWhiteSpace(creationTournament.Game))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Por favor, inserta el nombre del juego");
                return;
            }
            if (creationTournament.Config.NumberRounds < 1 || creationTournament.Config.NumberRounds > 32)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "El número de rondas es inválido, debe estar entre 1 y 32.");
                return;
            }

            var clone = creationTournament.Clone();
            if (clone.SaveAskingForName())
            {
                MainWindow.gMainWindow.currentTournament = clone;
                creationTournament = new Tournament();
                createTournamentWindow.IsOpen = false;
                CreateNewTournamentButton.IsEnabled = false;
                LoadTournamentButton.IsEnabled = false;
                MainWindow.gMainWindow.MainTab.SelectedIndex += 1;
                MainWindow.gMainWindow.OnPropertyChanged("currentTournament");
            }
        }

        private void LoadTournament_Click(object sender, RoutedEventArgs e)
        {
            var tournament = Tournament.LoadAskingForFile();
            if (tournament != null)
            {
                MainWindow.gMainWindow.currentTournament = tournament;

                CreateNewTournamentButton.IsEnabled = false;
                LoadTournamentButton.IsEnabled = false;
                MainWindow.gMainWindow.MainTab.SelectedIndex = MainWindow.gMainWindow.MainTab.SelectedIndex + 1;
                MainWindow.gMainWindow.OnPropertyChanged("currentTournament");
            }
        }

        private void ListboxGames_ChangedSelection(object sender, SelectionChangedEventArgs e)
        {
            var game = ((ComboBoxItem)combobox_Games.SelectedItem).Tag.ToString(); ;
            if (game == "DBA")
            {
                creationTournament.Config.NumberRounds = 5;
                creationTournament.Config.PointsPerWin = 3;
                creationTournament.Config.PointsPerDraw = 0;
                creationTournament.Config.PointsPerLoss = 1;
                creationTournament.Config.TagsStr = "DiferenciaPeanas, Campamentos, Generales";
                creationTournament.Config.ScoreFormula = "Puntos * 1000 + DiferenciaPeanas * 10 + Campamentos + Generales";
                creationTournament.OnPropertyChanged("Config");
            }
            else if (game == "BoltAction")
            {
                creationTournament.Config.NumberRounds = 3;
                creationTournament.Config.PointsPerWin = 3;
                creationTournament.Config.PointsPerDraw = 1;
                creationTournament.Config.PointsPerLoss = 0;
                creationTournament.Config.TagsStr = "DiferenciaOrdenesDestruidas, ObjetivoSecundario";
                creationTournament.Config.ScoreFormula = "Puntos * 1000 + ObjetivoSecundario * 1000 + DiferenciaOrdenesDestruidas";
                creationTournament.OnPropertyChanged("Config");
            }
        }
    }
}
