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
    public class TestData
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
        }.Select(x => new { File = x.Item1, Message = x.Item2, Line = x.Item3, Type = x.Item4 })
         .ToLookup(x => x.File)
         .Select(x => new { Group = x.Key, Problems = x.Select(x => new { x.Message, x.Line, x.Type }) }).ToList();
        public object FilesToClean => Enumerable.Range(0,100).Select(x=>new { Path = "file-number-" + x }).ToArray();
        public ReactiveCommand<Unit,Unit> CleanFiles => ReactiveCommand.Create(delegate{ });
        public ReactiveCommand<Unit,Unit> ResetCleanFiles => ReactiveCommand.Create(delegate{ });
    }

    public class TestMainVM
    {
        public object Monitored => new TestData[]
        {
            new TestData()
        };
    }

    [DoNotNotify]
    public class MainWindow : Window
    {
        public static readonly AvaloniaProperty<GridLength> C1WidthProperty = AvaloniaProperty.Register<MainWindow, GridLength>("C1Width", new GridLength(1, GridUnitType.Star));
        public GridLength C1Width { get => (GridLength)GetValue(C1WidthProperty); set => SetValue(C1WidthProperty, value); }

        public static readonly AvaloniaProperty<GridLength> C2WidthProperty = AvaloniaProperty.Register<MainWindow, GridLength>("C2Width", new GridLength(1, GridUnitType.Star));
        public GridLength C2Width { get => (GridLength)GetValue(C2WidthProperty); set => SetValue(C2WidthProperty, value); }

        public static readonly AvaloniaProperty<GridLength> C3WidthProperty = AvaloniaProperty.Register<MainWindow, GridLength>("C3Width", new GridLength(10, GridUnitType.Star));
        public GridLength C3Width { get => (GridLength)GetValue(C3WidthProperty); set => SetValue(C3WidthProperty, value); }

        public ICommand CloseCommand { get; }
        public ICommand MinCommand { get; }
        public ICommand MaxCommand { get; }

        public MainWindow()
        {
            CloseCommand = ReactiveCommand.Create(Close);
            MinCommand = ReactiveCommand.Create(() => WindowState = WindowState.Minimized);
            MaxCommand = ReactiveCommand.Create(() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var mt = this.FindControl<Control>("moveThumb");

            mt.PointerPressed += (o, e) =>
            {
                BeginMoveDrag(e);
            };

            var st = this.FindControl<Control>("sizeThumb");
            st.PointerPressed += (o, e) =>
            {
                BeginResizeDrag(WindowEdge.SouthEast, e);
            };
        }
    }
}

