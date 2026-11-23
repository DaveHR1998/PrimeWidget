using System.Text;

namespace PrimeWidget.Helpers;

public static class GeezNumberConverter
{
    private static readonly string[] Ones =
    [
        "", "፩", "፪", "፫", "፬", "፭", "፮", "፯", "፰", "፱"
    ];

    private static readonly string[] Tens =
    [
        "", "፲", "፳", "፴", "፵", "፶", "፷", "፸", "፹", "፺"
    ];

    /// <summary>
    /// Converts a positive integer (1 - 9999) to traditional Ge'ez numerals.
    /// If number is 0 or negative, returns empty string (no zero in Ge'ez).
    /// </summary>
    public static string ToGeez(int number)
    {
        if (number <= 0)
        {
            return string.Empty;
        }

        if (number > 9999)
        {
            return number.ToString();
        }

        var sb = new StringBuilder();

        int hundreds = number / 100;
        int rem = number % 100;

        if (hundreds > 0)
        {
            if (hundreds == 1)
            {
                sb.Append('፻');
            }
            else
            {
                sb.Append(ConvertUnder100(hundreds));
                sb.Append('፻');
            }
        }

        if (rem > 0)
        {
            sb.Append(ConvertUnder100(rem));
        }

        return sb.ToString();
    }

    private static string ConvertUnder100(int num)
    {
        if (num <= 0) return string.Empty;
        int tens = num / 10;
        int ones = num % 10;
        return Tens[tens] + Ones[ones];
    }

    /// <summary>
    /// Formats a minute value (0 - 59) in Ge'ez.
    /// In case of 0, returns empty string.
    /// For single digits (1 - 9), returns the Ge'ez numeral directly without leading zero (e.g. 5 -> ፭).
    /// </summary>
    public static string FormatGeezMinute(int minute)
    {
        if (minute <= 0 || minute > 59)
        {
            return string.Empty;
        }

        return ConvertUnder100(minute);
    }

    /// <summary>
    /// Formats an hour value in Ge'ez (1 - 12 or 1 - 24).
    /// If 0 (e.g. midnight in 24h), returns ፲፪ (12) or empty.
    /// </summary>
    public static string FormatGeezHour(int hour)
    {
        if (hour <= 0)
        {
            return string.Empty;
        }

        return ToGeez(hour);
    }
}
