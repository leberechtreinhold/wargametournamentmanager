﻿using EasyLocalization.Localization;
using Jace;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WargameTournamentManager
{
    public static class Matchmaker
    {
        public static DataTable GenerateRanking(Tournament tournament)
        {
            DataTable ranking = CreateRankingColumns(tournament.Config);
            var results = CalculateRanking(tournament);
            FillRankingData(tournament, ranking, results);
            return ranking;
        }

        // This matches using by id, without regards about repetition or whatever
        // Testing only
        public static List<Matchup> GenerateMatchupById(Tournament tournament, int roundNumber)
        {
            var matchups = new List<Matchup>();
            for (int i = 0; i < tournament.Players.Count; i += 2)
            {
                matchups.Add(new Matchup(roundNumber, i / 2, i, i + 1, tournament.Config.GetTagsIds()));
            }
            return matchups;
        }

        public static List<Matchup> GenerateMatchupByRandom(Tournament tournament, int roundNumber)
        {
            var matchups = new List<Matchup>();
            // Use current time as seed
            var players = tournament.Players.Shuffle(new Random()).ToList();
            var tables = tournament.Tables.Shuffle(new Random()).Select(t => t.Id).ToList();
            for (int i = 0; i < players.Count; i += 2)
            {
                matchups.Add(new Matchup(roundNumber, tables[i / 2], players[i].Id, players[i + 1].Id, tournament.Config.GetTagsIds()));
            }
            return matchups;
        }

        // Strict swiss is that players with the most points get matched against each other
        // Difference between strict or not is that there may be repeats
        public static List<Matchup> GenerateMatchupByStrictSwiss(Tournament tournament, int roundNumber)
        {
            var matchups = new List<Matchup>();
            var results = CalculateRanking(tournament);
            var tables = tournament.Tables.Shuffle(new Random()).Select(t => t.Id).ToList();

            for (int i = 0; i < results.Count; i += 2)
            {
                matchups.Add(new Matchup(roundNumber, tables[i / 2], results[i].player.Id, results[i + 1].player.Id, tournament.Config.GetTagsIds()));
            }
            return matchups;
        }

        public static List<Matchup> GenerateMatchupBySwiss(Tournament tournament, int roundNumber)
        {
            var matchups = new List<Matchup>();
            var results = CalculateRanking(tournament);
            while (results.Count > 0)
            {
                int player1 = results[0].player.Id;
                int i = 1;
                while (i < results.Count
                       && PlayersHavePlayedTogether(tournament, player1, results[i].player.Id))
                {
                    i++;
                }
                if (i == results.Count)
                {
                    i = 1;
                }
                int player2 = results[i].player.Id;
                // We put the tableId as 1 by default because it needs to be updated later
                matchups.Add(new Matchup(roundNumber, 1, player1, player2, tournament.Config.GetTagsIds()));
                results.RemoveAt(i);
                results.RemoveAt(0);
            }
            return matchups;
        }

        public static List<Matchup> GenerateMatchupByCityClub(Tournament tournament, int roundNumber)
        {
            var pairings = MatchmakerCityClubHelper.MakeMatchmaking(tournament.Players);
            HashSet<Player> matchedPlayers = new HashSet<Player>();
            var tables = tournament.Tables.Shuffle(new Random()).Select(t => t.Id).ToList();
            int tablesUsed = 0;

            var matchups = new List<Matchup>();
            foreach (var pairing in pairings)
            {
                if (!matchedPlayers.Contains(pairing.Key))
                {
                    matchups.Add(new Matchup(roundNumber, tables[tablesUsed], pairing.Key.Id, pairing.Value.Id, tournament.Config.GetTagsIds()));
                    tablesUsed++;
                    matchedPlayers.Add(pairing.Key);
                    matchedPlayers.Add(pairing.Value);
                }
            }
            return matchups;
        }

        private static bool PlayersHavePlayedTogether(Tournament t, int player1Id, int player2Id)
        {
            foreach (var round in t.Rounds)
            {
                foreach (var matchup in round.Matchups)
                {
                    if (matchup.PlayerBelongsToMatchup(player1Id))
                    {
                        if (matchup.PlayerBelongsToMatchup(player2Id))
                            return true;
                        else
                            break;
                    }
                }
            }
            return false;
        }

        private static DataTable CreateRankingColumns(Configuration config)
        {
            DataTable ranking = new DataTable();
            ranking.Columns.Add(LocalizationManager.Instance.GetValue("name"));
            ranking.Columns.Add(LocalizationManager.Instance.GetValue("total_score"));
            ranking.Columns.Add(LocalizationManager.Instance.GetValue("matchup_score"));
            ranking.Columns.Add(LocalizationManager.Instance.GetValue("faction"));
            foreach (var tag in config.Tags)
            {
                ranking.Columns.Add(tag.Name);
            }
            return ranking;
        }

        private static List<PlayerResult> CalculateRanking(Tournament tournament)
        {
            List<PlayerResult> results = new List<PlayerResult>();
            foreach (var player in tournament.Players)
            {
                (int matchScore, Dictionary<string, int> scorePerTag) = CalculatePlayerScore(tournament, player.Id);
                double totalScore = CalculatePlayerTotalScore(tournament, matchScore, scorePerTag);
                results.Add(new PlayerResult(player, totalScore, matchScore, scorePerTag));
            }
            results.Sort((x, y) => -1 * (x.totalScore).CompareTo(y.totalScore));
            return results;
        }

        private static (int, Dictionary<string, int>) CalculatePlayerScore(Tournament tournament, int playerId)
        {
            int score = 0;
            var scorePerTag = new Dictionary<string, int>();
            foreach (var round in tournament.Rounds)
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
                                score += tournament.Config.PointsPerDraw;
                            else if (matchup.IsWinner(playerId))
                                score += tournament.Config.PointsPerWin;
                            else
                                score += tournament.Config.PointsPerLoss;

                            matchup.UpdateTagTotalCalculation(playerId, tournament.Config.Tags, scorePerTag);
                        }
                    }
                }
            }
            return (score, scorePerTag);
        }

        private static double CalculatePlayerTotalScore(Tournament tournament, int matchScore, Dictionary<string, int> scorePerTag)
        {
            Dictionary<string, double> variables = new Dictionary<string, double>();
            variables.Add("Puntos", matchScore);
            variables.Add("Score", matchScore);
            foreach (var pair in scorePerTag)
            {
                variables.Add(pair.Key, pair.Value);
            }
            foreach (var tag in tournament.Config.Tags)
            {
                if (!variables.ContainsKey(tag.Name))
                {
                    variables.Add(tag.Name, 0);
                }
            }

            CalculationEngine engine = new CalculationEngine();
            return engine.Calculate(tournament.Config.ScoreFormula, variables);
        }

        public static bool TestPlayerScoreFormulaValid(Tournament tournament)
        {
            try
            {
                Dictionary<string, int> scorePerTag = new Dictionary<string, int>();
                foreach(var tag in tournament.Config.Tags)
                {
                    scorePerTag[tag.Name] = 0;
                }
                // Calculated tags may depend on previous values to we have to iterate two times
                foreach (var tag in tournament.Config.Tags)
                {
                    scorePerTag[tag.Name] = Matchup.CalculateTag(tag, scorePerTag, scorePerTag);
                }
                CalculatePlayerTotalScore(tournament, 0, scorePerTag);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void FillRankingData(Tournament tournament, DataTable ranking, List<PlayerResult> results)
        {
            var rankedPlayers = new List<object[]>(tournament.Players.Count);
            int columns = ranking.Columns.Count;

            foreach (var playerResult in results)
            {
                var rankedPlayer = new object[columns];
                rankedPlayer[0] = playerResult.player.Name;
                rankedPlayer[1] = playerResult.totalScore;
                rankedPlayer[2] = playerResult.matchScore;
                rankedPlayer[3] = playerResult.player.Faction;

                int i = 4;
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
                ranking.Rows.Add(playerRow);
            }
        }

        public static void UpdateMatchupsWithTables(List<Matchup> matchups, Tournament tournament)
        {
            // First naive assignment, each gets a number depending on matchmaking
            for (int i = 0; i < matchups.Count; i++)
            {
                matchups[i].TableId = i;
            }

            // If its the first round, no one has played in another player,
            // therefore no conflict possible
            if (matchups.First().Round == 0)
            {
                return;
            }

            var table_ids = tournament.GetTableIds();
            var tables_available_per_matchup = new Dictionary<Matchup, List<int>>();
            for (int i = 0; i < matchups.Count; i++)
            {
                var matchup = matchups[i];
                tables_available_per_matchup[matchup] = table_ids.Where(table_id =>
                    !HasEitherPlayerPlayedInTable(tournament, matchup.Player1Id, matchup.Player2Id, table_id)
                ).ToList();
                var player1 = matchup.Player1Id;
                var player2 = matchup.Player2Id;
            }
            var used_tables = new HashSet<int>();
            var pending_matchups = new List<Matchup>(matchups);
            var matchups_by_least_tables = pending_matchups.OrderBy(m => tables_available_per_matchup[m].Count);
            while (matchups_by_least_tables.Any())
            {
                var matchup = matchups_by_least_tables.First();

                if (tables_available_per_matchup[matchup].Count > 0)
                {
                    // TODO assign the tables that are available to the least amount
                    // of other tables instead of random. This does however has the 
                    // side effect of assigning not used tables which are available to
                    // anyone, so rethink?

                    // Select randomly one of the available ones
                    var available = tables_available_per_matchup[matchup];
                    var table_id = available.ElementAt(new Random().Next(0, available.Count));
                    matchup.TableId = table_id;
                }
                else
                {
                    // No tables left, so assign whatever is not used yet
                    var free_ids = table_ids.Where(id => !used_tables.Contains(id)).ToList();
                    var table_id = free_ids.ElementAt(new Random().Next(0, free_ids.Count));
                    matchup.TableId = table_id;
                }
                used_tables.Add(matchup.TableId);
                pending_matchups.Remove(matchup);
                foreach (var pending_matchup in pending_matchups)
                {
                    var available = tables_available_per_matchup[pending_matchup];
                    available.Remove(matchup.TableId);
                }
                matchups_by_least_tables = pending_matchups.OrderBy(m => tables_available_per_matchup[m].Count);
            }
        }

        private static bool HasPlayerPlayedInTable(Tournament tournament, int playerId, int tableNumber)
        {
            foreach (var round in tournament.Rounds)
            {
                foreach (var matchup in round.Matchups)
                {
                    if (matchup.TableId == tableNumber
                        && matchup.PlayerBelongsToMatchup(playerId))
                        return true;
                }
            }
            return false;
        }

        // Version of the previous with two playerIds. Faster, and even if 
        // it can be easily generalized with a list, we want this to be 
        // fast because it canbe called in n^2 matchups!!!
        // NOTE If its still slow, use a table where each player has the list
        // of tables he already played, and do a cross check
        private static bool HasEitherPlayerPlayedInTable(Tournament tournament, int player1Id, int player2Id, int tableNumber)
        {
            foreach (var round in tournament.Rounds)
            {
                foreach (var matchup in round.Matchups)
                {
                    if (matchup.TableId == tableNumber
                        && (matchup.PlayerBelongsToMatchup(player1Id)
                            || matchup.PlayerBelongsToMatchup(player2Id)))
                        return true;
                }
            }
            return false;
        }
        // Despite looking like a super easy problem, making a matchmaking where
        // each player has different criteria is a surprisingly complex
        // pairing problem, and we need to precalculate a lot of values.
        // It's easier with a proper datastructure in a class
        internal class MatchmakerCityClubHelper
        {
            IList<Player> players;
            double[,] values;
            Dictionary<Player, Player> playerMatchups;
            Dictionary<Player, double> playerValue;
            Dictionary<Player, int> playerReverseIndex;
            const double maxPairingValue = 1;

            MatchmakerCityClubHelper(IList<Player> _players)
            {
                players = _players;
                playerMatchups = new Dictionary<Player, Player>();
                playerValue = new Dictionary<Player, double>();

                int nPlayers = players.Count;
                playerReverseIndex = new Dictionary<Player, int>();
                for (int i = 0; i < nPlayers; i++)
                {
                    playerReverseIndex[players[i]] = i;
                }

                values = new double[nPlayers, nPlayers];
                for (int i = 0; i < nPlayers; i++)
                {
                    for (int j = 0; j < nPlayers; j++)
                    {
                        if (players[i] == players[j]) values[i, j] = -1;
                        else
                        {
                            // Aggregate all properties
                            values[i, j] = 0;
                            if (players[i].Club != players[j].Club) values[i, j] += 0.5;
                            if (players[i].City != players[j].City) values[i, j] += 0.5;
                        }
                    }
                }
            }

            void AddPairing(Player p1, Player p2, double value)
            {
                if (p1 == p2) throw new InvalidOperationException();
                if (playerMatchups.ContainsKey(p1) || playerMatchups.ContainsKey(p2)) throw new InvalidOperationException();

                playerMatchups[p1] = p2;
                playerMatchups[p2] = p1;
                playerValue[p1] = value;
                playerValue[p2] = value;
            }

            bool HasPairing(Player p)
            {
                return playerValue.ContainsKey(p);
            }

            Player RivalOf(Player p)
            {
                return playerMatchups[p];
            }

            int RivalIndex(Player p)
            {
                return playerReverseIndex[RivalOf(p)];
            }

            double PairingValue(Player p)
            {
                return playerValue[p];
            }

            void RemovePairing(Player p)
            {
                var p2 = playerMatchups[p];
                playerMatchups.Remove(p);
                playerMatchups.Remove(p2);
                playerValue.Remove(p);
                playerValue.Remove(p2);
            }

            bool MakeMatchSwappingIfBetter(Player p1, Player p2, double value)
            {
                bool p1_paired = HasPairing(p1);
                bool p2_paired = HasPairing(p2);

                if (!p1_paired && !p2_paired)
                {
                    //Console.WriteLine(string.Format("Adding [{0} vs {1}] ({2})", p1, p2, value));
                    AddPairing(p1, p2, value);
                    return true;
                }
                else if (p1_paired && p2_paired)
                {
                    var current_pairing_value = PairingValue(p1) + PairingValue(p2);
                    var rival_pairing_value = values[RivalIndex(p1), RivalIndex(p2)];
                    var swapped_pairing_value = value + rival_pairing_value;
                    if (swapped_pairing_value > current_pairing_value)
                    {
                        var rival_p1 = RivalOf(p1);
                        var rival_p2 = RivalOf(p2);
                        //Console.WriteLine(string.Format("Swapping [{0} vs {1}] & [{2} vs {3}] ({4}) for [{5} vs {6}] & [{7} vs {8}] ({9})",
                        //    p1, rival_p1, p2, rival_p2, current_pairing_value,
                        //    p1, p2, rival_p1, rival_p2, swapped_pairing_value));
                        RemovePairing(p1);
                        RemovePairing(p2);
                        AddPairing(p1, p2, value);
                        AddPairing(rival_p1, rival_p2, rival_pairing_value);
                        return true;
                    }
                    return false;
                }
                else if (p1_paired && !p2_paired)
                {
                    var current_pairing_value = PairingValue(p1);
                    var swapped_pairing_value = value;
                    if (swapped_pairing_value > current_pairing_value)
                    {
                        var rival_p1 = RivalOf(p1);
                        //Console.WriteLine(string.Format("Swapping [{0} vs {1}] & {2} ({3}) for [{4} vs {5}] & {6} ({7})",
                        //    p1, rival_p1, p2, current_pairing_value,
                        //    p1, p2, rival_p1, swapped_pairing_value));
                        RemovePairing(p1);
                        AddPairing(p1, p2, value);
                        return true;
                    }
                    return false;
                }
                else /*if (!p1_paired && p2_paired)*/
                {
                    var current_pairing_value = PairingValue(p2);
                    var swapped_pairing_value = value;
                    if (swapped_pairing_value > current_pairing_value)
                    {
                        var rival_p2 = RivalOf(p2);
                        //Console.WriteLine(string.Format("Swapping {0} & [{1} vs{2}] ({3}) for [{4} vs {5}] & {6} ({7})",
                        //    p1, p2, rival_p2, current_pairing_value,
                        //    p1, p2, rival_p2, swapped_pairing_value));
                        RemovePairing(p2);
                        AddPairing(p1, p2, value);
                        return true;
                    }
                    return false;
                }
            }
            /*
            List<string> players = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
            double[,] values = new double[,]
            {
                         a    b    c    d    e    f    g    h 
                  a   {  -1,   0, 0.5,   1,   1,   1,   1,   1},
                  b   {   0,  -1, 0.5,   1,   1,   1,   1,   1},
                  c   { 0.5, 0.5,  -1, 0.5,   1,   1,   1,   1},
                  d   {   1,   1, 0.5,  -1, 0.5,   1,   1,   1},
                  e   {   1,   1,   1, 0.5,  -1,   1,   1,   1},
                  f   {   1,   1,   1,   1,   1,  -1,   1,   1},
                  g   {   1,   1,   1,   1,   1,   1,  -1, 0.5},
                  h   {   1,   1,   1,   1,   1,   1, 0.5,  -1},
            };
            Should return A vs H, B vs E, C vs F, d vs G, total value 8 (max)
            */
            Dictionary<Player, Player> CalculatePairings()
            {
                int nPlayers = players.Count;

                for (int i = 0; i < nPlayers; i++)
                {
                    var p1 = players[i];
                    double current_max_p1 = -1;

                    for (int j = 0; j < nPlayers; j++)
                    {
                        var p2 = players[j];
                        if (p1 == p2) continue;

                        Console.WriteLine("Checking " + p1 + " vs " + p2);
                        var pairing_value = values[i, j];
                        if (pairing_value > current_max_p1)
                        {
                            var matched = MakeMatchSwappingIfBetter(p1, p2, pairing_value);
                            if (matched)
                            {
                                current_max_p1 = pairing_value;
                                if (current_max_p1 == maxPairingValue)
                                    break;
                            }
                        }
                    }
                }
                return playerMatchups;
            }

            public string ListMatchups()
            {
                StringBuilder sb = new StringBuilder();
                double total_value = 0;
                foreach (var kv in playerMatchups)
                {
                    sb.AppendFormat(" {0} vs {1} ({2})\n", kv.Key, kv.Value, PairingValue(kv.Key));
                    total_value += PairingValue(kv.Key);
                }
                sb.AppendFormat("Total value: {0}", total_value);
                return sb.ToString();
            }

            public static Dictionary<Player, Player> MakeMatchmaking(IList<Player> players)
            {
                var calculator = new MatchmakerCityClubHelper(players);
                return calculator.CalculatePairings();
            }
        }
    }

    public static class ExtensionMethods
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            T[] elements = source.ToArray();
            for (int i = elements.Length - 1; i >= 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                // ... except we don't really need to swap it fully, as we can
                // return it immediately, and afterwards it's irrelevant.
                int swapIndex = rng.Next(i + 1);
                yield return elements[swapIndex];
                elements[swapIndex] = elements[i];
            }
        }
    }

    internal class PlayerResult
    {
        public Player player { get; private set; }
        public double totalScore { get; private set; }
        public int matchScore { get; private set; }
        public Dictionary<string, int> scorePerTag { get; private set; }

        public PlayerResult(Player _player, double _totalScore, int _matchScore, Dictionary<string, int> _scorePerTag)
        {
            player = _player;
            totalScore = _totalScore;
            matchScore = _matchScore;
            scorePerTag = _scorePerTag;
        }

        public override string ToString() { return player.Name + "(" + totalScore + ")"; }
    }

}
