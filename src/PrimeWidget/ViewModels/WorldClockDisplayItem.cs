using System;

namespace PrimeWidget.ViewModels;

public class WorldClockDisplayItem : ObservableObject
{
    private string _timeZoneId = string.Empty;
    private string _displayLabel = string.Empty;
    private string _flagEmoji = string.Empty;
    private string _timeString = string.Empty;
    private string _periodString = string.Empty;

    public string TimeZoneId
    {
        get => _timeZoneId;
        set => SetProperty(ref _timeZoneId, value);
    }

    public string DisplayLabel
    {
        get => _displayLabel;
        set => SetProperty(ref _displayLabel, value);
    }

    public string FlagEmoji
    {
        get => _flagEmoji;
        set => SetProperty(ref _flagEmoji, value);
    }

    public string TimeString
    {
        get => _timeString;
        set => SetProperty(ref _timeString, value);
    }

    public string PeriodString
    {
        get => _periodString;
        set => SetProperty(ref _periodString, value);
    }
}
