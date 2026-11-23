using System;
using PrimeWidget.Models;

namespace PrimeWidget.Services;

public static class EthiopianCalendarConverter
{
    private static readonly string[] AmharicMonths =
    [
        "መስከረም", "ጥቅምት", "ኅዳር", "ታኅሣሥ", "ጥር", "የካቲት",
        "መጋቢት", "ሚያዝያ", "ግንቦት", "ሰኔ", "ሐምሌ", "ነሐሴ", "ጳጉሜን"
    ];

    private static readonly string[] EnglishMonths =
    [
        "Meskerem", "Tikimt", "Hidar", "Tahsas", "Tir", "Yakatit",
        "Magabit", "Miyazya", "Ginbot", "Sene", "Hamle", "Nehase", "Pagume"
    ];

    private static readonly string[] AmharicDays =
    [
        "እሑድ", "ሰኞ", "ማክሰኞ", "ረቡዕ", "ሐሙስ", "ዓርብ", "ቅዳሜ"
    ];

    private static readonly string[] EnglishDays =
    [
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    ];

    /// <summary>
    /// Converts a Gregorian DateTime into an EthiopianDate.
    /// </summary>
    public static EthiopianDate ToEthiopianDate(DateTime gregorian)
    {
        int jdn = GregorianToJdn(gregorian.Year, gregorian.Month, gregorian.Day);
        return JdnToEthiopian(jdn, (int)gregorian.DayOfWeek);
    }

    /// <summary>
    /// Calculates Gregorian date to Julian Day Number (JDN).
    /// </summary>
    private static int GregorianToJdn(int year, int month, int day)
    {
        int a = (14 - month) / 12;
        int y = year + 4800 - a;
        int m = month + 12 * a - 3;
        return day + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;
    }

    /// <summary>
    /// Converts Julian Day Number (JDN) to Ethiopian date.
    /// </summary>
    private static EthiopianDate JdnToEthiopian(int jdn, int dayOfWeekIndex)
    {
        int r = (jdn - 1723856) % 1461;
        int n = (r % 365) + 365 * (r / 1460);
        int year = 4 * ((jdn - 1723856) / 1461) + (r / 365) - (r / 1460);
        int month = (n / 30) + 1;
        int day = (n % 30) + 1;

        bool isLeap = year % 4 == 3;
        int monthIdx = Math.Clamp(month - 1, 0, 12);
        int dayIdx = Math.Clamp(dayOfWeekIndex, 0, 6);

        return new EthiopianDate
        {
            Year = year,
            Month = month,
            Day = day,
            MonthNameAmharic = AmharicMonths[monthIdx],
            MonthNameEnglish = EnglishMonths[monthIdx],
            DayNameAmharic = AmharicDays[dayIdx],
            DayNameEnglish = EnglishDays[dayIdx],
            IsLeapYear = isLeap
        };
    }

    /// <summary>
    /// Converts a 24-hour time to traditional Ethiopian 12-hour cycle.
    /// Ethiopian daytime starts at 6:00 AM (1:00 day / 1 ሰዓት ከቀኑ),
    /// and nighttime starts at 6:00 PM (1:00 night / 1 ሰዓት ከምሽቱ).
    /// </summary>
    public static (int Hour, int Minute, int Second, string PeriodAmharic, string PeriodEnglish) ToEthiopianTime(DateTime dt)
    {
        int h = dt.Hour;
        int ethHour = (h + 6) % 12;
        if (ethHour == 0) ethHour = 12;

        bool isDaytime = h >= 6 && h < 18;
        string amharicPeriod = isDaytime ? "ከቀኑ" : "ከምሽቱ";
        string englishPeriod = isDaytime ? "Day" : "Night";

        return (ethHour, dt.Minute, dt.Second, amharicPeriod, englishPeriod);
    }
}
