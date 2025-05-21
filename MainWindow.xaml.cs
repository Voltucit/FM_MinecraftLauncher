using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Panuon.WPF.UI;
using System.Management;
using System.Windows.Controls;
using Panuon.WPF.UI.Configurations;
using StarLight_Core.Authentication;
using StarLight_Core.Enum;
using StarLight_Core.Launch;
using StarLight_Core.Models.Launch;
using StarLight_Core.Utilities;
using StarLight_Core.Models.Authentication;

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
      ConfigSet();
      var setting = Application.Current.FindResource("toastSetting") as ToastSetting;

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


    void ConfigSet()
    {
        var config = JsonUtil.Load();
        LoginMod.SelectedIndex = config.LoginMode;
        PlayerName.Text  = config.Playername;
        MemorySlider.Value = config.Memory;
        if (!string.IsNullOrEmpty(config.GameVersion))
        {
            GameVersion.SelectedItem = GameVersion.Items
                .Cast<dynamic>()
                .FirstOrDefault(i => i.Id == config.GameVersion);
        }

        // 设置 Java 路径
        if (!string.IsNullOrEmpty(config.JavaPath))
        {
            JavaPath.SelectedItem = JavaPath.Items
                .Cast<dynamic>()
                .FirstOrDefault(j => j.JavaPath == config.JavaPath);
        }
    }

    private static ulong GetTotalMemory()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                return Convert.ToUInt64(obj["TotalPhysicalMemory"]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取内存失败: {ex.Message}");
        }
        return 0;
    }
    

    
    
    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        BaseAccount account;
        
        if (LoginMod.SelectedIndex==0)
        {
            var auth=new MicrosoftAuthentication("a9088867-a8c4-4d8d-a4a1-48a4eacb137b");
            var code =await  auth.RetrieveDeviceCodeInfo();
            Clipboard.Clear();
            Clipboard.SetText(code.UserCode);
            MessageBoxX.Show("登录代码:" + code.UserCode + "  已复制到剪切栏");
            try
            {
                Process.Start(new ProcessStartInfo(code.VerificationUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开浏览器: {ex.Message}");
            }
            var token = await auth.GetTokenResponse(code);
            account = await auth.MicrosoftAuthAsync(token, x =>
            {
                UserToken.Text = x;
            });
        }
        else
        {
              account = new OfflineAuthentication(PlayerName.Text).OfflineAuth();
        }
        LaunchConfig args = new() // 配置启动参数
        {
            Account = new()
            {
                BaseAccount = account // 账户
            },
            GameCoreConfig = new()
            {
                Root = ".minecraft/", // 游戏根目录(可以是绝对的也可以是相对的,自动判断)
                Version =GameVersion.Text, // 启动的版本
                IsVersionIsolation = true, //版本隔离
            
            },
            JavaConfig = new()
            {
                JavaPath = JavaPath.Text, // Java 路径(绝对路径)
                MaxMemory = (int)(MemorySlider.Value*1024),
                MinMemory = (int)(MemorySlider.Minimum*1024)
            }
        };
        var launch = new MinecraftLauncher(args); // 实例化启动器
        var la = await launch.LaunchAsync(ReportProgress);

        la.ErrorReceived += (output) => Console.WriteLine($"[错误] {output}");
        la.OutputReceived += (output) => Console.WriteLine($"[输出] {output}");
// 添加 Exited 事件监听
        la.Exited += (sender, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (la.Status == Status.Succeeded)
                {
                    Panuon.WPF.UI.Toast.Show("游戏已关闭");
                    ProgressTextBlock.Text = "等待启动";
                    ProgressBar.Value = 0;
                    ProgressBar.IsIndeterminate = false;
                    ProgressPercent.Text = "";
                }
                else
                {
                    MessageBoxX.Show("游戏异常退出: " + la.Exception?.Message);
                    ProgressTextBlock.Text = "游戏异常退出";
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = 0;
                }
            });
        };

        if (la.Status == Status.Succeeded)
        {
            Panuon.WPF.UI.Toast.Show("启动成功");
            ProgressTextBlock.Text = "游戏运行中";
            ProgressBar.Value = 100;
            ProgressBar.IsIndeterminate = false;
            ProgressPercent.Text = "";
        }
        else
        {
            MessageBoxX.Show("启动失败：" + la.Exception?.Message);
            ProgressTextBlock.Text = "启动失败";
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 0;
        }

    
    }
    private void ReportProgress(ProgressReport progress)
    {
        Dispatcher.Invoke((Action)(() =>
        {
            // 更新状态文本
            ProgressTextBlock.Text = progress.Description;

            // 如果有百分比信息，更新进度条
            if (progress.Percentage >= 0)
            {
                ProgressBar.IsIndeterminate = false;

                // 将 0-100 的 Percentage 转换为进度条值
                ProgressBar.Value = progress.Percentage;

                // 显示百分比文本
                ProgressPercent.Text = $"{progress.Percentage}%";
            }
            else
            {
                // 若百分比无效，则使用不确定模式
                ProgressBar.IsIndeterminate = true;
                ProgressPercent.Text = "";
            }

            // 可选：输出日志到控制台
            Console.WriteLine($"[进度] {progress.Description} - {progress.Percentage}%");
        }));
    }





    private void MemorySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ulong totalMemoryBytes = GetTotalMemory();
        double totalMemoryMb = totalMemoryBytes / (1024.0 * 1024.0*1024);
        int totalMemoryMbInt = (int)Math.Round(totalMemoryMb); // 将结果四舍五入为整数
        MemorySlider.Maximum = totalMemoryMbInt;
    }

    private void LoginMod_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LoginMod.SelectedIndex==0)
        {
            PlayerName.Visibility = Visibility.Collapsed;
            PlayerNames.Visibility = Visibility.Collapsed;
        }
        else
        {
            PlayerName.Visibility = Visibility.Visible;
            PlayerNames.Visibility = Visibility.Visible;
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        var config = new LauncherSettings()
        {
            LoginMode = LoginMod.SelectedIndex,
            Playername = PlayerName.Text,
            GameVersion = (GameVersion.SelectedItem as dynamic)?.Id ?? "",
            JavaPath = (JavaPath.SelectedItem as dynamic)?.JavaPath ?? "",
            Memory =  MemorySlider.Value
        };
        JsonUtil.Save(config);
    }
}