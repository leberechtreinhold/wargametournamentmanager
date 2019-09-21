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

        public MainWindow()
        {
            currentTournament = Tournament.CreateTestTournament();
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
            }
        }
    }

    public sealed class GetMatchupName : IMultiValueConverter
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
