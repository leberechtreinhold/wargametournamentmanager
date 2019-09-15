﻿using System;
using System.Collections.Generic;
using System.Data;
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
            NumberRounds = 3;

            PointsPerWin = 3;
            PointsPerLoss = 1;
            PointsPerDraw = 0;

            Tags = new List<string> { "Plaquetas eliminadas", "General eliminado", "Campamento saqueado" };
        }
    }
    public class Tournament
    {
        public string Name { get; set; }
        public string Game { get; set; }
        public string Date { get; set; }
        public int CurrentRound { get; set; }
        public IList<Player> Players { get; set; }
        public IList<Round> Rounds { get; set; }
        public Configuration Config { get; set; }
        public DataTable Ranking { get; set; }

        public Tournament()
        {
            Config = new Configuration();

            Name = "TestTournament";
            Game = "TestGame";
            Date = "Oct 10, 2017";
            CurrentRound = 2;

            Players = new List<Player> { new Player(), new Player(), new Player(), new Player() };

            CreateRanking();

            Rounds = new List<Round>();

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
            Rounds.Add(round1);

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
            Rounds.Add(round2);

            var round3 = new Round();
            round3.Number = 3;
            round3.Active = false;
            round3.Matchups = new List<Matchup>();
            Rounds.Add(round3);

            UpdateRanking();
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

        private void UpdateRanking()
        {
            Ranking.Rows.Clear();
            var rankedPlayers = new List<object[]>(Players.Count);
            int columns = Ranking.Columns.Count;
            foreach (var player in Players)
            {
                var rankedPlayer = new object[columns];
                (int score, Dictionary<string, int> scorePerTag) = CalculatePlayerScore(player.Id);
                rankedPlayer[0] = player.Name;
                rankedPlayer[1] = score;
                rankedPlayer[2] = player.Faction;

                int i = 3;
                foreach(var tagScore in scorePerTag)
                {
                    // TODO We should check that the column order is the same
                    // as the one we are adding here, because its not guaranteed
                    // for a dict to do so
                    rankedPlayer[i] = tagScore.Value;
                    i++;
                }

                rankedPlayers.Add(rankedPlayer);
            }

            rankedPlayers.Sort((x, y) => -1*((int)x[1]).CompareTo((int)y[1]));
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

        public Player()
        {
            Id = CurrentId;
            CurrentId++;
            Name = GetRandomName();
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

    public class Round
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
    }

    public class Matchup
    {
        public uint Table { get; set; }
        public uint Round { get; set; }
        public Result CurrentResult { get; set; }

        public int Player1Id { get; set; }
        public Dictionary<string, int> Player1Tags { get; set; }

        public int Player2Id { get; set; }
        public Dictionary<string, int> Player2Tags { get; set; }

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
