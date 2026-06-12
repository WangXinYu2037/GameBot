using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using GameBot.Frontend.Services;

namespace GameBot.Frontend
{
    /// <summary>
    /// 主窗口 - 测试前后端 DLL 连通性
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _backendLoaded = false;
        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBackend();
        }

        /// <summary>
        /// 初始化后端服务
        /// </summary>
        private void InitializeBackend()
        {
            _backendLoaded = BackendService.Instance.LoadFromAppDirectory();
            
            if (_backendLoaded)
            {
                MessageBox.Show("后端服务加载成功！");
            }
            else
            {
                MessageBox.Show("后端服务加载失败，请确保 GameBot.Backend.dll 在正确位置");
            }
        }

        /// <summary>
        /// 主按钮点击事件 - 根据当前状态调用开始或停止
        /// </summary>
        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                StopButton_Click(sender, e);
            }
            else
            {
                StartButton_Click(sender, e);
            }
        }

        /// <summary>
        /// 开始按钮 - 测试调用后端方法
        /// </summary>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // if (!_backendLoaded)
            // {
            //     MessageBox.Show("后端服务未加载");
            //     return;
            // }

            try
            {
                // 调用后端的测试方法
                // string greeting = BackendService.Instance.InvokeString("GetGreeting");
                // string time = BackendService.Instance.InvokeString("GetCurrentTime");
                // int sum = BackendService.Instance.InvokeInt("Add", 10, 20);

                // MessageBox.Show(
                //     $"前后端 DLL 连通测试成功！\n\n" +
                //     $"1. GetGreeting(): {greeting}\n" +
                //     $"2. GetCurrentTime(): {time}\n" +
                //     $"3. Add(10, 20): {sum}"
                // );

                // 切换为停止状态
                _isRunning = true;
                MainButton.Content = "停止";
                MainBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调用失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 停止按钮 - 测试 Stop 方法
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // if (!_backendLoaded)
            // {
            //     MessageBox.Show("后端服务未加载");
            //     return;
            // }

            // bool result = BackendService.Instance.InvokeBool("Stop");
            // MessageBox.Show(result ? "Stop() 调用成功" : "Stop() 调用失败");

            // 切换为开始状态
            _isRunning = false;
            MainButton.Content = "开始";
            MainBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        /// <summary>
        /// 鼠标进入按钮 - 添加阴影
        /// </summary>
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(60, 60, 60),
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.6
                };
            }
        }

        /// <summary>
        /// 鼠标离开按钮 - 移除阴影
        /// </summary>
        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Effect = null;
            }
        }

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            BackendService.Instance.Cleanup();
        }
    }
}
