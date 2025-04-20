using System.Windows;
using Panuon.WPF.UI;
using StarLight_Core.Authentication;
using StarLight_Core.Enum;
using StarLight_Core.Launch;
using StarLight_Core.Models.Launch;
using StarLight_Core.Utilities;


namespace 忘却的旋律_EP;
using System.Net;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : WindowX
{
   
    public MainWindow()
    {
      InitializeComponent();
      GetGameVer();
      GetJava();
    }

    void GetGameVer()
    {
        GameVersion.DisplayMemberPath = "Id";
        GameVersion.SelectedValuePath = "Id";
        GameVersion.ItemsSource = GameCoreUtil.GetGameCores();
        
    }

    
    void GetJava()
    {
        JavaPath.DisplayMemberPath = "JavaPath";
        JavaPath.SelectedValuePath = "JavaPath";
        JavaPath.ItemsSource = JavaUtil.GetJavas();
        
    }
    private async void Button_Click(object sender, RoutedEventArgs e)
    {
       
        var account = new OfflineAuthentication(PlayerName.Text).OfflineAuth();
        LaunchConfig args = new() // 配置启动参数

        {
            Account = new()
            {
                BaseAccount = account // 账户
            },
            GameCoreConfig = new()
            {
                Root = ".minecraft", // 游戏根目录(可以是绝对的也可以是相对的,自动判断)
                Version =GameVersion.Text, // 启动的版本
                IsVersionIsolation = true, //版本隔离
            
            },
            JavaConfig = new()
            {
                JavaPath = JavaPath.Text, // Java 路径(绝对路径)
                MaxMemory = 4096,
                MinMemory = 1000
            }
        };
        var launch = new MinecraftLauncher(args); // 实例化启动器
        var la = await launch.LaunchAsync(ReportProgress); // 启动

// 日志输出
        la.ErrorReceived += (output) => Console.WriteLine($"{output}");
        la.OutputReceived += (output) => Console.WriteLine($"{output}");

        if (la.Status == Status.Succeeded)
        {
            NoticeBox.Show("启动成功", "忘却的旋律Extreme", MessageBoxIcon.Info);
            // 启动成功执行操作
        }
        else
        {
            MessageBoxX.Show("启动失败"+la.Exception);
        }
    
    }
    private void ReportProgress(ProgressReport progress)
    {

    }
    


    
}