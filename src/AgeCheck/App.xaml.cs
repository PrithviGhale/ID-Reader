using System.Windows;
using Velopack;
namespace AgeCheck;
public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        var app = new App();
        app.InitializeComponent();
        app.Run(new MainWindow(args));
    }
}
