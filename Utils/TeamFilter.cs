using System;
using System.Collections.Generic;
using System.Linq;

namespace FantasyNBA.Utils
{
    public static class TeamFilter
    {
        private static readonly Dictionary<string, string> _canonicalNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ATL Hawks", "Atlanta Hawks" },
            { "Atlanta Hawks", "Atlanta Hawks" },

            { "BOS Celtics", "Boston Celtics" },
            { "Boston Celtics", "Boston Celtics" },

            { "BKN Nets", "Brooklyn Nets" },
            { "Brooklyn Nets", "Brooklyn Nets" },

            { "CHA Hornets", "Charlotte Hornets" },
            { "Charlotte Hornets", "Charlotte Hornets" },

            { "CHI Bulls", "Chicago Bulls" },
            { "Chicago Bulls", "Chicago Bulls" },

            { "CLE Cavaliers", "Cleveland Cavaliers" },
            { "Cleveland Cavaliers", "Cleveland Cavaliers" },

            { "DAL Mavericks", "Dallas Mavericks" },
            { "Dallas Mavericks", "Dallas Mavericks" },

            { "DEN Nuggets", "Denver Nuggets" },
            { "Denver Nuggets", "Denver Nuggets" },

            { "DET Pistons", "Detroit Pistons" },
            { "Detroit Pistons", "Detroit Pistons" },

            { "GSW", "Golden State Warriors" },
            { "GS Warriors", "Golden State Warriors" },
            { "Golden State Warriors", "Golden State Warriors" },

            { "HOU Rockets", "Houston Rockets" },
            { "Houston Rockets", "Houston Rockets" },

            { "IND Pacers", "Indiana Pacers" },
            { "Indiana Pacers", "Indiana Pacers" },

            { "LAC", "Los Angeles Clippers" },
            { "LA Clippers", "Los Angeles Clippers" },
            { "Los Angeles Clippers", "Los Angeles Clippers" },

            { "LAL", "Los Angeles Lakers" },
            { "LA Lakers", "Los Angeles Lakers" },
            { "Los Angeles Lakers", "Los Angeles Lakers" },

            { "MEM Grizzlies", "Memphis Grizzlies" },
            { "Memphis Grizzlies", "Memphis Grizzlies" },

            { "MIA Heat", "Miami Heat" },
            { "Miami Heat", "Miami Heat" },

            { "MIL Bucks", "Milwaukee Bucks" },
            { "Milwaukee Bucks", "Milwaukee Bucks" },

            { "MIN Timberwolves", "Minnesota Timberwolves" },
            { "Minnesota Timberwolves", "Minnesota Timberwolves" },

            { "NOP Pelicans", "New Orleans Pelicans" },
            { "New Orleans Pelicans", "New Orleans Pelicans" },

            { "NY Knicks", "New York Knicks" },
            { "New York Knicks", "New York Knicks" },

            { "OKC Thunder", "Oklahoma City Thunder" },
            { "Oklahoma City Thunder", "Oklahoma City Thunder" },

            { "ORL Magic", "Orlando Magic" },
            { "Orlando Magic", "Orlando Magic" },

            { "PHI 76ers", "Philadelphia 76ers" },
            { "Philly 76ers", "Philadelphia 76ers" },
            { "Philadelphia 76ers", "Philadelphia 76ers" },

            { "PHX Suns", "Phoenix Suns" },
            { "Phoenix Suns", "Phoenix Suns" },

            { "POR Trail Blazers", "Portland Trail Blazers" },
            { "Portland Trail Blazers", "Portland Trail Blazers" },

            { "SAC Kings", "Sacramento Kings" },
            { "Sacramento Kings", "Sacramento Kings" },

            { "SAS Spurs", "San Antonio Spurs" },
            { "San Antonio Spurs", "San Antonio Spurs" },

            { "TOR Raptors", "Toronto Raptors" },
            { "Toronto Raptors", "Toronto Raptors" },

            { "UTA Jazz", "Utah Jazz" },
            { "Utah Jazz", "Utah Jazz" },

            { "WAS Wizards", "Washington Wizards" },
            { "Washington Wizards", "Washington Wizards" }
        };

        private static readonly Dictionary<string, string> _cityMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "LA", "Los Angeles" },
            { "NY", "New York" },
            { "SF", "San Francisco" },
        };

        public static string NormalizeCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city)) return city ?? string.Empty;
            return _cityMap.TryGetValue(city, out var normalized) ? normalized : city;
        }
        public static string CanonicalizeTeamName(string name)
        {
            return _canonicalNameMap.TryGetValue(name, out var canonical) ? canonical : name;
        }

        public static HashSet<string> GetActiveTeamNames()
        {
            // Return all unique canonical names (i.e., all 30 official teams)
            return _canonicalNameMap.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
