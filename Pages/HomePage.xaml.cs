using System.Diagnostics;
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
using StarLight_Core.Models.Utilities;

namespace FMLauncher.Pages;

public partial class HomePage : Page
{
    
    private DateTime _gameStarTime;

    private  string? _JavaPath;
    private BaseAccount? _account;
    
    public HomePage()
    {
        InitializeComponent();
        _JavaPath = string.Empty;
        
        GetGameVer();
        GetJava();
        ConfigSet();

        Dispatcher.BeginInvoke(new Action(() =>
        {
            var setting = Application.Current.FindResource("toastSetting") as ToastSetting;
        }), System.Windows.Threading.DispatcherPriority.Background);

    }
     void GetGameVer()
    {
        GameVersion.DisplayMemberPath = "Id";
        GameVersion.SelectedValuePath = "Id";
        GameVersion.ItemsSource = GameCoreUtil.GetGameCores();
        
    }
    
    
    void GetJava()
    {
        var javas = JavaUtil.GetJavas();
        // 过滤重复的 Java 版本，保留第一个出现的版本
        var distinctJavas = javas
            .GroupBy(java => java.JavaSlugVersion)  // 按版本分组
            .Select(group => group.First())         // 每组取第一个
            .Where(java => !string.IsNullOrEmpty(java?.JavaPath) && 
                           (java.JavaSlugVersion >0))  // 替换 HasValue
            .ToList();
    
        JavaPath.ItemsSource = distinctJavas;
    }


    void ConfigSet()
    {
        var config = JsonUtil.Load();
        LoginMod.SelectedIndex = config.LoginMode;
        PlayerName.Text = config.Playername;
        MemorySlider.Value = config.Memory;

        // 设置登录模式相关的UI状态
        UpdateLoginModeUI(config.LoginMode);

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
        
        if (config.LoginMode == 0 && 
            !string.IsNullOrEmpty(config.RefreshToken) && 
            !string.IsNullOrEmpty(config.AccessToken))
        {
            var tokenAge = DateTime.Now - config.TokenExpiry;
            if (tokenAge.TotalSeconds < config.ExpiresIn)
            {
                // 使用Dispatcher在后台线程执行自动登录
                Dispatcher.BeginInvoke(new Action(async () => {
                    try
                    {
                        await AutoLoginAsync(config);
                        // 自动登录成功后隐藏登录按钮
                        Dispatcher.Invoke(() => {
                            LoginButton.Visibility = Visibility.Collapsed;
                            TextUnloginBlock.Visibility = Visibility.Collapsed;
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"自动登录异常: {ex.Message}");
                        // 自动登录失败后显示登录按钮
                        Dispatcher.Invoke(() => {
                            LoginButton.Visibility = Visibility.Visible;
                            TextUnloginBlock.Visibility = Visibility.Visible;
                        });
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                // Token过期时显示登录按钮
                LoginButton.Visibility = Visibility.Visible;
                TextUnloginBlock.Visibility = Visibility.Visible;
            }
        }
        else if (config.LoginMode == 0)
        {
            // 没有有效token时显示登录按钮
            LoginButton.Visibility = Visibility.Visible;
            TextUnloginBlock.Visibility = Visibility.Visible;
        }


    }

    private void UpdateLoginModeUI(int loginMode)
    {
        if (loginMode == 0)
        {
            PlayerName.Visibility = Visibility.Collapsed;
            PlayerNames.Visibility = Visibility.Collapsed;
            OnlineBorder.Visibility = Visibility.Visible;
            UserToken.Visibility = Visibility.Visible;
        
            // 确保登录相关控件初始状态正确
            var config = JsonUtil.Load();
            if (!string.IsNullOrEmpty(config.AccessToken) && 
                !string.IsNullOrEmpty(config.RefreshToken))
            {
                var tokenAge = DateTime.Now - config.TokenExpiry;
                if (tokenAge.TotalSeconds < config.ExpiresIn)
                {
                    // 有有效token时隐藏登录按钮
                    LoginButton.Visibility = Visibility.Collapsed;
                    TextUnloginBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // token过期时显示登录按钮
                    LoginButton.Visibility = Visibility.Visible;
                    TextUnloginBlock.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // 没有token时显示登录按钮
                LoginButton.Visibility = Visibility.Visible;
                TextUnloginBlock.Visibility = Visibility.Visible;
            }
        }
        else
        {
            PlayerName.Visibility = Visibility.Visible;
            PlayerNames.Visibility = Visibility.Visible;
            OnlineBorder.Visibility = Visibility.Collapsed;
            UserToken.Visibility = Visibility.Collapsed;
            LoginButton.Visibility = Visibility.Collapsed;
            TextUnloginBlock.Visibility = Visibility.Collapsed;
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
       MicrosoftAuthentication? auth = null;
        
       
    if (LoginMod.SelectedIndex == 0)
    {
        auth = new MicrosoftAuthentication("a9088867-a8c4-4d8d-a4a1-48a4eacb137b");
        
        // 检查是否已有有效的访问令牌
        var config = JsonUtil.Load();
        bool shouldSkipLogin = false;
        if (!string.IsNullOrEmpty(config.AccessToken) && 
            !string.IsNullOrEmpty(config.RefreshToken))
        {
            var tokenAge = DateTime.Now - config.TokenExpiry;
            if (tokenAge.TotalSeconds < config.ExpiresIn)
            {
                // 令牌仍然有效，跳过登录流程
                Console.WriteLine("已使用缓存信息");
                _account = new MicrosoftAccount // 需要根据实际API调整
                {
                    Name = config.OnlineUserName,
                    Uuid = config.OnlineUserUUID,
                    AccessToken = config.AccessToken
                };
                
                // 显示用户名
                OnlineUserName.Text = $"欢迎, {_account.Name}";
                OnlineUserName.Visibility = Visibility.Visible;
                LogoutButton.Visibility = Visibility.Visible;
                shouldSkipLogin = true;
            }
        }
        
        if (!shouldSkipLogin)
        {
            // 执行新的登录流程
            var code = await auth.RetrieveDeviceCodeInfo();
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
            _account = await auth.MicrosoftAuthAsync(token, x =>
            {
                UserToken.Text = x;
            });

            // 保存令牌信息
            config = JsonUtil.Load(); // 重新加载配置
            config.AccessToken = token.AccessToken;
            config.RefreshToken = token.RefreshToken;
            config.ExpiresIn = token.ExpiresIn;
            config.OnlineUserName = _account.Name;
            config.OnlineUserUUID = _account.Uuid;
            config.TokenExpiry = DateTime.Now.AddSeconds(token.ExpiresIn);
            JsonUtil.Save(config);
            
            OnlineUserName.Text = $"欢迎, {_account.Name}";
            OnlineUserName.Visibility = Visibility.Visible;
            LogoutButton.Visibility = Visibility.Visible;
            TextUnloginBlock.Visibility= Visibility.Collapsed;
            LoginButton.Visibility = Visibility.Collapsed;
        }
    }
    else
    {
        _account = new OfflineAuthentication(PlayerName.Text).OfflineAuth();
    }

        if (JavaPath.SelectedItem is JavaInfo selectedItem)
        {
            _JavaPath = selectedItem.JavaPath;
        }else
        {
            MessageBoxX.Show("请选择一个Java");
            return;
        }
        
        
        LaunchConfig args = new() // 配置启动参数
        {
            Account = new()
            {
                BaseAccount = _account // 账户
            },
            GameCoreConfig = new()
            {
                Root = ".minecraft", // 游戏根目录(可以是绝对的也可以是相对的,自动判断)
                Version =GameVersion.Text, // 启动的版本
                IsVersionIsolation = true, //版本隔离
            
            },
            JavaConfig = new()
            {
                JavaPath =_JavaPath , // Java 路径(绝对路径)
                MaxMemory = (int)(MemorySlider.Value*1024),
                MinMemory = (int)(MemorySlider.Minimum*1024)
            }
            
        };
        
        _gameStarTime=DateTime.Now;
        
        
        var launch = new MinecraftLauncher(args); // 实例化启动器
        ProgressBar.Visibility = Visibility.Visible;
        
        var la = await launch.LaunchAsync(ReportProgress);

        la.ErrorReceived += (output) => Console.WriteLine($"[错误] {output}");
        la.OutputReceived += (output) => Console.WriteLine($"[输出] {output}");
// 添加 Exited 事件监听
        la.Exited += (sender, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                var duration = DateTime.Now - _gameStarTime;
                bool isCrash = duration.TotalMilliseconds < 1000; // 小于 1 秒判定为崩溃

                if (la.Status == Status.Succeeded)
                {
                    if (isCrash)
                    {
                        MessageBoxX.Show("游戏异常关闭,请检查Java版本是否选择错误或是资源缺失"+"\n错误信息:"+la.Exception);
                        ProgressTextBlock.Text = "等待启动";
                        ProgressBar.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Toast.Show("游戏已关闭");
                        ProgressTextBlock.Text = "等待启动";
                        ProgressBar.Visibility = Visibility.Collapsed;
                    }
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
                    ProgressBar.Visibility = Visibility.Collapsed;
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
            MessageBoxX.Show("启动失败"+"\n错误信息:"+la.Exception?.Message);
            Console.WriteLine(la.Exception);
            ProgressTextBlock.Text = "等待启动";
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
        // 保存当前选择的登录模式
        var config = JsonUtil.Load();
        config.LoginMode = LoginMod.SelectedIndex;
        JsonUtil.Save(config);
    
        if (LoginMod.SelectedIndex == 0)
        {
            PlayerName.Visibility = Visibility.Collapsed;
            PlayerNames.Visibility = Visibility.Collapsed;
            OnlineBorder.Visibility = Visibility.Visible;
            UserToken.Visibility = Visibility.Visible;
    
            // 切换到正版登录时，根据是否有有效token决定是否显示登录按钮
            if (!string.IsNullOrEmpty(config.AccessToken) && 
                !string.IsNullOrEmpty(config.RefreshToken))
            {
                var tokenAge = DateTime.Now - config.TokenExpiry;
                if (tokenAge.TotalSeconds < config.ExpiresIn)
                {
                    LoginButton.Visibility = Visibility.Collapsed;
                    TextUnloginBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LoginButton.Visibility = Visibility.Visible;
                    TextUnloginBlock.Visibility = Visibility.Visible;
                }
            }
            else
            {
                LoginButton.Visibility = Visibility.Visible;
                TextUnloginBlock.Visibility = Visibility.Visible;
            }
        }
        else
        {
            PlayerName.Visibility = Visibility.Visible;
            PlayerNames.Visibility = Visibility.Visible;
            OnlineBorder.Visibility = Visibility.Collapsed;
            UserToken.Visibility = Visibility.Collapsed;
            LoginButton.Visibility = Visibility.Collapsed;
            TextUnloginBlock.Visibility = Visibility.Collapsed;
        }
    }



    public void SaveSettings()
    {
        var config = JsonUtil.Load();
    
        config.LoginMode = LoginMod.SelectedIndex;
        config.Playername = PlayerName.Text;
    
        // 增加空值检查
        if (GameVersion.SelectedItem != null)
        {
            config.GameVersion = (GameVersion.SelectedItem as dynamic)?.Id ?? "";
        }
    
        if (JavaPath.SelectedItem != null)
        {
            config.JavaPath = (JavaPath.SelectedItem as dynamic)?.JavaPath ?? "";
        }
    
        config.Memory = MemorySlider.Value;
    
        JsonUtil.Save(config);
    }

    
    private async Task AutoLoginAsync(LauncherSettings config)
    {
        try
        {
            // 添加超时机制
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                var auth = new MicrosoftAuthentication("a9088867-a8c4-4d8d-a4a1-48a4eacb137b");

                if (!string.IsNullOrEmpty(config.RefreshToken))
                {
                    var tokenInfo = new GetTokenResponse
                    {
                        AccessToken = config.AccessToken,
                        RefreshToken = config.RefreshToken,
                        ExpiresIn = config.ExpiresIn
                    };

                    // 使用带取消令牌的认证方法（如果API支持）
                    _account = await auth.MicrosoftAuthAsync(tokenInfo, x =>
                    {
                        Dispatcher.Invoke(() => {
                            UserToken.Text = x;
                            if (_account != null)
                            {
                                OnlineUserName.Text = $"欢迎, {_account.Name}";
                                OnlineUserName.Visibility = Visibility.Visible;
                                LogoutButton.Visibility = Visibility.Visible;
                            }
                        });
                    }, config.RefreshToken);
                }
            }
            OnlineUserName.Text = $"欢迎, {_account.Name}";
            OnlineUserName.Visibility = Visibility.Visible;
            LogoutButton.Visibility = Visibility.Visible;
         
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("自动登录超时");
            Toast.Show("登录超时，请重新登录");
        }
        catch (Exception ex)
        {
           MessageBoxX.Show($"自动登录失败: {ex.Message}");
            // 清除无效的令牌信息
            var cleanConfig = JsonUtil.Load();
            cleanConfig.AccessToken = null;
            cleanConfig.RefreshToken = null;
            JsonUtil.Save(cleanConfig);
        }
    }

    private void Logout(object sender, RoutedEventArgs e)
    {
        var config = JsonUtil.Load();
        config.AccessToken = null;
        config.RefreshToken = null;
        config.OnlineUserUUID = null;
        config.OnlineUserName = null;
        config.LoginMode = 1;
        JsonUtil.Save(config);

        Toast.Show("登出成功");
        // 更新UI状态
        OnlineUserName.Visibility = Visibility.Collapsed;
        OnlineUserName.Text = "";
        LogoutButton.Visibility = Visibility.Collapsed;
        LoginButton.Visibility = Visibility.Visible;
        TextUnloginBlock.Visibility = Visibility.Visible;
        UserToken.Text = "";
        UserToken.Visibility = Visibility.Collapsed;
        _account = null;
    
        // 更新登录模式UI状态
        UpdateLoginModeUI(1);
    }

    
    private async void Login_Click(object sender, RoutedEventArgs e)
{
    // 触发微软登录流程
    if (LoginMod.SelectedIndex == 0)
    {
        try
        {
            var auth = new MicrosoftAuthentication("a9088867-a8c4-4d8d-a4a1-48a4eacb137b");
            
            // 执行新的登录流程
            var code = await auth.RetrieveDeviceCodeInfo();
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
            _account = await auth.MicrosoftAuthAsync(token, x =>
            {
                UserToken.Text = x;
            });

            // 保存令牌信息
            var config = JsonUtil.Load(); // 重新加载配置
            config.AccessToken = token.AccessToken;
            config.RefreshToken = token.RefreshToken;
            config.ExpiresIn = token.ExpiresIn;
            config.OnlineUserName = _account.Name;
            config.OnlineUserUUID = _account.Uuid;
            config.TokenExpiry = DateTime.Now.AddSeconds(token.ExpiresIn);
            JsonUtil.Save(config);
            
            OnlineUserName.Text = $"欢迎, {_account.Name}";
            OnlineUserName.Visibility = Visibility.Visible;
            LogoutButton.Visibility = Visibility.Visible;

            Toast.Show("登录成功");
            LoginButton.Visibility= Visibility.Collapsed;
            TextUnloginBlock.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBoxX.Show($"登录失败: {ex.Message}");
            LoginButton.Visibility = Visibility.Visible;
            TextUnloginBlock.Visibility = Visibility.Visible;
        }
    }
}

}