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
using System.Windows.Shapes;

namespace WargameTournamentManager
{
    /// <summary>
    /// Interaction logic for TableScreen.xaml
    /// </summary>
    public partial class TableScreen : UserControl, INotifyPropertyChanged
    {
        Table editingTable { get; set; }
        public TableScreen()
        {
            editingTable = new Table();
            InitializeComponent();
        }

        private void AddTable_Click(object sender, RoutedEventArgs e)
        {
            editingTable = new Table();
            editTableWindow.DataContext = this.editingTable;
            editTableWindow.IsOpen = true;
        }

        private void EditTable_Click(object sender, RoutedEventArgs e)
        {
            var table = ((Button)sender).DataContext as Table;
            if (table == null) return;

            editingTable = table.Clone();
            editTableWindow.DataContext = this.editingTable;
            editTableWindow.IsOpen = true;
        }

        private void SaveTable_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(editingTable.Name))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Por favor, inserta un nombre para la mesa");
                return;
            }
            if (!MainWindow.gMainWindow.currentTournament.CanAddTableName(editingTable.Name, editingTable.Id))
            {
                MainWindow.gMainWindow.ShowMessageAsync("Error", "Este nombre de jugador ya ha sido introducido. Por favor, cambia el nombre a este jugador.");
                return;
            }

            bool is_table_creation = editingTable.Id == -1;
            if (is_table_creation)
            {
                MainWindow.gMainWindow.currentTournament.AddTable(editingTable.Clone());
            }
            else
            {
                MainWindow.gMainWindow.currentTournament.UpdateTableData(editingTable);
            }

            editingTable = new Table();
            OnPropertyChanged("editingTable");

            // For some reason, despite AddPlayer being notifiable and raising the change
            // on players, the datagrid doesnt refresh automatically...
            TableListDataGrid.Items.Refresh();

            if (is_table_creation)
            {
                TableListDataGridScroll.ScrollToBottom();
            }
            editTableWindow.IsOpen = false;
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (TableListDataGrid.SelectedItems == null || TableListDataGrid.SelectedItems.Count == 0)
                return;

            foreach (Player player in TableListDataGrid.SelectedItems)
            {
                MainWindow.gMainWindow.currentTournament.DeletePlayer(player.Id);
            }
            TableListDataGrid.Items.Refresh();
        }

        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
