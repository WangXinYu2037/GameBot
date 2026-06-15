using System;
using GameBot.Backend.Services;
using OpenCvSharp;

namespace GameBot.Backend
{
    /// <summary>
    /// Test 类 - ADB 功能测试工具
    /// 
    /// 用于测试 ADB 连接、截图等功能是否正常工作。
    /// </summary>
    public static class Test
    {
        #region AdbService 测试

        /// <summary>
        /// 测试 AdbService 的连接功能
        /// </summary>
        /// <param name="adbPath">ADB 可执行文件路径</param>
        /// <param name="deviceAddress">设备地址（IP:端口）</param>
        /// <returns>测试是否成功</returns>
        public static bool TestAdbConnection(string adbPath, string deviceAddress)
        {
            Console.WriteLine("=== 开始测试 AdbService 连接功能 ===");
            
            using (var adbService = new AdbService())
            {
                try
                {
                    // 测试连接
                    Console.WriteLine($"连接到设备: {deviceAddress}");
                    bool connected = adbService.Connect(adbPath, deviceAddress);
                    
                    if (!connected)
                    {
                        Console.WriteLine("连接失败");
                        return false;
                    }
                    
                    Console.WriteLine("连接成功");
                    
                    // 测试获取设备列表
                    Console.WriteLine("\n获取设备列表:");
                    string devices = adbService.GetDevices();
                    Console.WriteLine(devices);
                    
                    // 测试设备可用性
                    bool available = adbService.IsDeviceAvailable();
                    Console.WriteLine($"设备可用性: {(available ? "可用" : "不可用")}");
                    
                    // 测试截图
                    Console.WriteLine("\n测试截图功能:");
                    string? screenshot = adbService.CaptureScreen();
                    if (!string.IsNullOrEmpty(screenshot))
                    {
                        Console.WriteLine($"截图成功，数据大小: {screenshot.Length} 字节");
                    }
                    else
                    {
                        Console.WriteLine("截图失败");
                    }
                    
                    // 断开连接
                    adbService.Disconnect();
                    Console.WriteLine("\n已断开连接");
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"测试异常: {ex.Message}");
                    return false;
                }
            }
        }

        #endregion

        #region GameBotTask 测试

        /// <summary>
        /// 测试 GameBotTask 的完整流程
        /// </summary>
        /// <param name="adbPath">ADB 可执行文件路径</param>
        /// <param name="deviceAddress">设备地址（IP:端口）</param>
        public static async System.Threading.Tasks.Task TestGameBotTaskAsync(string adbPath, string deviceAddress)
        {
            Console.WriteLine("\n=== 开始测试 GameBotTask ===");
            
            using (var task = new GameBotTask())
            {
                try
                {
                    // 测试连接
                    Console.WriteLine($"连接到设备: {deviceAddress}");
                    bool connected = task.ConnectAdb(adbPath, deviceAddress);
                    
                    if (!connected)
                    {
                        Console.WriteLine("连接失败");
                        return;
                    }
                    
                    Console.WriteLine("连接成功");
                    
                    // 测试启动任务（运行3秒后停止）
                    // Console.WriteLine("\n启动任务流程（3秒后自动停止）:");
                    var taskTask = task.StartTaskAsync();
                    
                    // 等待3秒后停止
                    // await System.Threading.Tasks.Task.Delay(3000);
                    // task.StopTask();
                    
                    // 等待任务结束
                    await taskTask;
                    
                    Console.WriteLine("GameBotTask 测试完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"测试异常: {ex.Message}");       
                }
            }
        }

        #endregion

        #region 便捷测试方法


        /// <summary>
        /// 带参数的便捷测试
        /// </summary>
        /// <param name="adbPath">ADB 路径</param>
        /// <param name="deviceAddress">设备地址</param>
        public static void QuickTest(string adbPath, string deviceAddress)
        {
            Console.WriteLine($"测试配置:\nADB路径: {adbPath}\n设备地址: {deviceAddress}\n");
            
            
            // 测试连接
            // bool connected = TestAdbConnection(adbPath, deviceAddress);
            bool connected = true;
            if (connected)
            {
                // 如果连接成功，测试 GameBotTask
                var task = TestGameBotTaskAsync(adbPath, deviceAddress);
                task.Wait();
            }
        }

        #endregion

        #region 测试主函数
        static void Main(string[] args)
        {
            string adbPath = @"D:\entertainment\MuMu\MuMuPlayer-12.0\shell\adb.exe";
            string deviceAddress = "127.0.0.1:16384"; // MuMu 默认端口
            QuickTest(adbPath, deviceAddress);
        }
        #endregion
    }
}
