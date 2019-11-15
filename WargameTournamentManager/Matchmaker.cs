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
                matchups.Add(new Matchup(roundNumber, i, i + 1, tournament.Config.Tags));
            }
            return matchups;
        }

        public static List<Matchup> GenerateMatchupByRandom(Tournament tournament, int roundNumber)
        {
            var matchups = new List<Matchup>();
            // Use current time as seed
            var players = tournament.Players.Shuffle(new Random()).ToList();
            for (int i = 0; i < players.Count; i += 2)
            {
                matchups.Add(new Matchup(roundNumber, players[i].Id, players[i + 1].Id, tournament.Config.Tags));
            }
            return matchups;
        }

        // Strict swiss is that players with the most points get matched against each other
        // Difference between strict or not is that there may be repeats
        public static List<Matchup> GenerateMatchupByStrictSwiss(Tournament tournament, int roundNumber)
        {
            var matchups = new List<Matchup>();
            var results = CalculateRanking(tournament);
            for (int i = 0; i < results.Count; i += 2)
            {
                matchups.Add(new Matchup(roundNumber, results[i].player.Id, results[i + 1].player.Id, tournament.Config.Tags));
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
                matchups.Add(new Matchup(roundNumber, player1, player2, tournament.Config.Tags));
                results.RemoveAt(i);
                results.RemoveAt(0);
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
            ranking.Columns.Add("Nombre");
            ranking.Columns.Add("Puntuación");
            ranking.Columns.Add("Facción");
            foreach (var tag in config.Tags)
            {
                ranking.Columns.Add(tag);
            }
            return ranking;
        }

        private static List<PlayerResult> CalculateRanking(Tournament tournament)
        {
            List<PlayerResult> results = new List<PlayerResult>();
            foreach (var player in tournament.Players)
            {
                (int score, Dictionary<string, int> scorePerTag) = CalculatePlayerScore(tournament, player.Id);
                results.Add(new PlayerResult(player, score, scorePerTag));
            }
            results.Sort((x, y) => -1 * (x.score).CompareTo(y.score));
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

                            matchup.UpdateTagTotalCalculation(playerId, scorePerTag);
                        }
                    }
                }
            }
            return (score, scorePerTag);
        }

        private static void FillRankingData(Tournament tournament, DataTable ranking, List<PlayerResult> results)
        {
            var rankedPlayers = new List<object[]>(tournament.Players.Count);
            int columns = ranking.Columns.Count;

            foreach (var playerResult in results)
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
                ranking.Rows.Add(playerRow);
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
        public int score { get; private set; }
        public Dictionary<string, int> scorePerTag { get; private set; }

        public PlayerResult(Player _player, int _score, Dictionary<string, int> _scorePerTag)
        {
            player = _player;
            score = _score;
            scorePerTag = _scorePerTag;
        }

        public override string ToString() { return player.Name + "(" + score + ")"; }
    }

}
