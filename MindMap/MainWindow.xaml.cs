using Microsoft.Extensions.DependencyInjection;
using MindMap.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MindMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // DI로부터 DocumentVM 해와서 DataContext 지정
            if (Application.Current is App app)
                    DataContext = app.Services.GetRequiredService<DocumentVM>();
        }
    }
}