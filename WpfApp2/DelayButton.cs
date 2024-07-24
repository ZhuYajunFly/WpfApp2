using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfApp2
{
    public class DelayButton : Button
    {
        public int DelayInMilliseconds
        {
            get => (int)GetValue(DelayInMillisecondsProperty);
            set => SetValue(DelayInMillisecondsProperty, value);
        }

        public static readonly DependencyProperty DelayInMillisecondsProperty = DependencyProperty.Register(
          nameof(DelayInMilliseconds),
          typeof(int),
          typeof(DelayButton),
          new PropertyMetadata(0, OnDelayInMillisecondsChanged));

        private static void OnDelayInMillisecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
          => ((DelayButton)d).UpdateProgressAnimation();

        public SolidColorBrush ProgressBrush
        {
            get => (SolidColorBrush)GetValue(ProgressBrushProperty);
            set => SetValue(ProgressBrushProperty, value);
        }

        public static readonly DependencyProperty ProgressBrushProperty = DependencyProperty.Register(
          nameof(ProgressBrush),
          typeof(SolidColorBrush),
          typeof(DelayButton),
          new PropertyMetadata(Brushes.DarkRed, OnProgressBrushChanged));

        private static void OnProgressBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
          => ((DelayButton)d).UpdateProgressAnimation();

        private CancellationTokenSource? delayCancellationTokenSource;
        private bool isClickValid;
        private bool IsExecutingKeyAction => Keyboard.IsKeyDown(Key.Space) || Keyboard.IsKeyDown(Key.End);
        private bool IsExecutingMouseAction => Mouse.LeftButton is MouseButtonState.Pressed;
        private int reentrancyCounter;
        private ProgressBar part_ProgressBar;
        private Storyboard progressStoryBoard;

        static DelayButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DelayButton), new FrameworkPropertyMetadata(typeof(DelayButton)));
        }

        public DelayButton()
        {
            this.ClickMode = ClickMode.Press;
            this.progressStoryBoard = new Storyboard()
            {
                FillBehavior = FillBehavior.HoldEnd,
            };
            UpdateProgressAnimation();
        }

        private void UpdateProgressAnimation()
        {
            if (this.progressStoryBoard.IsFrozen)
            {
                this.progressStoryBoard = this.progressStoryBoard.Clone();
            }

            var delayDuration = TimeSpan.FromMilliseconds(this.DelayInMilliseconds);
            var progressAnimation = new DoubleAnimation(0, 100, new Duration(delayDuration), FillBehavior.HoldEnd);
            Storyboard.SetTargetName(progressAnimation, "PART_ProgressBar");
            Storyboard.SetTargetProperty(progressAnimation, new PropertyPath(ProgressBar.ValueProperty));
            this.progressStoryBoard.Children.Add(progressAnimation);

            var colorAnimation = new ColorAnimation(this.ProgressBrush.Color, Colors.Green, new Duration(delayDuration), FillBehavior.HoldEnd);
            Storyboard.SetTargetName(colorAnimation, "PART_ProgressBar");
            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(0).(1)", Control.ForegroundProperty, SolidColorBrush.ColorProperty));
            this.progressStoryBoard.Children.Add(colorAnimation);

            this.progressStoryBoard.Freeze();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.part_ProgressBar = GetTemplateChild("PART_ProgressBar") as ProgressBar;
        }

        protected override async void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.IsExecutingKeyAction || this.ClickMode is ClickMode.Hover)
            {
                return;
            }

            try
            {
                this.delayCancellationTokenSource = new CancellationTokenSource();
                await DelayActionAsync(this.delayCancellationTokenSource.Token);
                base.OnMouseLeftButtonDown(e);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                this.delayCancellationTokenSource?.Dispose();
                this.delayCancellationTokenSource = null;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (this.IsExecutingKeyAction || this.ClickMode is ClickMode.Hover)
            {
                return;
            }

            this.delayCancellationTokenSource?.Cancel();
            StopProgressAnimation();

            if (this.ClickMode is ClickMode.Release)
            {
                if (!this.isClickValid)
                {
                    return;
                }
            }

            base.OnMouseLeftButtonUp(e);
            this.isClickValid = false;
        }

        protected override async void OnMouseEnter(MouseEventArgs e)
        {
            if (this.IsExecutingKeyAction)
            {
                return;
            }

            if (this.ClickMode is ClickMode.Hover)
            {
                try
                {
                    this.delayCancellationTokenSource = new CancellationTokenSource();
                    await DelayActionAsync(this.delayCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    this.delayCancellationTokenSource?.Dispose();
                    this.delayCancellationTokenSource = null;
                }
            }

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (this.IsExecutingKeyAction)
            {
                return;
            }

            if (this.ClickMode is ClickMode.Hover)
            {
                this.delayCancellationTokenSource?.Cancel();
                StopProgressAnimation();
            }

            base.OnMouseLeave(e);
        }

        protected override async void OnKeyDown(KeyEventArgs e)
        {
            if (this.ClickMode is ClickMode.Hover)
            {
                return;
            }

            if (e.Key is Key.Enter or Key.Space)
            {
                if (this.IsExecutingMouseAction || this.reentrancyCounter > 0)
                {
                    return;
                }

                try
                {
                    this.reentrancyCounter++;
                    this.delayCancellationTokenSource = new CancellationTokenSource();
                    await DelayActionAsync(this.delayCancellationTokenSource.Token);

                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    this.delayCancellationTokenSource?.Dispose();
                    this.delayCancellationTokenSource = null;
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (this.ClickMode is ClickMode.Hover)
            {
                return;
            }

            if (e.Key is Key.Enter or Key.Space)
            {
                if (this.IsExecutingMouseAction)
                {
                    return;
                }

                this.delayCancellationTokenSource?.Cancel();
                StopProgressAnimation();

                if (this.ClickMode is ClickMode.Release)
                {
                    if (!this.isClickValid)
                    {
                        return;
                    }
                }

                this.isClickValid = false;
            }

            base.OnKeyUp(e);
            this.reentrancyCounter--;
        }

        private async Task DelayActionAsync(CancellationToken cancellationToken)
        {
            var delayDuration = TimeSpan.FromMilliseconds(this.DelayInMilliseconds);
            cancellationToken.ThrowIfCancellationRequested();
            StartProgressAnimation();
            await Task.Delay(delayDuration, cancellationToken);
            this.isClickValid = true;
        }

        private void StartProgressAnimation()
        {
            if (this.part_ProgressBar is null)
            {
                return;
            }

            this.progressStoryBoard.Begin(this.part_ProgressBar, isControllable: true);
        }

        private void StopProgressAnimation()
        {
            if (this.part_ProgressBar is null)
            {
                return;
            }

            this.progressStoryBoard.Stop(this.part_ProgressBar);
            this.progressStoryBoard.Remove(this.part_ProgressBar);
        }
    }
}