using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace WargameTournamentManager
{
    public enum Result
    {
        STILL_PLAYING,
        PLAYER1_WIN,
        PLAYER1_LOSS,
        DRAW
    }

    public enum TagType
    {
        Number,
        Calculated
    }

    public enum MatchmakingType
    {
        Id,
        Random,
        CityClub,
        Swiss,
        StrictSwiss
    }

    public class Tag
    {
        public string Name { get; set; }
        public TagType Type { get; set; }
        public string Information { get; set; }

        public Tag()
        {
            Name = "Default";
            Type = TagType.Number;
            Information = "";
        }

        public Tag(string name)
        {
            Name = name;
            Type = TagType.Number;
            Information = "";
        }

        public Tag(string name, TagType type, string information)
        {
            Name = name;
            Type = type;
            Information = information;
        }

        public Tag Clone()
        {
            Tag clone = new Tag();
            clone.Name = Name;
            clone.Type = Type;
            clone.Information = Information;

            return clone;
        }
    }
    
    public class Configuration
    {
        public int NumberRounds { get; set; }
        public int PointsPerWin { get; set; }
        public int PointsPerLoss { get; set; }
        public int PointsPerDraw { get; set; }
        public string ScoreFormula { get; set; }
        public IList<Tag> Tags { get; set; }
        public MatchmakingType FirstRoundMatchmaking { get; set; }
        public MatchmakingType RoundMatchmaking { get; set; }

        public Configuration()
        {
            NumberRounds = DBA.GetDefaultRounds();
            PointsPerWin = DBA.GetDefaultPointsPerWin();
            PointsPerDraw = DBA.GetDefaultPointsPerDraw();
            PointsPerLoss = DBA.GetDefaultPointsPerLoss();
            Tags = DBA.GetDefaultTags();
            ScoreFormula = DBA.GetDefaultScoreFormula();
            FirstRoundMatchmaking = MatchmakingType.CityClub;
            RoundMatchmaking = MatchmakingType.Swiss;
        }

        public Configuration Clone()
        {
            Configuration clone = new Configuration();
            clone.NumberRounds = NumberRounds;
            clone.PointsPerWin = PointsPerWin;
            clone.PointsPerDraw = PointsPerDraw;
            clone.PointsPerLoss = PointsPerLoss;
            clone.ScoreFormula = ScoreFormula;
            clone.FirstRoundMatchmaking = FirstRoundMatchmaking;
            clone.RoundMatchmaking = RoundMatchmaking;

            clone.Tags = new List<Tag>();
            foreach (var tag in Tags)
            {
                clone.Tags.Add(tag.Clone());
            }
            return clone;
        }

        public IList<string> GetTagsIds()
        {
            return Tags.Select(x => x.Name).ToList();
        }

    }

    public class Tournament : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public int CurrentRound { get; set; }
        public IList<Player> Players { get; set; }
        public bool PlayerListLocked { get; set; }
        public IList<Round> Rounds { get; set; }
        public Configuration Config { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }

        // From top score to bottom, a cached list of players with
        // the score, used for the Ranking. Always autocalculated
        [JsonIgnoreAttribute]
        public DataTable Ranking { get; set; }

        public Tournament()
        {
            Config = new Configuration();
            Players = new List<Player>();
            Rounds = new List<Round>();
            PlayerListLocked = false;
            FilePath = GetDefaultFilePath();
            FileName = "tournament.tour";
        }

        public Tournament Clone()
        {
            Tournament clone = new Tournament();
            clone.Name = Name;
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

        public void UpdateRanking()
        {
            Ranking = Matchmaker.GenerateRanking(this);
            OnPropertyChanged("Ranking");
        }

        public bool CanAddPlayer(Player newPlayer)
        {
            return !Players.Any(p => p.Name == newPlayer.Name);
        }

        // Check if theres any other player that is not playerId with that name
        public bool CanAddPlayerName(string newPlayerName, int playerId)
        {
            return !Players.Any(p => p.Name == newPlayerName && p.Id != playerId);
        }

        public void AddPlayer(Player newPlayer)
        {
            if (Players.Count == 0)
                newPlayer.Id = 0;
            else
                newPlayer.Id = Players.Max(player => player.Id) + 1;

            Players.Add(newPlayer);
            OnPropertyChanged("Players");

            UpdateRanking();
            Save();
        }

        public void DeletePlayer(int playerId)
        {
            // Assume that there is no matchup going on, otherwise this
            // breaks becuase the matchup will point to a bad id!
            int index = Players.ToList().FindIndex(player => player.Id == playerId);
            Players.RemoveAt(index);
            OnPropertyChanged("Players");

            UpdateRanking();
            Save();
        }

        // Given a player info data, edits the current object with that id with that data
        // BUT never changes the reference and doesnt change the input
        public void UpdatePlayerData(Player inputPlayer)
        {
            var player = Players.First(p => p.Id == inputPlayer.Id);
            player.UpdateData(inputPlayer);
            Save();
        }

        public void LockPlayerList()
        {
            if (Players.Count % 2 != 0)
                throw new InvalidOperationException("No se puede bloquear la lista de jugadores con un número impar.");
            if (Players.Count == 0)
                throw new InvalidOperationException("No se puede bloquear la lista de jugadores sin tener al menos uno.");

            SaveWithBackup("prelock");

            PlayerListLocked = true;
            Rounds.ToList().ForEach((round) =>
            {
                round.Active = false;
                round.OnPropertyChanged("Active");
            });
            Rounds.First().Active = true;
            Rounds.First().OnPropertyChanged("Active");

            OnPropertyChanged("PlayerListLocked");

            Save();
        }

        public void UnlockPlayerList()
        {
            SaveWithBackup("preunlock");

            PlayerListLocked = false;
            Rounds.ToList().ForEach((round) =>
            {
                round.Active = false;
                round.OnPropertyChanged("Active");
                round.Matchups = new List<Matchup>();
                round.OnPropertyChanged("Matchups");

            });
            OnPropertyChanged("PlayerListLocked");

            Save();
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

        public void GenerateMatchup()
        {
            if (CurrentRound == 0)
                GenerateMatchupByType(Config.FirstRoundMatchmaking);
            else
                GenerateMatchupByType(Config.RoundMatchmaking);
        }

        private void GenerateMatchupByType(MatchmakingType type)
        {
            var round = Rounds[CurrentRound];
            List<Matchup> matchups = null;

            if (type == MatchmakingType.Id)
                matchups = Matchmaker.GenerateMatchupById(this, round.Number);
            else if (type == MatchmakingType.Random)
                matchups = Matchmaker.GenerateMatchupByRandom(this, round.Number);
            else if (type == MatchmakingType.StrictSwiss)
                matchups = Matchmaker.GenerateMatchupByStrictSwiss(this, round.Number);
            else if (type == MatchmakingType.Swiss)
                matchups = Matchmaker.GenerateMatchupBySwiss(this, round.Number);
            else if (type == MatchmakingType.CityClub)
                matchups = Matchmaker.GenerateMatchupByCityClub(this, round.Number);
            else
                throw new ArgumentException("Unrecognized matchmaking type: " + type);

            Matchmaker.UpdateMatchupsWithTables(matchups, this);
            round.Matchups = matchups;
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

                var settings = new JsonSerializerSettings();
                settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                var tournament = JsonConvert.DeserializeObject<Tournament>(json, settings);

                // The user may have change the name of the file, and also the location,
                // therefore, update it
                tournament.FileName = Path.GetFileName(openFileDialog.FileName);
                tournament.FilePath = Path.GetDirectoryName(openFileDialog.FileName);
                tournament.UpdateRanking();
                return tournament;
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

        public void FinishRound(Round round)
        {
            if (!PlayerListLocked)
            {
                throw new InvalidOperationException("No se puede cerrar una ronda sin que la lista de jugadores esté cerrada.");
            }
            if (!round.Active)
            {
                throw new InvalidOperationException("No se puede cerrar una ronda que no está activa.");
            }
            if (round.Number != CurrentRound + 1)
            {
                throw new InvalidOperationException("No se puede cerrar una ronda que no sea la actual.");
            }
            int n_matchups = round.Matchups.Count;
            if (n_matchups == 0)
            {
                throw new InvalidOperationException("No se puede cerrar una ronda sin enfrentamientos.");
            }
            foreach (var matchup in round.Matchups)
            {
                if (matchup.CurrentResult == Result.STILL_PLAYING)
                {
                    throw new InvalidOperationException("No se puede cerrar una ronda que todavía tiene enfrentamientos sin terminar.");
                }
            }

            SaveWithBackup("pre_round_" + round.Number + "_finish");
            int index = round.Number - 1;
            Rounds[index].Active = false;
            Rounds[index].OnPropertyChanged("Active");
            if (index < Rounds.Count - 1)
            {
                Rounds[index + 1].Active = true;
                Rounds[index + 1].OnPropertyChanged("Active");
                CurrentRound = index + 1;
                OnPropertyChanged("CurrentRound");
            }
            Save();
        }

        public void ExportRankingAskingUser()
        {
            IEnumerable<string> columnNames = Ranking.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName);

            StringBuilder content = new StringBuilder();
            content.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in Ranking.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                content.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText("test.csv", content.ToString());

            Directory.CreateDirectory(FilePath);
            SaveFileDialog csvFile = new SaveFileDialog();

            // Tournament file
            csvFile.FileName = "tournament" + Name + ".csv";
            csvFile.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";

            bool? saved = csvFile.ShowDialog();
            if (saved != true)
            {
                return;
            }

            File.WriteAllText(csvFile.FileName, content.ToString());
        }
    }

    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Club { get; set; }
        public string Contact { get; set; }
        public string Faction { get; set; }
        public bool Paid { get; set; }

        public Player(int id = -1)
        {
            Id = id;
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

        // Like Clone but without changing the refs!
        public void UpdateData(Player data)
        {
            Id = data.Id;
            Name = data.Name;
            City = data.City;
            Club = data.Club;
            Contact = data.Contact;
            Faction = data.Faction;
            Paid = data.Paid;
        }

        public override string ToString()
        {
            return Name;
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
        public int Table { get; set; }
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

        public void UpdateTagTotalCalculation(int playerId, IList<Tag> configTags, Dictionary<string, int> scorePerTag)
        {
            if (CurrentResult == Result.STILL_PLAYING) return;
            if (playerId != Player1Id && playerId != Player2Id) throw new ArgumentException();

            Dictionary<string, int> tags;
            Dictionary<string, int> tags_opponent;
            if (playerId == Player1Id)
            {
                tags = Player1Tags;
                tags_opponent = Player2Tags;
            }
            else
            {
                tags = Player2Tags;
                tags_opponent = Player1Tags;
            }

            foreach (var tag in configTags)
            {
                if (!scorePerTag.ContainsKey(tag.Name)) scorePerTag[tag.Name] = 0;
                scorePerTag[tag.Name] += CalculateTag(tag, tags, tags_opponent);
            }
        }

        public static int CalculateTag(Tag tag, Dictionary<string, int> tags, Dictionary<string, int> tags_opponent)
        {
            if (tag.Type != TagType.Calculated)
            {
                return tags[tag.Name];
            }
            else
            {
                // At the moment the only function supported is diff
                if (!tag.Information.ToLowerInvariant().StartsWith("diff("))
                    throw new InvalidOperationException("Unrecognized function for tag " + tag.Name);

                // Calculate diff(tag), so we remove diff( and ), and get the
                // tag and substract
                var calctag = tag.Information.Substring(5, tag.Information.Length - 6);
                return tags[calctag] - tags_opponent[calctag];
            }
        }
    }
}
