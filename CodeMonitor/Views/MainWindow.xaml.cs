using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PropertyChanged;
using ReactiveUI;

namespace CodeMonitor.Views
{
    public class dmd
    {
        public string Name => "Test dir";
        public string Status => "Testing";
        public object ProblemGroups => new[]
        {
            ("file2", "msg",1,"tp1"),
            ("file2", "msg",1,"tp1"),
            ("file3", "msg",1,"tp1"),
            ("file3", "msg",1,"tp1"),
            ("file5", "msg",1,"tp1"),
            ("file5", "msg",1,"tp1"),
            ("file5", "msg",1,"tp1")
        }.Select(x => new { File = x.Item1, Message = x.Item2, Line = x.Item3, Type = x.Item4 }).ToList();
    }

    public class dvm
    {
        public object Monitored => new dmd[]
        {
            new dmd()
        };
    }

    [DoNotNotify]
    public class MainWindow : Window
    {
        public static readonly AvaloniaProperty<GridLength> C1WidthProperty = AvaloniaProperty.Register<MainWindow, GridLength>("C1Width", new GridLength(1, GridUnitType.Star));
        public GridLength C1Width { get => GetValue(C1WidthProperty); set => SetValue(C1WidthProperty, value); }

        public static readonly AvaloniaProperty<GridLength> C2WidthProperty = AvaloniaProperty.Register<MainWindow, GridLength>("C2Width", new GridLength(1, GridUnitType.Star));
        public GridLength C2Width { get => GetValue(C2WidthProperty); set => SetValue(C2WidthProperty, value); }

        public static readonly AvaloniaProperty<GridLength> C3WidthProperty = AvaloniaProperty.Register<MainWindow, GridLength>("C3Width", new GridLength(10, GridUnitType.Star));
        public GridLength C3Width { get => GetValue(C3WidthProperty); set => SetValue(C3WidthProperty, value); }

        public ICommand CloseCommand { get; }
        public ICommand MinCommand { get; }
        public ICommand MaxCommand { get; }

        public MainWindow()
        {
            CloseCommand = ReactiveCommand.Create(Close);
            MinCommand = ReactiveCommand.Create(() => WindowState = WindowState.Minimized);
            MaxCommand = ReactiveCommand.Create(() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var mt = this.FindControl<Control>("moveThumb");

             Point? _lastPoint = null;
            var originScren = PixelPoint.Origin;
            var windowStart = PixelPoint.Origin;

            mt.PointerMoved += (o, e) =>
              {
                  if (_lastPoint.HasValue)
                  {
                      var delta = e.GetPosition(this) - _lastPoint.Value;
                      var vectorScreen = this.PointToScreen(new Point(delta.X, delta.Y));
                      Position = new PixelPoint(windowStart.X + vectorScreen.X - originScren.X, windowStart.Y + vectorScreen.Y - originScren.Y);
                  }
              };
            mt.PointerPressed += (o, e) =>
            {
                originScren = this.PointToScreen(new Point(0, 0));
                windowStart = Position;
                e.Pointer.Capture((Control)e.Source);
                e.Handled = true;
                _lastPoint = e.GetPosition(this);
            };

            mt.PointerReleased += (o, e) =>
            {
                if (_lastPoint.HasValue)
                {
                    e.Pointer.Capture(null);
                    e.Handled = true;
                    _lastPoint = null;
                }
            };

            var st = this.FindControl<Control>("sizeThumb");

            var sizeStart = Size.Empty;
            st.PointerMoved += (o, e) =>
              {
                  if (_lastPoint.HasValue)
                  {
                      var delta = e.GetPosition(this) - _lastPoint.Value;

                      ClientSize = sizeStart + new Size(delta.X, delta.Y);
                  }
              };
            st.PointerPressed += (o, e) =>
            {
                sizeStart = ClientSize;
                e.Pointer.Capture((Control)e.Source);
                e.Handled = true;
                _lastPoint = e.GetPosition(this);
            };

            st.PointerReleased += (o, e) =>
            {
                if (_lastPoint.HasValue)
                {
                    e.Pointer.Capture(null);
                    e.Handled = true;
                    _lastPoint = null;
                }
            };
        }
    }
}

