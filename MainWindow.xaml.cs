using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Panuon.WPF.UI;
using 忘却的旋律_EP.Pages;

namespace 忘却的旋律_EP;
using System.Net;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : WindowX
{
    private Button _activeButton;
    
    public MainWindow()
    {
        InitializeComponent();
        // 初始化默认激活按钮
        _activeButton = HomeButton;
        SetActiveButtonStyle(HomeButton);
        // 导航到主页
        MainFrame.Navigate(new HomePage());
    }
    
    private void NavigateToHome(object sender, RoutedEventArgs e)
    {
        if (_activeButton != HomeButton)
        {
            PerformNavigation(new HomePage(), HomeButton);
        }
    }

    private void NavigateToGameSetting(object sender, RoutedEventArgs e)
    {
        if (_activeButton != GameSettingButton)
        {
            PerformNavigation(new GameSetting(), GameSettingButton);
        }
    }
    
    private void NavigateToAbout(object sender, RoutedEventArgs e)
    {
        if (_activeButton != AboutButton)
        {
            PerformNavigation(new About(), AboutButton);
        }
    }
    
    private void PerformNavigation(object page, Button targetButton)
    {
        // 执行淡出动画
        var fadeOutStoryboard = (System.Windows.Media.Animation.Storyboard)FindResource("FadeOutStoryboard");
        fadeOutStoryboard.Completed += (s, e) =>
        {
            // 导航到新页面
            MainFrame.Navigate(page);
            // 更新激活按钮
            SetActiveButtonStyle(targetButton);
            // 执行淡入动画
            var fadeInStoryboard = (System.Windows.Media.Animation.Storyboard)FindResource("FadeInStoryboard");
            fadeInStoryboard.Begin(MainFrame);
        };
        fadeOutStoryboard.Begin(MainFrame);
    }
    
    private void SetActiveButtonStyle(Button activeButton)
    {
        // 重置之前激活的按钮
        if (_activeButton != null)
        {
            _activeButton.ClearValue(Button.BackgroundProperty);
            _activeButton.ClearValue(Button.ForegroundProperty);
        }
        
        // 设置新的激活按钮样式
        activeButton.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
        activeButton.Foreground = new SolidColorBrush(Colors.White);
        _activeButton = activeButton;
    }
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (MainFrame.Content is HomePage homePage)
        {
            homePage.SaveSettings();
        }
    }

}
