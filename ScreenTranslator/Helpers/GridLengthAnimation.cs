using System.Windows;
using System.Windows.Media.Animation;

namespace ScreenTranslator.Helpers;

public class GridLengthAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public override Type TargetPropertyType => typeof(GridLength);

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue,
        AnimationClock animationClock)
    {
        var from = From;
        var to = To;
        var progress = animationClock.CurrentProgress ?? 0.0;

        // EaseInOut cubic
        progress = progress < 0.5
            ? 4 * progress * progress * progress
            : 1 - Math.Pow(-2 * progress + 2, 3) / 2;

        var fromVal = from.Value;
        var toVal = to.Value;
        var current = fromVal + (toVal - fromVal) * progress;

        return from.IsStar || to.IsStar
            ? new GridLength(current, GridUnitType.Star)
            : new GridLength(current, GridUnitType.Pixel);
    }
}
