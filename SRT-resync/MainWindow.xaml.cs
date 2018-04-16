using System.Reflection;
using System.Windows;

namespace SRT_resync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static string Version { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DataContext = new SubtitleViewModel(); 
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            SizeToContent = SizeToContent.Manual;
            MinHeight = ActualHeight;
            MinWidth = ActualWidth;
        }

        private void AboutMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var about = new About();
            about.ShowDialog();
        }
    }
}
