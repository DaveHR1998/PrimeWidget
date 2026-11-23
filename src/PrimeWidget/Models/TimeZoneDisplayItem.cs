using System;

namespace PrimeWidget.Models;

public class TimeZoneDisplayItem
{
    public string Id { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UtcOffsetString { get; set; } = string.Empty;
    public string SearchableText { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = "🌍";

    public TimeZoneDisplayItem() { }

    public TimeZoneDisplayItem(TimeZoneInfo tz)
    {
        Id = tz.Id;
        DisplayName = tz.DisplayName;

        TimeSpan offset = tz.BaseUtcOffset;
        string sign = offset >= TimeSpan.Zero ? "+" : "-";
        UtcOffsetString = $"UTC{sign}{Math.Abs(offset.Hours):D2}:{Math.Abs(offset.Minutes):D2}";

        CityName = ExtractCityOrFriendlyName(tz);
        FlagEmoji = GuessFlagEmoji(tz.Id, CityName);

        SearchableText = $"{CityName} {DisplayName} {Id} {UtcOffsetString}".ToLowerInvariant();
    }

    public static string ExtractCityOrFriendlyName(TimeZoneInfo tz)
    {
        if (tz.Id == "E. Africa Standard Time")
        {
            return "Addis Ababa, Nairobi";
        }

        string name = tz.DisplayName;
        int closeParen = name.IndexOf(')');
        if (closeParen >= 0 && closeParen < name.Length - 1)
        {
            string cities = name[(closeParen + 1)..].Trim();
            return cities;
        }

        return tz.StandardName;
    }

    public static string GuessFlagEmoji(string tzId, string cityName = "")
    {
        string combined = $"{tzId} {cityName}".ToLowerInvariant();

        if (combined.Contains("addis") || combined.Contains("ethiopia")) return "🇪🇹";
        if (combined.Contains("nairobi") || combined.Contains("kenya")) return "🇰🇪";
        if (combined.Contains("new york") || combined.Contains("eastern standard") || combined.Contains("pacific") || combined.Contains("central standard time") || combined.Contains("mountain standard")) return "🇺🇸";
        if (combined.Contains("london") || combined.Contains("gmt standard") || combined.Contains("dublin") || combined.Contains("edinburgh")) return "🇬🇧";
        if (combined.Contains("dubai") || combined.Contains("arabian standard") || combined.Contains("abu dhabi")) return "🇦🇪";
        if (combined.Contains("tokyo") || combined.Contains("japan standard")) return "🇯🇵";
        if (combined.Contains("paris") || combined.Contains("berlin") || combined.Contains("rome") || combined.Contains("madrid") || combined.Contains("amsterdam") || combined.Contains("w. europe") || combined.Contains("romance") || combined.Contains("central europe")) return "🇪🇺";
        if (combined.Contains("beijing") || combined.Contains("china standard") || combined.Contains("shanghai") || combined.Contains("hong kong")) return "🇨🇳";
        if (combined.Contains("india standard") || combined.Contains("delhi") || combined.Contains("mumbai") || combined.Contains("kolkata")) return "🇮🇳";
        if (combined.Contains("cairo") || combined.Contains("egypt")) return "🇪🇬";
        if (combined.Contains("singapore")) return "🇸🇬";
        if (combined.Contains("seoul") || combined.Contains("korea")) return "🇰🇷";
        if (combined.Contains("sydney") || combined.Contains("melbourne") || combined.Contains("aus central") || combined.Contains("aus eastern")) return "🇦🇺";
        if (combined.Contains("toronto") || combined.Contains("vancouver") || combined.Contains("canada")) return "🇨🇦";
        if (combined.Contains("johannesburg") || combined.Contains("south africa")) return "🇿🇦";
        if (combined.Contains("moscow") || combined.Contains("russian")) return "🇷🇺";
        if (combined.Contains("brazil") || combined.Contains("sao paulo") || combined.Contains("brasilia")) return "🇧🇷";
        if (combined.Contains("mexico")) return "🇲🇽";
        if (combined.Contains("riyadh") || combined.Contains("saudi")) return "🇸🇦";
        if (combined.Contains("bangkok") || combined.Contains("thailand")) return "🇹🇭";

        if (combined.Contains("africa")) return "🇪🇹";
        return "🌍";
    }
}
