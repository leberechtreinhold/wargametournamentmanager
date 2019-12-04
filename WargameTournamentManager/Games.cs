using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WargameTournamentManager
{
    static class DBA
    {
        public static string GetName() { return "De Bellis Antiquitatis"; }
        public static int GetDefaultRounds() { return 5; }
        public static int GetDefaultPointsPerWin() { return 3; }
        public static int GetDefaultPointsPerDraw() { return 0; }
        public static int GetDefaultPointsPerLoss() { return 1; }
        public static IList<Tag> GetDefaultTags()
        {
            return new List<Tag> {
                new Tag("PeanasEliminadas"),
                new Tag("Campamentos"),
                new Tag("Generales"),
                new Tag("DiferenciaPeanas", TagType.Calculated, "diff(PeanasEliminadas)")
            };
        }
        public static string GetDefaultScoreFormula() { return "Puntos * 1000 + DiferenciaPeanas * 10 + Campamentos + Generales"; }
    }

    static class BoltAction
    {
        public static string GetName() { return "Bolt Action"; }
        public static int GetDefaultRounds() { return 3; }
        public static int GetDefaultPointsPerWin() { return 3; }
        public static int GetDefaultPointsPerDraw() { return 1; }
        public static int GetDefaultPointsPerLoss() { return 0; }
        public static IList<Tag> GetDefaultTags()
        {
            return new List<Tag> {
                new Tag("ObjetivoSecundario"),
                new Tag("OrdenesDestruidas"),
                new Tag("PuntosDestruidos"),
                new Tag("DiferenciaOrdenes", TagType.Calculated, "diff(OrdenesDestruidas)"),
                new Tag("DiferenciaPuntos", TagType.Calculated, "diff(PuntosDestruidos)")

            };
        }
        public static string GetDefaultScoreFormula() { return "Puntos * 10000 + ObjetivoSecundario * 10000 + DiferenciaPuntos"; }
    }
}
