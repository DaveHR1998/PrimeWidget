namespace PrimeWidget.Models;

public record EthiopianDate
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int Day { get; init; }
    public string MonthNameAmharic { get; init; } = string.Empty;
    public string MonthNameEnglish { get; init; } = string.Empty;
    public string DayNameAmharic { get; init; } = string.Empty;
    public string DayNameEnglish { get; init; } = string.Empty;
    public bool IsLeapYear { get; init; }

    public string ToAmharicString(bool includeEra = true) =>
        $"{DayNameAmharic}፣ {MonthNameAmharic} {Day}፣ {Year}{(includeEra ? " ዓ.ም." : "")}";

    public string ToEnglishString(bool includeEra = true) =>
        $"{DayNameEnglish}, {MonthNameEnglish} {Day}, {Year}{(includeEra ? " E.C." : "")}";

    public string ToShortString(bool amharic = true, bool geezNumerals = false)
    {
        string dayStr = geezNumerals ? Helpers.GeezNumberConverter.ToGeez(Day) : Day.ToString();
        string yearStr = geezNumerals ? Helpers.GeezNumberConverter.ToGeez(Year) : Year.ToString();

        return amharic 
            ? (geezNumerals ? $"{MonthNameAmharic} {dayStr}፣ {yearStr}" : $"{MonthNameAmharic} {dayStr}, {yearStr}")
            : $"{MonthNameEnglish} {dayStr}, {yearStr}";
    }
}
