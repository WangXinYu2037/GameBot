using System;
using System.Diagnostics;

namespace GameBot.Backend.Services
{
    /// <summary>
    /// ADB 服务类 - 负责与 ADB 命令行工具交互
    /// </summary>
    public class AdbService : IDisposable
    {
        #region 私有字段

        /// <summary>
        /// ADB 可执行文件路径
        /// </summary>
        private string _adbPath;

        /// <summary>
        /// 设备地址（IP:端口）
        /// </summary>
        private string _deviceAddress;

        /// <summary>
        /// 是否已连接设备
        /// </summary>
        private bool _isConnected;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// 获取当前设备地址
        /// </summary>
        public string DeviceAddress => _deviceAddress;

        #endregion

        #region 初始化

        /// <summary>
        /// 构造函数
        /// </summary>
        public AdbService()
        {
            _adbPath = string.Empty;
            _deviceAddress = string.Empty;
            _isConnected = false;
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 连接到 ADB 设备
        /// </summary>
        /// <param name="adbPath">ADB 可执行文件路径</param>
        /// <param name="address">设备地址（IP:端口）</param>
        /// <returns>连接是否成功</returns>
        public bool Connect(string adbPath, string address)
        {
            try
            {
                // 验证 ADB 路径
                if (!System.IO.File.Exists(adbPath))
                {
                    Console.WriteLine($"错误：ADB 文件不存在 - {adbPath}");
                    return false;
                }

                _adbPath = adbPath;
                _deviceAddress = address;

                // 执行连接命令
                string result = RunAdbCommand($"connect {address}");
                
                // 检查连接结果
                _isConnected = result.Contains("connected") || result.Contains("already connected");
                
                if (_isConnected)
                {
                    Console.WriteLine($"成功连接到设备：{address}");
                }
                else
                {
                    Console.WriteLine($"连接失败：{result}");
                }

                return _isConnected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接异常：{ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        public bool Disconnect()
        {
            try
            {
                if (!_isConnected)
                {
                    return true;
                }

                RunAdbCommand($"disconnect {_deviceAddress}");
                _isConnected = false;
                _deviceAddress = string.Empty;
                
                Console.WriteLine("已断开设备连接");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开连接异常：{ex.Message}");
                return false;
            }
        }

        #endregion

        #region 基础操作

        /// <summary>
        /// 获取设备列表
        /// </summary>
        /// <returns>设备列表字符串</returns>
        public string GetDevices()
        {
            return RunAdbCommand("devices");
        }

        /// <summary>
        /// 检查设备是否可用
        /// </summary>
        /// <returns>设备是否可用</returns>
        public bool IsDeviceAvailable()
        {
            if (!_isConnected)
            {
                return false;
            }

            string result = RunAdbCommand("shell getprop sys.boot_completed");
            Console.WriteLine(result);
            return result.Contains("1");
        }

        #endregion

        #region 截图功能

        /// <summary>
        /// 截取设备屏幕
        /// </summary>
        /// <returns>截图的 Base64 编码，失败返回 null</returns>
        public string? CaptureScreen()
        {
            if (!_isConnected)
            {
                Console.WriteLine("未连接设备");
                return null;
            }

            try
            {
                // 截图保存目录（项目目录下的 Test 文件夹）
                string screenshotDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory, 
                    "Test"
                );
                
                // 生成带时间戳的文件名
                // string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                // string localFile = System.IO.Path.Combine(screenshotDir, fileName);
                
                // 生成带时间戳的临时文件名
                string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string localFile = System.IO.Path.Combine(screenshotDir, fileName);
                
                // 截取到设备
                RunAdbCommand($"shell screencap -p /sdcard/screenshot.png");
                
                // 拉取到本地
                RunAdbCommand($"pull /sdcard/screenshot.png \"{localFile}\"");
                
                // 清理设备上的文件
                RunAdbCommand("shell rm /sdcard/screenshot.png");

                // 读取文件并转换为 Base64
                if (System.IO.File.Exists(localFile))
                {
                    byte[] imageBytes = System.IO.File.ReadAllBytes(localFile);
                    string base64 = Convert.ToBase64String(imageBytes);
                    
                    // 读取后立即删除本地临时文件
                    // System.IO.File.Delete(localFile);
                    
                    // Console.WriteLine("截图成功（临时文件已删除）");
                    return base64;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"截图失败：{ex.Message}");
                return null;
            }
        }

        #endregion

        #region 公共辅助方法

        /// <summary>
        /// 点击指定坐标位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>命令执行结果</returns>
        public string Tap(int x, int y)
        {
            return RunAdbCommand($"shell input tap {x} {y}");
        }

        /// <summary>
        /// 执行 ADB 命令
        /// </summary>
        /// <param name="command">ADB 命令（不包含 adb 前缀）</param>
        /// <returns>命令输出结果</returns>
        public string RunAdbCommand(string command)
        {
            // 如果已连接设备，添加设备指定参数
            string fullCommand = string.IsNullOrEmpty(_deviceAddress) 
                ? command 
                : $"-s {_deviceAddress} {command}";
            
            try
            {
                // 创建进程
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _adbPath,
                    Arguments = fullCommand,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                // 执行命令
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // 如果有错误输出，返回错误信息
                    if (!string.IsNullOrEmpty(error))
                    {
                        return error;
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                return $"执行命令失败：{ex.Message}";
            }
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        #endregion
    }
}
