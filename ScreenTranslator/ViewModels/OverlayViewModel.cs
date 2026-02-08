using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenTranslator.Models;

namespace ScreenTranslator.ViewModels;

public partial class OverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private double _selectionX;

    [ObservableProperty]
    private double _selectionY;

    [ObservableProperty]
    private double _selectionWidth;

    [ObservableProperty]
    private double _selectionHeight;

    [ObservableProperty]
    private bool _isSelecting;

    private double _startX, _startY;

    public ScreenRegion? SelectedRegion { get; private set; }
    public bool Confirmed { get; private set; }

    public double DpiX { get; set; } = 1.0;
    public double DpiY { get; set; } = 1.0;

    public void StartSelection(double x, double y)
    {
        _startX = x;
        _startY = y;
        SelectionX = x;
        SelectionY = y;
        SelectionWidth = 0;
        SelectionHeight = 0;
        IsSelecting = true;
    }

    public void UpdateSelection(double x, double y)
    {
        if (!IsSelecting) return;

        SelectionX = Math.Min(_startX, x);
        SelectionY = Math.Min(_startY, y);
        SelectionWidth = Math.Abs(x - _startX);
        SelectionHeight = Math.Abs(y - _startY);
    }

    public void EndSelection(double x, double y)
    {
        if (!IsSelecting) return;

        UpdateSelection(x, y);
        IsSelecting = false;

        if (SelectionWidth > 5 && SelectionHeight > 5)
        {
            SelectedRegion = new ScreenRegion
            {
                X = (int)(SelectionX * DpiX),
                Y = (int)(SelectionY * DpiY),
                Width = (int)(SelectionWidth * DpiX),
                Height = (int)(SelectionHeight * DpiY)
            };
            Confirmed = true;
        }
    }

    public void Cancel()
    {
        IsSelecting = false;
        Confirmed = false;
        SelectedRegion = null;
    }
}
