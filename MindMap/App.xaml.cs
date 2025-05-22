using MindMap.Infrastructure;
using System.Configuration;
using System.Data;
using System.Windows;

namespace MindMap
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider Services { get; }

        public App()
        {
            Services = Bootstrap.BuildContainer();
            InitializeComponent();
        }
    }

}
