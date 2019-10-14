using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for PlayersScreen.xaml
    /// </summary>
    public partial class PlayersScreen : UserControl, INotifyPropertyChanged
    {
        public Player editingPlayer { get; set; }

        public PlayersScreen()
        {
            editingPlayer = new Player();
            InitializeComponent();
        }

        private void AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            editingPlayer = new Player();
            editPlayerWindow.DataContext = this.editingPlayer;
            editPlayerWindow.IsOpen = true;
        }

        private void EditPlayer_Click(object sender, RoutedEventArgs e)
        {
            editingPlayer = new Player();
            editPlayerWindow.DataContext = this.editingPlayer;
            editPlayerWindow.IsOpen = true;
        }

        private void PlayerListLock_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.gMainWindow.currentTournament.PlayerListLocked)
            {
                UnlockPlayerList();
            }
            else
            {
                LockPlayerList();
            }
        }

        private async void LockPlayerList()
        {
            var option = await MainWindow.gMainWindow.ShowMessageAsync("Aviso", 
                "Si bloqueas la lista de jugadores ya no podrás añadir ni "
                + "eliminar ningún jugador, aunque seguirás pudiendo editar " 
                + "los detalles de estos. ¿Quieres continuar?", 
                MessageDialogStyle.AffirmativeAndNegative);
            if (option == MessageDialogResult.Affirmative)
            {
                MainWindow.gMainWindow.currentTournament.PlayerListLocked = true;
                MainWindow.gMainWindow.OnPropertyChanged("currentTournament");
            }

        }

        private async void UnlockPlayerList()
        {
            var option = await MainWindow.gMainWindow.ShowMessageAsync("Aviso",
                "Si se desbloqueas la lista de jugadores podrás añadir y "
                + "eliminar jugadores, pero todos los resultados de juego, "
                + "incluyendo rondas y emparejamientos, serán eliminados. "
                + "¿Quieres continuar?",
                MessageDialogStyle.AffirmativeAndNegative);
            if (option == MessageDialogResult.Affirmative)
            {
                MainWindow.gMainWindow.currentTournament.PlayerListLocked = false;
                MainWindow.gMainWindow.OnPropertyChanged("currentTournament");
            }

        }

        private void SavePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(editingPlayer.Name))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Por favor, inserta un nombre para el jugador");
                return;
            }
            if (!MainWindow.gMainWindow.currentTournament.CanAddPlayerName(editingPlayer.Name))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Este jugador ya ha sido introducido. Por favor, cambia el nombre a este jugador.");
                return;
            }

            MainWindow.gMainWindow.currentTournament.AddPlayer(editingPlayer.Clone());
            editingPlayer = new Player();
            
            MainWindow.gMainWindow.OnPropertyChanged("currentTournament");
            playerListDataGrid.ItemsSource = null;
            playerListDataGrid.ItemsSource = MainWindow.gMainWindow.currentTournament.Players;
            // TODO Save
            editPlayerWindow.IsOpen = false;
        }

        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
