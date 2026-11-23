using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PrimeWidget.Models;
using PrimeWidget.ViewModels;

namespace PrimeWidget.Views.Controls;

public partial class AnalogClockControl : UserControl
{
    private TimeWidgetViewModel? _subscribedVm;

    public AnalogClockControl()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            AttachViewModel(DataContext as TimeWidgetViewModel);
            DrawClockDial();
        };
        Unloaded += (s, e) => DetachViewModel();
        DataContextChanged += (s, e) =>
        {
            AttachViewModel(e.NewValue as TimeWidgetViewModel);
            DrawClockDial();
        };
    }

    private void AttachViewModel(TimeWidgetViewModel? vm)
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _subscribedVm = vm;

        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void DetachViewModel()
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimeWidgetViewModel.DialStyle))
        {
            Dispatcher.Invoke(DrawClockDial);
        }
    }

    public void DrawClockDial()
    {
        TicksCanvas.Children.Clear();

        double centerX = 90;
        double centerY = 90;
        double radius = 76;

        var vm = DataContext as TimeWidgetViewModel;
        bool hasLabels = vm != null && vm.DialStyle != AnalogDialStyle.Minimalist;

        // Draw 12 hour positions
        for (int i = 0; i < 12; i++)
        {
            double angleDeg = i * 30.0;
            double angleRad = (angleDeg - 90) * Math.PI / 180.0;
            bool isCardinal = i % 3 == 0; // 12, 3, 6, 9

            if (isCardinal && hasLabels)
            {
                // When labels (Ge'ez or Roman or Digits) are displayed, omit the cardinal tick lines so they don't overlap
                continue;
            }

            if (isCardinal)
            {
                // Clean dash tick
                double tickLen = 7.0;
                double startX = centerX + (radius - tickLen) * Math.Cos(angleRad);
                double startY = centerY + (radius - tickLen) * Math.Sin(angleRad);
                double endX = centerX + radius * Math.Cos(angleRad);
                double endY = centerY + radius * Math.Sin(angleRad);

                var line = new Line
                {
                    X1 = startX,
                    Y1 = startY,
                    X2 = endX,
                    Y2 = endY,
                    StrokeThickness = 2.0,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                line.SetResourceReference(Shape.StrokeProperty, "ClockTicksBrush");
                TicksCanvas.Children.Add(line);
            }
            else
            {
                // Subtle dot for other hours
                double dotRadius = 1.6;
                double dotDist = radius - 3.0;
                double dotX = centerX + dotDist * Math.Cos(angleRad) - dotRadius;
                double dotY = centerY + dotDist * Math.Sin(angleRad) - dotRadius;

                var ellipse = new Ellipse
                {
                    Width = dotRadius * 2,
                    Height = dotRadius * 2
                };
                ellipse.SetResourceReference(Shape.FillProperty, "ClockTicksBrush");
                Canvas.SetLeft(ellipse, dotX);
                Canvas.SetTop(ellipse, dotY);
                TicksCanvas.Children.Add(ellipse);
            }
        }
    }
}
