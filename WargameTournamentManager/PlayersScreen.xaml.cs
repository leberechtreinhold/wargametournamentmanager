using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            var player = ((Button)sender).DataContext as Player;
            if (player == null) return;

            // NEVER EVER edit the same refernece as the one in the viewmodel,
            // because the user may not want to save changes!
            editingPlayer = player.Clone();
            editPlayerWindow.DataContext = this.editingPlayer;
            editPlayerWindow.IsOpen = true;
        }

        private void SavePlayer_Click(object sender, RoutedEventArgs e)
        {
            // This can be called both when creating a player and when editing
            // a existing one!

            if (string.IsNullOrWhiteSpace(editingPlayer.Name))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Por favor, inserta un nombre para el jugador");
                return;
            }
            if (!MainWindow.gMainWindow.currentTournament.CanAddPlayerName(editingPlayer.Name, editingPlayer.Id))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Este nombre de jugador ya ha sido introducido. Por favor, cambia el nombre a este jugador.");
                return;
            }

            bool is_player_creation = editingPlayer.Id == -1;
            if (is_player_creation)
            {
                MainWindow.gMainWindow.currentTournament.AddPlayer(editingPlayer.Clone());
            }
            else
            {
                MainWindow.gMainWindow.currentTournament.UpdatePlayerData(editingPlayer);
            }
            
            editingPlayer = new Player();
            OnPropertyChanged("editingPlayer");

            // For some reason, despite AddPlayer being notifiable and raising the change
            // on players, the datagrid doesnt refresh automatically...
            PlayerListDataGrid.Items.Refresh();

            if (is_player_creation)
            {
                PlayerListDataGridScroll.ScrollToBottom();
            }
            editPlayerWindow.IsOpen = false;
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerListDataGrid.SelectedItems == null || PlayerListDataGrid.SelectedItems.Count == 0)
                return;

            foreach(Player player in PlayerListDataGrid.SelectedItems)
            {
                MainWindow.gMainWindow.currentTournament.DeletePlayer(player.Id);
            }
            PlayerListDataGrid.Items.Refresh();
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
                try
                {
                    MainWindow.gMainWindow.currentTournament.LockPlayerList();
                    MainWindow.gMainWindow.OnPropertyChanged("currentTournament");
                }
                catch (InvalidOperationException ex)
                {
                    await MainWindow.gMainWindow.ShowMessageAsync("Error", ex.Message);
                }
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
                MainWindow.gMainWindow.currentTournament.UnlockPlayerList();
            }

        }

        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class PlayerListLockedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return MainWindow.gMainWindow.currentTournament.PlayerListLocked;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    public class PlayerListUnlockedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !MainWindow.gMainWindow.currentTournament.PlayerListLocked;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class PlayerListLockToggleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (MainWindow.gMainWindow.currentTournament.PlayerListLocked)
            {
                return "Abrir la lista de jugadores";
            }
            else
            {
                return "Cerrar lista de jugadores";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class PlayerListCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format("Hay {0} jugadores registrados", MainWindow.gMainWindow.currentTournament.Players.Count);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
