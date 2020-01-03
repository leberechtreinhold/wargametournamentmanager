using EasyLocalization.Localization;
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
                new Tag(LocalizationManager.Instance.GetValue("destroyed_bases_tag")),
                new Tag(LocalizationManager.Instance.GetValue("camps_tag")),
                new Tag(LocalizationManager.Instance.GetValue("generals_tag")),
                new Tag(LocalizationManager.Instance.GetValue("destroyed_bases_diff_tag"), 
                    TagType.Calculated, 
                    $"diff({LocalizationManager.Instance.GetValue("destroyed_bases_tag")})")
            };
        }
        public static string GetDefaultScoreFormula() { return
            $"{LocalizationManager.Instance.GetValue("score_tag")} * 1000 "
            + $" + {LocalizationManager.Instance.GetValue("destroyed_bases_diff_tag")} * 10 "
            + $" + {LocalizationManager.Instance.GetValue("camps_tag")}"
            + $" + {LocalizationManager.Instance.GetValue("generals_tag")}"; }
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
                new Tag(LocalizationManager.Instance.GetValue("secondary_objective_tag")),
                new Tag(LocalizationManager.Instance.GetValue("destroyed_orders_tag")),
                new Tag(LocalizationManager.Instance.GetValue("destroyed_points_tag")),
                new Tag(LocalizationManager.Instance.GetValue("destroyed_orders_diff_tag"), 
                    TagType.Calculated, 
                    $"diff({LocalizationManager.Instance.GetValue("destroyed_orders_tag")})"),
                new Tag(LocalizationManager.Instance.GetValue("destroyed_points_diff_tag"), 
                    TagType.Calculated, 
                    $"diff({LocalizationManager.Instance.GetValue("destroyed_points_tag")})")

            };
        }
        public static string GetDefaultScoreFormula() { return 
                $"{LocalizationManager.Instance.GetValue("score_tag")} * 10000 "
                + $" + {LocalizationManager.Instance.GetValue("secondary_objective_tag")} * 10000 "
                + $" + {LocalizationManager.Instance.GetValue("destroyed_points_diff_tag")}"; }
    }
}
