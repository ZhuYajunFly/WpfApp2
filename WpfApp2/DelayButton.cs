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
          => ((DelayButton)d).UpdateeProgressAnimation();

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
          => ((DelayButton)d).UpdateeProgressAnimation();

        private CancellationTokenSource? delayCancellationTokenSource;
        private bool isClickValid;
        private bool isExecutingKeyAction;
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
            UpdateeProgressAnimation();
        }

        private void UpdateeProgressAnimation()
        {
            if (this.progressStoryBoard.IsFrozen)
            {
                this.progressStoryBoard = this.progressStoryBoard.Clone();
            }

            var delayDuration = TimeSpan.FromMilliseconds(this.DelayInMilliseconds);
            var progressAnimation = new DoubleAnimation(0, 100, new Duration(delayDuration), FillBehavior.HoldEnd);
            Storyboard.SetTargetProperty(progressAnimation, new PropertyPath(ProgressBar.ValueProperty));
            this.progressStoryBoard.Children.Add(progressAnimation);

            var colorAnimation = new ColorAnimation(this.ProgressBrush.Color, Colors.Green, new Duration(delayDuration), FillBehavior.HoldEnd);
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
            try
            {
                if (this.ClickMode is ClickMode.Hover)
                {
                    this.delayCancellationTokenSource = new CancellationTokenSource();
                    await DelayActionAsync(this.delayCancellationTokenSource.Token);
                }

                base.OnMouseEnter(e);
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

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            this.delayCancellationTokenSource?.Cancel();
            StopProgressAnimation();
            base.OnMouseLeave(e);
        }

        protected override async void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key is Key.Enter or Key.Space)
            {
                if (this.isExecutingKeyAction)
                {
                    return;
                }

                this.isExecutingKeyAction = true;

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

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key is Key.Enter or Key.Space)
            {
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
                this.isExecutingKeyAction = false;
            }

            base.OnKeyUp(e);
        }

        private async Task DelayActionAsync(CancellationToken cancellationToken)
        {
            this.delayCancellationTokenSource = new CancellationTokenSource();
            var delayDuration = TimeSpan.FromMilliseconds(this.DelayInMilliseconds);
            StartProgressAnimation();
            await Task.Delay(delayDuration, cancellationToken);
            this.isClickValid = true;
        }

        private void StartProgressAnimation()
          => this.part_ProgressBar?.BeginStoryboard(this.progressStoryBoard, HandoffBehavior.SnapshotAndReplace, true);

        private void StopProgressAnimation()
        {
            this.progressStoryBoard.Stop();
            this.progressStoryBoard.Remove(this.part_ProgressBar);
        }
    }
}