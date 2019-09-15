using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace WargameTournamentManager
{
    /// <summary>
    /// Interaction logic for MatchupsScreen.xaml
    /// </summary>
    public partial class MatchupsScreen : UserControl
    {
        public MatchupsScreen()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public sealed class GetMatchupResult : IMultiValueConverter
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

    public sealed class GetRoundActiveOrNot : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var round = value as Round;
            if (round == null) return null;

            if (round.Active) return "Ronda en curso";
            else return "NOTA: Ronda no activa, no se pueden editar los resultados de los enfrentamientos.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
