using EasyLocalization.Localization;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

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
            this.Loaded += OnLoad;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            MainWindow.gMainWindow.AddToRefreshOnLanguageChange(FindResource("enumTagTypes") as ObjectDataProvider);
        }

        private void CreateNewTournament_Click(object sender, RoutedEventArgs e)
        {
            // Always create a new one to refresh options, and also to 
            // reflect the current localization
            this.creationTournament = new Tournament();
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
            if (creationTournament.Config.NumberRounds < 1 || creationTournament.Config.NumberRounds > 32)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "El número de rondas es inválido, debe estar entre 1 y 32.");
                return;
            }
            if (!Matchmaker.TestPlayerScoreFormulaValid(creationTournament))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Las fórmulas de puntuación no son correctas. Revisa que las fórmulas sean válidas y usen los nombres que correspondan.");
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
                creationTournament.Config.NumberRounds = DBA.GetDefaultRounds();
                creationTournament.Config.PointsPerWin = DBA.GetDefaultPointsPerWin();
                creationTournament.Config.PointsPerDraw = DBA.GetDefaultPointsPerDraw();
                creationTournament.Config.PointsPerLoss = DBA.GetDefaultPointsPerLoss();
                creationTournament.Config.Tags = DBA.GetDefaultTags();
                creationTournament.Config.ScoreFormula = DBA.GetDefaultScoreFormula();
                creationTournament.OnPropertyChanged("Config");
            }
            else if (game == "BoltAction")
            {
                creationTournament.Config.NumberRounds = BoltAction.GetDefaultRounds();
                creationTournament.Config.PointsPerWin = BoltAction.GetDefaultPointsPerWin();
                creationTournament.Config.PointsPerDraw = BoltAction.GetDefaultPointsPerDraw();
                creationTournament.Config.PointsPerLoss = BoltAction.GetDefaultPointsPerLoss();
                creationTournament.Config.Tags = BoltAction.GetDefaultTags();
                creationTournament.Config.ScoreFormula = BoltAction.GetDefaultScoreFormula();
                creationTournament.OnPropertyChanged("Config");
            }
        }
    }

    public class TagTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is TagType))
            {
                return "";
            }

            if ((TagType)value == TagType.Calculated)
            {
                return LocalizationManager.Instance.GetValue("tagtype_calculated");
            }
            return LocalizationManager.Instance.GetValue("tagtype_number");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is string))
            {
                return TagType.Number;
            }

            if ((string)value == LocalizationManager.Instance.GetValue("tagtype_calculated"))
            {
                return TagType.Calculated;
            }
            return TagType.Number;
        }
    }

}
