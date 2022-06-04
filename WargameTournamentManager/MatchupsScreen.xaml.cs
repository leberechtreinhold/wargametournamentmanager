using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Threading.Tasks;

namespace WargameTournamentManager
{
    /// <summary>
    /// Interaction logic for MatchupsScreen.xaml
    /// </summary>
    public partial class MatchupsScreen : UserControl
    {
        public ViewMatchup EditingMatchup { get; set; }
        public ViewChangeMatchups SwappingMatchup { get; set; }
        public ViewChangeTables SwappingTables { get; set; }

        public MatchupsScreen()
        {
            InitializeComponent();
        }

        private async void EditMatchup_Click(object sender, RoutedEventArgs e)
        {
            var matchup = ((Button)e.Source).DataContext as Matchup;
            if (matchup == null) return;

            var tour = MainWindow.gMainWindow.currentTournament;
            if (matchup.Round != tour.CurrentRound)
            {
                var confirm = await MainWindow.gMainWindow.ShowMessageAsync("Aviso",
                    "Estás editando el enfrentamiento de una ronda no activa. "
                    + "Si lo haces, los enfrentamientos generados actualmente "
                    + "no corresponderán con el ranking. ¿Estás seguro?",
                    MessageDialogStyle.AffirmativeAndNegative);
                if (confirm == MessageDialogResult.Negative)
                {
                    return;
                }

            }
            EditingMatchup = new ViewMatchup(matchup, tour);
            editMatchupWindow.DataContext = EditingMatchup;
            editMatchupWindow.IsOpen = true;
        }

        private void SaveMatchup_Click(object sender, RoutedEventArgs e)
        {
            EditingMatchup.UpdateMatchupWithView();
            editMatchupWindow.IsOpen = false;
        }

        private void GeneratePairings_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.gMainWindow.currentTournament.PlayerListLocked)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden generar enfrentamientos para una ronda sin que la lista de jugadores esté cerrada.");
                return;
            }
            var selectedRound = ((Button)sender).DataContext as Round;
            if (!selectedRound.Active)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden generar enfrentamientos para una ronda que no está activa.");
                return;
            }
            // TODO ASK!!!
            MainWindow.gMainWindow.currentTournament.GenerateMatchup();
        }

        private void ChangePairings_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.gMainWindow.currentTournament.PlayerListLocked)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden cambiar enfrentamientos para una ronda sin que la lista de jugadores esté cerrada.");
                return;
            }
            var selectedRound = ((Button)sender).DataContext as Round;
            if (!selectedRound.Active)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden cambiar enfrentamientos para una ronda que no está activa.");
                return;
            }
            if (MainWindow.gMainWindow.currentTournament.Players.Count < 4)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden cambiar enfrentamientos con menos de 4 jugadores.");
                return;
            }

            SwappingMatchup = new ViewChangeMatchups(MainWindow.gMainWindow.currentTournament, selectedRound);
            changeMatchupsWindow.DataContext = SwappingMatchup;
            changeMatchupsWindow.IsOpen = true;
        }

        private void ApplyChangePairing_Click(object sender, RoutedEventArgs e)
        {
            var pairs = ((Button)sender).DataContext as ViewChangeMatchups;
            if (pairs == null) return;
            var tour = pairs.SourceTournament;
            var matchups = tour.Rounds[tour.CurrentRound].Matchups;
            var matchup1 = matchups[pairs.FirstPair];
            var matchup2 = matchups[pairs.SecondPair];
            matchup1.SwapSecondPlayer(matchup2);
            pairs.UpdateMatchupWithView();
        }

        private void ChangeTables_Click(object sender, RoutedEventArgs e)
        {
            if (!MainWindow.gMainWindow.currentTournament.PlayerListLocked)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden cambiar mesas para una ronda sin que la lista de jugadores esté cerrada.");
                return;
            }
            var selectedRound = ((Button)sender).DataContext as Round;
            if (!selectedRound.Active)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden cambiar mesas para una ronda que no está activa.");
                return;
            }
            if (MainWindow.gMainWindow.currentTournament.Tables.Count < 2)
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "No se pueden cambiar mesas con menos de 2 mesas.");
                return;
            }

            SwappingTables = new ViewChangeTables(MainWindow.gMainWindow.currentTournament, selectedRound);
            changeTablesWindow.DataContext = SwappingTables;
            changeTablesWindow.IsOpen = true;
        }

        private void ApplyChangeTables_Click(object sender, RoutedEventArgs e)
        {
            changeMatchupsWindow.IsOpen = false;
            var tables = ((Button)sender).DataContext as ViewChangeTables;
            if (tables == null) return;
            var tour = tables.SourceTournament;
            var matchups = tour.Rounds[tour.CurrentRound].Matchups;
            var matchup1 = matchups.First(m => m.TableId == tables.FirstTable);
            var matchup2 = matchups.First(m => m.TableId == tables.SecondTable);
            matchup1.TableId = tables.SecondTable;
            matchup2.TableId = tables.FirstTable;
            tables.UpdateMatchupWithView();
            changeTablesWindow.IsOpen = false;
        }

        private async void CloseRound_Click(object sender, RoutedEventArgs e)
        {
            var selectedRound = ((Button)sender).DataContext as Round;
            var oldIndex = MainWindow.gMainWindow.currentTournament.CurrentRound;
            try
            {
                MainWindow.gMainWindow.currentTournament.FinishRound(selectedRound);
            }
            catch (InvalidOperationException ex)
            {
                await MainWindow.gMainWindow.ShowMessageAsync("Error", ex.Message);
            }

            var newIndex = MainWindow.gMainWindow.currentTournament.CurrentRound;
            if (oldIndex != newIndex)
            {
                TabRounds.SelectedIndex = newIndex;

            }
        }

        private void GeneratingGridTagsMatchup_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(int))
            {
                var col = new DataGridNumericUpDownColumn();
                col.Header = e.Column.Header;
                Binding binding = new Binding(e.PropertyName);
                col.Binding = binding;
                e.Column = col;
            }
        }
    }

    // The matchup is very different from view from the model, because the model is
    // a strict hierarchy with no links up, and the view is something with many options
    // context dependant with translated, autogenerated strings. Also WPF conversion
    // from WPF is not direct
    public class ViewMatchup
    {
        public Matchup SourceMatchup { get; set; }
        public Tournament SourceTournament { get; set; }
        public DataTable Tags { get; set; }
        public int IndexCurrentResult { get; set; }

        public List<string> Results
        {
            get
            {
                return new List<string>
                {
                    "Jugándose",
                    string.Format("Victoria de {0}", Player1Name),
                    string.Format("Victoria de {0}", Player2Name),
                    "Empate"
                };
            }
        }
        public string MatchupName
        {
            get
            {
                return string.Format("{0} vs {1}", Player1Name, Player2Name);
            }
        }

        public string Player1Name
        {
            get
            {
                return SourceTournament.Players[SourceMatchup.Player1Id].Name;
            }
        }

        public string Player2Name
        {
            get
            {
                return SourceTournament.Players[SourceMatchup.Player2Id].Name;
            }
        }

        public ViewMatchup(Matchup _sourceMatchup, Tournament _sourceTournament)
        {
            SourceMatchup = _sourceMatchup;
            SourceTournament = _sourceTournament;

            IndexCurrentResult = (int)SourceMatchup.CurrentResult;
            Tags = new DataTable();
            var column = Tags.Columns.Add("Tag", typeof(string));
            column.ReadOnly = true;
            Tags.Columns.Add(Player1Name, typeof(int));
            Tags.Columns.Add(Player2Name, typeof(int));

            foreach (var tag in SourceTournament.Config.Tags)
            {
                if (tag.Type == TagType.Calculated) continue;

                Tags.Rows.Add(tag.Name, SourceMatchup.Player1Tags[tag.Name], SourceMatchup.Player2Tags[tag.Name]);
            }
        }

        public void UpdateMatchupWithView()
        {
            SourceMatchup.CurrentResult = (Result)IndexCurrentResult;
            foreach (DataRow row in Tags.Rows)
            {
                var tagname = (string)row["Tag"];
                var player1 = (int)row[Player1Name];
                var player2 = (int)row[Player2Name];
                SourceMatchup.Player1Tags[tagname] = player1;
                SourceMatchup.Player2Tags[tagname] = player2;
            }
            foreach (var tag in SourceTournament.Config.Tags)
            {
                if (tag.Type != TagType.Calculated) continue;
                SourceMatchup.Player1Tags[tag.Name] = Matchup.CalculateTag(tag, SourceMatchup.Player1Tags, SourceMatchup.Player2Tags);
                SourceMatchup.Player2Tags[tag.Name] = Matchup.CalculateTag(tag, SourceMatchup.Player2Tags, SourceMatchup.Player1Tags);
            }
            SourceTournament.UpdateRanking();

            SourceTournament.Save();

            // Band aid because there should be no need to execute this 
            // to make the datagrid refresh
            var round = SourceTournament.Rounds[SourceMatchup.Round - 1];
            var updated_matchups = new List<Matchup>();
            foreach (var matchup in round.Matchups)
            {
                updated_matchups.Add(matchup);
            }
            round.Matchups = updated_matchups;
            round.OnPropertyChanged("Matchups");
        }
    }

    public class ViewChangeMatchups
    {
        public Tournament SourceTournament { get; set; }
        public List<string> CurrentPairings { get; set; }
        public int FirstPair { get; set; }
        public int SecondPair { get; set; }

        public ViewChangeMatchups(Tournament tour, Round round)
        {
            SourceTournament = tour;
            CurrentPairings = new List<string>();
            foreach (var matchup in round.Matchups)
            {
                CurrentPairings.Add(matchup.GetMatchupName(tour));
            }
            FirstPair = 0;
            SecondPair = 1;
        }

        public void UpdateMatchupWithView()
        {
            SourceTournament.UpdateRanking();

            SourceTournament.Save();

            // We can only do this on the current round!
            var round = SourceTournament.Rounds[SourceTournament.CurrentRound];
            var updated_matchups = new List<Matchup>();
            foreach (var matchup in round.Matchups)
            {
                updated_matchups.Add(matchup);
            }
            round.Matchups = updated_matchups;
            round.OnPropertyChanged("Matchups");
        }
    }

    public class ViewChangeTables
    {
        public Tournament SourceTournament { get; set; }
        public List<string> CurrentTables { get; set; }
        public int FirstTable { get; set; }
        public int SecondTable { get; set; }

        public ViewChangeTables(Tournament tour, Round round)
        {
            SourceTournament = tour;
            CurrentTables = new List<string>();
            foreach (var table in tour.Tables)
            {
                var matchup = round.Matchups.FirstOrDefault(m => m.TableId == table.Id);
                if (matchup != null)
                {
                    var desc = matchup.GetMatchupName(tour);
                    CurrentTables.Add($"{table.Name} ({desc})");
                }
                else
                {
                    CurrentTables.Add($"{table.Name}");
                }
            }
            FirstTable = 0;
            SecondTable = 1;
        }

        public void UpdateMatchupWithView()
        {
            SourceTournament.UpdateRanking();

            SourceTournament.Save();

            // We can only do this on the current round!
            var round = SourceTournament.Rounds[SourceTournament.CurrentRound];
            var updated_matchups = new List<Matchup>();
            foreach (var matchup in round.Matchups)
            {
                updated_matchups.Add(matchup);
            }
            round.Matchups = updated_matchups;
            round.OnPropertyChanged("Matchups");
        }
    }

    public sealed class GetMatchupResultConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return null;

            Matchup matchup = values[0] as Matchup;
            Tournament tournament = values[1] as Tournament;
            if (matchup == null || tournament == null)
                return null;

            if (matchup.CurrentResult == Result.STILL_PLAYING)
            {
                return "Jugando";
            }
            else if (matchup.CurrentResult == Result.DRAW)
            {
                return "Empate";
            }
            else
            {
                int winner = matchup.CurrentResult == Result.PLAYER1_WIN ? matchup.Player1Id : matchup.Player2Id;
                return string.Format("Victoria para {0}", tournament.Players[winner].Name);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class GetMatchupTagSummaryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return null;

            Matchup matchup = values[0] as Matchup;
            Tournament tournament = values[1] as Tournament;
            if (matchup == null || tournament == null)
                return null;

            string[] tags = new string[matchup.Player1Tags.Count];
            int i = 0;
            foreach (var kv in matchup.Player1Tags)
            {
                var tag = kv.Key;

                tags[i] = string.Format("{0} {1}/{2}", tag, matchup.Player1Tags[tag], matchup.Player2Tags[tag]);
                i++;
            }
            return string.Join(", ", tags);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class GetRoundActiveOrNotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value) return "Ronda en curso";
            else return "NOTA: Ronda no activa, no se pueden editar los resultados de los enfrentamientos ni generar enfrentamientos.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class GetTableNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int tableId = (int)value;
            var tour = MainWindow.gMainWindow.currentTournament;
            return tour.Tables.First(table => table.Id == tableId).Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class GetMatchupNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3) return null;

            var player1Id = values[0] as int?;
            var player2Id = values[1] as int?;
            Tournament tournament = values[2] as Tournament;
            if (player1Id == null || player2Id == null || tournament == null)
                return null;

            var player1 = tournament.Players[(int)player1Id];
            var player2 = tournament.Players[(int)player2Id];
            return string.Format("{0} vs {1}", player1.Name, player2.Name);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
