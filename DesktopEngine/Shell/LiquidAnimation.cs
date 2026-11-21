using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Backdrop.NET.Shell;

/// <summary>
/// The shell's one show/hide animation: the element grows out of an anchor point like a drop of liquid,
/// overshooting and wobbling before it settles, with width and height slightly out of step so it
/// squashes and stretches. Hiding pulls it back into the same point.
/// </summary>
public static class LiquidAnimation {

    /// <summary>Anchor for things that rise from the bottom edge (the bar, the Start menu).</summary>
    public static readonly Point FromBottom = new(0.5, 1.0);

    /// <summary>Anchor for things that pour out of their right-hand side.</summary>
    public static readonly Point FromRight = new(1.0, 0.5);

    private static readonly Duration FadeIn = TimeSpan.FromMilliseconds(140);
    private static readonly Duration FadeOut = TimeSpan.FromMilliseconds(180);
    private static readonly Duration Retract = TimeSpan.FromMilliseconds(240);

    public static void Show(FrameworkElement element, Point origin, TimeSpan delay = default) {
        ScaleTransform scale = GetScale(element, origin);

        if (element.Visibility != Visibility.Visible) {
            // Start as a small, wide puddle at the anchor
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            element.BeginAnimation(UIElement.OpacityProperty, null);
            scale.ScaleX = origin.X is 0.0 or 1.0 ? 0.0 : 0.55;
            scale.ScaleY = origin.Y is 0.0 or 1.0 ? 0.0 : 0.55;
            element.Opacity = 0.0;
            element.Visibility = Visibility.Visible;
        }

        // The axis that grows from the edge wobbles longest; the other settles sooner, out of phase
        bool vertical = origin.Y is 0.0 or 1.0;
        DoubleAnimation main = Spring(1.0, 720, 2, 4.5, delay);
        DoubleAnimation cross = Spring(1.0, 560, 1, 3.5, delay);

        scale.BeginAnimation(vertical ? ScaleTransform.ScaleYProperty : ScaleTransform.ScaleXProperty, main);
        scale.BeginAnimation(vertical ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty, cross);
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, FadeIn) { BeginTime = delay });
    }

    /// <summary>
    /// Plays the hide animation, then sets the element to <see cref="Visibility.Hidden"/> (keeping its
    /// layout slot) and calls <paramref name="completed"/>. Nothing happens if it was shown again meanwhile.
    /// </summary>
    public static void Hide(FrameworkElement element, Point origin, TimeSpan delay = default, Action? completed = null) {
        ScaleTransform scale = GetScale(element, origin);
        bool vertical = origin.Y is 0.0 or 1.0;

        // BackEase: bulges a little past its size first, then gets sucked back into the anchor
        DoubleAnimation main = new(0.0, Retract) {
            BeginTime = delay,
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.45 }
        };
        DoubleAnimation cross = new(0.6, Retract) {
            BeginTime = delay,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        DoubleAnimation fade = new(0.0, FadeOut) {
            BeginTime = delay + TimeSpan.FromMilliseconds(60)
        };

        fade.Completed += (_, _) => {
            // A Show() that started in the meantime has replaced this animation and raised the opacity again
            if (element.Opacity > 0.01) {
                return;
            }

            element.Visibility = Visibility.Hidden;
            completed?.Invoke();
        };

        scale.BeginAnimation(vertical ? ScaleTransform.ScaleYProperty : ScaleTransform.ScaleXProperty, main);
        scale.BeginAnimation(vertical ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty, cross);
        element.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    /// <summary>Stops any animation and leaves the element fully shown.</summary>
    public static void Reset(FrameworkElement element) {
        if (element.RenderTransform is ScaleTransform scale && !scale.IsFrozen) {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;
        }

        element.BeginAnimation(UIElement.OpacityProperty, null);
        element.Opacity = 1.0;
        element.Visibility = Visibility.Visible;
    }

    private static DoubleAnimation Spring(double to, double milliseconds, int oscillations, double springiness, TimeSpan delay) {
        return new DoubleAnimation(to, TimeSpan.FromMilliseconds(milliseconds)) {
            BeginTime = delay,
            EasingFunction = new ElasticEase {
                EasingMode = EasingMode.EaseOut,
                Oscillations = oscillations,
                Springiness = springiness
            }
        };
    }

    private static ScaleTransform GetScale(FrameworkElement element, Point origin) {
        element.RenderTransformOrigin = origin;

        if (element.RenderTransform is ScaleTransform existing && !existing.IsFrozen) {
            return existing;
        }

        ScaleTransform scale = new(1.0, 1.0);
        element.RenderTransform = scale;
        return scale;
    }

}