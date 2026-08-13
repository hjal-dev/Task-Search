using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using TaskSearch.Logging;

namespace TaskSearch.Eft
{
    internal static class EftTraders
    {
        internal static void CollectNames(string traderId, ICollection<string> into)
        {
            if (string.IsNullOrEmpty(traderId) || into == null)
            {
                return;
            }

            try
            {
                if (!Singleton<GlobalConfiguration>.Instantiated)
                {
                    return;
                }

                GlobalConfiguration settings = Singleton<GlobalConfiguration>.Instance;

                if (settings?.TradersSettings == null)
                {
                    return;
                }

                if (!settings.TradersSettings.TryGetValue(traderId, out GlobalConfiguration.TraderSettings trader)
                    || trader == null)
                {
                    return;
                }

                AddLocalized(trader.Nickname, into);
                AddLocalized(trader.FullName, into);
                AddLocalized(trader.FirstName, into);
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "trader:" + traderId,
                    $"Could not resolve trader '{traderId}': {ex.Message}");
            }
        }

        private static void AddLocalized(string localeKey, ICollection<string> into)
        {
            if (EftLocalization.TryLocalize(localeKey, out string value))
            {
                into.Add(value);
            }
        }
    }
}
