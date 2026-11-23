using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PrimeWidget.Helpers;
using PrimeWidget.Models;
using PrimeWidget.Services;
using PrimeWidget.ViewModels;

namespace PrimeWidget;

public static class DateVerification
{
    public static int PassedCount { get; private set; }
    public static int FailedCount { get; private set; }

    public static void RunTests()
    {
        PassedCount = 0;
        FailedCount = 0;

        // 1. Ethiopian Calendar: Specific historical & modern anchor dates
        Assert("New Year 2019 E.C. (Sep 11, 2026)", () =>
        {
            var eth = EthiopianCalendarConverter.ToEthiopianDate(new DateTime(2026, 9, 11));
            if (eth.Year != 2019 || eth.Month != 1 || eth.Day != 1)
                throw new Exception($"Expected 2019-01-01, got {eth.Year}-{eth.Month}-{eth.Day}");
            if (eth.MonthNameAmharic != "መስከረም")
                throw new Exception($"Expected መስከረም, got {eth.MonthNameAmharic}");
            if (eth.DayNameAmharic != "ዓርብ")
                throw new Exception($"Expected ዓርብ, got {eth.DayNameAmharic}");
            if (eth.MonthNameEnglish != "Meskerem")
                throw new Exception($"Expected Meskerem, got {eth.MonthNameEnglish}");
        });

        Assert("Pagume 5 end of year check (Sep 10, 2026)", () =>
        {
            var eth = EthiopianCalendarConverter.ToEthiopianDate(new DateTime(2026, 9, 10));
            if (eth.Year != 2018 || eth.Month != 13 || eth.Day != 5)
                throw new Exception($"Expected 2018-13-05, got {eth.Year}-{eth.Month}-{eth.Day}");
            if (eth.MonthNameAmharic != "ጳጉሜን" && eth.MonthNameAmharic != "ጳጉሜ")
                throw new Exception($"Expected ጳጉሜን, got {eth.MonthNameAmharic}");
        });

        Assert("Current Date (Oct 25, 2026 = Tikimt 15, 2019 E.C.)", () =>
        {
            var eth = EthiopianCalendarConverter.ToEthiopianDate(new DateTime(2026, 10, 25));
            if (eth.Year != 2019 || eth.Month != 2 || eth.Day != 15)
                throw new Exception($"Expected 2019-02-15, got {eth.Year}-{eth.Month}-{eth.Day}");
            if (eth.MonthNameAmharic != "ጥቅምት")
                throw new Exception($"Expected ጥቅምት, got {eth.MonthNameAmharic}");
            if (eth.DayNameAmharic != "እሑድ")
                throw new Exception($"Expected እሑድ, got {eth.DayNameAmharic}");
        });

        Assert("Traditional Ethiopian 12-Hour Daytime cycle", () =>
        {
            // 6:00 AM -> 12:00
            var t6 = EthiopianCalendarConverter.ToEthiopianTime(new DateTime(2026, 10, 25, 6, 0, 0));
            if (t6.Hour != 12) throw new Exception($"6:00 AM expected 12, got {t6.Hour}");

            // 7:00 AM -> 1:00
            var t7 = EthiopianCalendarConverter.ToEthiopianTime(new DateTime(2026, 10, 25, 7, 0, 0));
            if (t7.Hour != 1) throw new Exception($"7:00 AM expected 1, got {t7.Hour}");

            // 12:00 PM (Noon) -> 6:00
            var t12 = EthiopianCalendarConverter.ToEthiopianTime(new DateTime(2026, 10, 25, 12, 0, 0));
            if (t12.Hour != 6) throw new Exception($"12:00 PM expected 6, got {t12.Hour}");
        });

        // 2. Settings Defaults & 0% Opacity Persistence
        Assert("WidgetSettings Defaults & 0% Opacity Roundtrip", () =>
        {
            var s = new WidgetSettings
            {
                Opacity = 0.0,
                StartWithWindows = true,
                ThemeMode = WidgetThemeMode.DarkGlass,
                Accent = AccentColorTheme.Cyan,
                ViewMode = WidgetViewMode.Combined
            };

            string json = JsonSerializer.Serialize(s);
            var deserialized = JsonSerializer.Deserialize<WidgetSettings>(json);
            if (deserialized == null) throw new Exception("Deserialization returned null");
            if (deserialized.Opacity != 0.0) throw new Exception($"Opacity not preserved: {deserialized.Opacity}");
            if (!deserialized.StartWithWindows) throw new Exception("StartWithWindows not preserved");
            if (deserialized.ThemeMode != WidgetThemeMode.DarkGlass) throw new Exception("ThemeMode not preserved");
        });

        // 3. Theme Manager: 0% Absolute Transparency
        Assert("0% Absolute Transparency: background, border, shadow vanish completely", () =>
        {
            if (Application.Current == null) return;
            ThemeManager.ApplyTheme(WidgetThemeMode.DarkGlass, AccentColorTheme.Monochrome, 0.0);
            var res = Application.Current.Resources;

            var bg = (SolidColorBrush)res["WidgetBackgroundBrush"];
            var border = (SolidColorBrush)res["WidgetBorderBrush"];
            var clockBorder = (SolidColorBrush)res["ClockBorderBrush"];
            var clockFace = (SolidColorBrush)res["ClockFaceBrush"];
            double shadow = (double)res["WidgetShadowOpacity"];

            if (bg.Color.A != 0) throw new Exception($"Background alpha expected 0, got {bg.Color.A}");
            if (border.Color.A != 0) throw new Exception($"Border alpha expected 0, got {border.Color.A}");
            if (clockBorder.Color.A != 0) throw new Exception($"Clock border alpha expected 0, got {clockBorder.Color.A}");
            if (clockFace.Color.A != 0) throw new Exception($"Clock face alpha expected 0, got {clockFace.Color.A}");
            if (shadow != 0.0) throw new Exception($"Shadow opacity expected 0.0, got {shadow}");
        });

        // 4. Autostart Registry Query
        Assert("AutostartService safe query", () =>
        {
            // Just verifying no unhandled exception
            _ = AutostartService.IsAutostartEnabled();
        });

        // 5. Ge'ez Numeral Conversion & Zero-Suppression (፩, ፪, ፫...)
        Assert("Ge'ez Numerals: Zero suppression (0 returns empty)", () =>
        {
            if (GeezNumberConverter.ToGeez(0) != string.Empty)
                throw new Exception("0 should return empty string");
            if (GeezNumberConverter.FormatGeezMinute(0) != string.Empty)
                throw new Exception("Minute 0 should return empty string (no zero)");
        });

        Assert("Ge'ez Numerals: Single-digit minutes without leading zero (5 -> ፭)", () =>
        {
            string min5 = GeezNumberConverter.FormatGeezMinute(5);
            if (min5 != "፭") throw new Exception($"Expected ፭, got {min5}");

            string min1 = GeezNumberConverter.FormatGeezMinute(1);
            if (min1 != "፩") throw new Exception($"Expected ፩, got {min1}");

            string min9 = GeezNumberConverter.FormatGeezMinute(9);
            if (min9 != "፱") throw new Exception($"Expected ፱, got {min9}");
        });

        Assert("Ge'ez Numerals: Double-digit minutes (10 -> ፲, 54 -> ፶፬)", () =>
        {
            string min10 = GeezNumberConverter.FormatGeezMinute(10);
            if (min10 != "፲") throw new Exception($"Expected ፲, got {min10}");

            string min54 = GeezNumberConverter.FormatGeezMinute(54);
            if (min54 != "፶፬") throw new Exception($"Expected ፶፬, got {min54}");

            string min59 = GeezNumberConverter.FormatGeezMinute(59);
            if (min59 != "፶፱") throw new Exception($"Expected ፶፱, got {min59}");
        });

        Assert("Ge'ez Numerals: Ethiopian Calendar Years (2019 -> ፳፻፲፱)", () =>
        {
            string y2019 = GeezNumberConverter.ToGeez(2019);
            if (y2019 != "፳፻፲፱") throw new Exception($"Expected ፳፻፲፱, got {y2019}");

            string y2017 = GeezNumberConverter.ToGeez(2017);
            if (y2017 != "፳፻፲፯") throw new Exception($"Expected ፳፻፲፯, got {y2017}");

            string d16 = GeezNumberConverter.ToGeez(16);
            if (d16 != "፲፮") throw new Exception($"Expected ፲፮, got {d16}");
        });

        Assert("Ge'ez Numerals: EthiopianDate.ToShortString with Ge'ez numbers", () =>
        {
            var eth = new EthiopianDate
            {
                Year = 2019,
                Month = 2,
                Day = 16,
                MonthNameAmharic = "ጥቅምት"
            };
            string shortGeez = eth.ToShortString(amharic: true, geezNumerals: true);
            if (shortGeez != "ጥቅምት ፲፮፣ ፳፻፲፱")
                throw new Exception($"Expected 'ጥቅምት ፲፮፣ ፳፻፲፱', got '{shortGeez}'");
        });

        Assert("Ge'ez Numerals: WidgetSettings NumeralSystem Serialization", () =>
        {
            var s = new WidgetSettings
            {
                Numerals = NumeralSystem.EthiopianGeEz
            };
            string json = JsonSerializer.Serialize(s);
            var deserialized = JsonSerializer.Deserialize<WidgetSettings>(json);
            if (deserialized == null) throw new Exception("Deserialization returned null");
            if (deserialized.Numerals != NumeralSystem.EthiopianGeEz)
                throw new Exception("Numerals setting was not preserved");
        });

        // 6. Contextual 4-period Ethiopian/Amharic time periods (ጠዋት, ከቀኑ, ማታ, ከሌሊቱ)
        Assert("Amharic Time Periods: Contextual 4-period calculation", () =>
        {
            // 8:00 AM -> ጠዋት (Morning)
            var morning = TimeWidgetViewModel.GetAmharicTimePeriod(new DateTime(2026, 10, 26, 8, 0, 0));
            if (morning != "ጠዋት") throw new Exception($"Expected ጠዋት, got {morning}");

            // 2:00 PM (14:00) -> ከቀኑ (Afternoon)
            var afternoon = TimeWidgetViewModel.GetAmharicTimePeriod(new DateTime(2026, 10, 26, 14, 0, 0));
            if (afternoon != "ከቀኑ") throw new Exception($"Expected ከቀኑ, got {afternoon}");

            // 8:00 PM (20:00) -> ማታ (Evening/Night)
            var evening = TimeWidgetViewModel.GetAmharicTimePeriod(new DateTime(2026, 10, 26, 20, 0, 0));
            if (evening != "ማታ") throw new Exception($"Expected ማታ, got {evening}");

            // 2:00 AM -> ከሌሊቱ (Late Night)
            var night = TimeWidgetViewModel.GetAmharicTimePeriod(new DateTime(2026, 10, 26, 2, 0, 0));
            if (night != "ከሌሊቱ") throw new Exception($"Expected ከሌሊቱ, got {night}");
        });

        // 7. Hourly Chime: Quiet hours logic
        Assert("Hourly Chime: Quiet hours calculation across midnight", () =>
        {
            // 22:00 - 07:00
            if (!HourlyChimeService.IsInQuietHours(23, 22, 7)) throw new Exception("23:00 should be in quiet hours");
            if (!HourlyChimeService.IsInQuietHours(3, 22, 7)) throw new Exception("03:00 should be in quiet hours");
            if (HourlyChimeService.IsInQuietHours(12, 22, 7)) throw new Exception("12:00 should NOT be in quiet hours");
        });

        // 8. Hourly Chime: Waveform synthesis and RIFF header
        Assert("Hourly Chime: Procedural audio synthesis for all chime presets", () =>
        {
            foreach (HourlyChimeSound sound in Enum.GetValues<HourlyChimeSound>())
            {
                using var stream = HourlyChimeService.GenerateChimeStream(sound);
                if (stream.Length < 1000) throw new Exception($"Chime stream too short for {sound}: {stream.Length} bytes");
                byte[] header = new byte[4];
                stream.Read(header, 0, 4);
                string riff = System.Text.Encoding.ASCII.GetString(header);
                if (riff != "RIFF") throw new Exception($"Invalid WAV header: expected RIFF, got {riff}");
            }
        });

        // 9. New Feature Settings Serialization
        Assert("Settings: Serialization of Backdrop, Scale, Chime, and Focus settings", () =>
        {
            var s = new WidgetSettings
            {
                Backdrop = BackdropStyle.Acrylic,
                WidgetScale = 1.25,
                IsClickThrough = true,
                EnableHourlyChime = true,
                ChimeSound = HourlyChimeSound.CrystalDigital,
                ShowWorldTimezone = true
            };
            string json = JsonSerializer.Serialize(s);
            var d = JsonSerializer.Deserialize<WidgetSettings>(json);
            if (d == null) throw new Exception("Deserialization failed");
            if (d.Backdrop != BackdropStyle.Acrylic) throw new Exception("Backdrop mismatch");
            if (Math.Abs(d.WidgetScale - 1.25) > 0.001) throw new Exception("WidgetScale mismatch");
            if (!d.IsClickThrough) throw new Exception("IsClickThrough mismatch");
            if (!d.EnableHourlyChime) throw new Exception("EnableHourlyChime mismatch");
            if (d.ChimeSound != HourlyChimeSound.CrystalDigital) throw new Exception("ChimeSound mismatch");
            if (!d.ShowWorldTimezone) throw new Exception("ShowWorldTimezone mismatch");
        });
    }

    private static void Assert(string testName, Action action)
    {
        try
        {
            action();
            PassedCount++;
            System.Diagnostics.Debug.WriteLine($"[PASS] {testName}");
        }
        catch (Exception ex)
        {
            FailedCount++;
            System.Diagnostics.Debug.WriteLine($"[FAIL] {testName}: {ex.Message}");
            throw;
        }
    }
}
