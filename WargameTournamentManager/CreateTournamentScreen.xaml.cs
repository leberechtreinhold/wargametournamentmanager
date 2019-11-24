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
    }
}
