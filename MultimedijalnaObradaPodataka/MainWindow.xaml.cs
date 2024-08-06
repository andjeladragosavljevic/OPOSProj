using System.Windows;
using System.Windows.Controls;

namespace OposMMOP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        public static StackPanel ProgressBarsStackPanel { get; set; }
    }
}
