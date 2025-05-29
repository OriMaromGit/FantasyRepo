namespace FantasyNBA.Models
{
    public static class PlayerExtensions
    {
        public static bool ApplyUpdatesIfChanged(this Player target, Player source)
        {
            bool changed = false;

            void UpdateIfDifferent<T>(Func<Player, T> selector, Action<Player, T> updater)
            {
                var oldValue = selector(target);
                var newValue = selector(source);

                if (!Equals(oldValue, newValue))
                {
                    updater(target, newValue);
                    changed = true;
                }
            }

            UpdateIfDifferent(p => p.FirstName, (p, v) => p.FirstName = v);
            UpdateIfDifferent(p => p.LastName, (p, v) => p.LastName = v);
            UpdateIfDifferent(p => p.Position, (p, v) => p.Position = v);
            UpdateIfDifferent(p => p.Height, (p, v) => p.Height = v);
            UpdateIfDifferent(p => p.Weight, (p, v) => p.Weight = v);
            UpdateIfDifferent(p => p.College, (p, v) => p.College = v);
            UpdateIfDifferent(p => p.Country, (p, v) => p.Country = v);
            UpdateIfDifferent(p => p.CurrentTeamId, (p, v) => p.CurrentTeamId = v);
            UpdateIfDifferent(p => p.IsActive, (p, v) => p.IsActive = v);
            UpdateIfDifferent(p => p.NbaStartYear, (p, v) => p.NbaStartYear = v);
            UpdateIfDifferent(p => p.JerseyNumber, (p, v) => p.JerseyNumber = v);
            UpdateIfDifferent(p => p.ExternalApiDataJson, (p, v) => p.ExternalApiDataJson = v);

            return changed;
        }
    }
}
