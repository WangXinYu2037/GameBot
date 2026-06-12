using System;
using System.Threading.Tasks;
using GameBot.Backend.Services;

namespace GameBot.Backend
{
    /// <summary>
    /// GameBotTask 类 - 任务流程管理器
    /// 
    /// 负责编排和执行游戏自动化任务流程。
    /// </summary>
    public class GameBotTask : IDisposable
    {
        #region 私有字段

        private AdbService _adbService;
        private Services.GameBotService _gameBotService;
        private bool _isRunning;
        private bool _isCancelled;

        // 四个招募槽位配置
        private GameBotService.RecruitSlot[] _recruitSlots = {
            // 槽位1：左上角区域
            new GameBotService.RecruitSlot { 
                Index = 0, 
                StatusRegion1 = new Rect(420, 700, 400, 100),      // 判断是否完成：聘用候选人
                StatusRegion2 = new Rect(400, 650, 500, 80),       // 判断是否空闲：开始招募干员
                StatusRegion3 = new Rect(700, 725, 300, 80),       // 判断是否进行中：立即招募
                CountdownRegion = new Rect(130, 580, 290, 90)      // 倒计时显示区域
            }
            // // 槽位2：右上角区域
            // new GameBotService.RecruitSlot { 
            //     Index = 1, 
            //     StatusRegion1 = new Rect(1300, 700, 400, 100),     // 判断是否完成：聘用候选人
            //     StatusRegion2 = new Rect(1280, 650, 500, 80),     // 判断是否空闲：开始招募干员
            //     StatusRegion3 = new Rect(1500, 725, 300, 80),     // 判断是否进行中：立即招募
            //     CountdownRegion = new Rect(1350, 780, 250, 60)     // 倒计时显示区域
            // },
            // // 槽位3：左下角区域
            // new GameBotService.RecruitSlot { 
            //     Index = 2, 
            //     StatusRegion1 = new Rect(420, 1400, 400, 100),    // 判断是否完成：聘用候选人
            //     StatusRegion2 = new Rect(400, 1350, 500, 80),     // 判断是否空闲：开始招募干员
            //     StatusRegion3 = new Rect(700, 1375, 300, 80),     // 判断是否进行中：立即招募
            //     CountdownRegion = new Rect(550, 1430, 250, 60)     // 倒计时显示区域
            // },
            // // 槽位4：右下角区域
            // new GameBotService.RecruitSlot { 
            //     Index = 3, 
            //     StatusRegion1 = new Rect(1300, 1400, 400, 100),    // 判断是否完成：聘用候选人
            //     StatusRegion2 = new Rect(1280, 1350, 500, 80),    // 判断是否空闲：开始招募干员
            //     StatusRegion3 = new Rect(1500, 1375, 300, 80),    // 判断是否进行中：立即招募
            //     CountdownRegion = new Rect(1350, 1430, 250, 60)    // 倒计时显示区域
            // }
        };

        #endregion

        #region 公共属性

        public bool IsRunning => _isRunning;
        public bool IsAdbConnected => _adbService?.IsConnected ?? false;

        #endregion

        #region 初始化

        public GameBotTask()
        {
            _adbService = new AdbService();
            _gameBotService = new Services.GameBotService();
        }

        #endregion

        #region 连接管理

        public bool ConnectAdb(string adbPath, string deviceAddress)
        {
            bool result = _adbService.Connect(adbPath, deviceAddress);
            if (result)
            {
                _gameBotService.Initialize(_adbService);
            }
            return result;
        }

        public void DisconnectAdb()
        {
            _adbService.Disconnect();
            _gameBotService.Stop();
        }

        #endregion

        #region 任务执行

        public async Task StartTaskAsync()
        {
            if (_isRunning) return;
            if (!_adbService.IsConnected) return;

            _isRunning = true;
            _isCancelled = false;
            _gameBotService.Start();

            try
            {
                await ExecuteTaskFlowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"任务执行异常：{ex.Message}");
            }
            finally
            {
                _isRunning = false;
                _gameBotService.Stop();
            }
        }

        public void StopTask()
        {
            _isCancelled = true;
        }

        /// <summary>
        /// 核心任务流程：截图 -> 识别槽位状态 -> 执行操作
        /// </summary>
        private async Task ExecuteTaskFlowAsync()
        {
            while (!_isCancelled)
            {
                // 步骤1：截取屏幕
                string? screenshotBase64 = await _gameBotService.CaptureScreenAsync();
                if (screenshotBase64 == null)
                {
                    Console.WriteLine("截图失败，跳过本轮");
                    await Task.Delay(1000);
                    continue;
                }

                // 步骤2-3：识别槽位状态并执行操作
                bool hasAction = await _gameBotService.ProcessRecruitSlotsAsync(screenshotBase64, _recruitSlots);
                _isCancelled = true;
                
                // 如果没有执行任何操作，等待一段时间后重试
                if (!hasAction)
                {
                    Console.WriteLine("所有槽位状态未变化，等待10秒后重试");
                    await Task.Delay(10000);
                }
                else
                {
                    // 执行了操作，等待2秒后重新检查
                    await Task.Delay(2000);
                }
            }
        }

        #endregion

        #region 资源清理

        public void Dispose()
        {
            StopTask();
            DisconnectAdb();
            _adbService?.Dispose();
        }

        #endregion
    }
}
