using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;

namespace WargameTournamentManager
{
    public enum Result
    {
        STILL_PLAYING,
        PLAYER1_WIN,
        PLAYER1_LOSS,
        DRAW
    }

    public class Configuration
    {
        public int NumberRounds { get; set; }
        public int PointsPerWin { get; set; }
        public int PointsPerLoss { get; set; }
        public int PointsPerDraw { get; set; }
        public IList<string> Tags { get; set; }

        public Configuration()
        {
            Tags = new List<string>();

            NumberRounds = 3;

            PointsPerWin = 3;
            PointsPerDraw = 2;
            PointsPerLoss = 1;
        }

        public Configuration Clone()
        {
            Configuration clone = new Configuration();
            clone.NumberRounds = NumberRounds;
            clone.PointsPerWin = PointsPerWin;
            clone.PointsPerDraw = PointsPerDraw;
            clone.PointsPerLoss = PointsPerLoss;

            clone.Tags = clone.Tags.Concat(Tags).ToList();

            return clone;
        }

        public static Configuration CreateTestConfiguration()
        {
            Configuration config = new Configuration();

            config.NumberRounds = 5;

            // DBA Style! Draw is worse than Loss
            config.PointsPerWin = 3;
            config.PointsPerLoss = 1;
            config.PointsPerDraw = 0;

            config.Tags = new List<string> { "Plaquetas eliminadas", "General eliminado", "Campamento saqueado" };

            return config;
        }
    }

    internal class PlayerResult
    {
        public Player player { get; private set; }
        public int score { get; private set; }
        public Dictionary<string, int> scorePerTag { get; private set; }

        public PlayerResult(Player _player, int _score, Dictionary<string, int> _scorePerTag)
        {
            player = _player;
            score = _score;
            scorePerTag = _scorePerTag;
        }
    }
    public class Tournament : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Game { get; set; }
        public string Date { get; set; }
        public int CurrentRound { get; set; }
        public IList<Player> Players { get; set; }
        public bool PlayerListLocked { get; set; }
        public IList<Round> Rounds { get; set; }
        public Configuration Config { get; set; }
        public DataTable Ranking { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }

        // From top score to bottom, a cached list of players with
        // the score, used for the Ranking. Always autocalculated
        [JsonIgnoreAttribute]
        private IList<PlayerResult> cachedRankingResults;

        public Tournament()
        {
            Config = new Configuration();
            Players = new List<Player>();
            Rounds = new List<Round>();
            PlayerListLocked = false;
            FilePath = GetDefaultFilePath();
            FileName = "tournament.tour";
            cachedRankingResults = new List<PlayerResult>();
        }

        public Tournament Clone()
        {
            Tournament clone = new Tournament();
            clone.Name = Name;
            clone.Game = Game;
            clone.Date = Date;
            clone.CurrentRound = CurrentRound;
            clone.PlayerListLocked = PlayerListLocked;
            foreach (var player in Players)
            {
                clone.Players.Add(player.Clone());
            }
            foreach (var round in Rounds)
            {
                clone.Rounds.Add(round.Clone());
            }
            clone.Config = Config.Clone();
            clone.CreateRanking();
            clone.UpdateRanking();

            if (clone.Rounds.Count == 0)
            {
                clone.ResetRounds();
            }
            return clone;
        }

        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public static Tournament CreateTestTournament()
        {
            var tournament = new Tournament();

            tournament.Config = Configuration.CreateTestConfiguration();

            tournament.Name = "TestTournament";
            tournament.Game = "TestGame";
            tournament.Date = "Oct 10, 2017";
            tournament.CurrentRound = 2;

            tournament.Players = new List<Player> { new Player(true), new Player(true), new Player(true), new Player(true) };
            tournament.PlayerListLocked = true;

            tournament.Rounds = new List<Round>();

            tournament.CreateRanking();

            // Round has finished, player0 has won against player 1, and player2 and player3 has a draw
            var round1 = new Round();
            round1.Number = 1;
            round1.Active = false;
            round1.Matchups = new List<Matchup>
            {
                new Matchup { Player1Id = 0,
                              Player1Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 1 },
                                  { "General eliminado", 0 },
                                  { "Campamento saqueado", 0 }
                              },
                              Player2Id = 1,
                              Player2Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 4 },
                                  { "General eliminado", 0 },
                                  { "Campamento saqueado", 1 }
                              },
                              Round = 1, Table = 1, CurrentResult = Result.PLAYER1_LOSS },
                new Matchup { Player1Id = 2,
                              Player1Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 2 },
                                  { "General eliminado", 0 },
                                  { "Campamento saqueado", 0 }
                              },
                              Player2Id = 3,
                              Player2Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 3 },
                                  { "General eliminado", 0 },
                                  { "Campamento saqueado", 0 }
                              },
                              Round = 1, Table = 2, CurrentResult = Result.DRAW },
            };
            tournament.Rounds.Add(round1);

            // Round 2 is in progress, player3 has won against player1, and player0 is still playing against playing player2           
            var round2 = new Round();
            round2.Number = 2;
            round2.Active = true;
            round2.Matchups = new List<Matchup>
            {
                new Matchup { Player1Id = 3,
                              Player1Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 2 },
                                  { "General eliminado", 1 },
                                  { "Campamento saqueado", 0 }
                              },
                              Player2Id = 1,
                              Player2Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 4 },
                                  { "General eliminado", 0 },
                                  { "Campamento saqueado", 1 }
                              },
                              Round = 2, Table = 2, CurrentResult = Result.PLAYER1_WIN},
                new Matchup { Player1Id = 2,
                              Player1Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 0 },
                                  { "General eliminado", 0 },
                                  { "Campamento saqueado", 0 }
                              },
                              Player2Id = 0,
                              Player2Tags = new Dictionary<string, int> {
                                  { "Plaquetas eliminadas", 0 },
                                  { "General eliminado", 0 },
                                  { "Campamento saqueado", 0 }
                              },
                              Round = 2, Table = 1, CurrentResult = Result.STILL_PLAYING},
            };
            tournament.Rounds.Add(round2);

            var round3 = new Round();
            round3.Number = 3;
            round3.Active = false;
            round3.Matchups = new List<Matchup>();
            tournament.Rounds.Add(round3);

            tournament.UpdateRanking();

            return tournament;
        }

        private void CreateRanking()
        {
            Ranking = new DataTable();
            Ranking.Columns.Add("Nombre");
            Ranking.Columns.Add("Puntuación");
            Ranking.Columns.Add("Facción");
            foreach (var tag in Config.Tags)
            {
                Ranking.Columns.Add(tag);
            }
        }

        private void CalculateRanking()
        {
            List<PlayerResult> results = new List<PlayerResult>();
            foreach (var player in Players)
            {
                (int score, Dictionary<string, int> scorePerTag) = CalculatePlayerScore(player.Id);
                results.Add(new PlayerResult(player, score, scorePerTag));
            }
            results.Sort((x, y) => -1 * (x.score).CompareTo(y.score));
            cachedRankingResults = results;
        }

        private void UpdateRanking()
        {
            CalculateRanking();

            Ranking.Rows.Clear();
            var rankedPlayers = new List<object[]>(Players.Count);
            int columns = Ranking.Columns.Count;

            foreach (var playerResult in cachedRankingResults)
            {
                var rankedPlayer = new object[columns];
                rankedPlayer[0] = playerResult.player.Name;
                rankedPlayer[1] = playerResult.score;
                rankedPlayer[2] = playerResult.player.Faction;

                int i = 3;
                foreach (var tagScore in playerResult.scorePerTag)
                {
                    // TODO We should check that the column order is the same
                    // as the one we are adding here, because its not guaranteed
                    // for a dict to do so
                    rankedPlayer[i] = tagScore.Value;
                    i++;
                }

                rankedPlayers.Add(rankedPlayer);
            }

            foreach (var playerRow in rankedPlayers)
            {
                Ranking.Rows.Add(playerRow);
            }
        }

        private (int, Dictionary<string, int>) CalculatePlayerScore(int playerId)
        {
            int score = 0;
            var scorePerTag = new Dictionary<string, int>();
            foreach (var round in Rounds)
            {
                foreach (var matchup in round.Matchups)
                {
                    if (matchup.PlayerBelongsToMatchup(playerId))
                    {
                        if (matchup.CurrentResult == Result.STILL_PLAYING)
                            continue;
                        else
                        {
                            if (matchup.CurrentResult == Result.DRAW)
                                score += Config.PointsPerDraw;
                            else if (matchup.IsWinner(playerId))
                                score += Config.PointsPerWin;
                            else
                                score += Config.PointsPerLoss;

                            matchup.UpdateTags(playerId, scorePerTag);
                        }
                    }
                }
            }
            return (score, scorePerTag);
        }

        public bool CanAddPlayer(Player newPlayer)
        {
            return !Players.Any(p => p.Name == newPlayer.Name);
        }

        public bool CanAddPlayerName(string newPlayerName)
        {
            return !Players.Any(p => p.Name == newPlayerName);
        }

        public void AddPlayer(Player newPlayer)
        {
            // TODO: Validate there are no rounds running et al
            Players.Add(newPlayer);
            OnPropertyChanged("Players");
        }

        public void LockPlayerList()
        {
            if (Players.Count % 2 != 0)
                throw new InvalidOperationException("No se puede bloquear la lista de jugadores con un número impar.");
            if (Players.Count == 0)
                throw new InvalidOperationException("No se puede bloquear la lista de jugadores sin tener al menos uno.");
            PlayerListLocked = true;
            OnPropertyChanged("PlayerListLocked");
        }

        public void UnlockPlayerList()
        {
            // TODO: Validations
            PlayerListLocked = false;
            OnPropertyChanged("PlayerListLocked");
        }

        public void ResetRounds()
        {
            Rounds = new List<Round>();
            for (int i = 0; i < Config.NumberRounds; i++)
            {
                Rounds.Add(new Round(i + 1));
            }
            UpdateRanking();
        }

        // This matches using by id, without regards about repetition or whatever
        // Testing only
        public void GenerateMatchupById()
        {
            var round = Rounds[CurrentRound];
            round.Matchups = new List<Matchup>();
            for (int i = 0; i < Players.Count; i += 2)
            {
                round.Matchups.Add(new Matchup(round.Number, i, i + 1, Config.Tags));
            }
            round.OnPropertyChanged("Matchups");
        }

        private static string GetDefaultFilePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WargameTournamentManager");
        }

        // Asks the user for a file to load the tournament file. The user may cancel
        // this operation because there is a dialog, therefore this may return null
        public static Tournament LoadAskingForFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string folder = GetDefaultFilePath();
            Directory.CreateDirectory(folder);
            openFileDialog.InitialDirectory = folder;
            openFileDialog.Filter = "Tournament files (*.tour)|*.tour|All files (*.*)|*.*";

            bool? loaded = openFileDialog.ShowDialog();

            if (loaded == true)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                return JsonConvert.DeserializeObject<Tournament>(json);
            }
            return null;
        }

        private void SaveInternal(string directory, string name)
        {
            Directory.CreateDirectory(directory);

            string json = JsonConvert.SerializeObject(this);
            using (StreamWriter sw = new StreamWriter(Path.Combine(directory, name)))
            {
                sw.Write(json);
            }
        }

        public void Save()
        {
            SaveInternal(FilePath, FileName);
        }

        public void SaveWithBackup(string backupInfo)
        {
            var oldFilename = Path.GetFileNameWithoutExtension(FileName);
            var oldExtension = Path.GetExtension(FileName);

            string name = string.Format("{0}_{1}_{2}{3}",
                oldFilename, backupInfo, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"), oldExtension);
            SaveInternal(FilePath, name);
            Save();
        }

        // In this case the user chooses a name from a dialog. They may choose to cancel
        // the operation by closing the dialog, and in that case the method returns false
        public bool SaveAskingForName()
        {
            Directory.CreateDirectory(FilePath);
            SaveFileDialog savefile = new SaveFileDialog();

            // Tournament file
            savefile.FileName = "tournament" + Name + ".tour";
            savefile.Filter = "Tournament files (*.tour)|*.tour|All files (*.*)|*.*";
            savefile.InitialDirectory = FilePath;

            bool? saved = savefile.ShowDialog();
            if (saved != true)
            {
                return false;
            }

            FilePath = Path.GetDirectoryName(savefile.FileName);
            FileName = Path.GetFileName(savefile.FileName);
            Save();
            return true;
        }
    }

    // For easily accesible RNG, not threadsafe
    // Only used for testing
    public static class Globals
    {
        public static Random random = new Random();
        public static int index = 0;
    }

    public class Player
    {
        private static int CurrentId = 0;
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Club { get; set; }
        public string Contact { get; set; }
        public string Faction { get; set; }
        public bool Paid { get; set; }

        public Player(bool autogenerate = false)
        {
            Id = CurrentId;
            CurrentId++;
            if (autogenerate) Name = GetRandomName();
        }

        public Player Clone()
        {
            Player clone = new Player();
            clone.Id = Id;
            clone.Name = Name;
            clone.City = City;
            clone.Club = Club;
            clone.Contact = Contact;
            clone.Faction = Faction;
            clone.Paid = Paid;

            return clone;
        }

        private static string GetRandomName()
        {
            var names = new List<string> {
                "Paco", "Manolo", "José", "Javier", "Jesús", "Iker", "Ion",
                "Miguel", "Sergio", "Daniel", "David", "Aquiles", "Menon",
                "Pericles", "Heracles", "Demetrius", "Poliperconte", "Antipatro",
                "Seleuco", "Perdicas"};
            int index = Globals.random.Next(names.Count);
            Globals.index++;
            return names[index] + Globals.index;
        }
    }

    public class Round : INotifyPropertyChanged
    {
        public int Number { get; set; }
        public bool Active { get; set; }
        public IList<Matchup> Matchups { get; set; }

        public Round()
        {
            Number = 0;
            Active = false;
            Matchups = new List<Matchup>();
        }
        public Round(int number)
        {
            Number = number;
            Active = false;
            Matchups = new List<Matchup>();
        }

        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public Round Clone()
        {
            Round clone = new Round();
            clone.Number = Number;
            clone.Active = Active;
            foreach (var matchup in Matchups)
            {
                clone.Matchups.Add(matchup.Clone());
            }
            return clone;
        }
    }

    public class Matchup
    {
        public uint Table { get; set; }
        public int Round { get; set; }
        public Result CurrentResult { get; set; }

        public int Player1Id { get; set; }
        public Dictionary<string, int> Player1Tags { get; set; }

        public int Player2Id { get; set; }
        public Dictionary<string, int> Player2Tags { get; set; }

        public Matchup()
        {
            Player1Tags = new Dictionary<string, int>();
            Player2Tags = new Dictionary<string, int>();
        }

        public Matchup(int round, int player1Id, int player2Id, IList<string> tags)
        {
            Round = round;
            CurrentResult = Result.STILL_PLAYING;

            Player1Id = player1Id;
            Player2Id = player2Id;
            Player1Tags = new Dictionary<string, int>();
            Player2Tags = new Dictionary<string, int>();

            foreach (var tag in tags)
            {
                Player1Tags.Add(tag, 0);
                Player2Tags.Add(tag, 0);
            }
        }

        public Matchup Clone()
        {
            Matchup clone = new Matchup();
            clone.Table = Table;
            clone.Round = Round;
            clone.CurrentResult = CurrentResult;
            clone.Player1Id = Player1Id;
            clone.Player1Tags = new Dictionary<string, int>(Player1Tags);
            clone.Player2Id = Player2Id;
            clone.Player2Tags = new Dictionary<string, int>(Player2Tags);

            return clone;
        }

        public bool PlayerBelongsToMatchup(int playerId)
        {
            return playerId == Player1Id || playerId == Player2Id;
        }

        public bool IsWinner(int playerId)
        {
            return CurrentResult != Result.DRAW
                && CurrentResult != Result.STILL_PLAYING
                && (CurrentResult == Result.PLAYER1_WIN && playerId == Player1Id
                 || CurrentResult == Result.PLAYER1_LOSS && playerId == Player2Id);
        }

        public void UpdateTags(int playerId, Dictionary<string, int> scorePerTag)
        {
            if (CurrentResult == Result.STILL_PLAYING) return;
            if (playerId != Player1Id && playerId != Player2Id) throw new ArgumentException();

            var tags = playerId == Player1Id ? Player1Tags : Player2Tags;
            foreach (var tag in tags)
            {
                if (!scorePerTag.ContainsKey(tag.Key)) scorePerTag[tag.Key] = 0;
                scorePerTag[tag.Key] += tag.Value;
            }
        }
    }
}
