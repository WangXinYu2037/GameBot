using System;
using System.Linq;
using System.Threading.Tasks;

namespace GameBot.Backend.Services
{
    /// <summary>
    /// GameBotService 类 - 游戏机器人后端服务核心类
    /// 
    /// 提供截图、文字识别、操作执行等核心功能。
    /// </summary>
    public class GameBotService : IDisposable
    {
        #region 私有字段

        private bool _isRunning;
        private AdbService _adbService;
        private OcrService _ocrService;
        private RecruitService _recruitService;

        #endregion

        #region 公共属性

        public bool IsRunning => _isRunning;

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 初始化服务
        /// </summary>
        /// <param name="adbService">ADB 服务实例</param>
        public void Initialize(AdbService adbService)
        {
            _isRunning = false;
            _adbService = adbService;
            _ocrService = OcrService.Instance;
            _recruitService = new RecruitService(adbService, this);
        }

        public void Dispose()
        {
            Stop();
            _ocrService?.Dispose();
        }

        #endregion

        #region 任务开始结束控制

        public bool Start()
        {
            _isRunning = true;
            return true;
        }

        public bool Stop()
        {
            _isRunning = false;
            return true;
        }

        #endregion

        #region 截图服务 调用AdbService

        /// <summary>
        /// 步骤1：截取设备屏幕
        /// </summary>
        /// <returns>截图的 Base64 编码，失败返回 null</returns>
        public async Task<string?> CaptureScreenAsync()
        {
            if (_adbService is null || !_adbService.IsConnected)
            {
                Console.WriteLine("ADB 服务未初始化或未连接");
                return null;
            }

            // 调用 AdbService 截图
            string? screenshotBase64 = _adbService.CaptureScreen();
            Console.WriteLine(screenshotBase64 != null ? "截图成功" : "截图失败");
            
            return await Task.FromResult(screenshotBase64);
        }
        #endregion

        #region 文字识别服务 调用OcrService
        /// <summary>
        /// 步骤2：图像文字识别（单个区域）
        /// </summary>
        /// <param name="screenshotBase64">截图的 Base64 编码</param>
        /// <param name="region">识别区域（可为null，识别全图）</param>
        /// <returns>识别到的文字，失败返回 null</returns>
        public async Task<string?> RecognizeTextAsync(string? screenshotBase64, Rect? region)
        {
            if (string.IsNullOrEmpty(screenshotBase64))
            {
                Console.WriteLine("截图数据为空，无法识别");
                return null;
            }

            // 调用 OCR 服务进行单区域文字识别
            string? recognizedText = await _ocrService.RecognizeTextInRegionAsync(screenshotBase64, region);
            
            Console.WriteLine($"单区域识别完成: {recognizedText ?? "空"}");
            
            return recognizedText;
        }

        /// <summary>
        /// 识别多个区域的文字
        /// </summary>
        /// <param name="screenshotBase64">截图的 Base64 编码</param>
        /// <param name="regions">识别区域数组</param>
        /// <returns>各区域识别到的文字数组</returns>
        public async Task<string[]?> RecognizeTextAsync(string? screenshotBase64, Rect[] regions)
        {
            if (string.IsNullOrEmpty(screenshotBase64))
            {
                Console.WriteLine("截图数据为空，无法识别");
                return null;
            }

            // 调用 OCR 服务进行多区域文字识别
            string?[] recognizedText = await _ocrService.RecognizeTextInRegionsAsync(screenshotBase64, regions);
            
            if (recognizedText != null && recognizedText.Length > 0)
            {
                Console.WriteLine($"识别完成，共 {recognizedText.Length} 个区域");
                for (int i = 0; i < recognizedText.Length; i++)
                {
                    Console.WriteLine($"区域 {i + 1}: {(recognizedText[i] ?? "空")}");
                }
            }
            else
            {
                Console.WriteLine("文字识别失败");
            }
            
            return recognizedText?.Select(t => t ?? string.Empty).ToArray();
        }
        #endregion

        #region 招募槽位管理 调用RecruitService

        /// <summary>
        /// 招募槽位状态枚举
        /// </summary>
        public enum RecruitSlotStatus
        {
            Unknown,
            Idle,           // 空闲 - 显示"开始招募"按钮
            InProgress,     // 进行中 - 显示倒计时
            Completed       // 已完成 - 显示"聘用干员"按钮
        }

        /// <summary>
        /// 招募槽位信息
        /// </summary>
        public struct RecruitSlot
        {
            public int Index;
            
            // 三个识别区域用于判断状态
            public Rect StatusRegion1;  // 区域1：主要状态区域（判断是否完成：聘用候选人）
            public Rect StatusRegion2;  // 区域2：按钮区域（判断是否空闲：开始招募干员）
            public Rect StatusRegion3;  // 区域3：辅助区域（判断是否进行中：立即招募）
            
            public Rect CountdownRegion;  // 倒计时显示区域（每个槽位独立）
            
            public RecruitSlotStatus Status;
            public string RecognizedText1;   // 区域1识别结果
            public string RecognizedText2;   // 区域2识别结果
            public string RecognizedText3;   // 区域3识别结果
        }

        /// <summary>
        /// 步骤3：识别所有招募槽位状态并执行操作，此处是GameBotService的主方法
        /// </summary>
        /// <param name="screenshotBase64">截图的 Base64 编码</param>
        /// <param name="slots">槽位配置</param>
        /// <returns>是否执行了操作</returns>
        public async Task<bool> ProcessRecruitSlotsAsync(string? screenshotBase64, RecruitSlot[] slots)
        {
            if (string.IsNullOrEmpty(screenshotBase64))
            {
                Console.WriteLine("截图数据为空，无法识别");
                return false;
            }

            if (slots == null || slots.Length == 0)
            {
                Console.WriteLine("未配置槽位");
                return false;
            }

            // 步骤1：识别所有槽位状态
            await RecognizeAllSlotsStatusAsync(screenshotBase64, slots);

            // 步骤2：根据状态执行操作
            return await ExecuteSlotActionsAsync(slots);

        }


        

        /// <summary>
        /// 识别所有招募槽位的状态（使用三个区域）
        /// </summary>
        private async Task RecognizeAllSlotsStatusAsync(string screenshotBase64, RecruitSlot[] slots)
        {
            // 收集所有槽位的三个识别区域
            Rect[] allRegions = new Rect[slots.Length * 3];
            for (int i = 0; i < slots.Length; i++)
            {
                allRegions[i * 3] = slots[i].StatusRegion1;
                allRegions[i * 3 + 1] = slots[i].StatusRegion2;
                allRegions[i * 3 + 2] = slots[i].StatusRegion3;
            }

            // 批量识别所有区域
            string[]? results = await RecognizeTextAsync(screenshotBase64, allRegions);

            if (results != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    int baseIndex = i * 3;
                    slots[i].RecognizedText1 = results.Length > baseIndex ? results[baseIndex] : string.Empty;
                    slots[i].RecognizedText2 = results.Length > baseIndex + 1 ? results[baseIndex + 1] : string.Empty;
                    slots[i].RecognizedText3 = results.Length > baseIndex + 2 ? results[baseIndex + 2] : string.Empty;
                    
                    // 根据三个区域的识别结果综合判断状态
                    slots[i].Status = ParseSlotStatus(slots[i].RecognizedText1, slots[i].RecognizedText2, slots[i].RecognizedText3);
                    
                    Console.WriteLine($"槽位 {i + 1}: 状态={slots[i].Status}");
                    Console.WriteLine($"  区域1: {slots[i].RecognizedText1}");
                    Console.WriteLine($"  区域2: {slots[i].RecognizedText2}");
                    Console.WriteLine($"  区域3: {slots[i].RecognizedText3}");
                }
            }
        }

        /// <summary>
        /// 根据三个区域的识别文本综合解析槽位状态
        /// </summary>
        private RecruitSlotStatus ParseSlotStatus(string text1, string text2, string text3)
        {
            // 合并所有区域的文本进行判断
            string combinedText = (text1 ?? string.Empty) + " " + 
                                 (text2 ?? string.Empty) + " " + 
                                 (text3 ?? string.Empty);
            
            if (string.IsNullOrWhiteSpace(combinedText))
                return RecruitSlotStatus.Unknown;

            string lowerText = combinedText.ToLower();
            
            // 已完成状态：包含"聘用"、"干员"、"完成"等关键词
            if (lowerText.Contains("聘用候选人"))
                return RecruitSlotStatus.Completed;

            
            // 空闲状态：包含"开始"、"招募"等关键词
            else if (lowerText.Contains("招募干员")||lowerText.Contains("开始招募"))
                return RecruitSlotStatus.Idle;

            
            // 进行中状态：包含时间相关关键词
            else if (lowerText.Contains("立即招募"))
                return RecruitSlotStatus.InProgress;
            
            else
                return RecruitSlotStatus.Unknown;

        }

        /// <summary>
        /// 执行槽位操作,根据状态执行不同的操作
        /// </summary>
        private async Task<bool> ExecuteSlotActionsAsync(RecruitSlot[] slots)
        {
            bool hasAction = false;

            if (_recruitService == null)
            {
                Console.WriteLine("RecruitService 未初始化");
                return false;
            }

            foreach (var slot in slots)
            {
                switch (slot.Status)
                {
                    case RecruitSlotStatus.Completed:
                        // 已完成状态：点击聘用按钮直到进入空闲状态
                        await _recruitService.HandleCompletedState(slot);
                        hasAction = true;
                        break;
                    
                    // case RecruitSlotStatus.Idle:
                    //     // 空闲状态：点击开始招募，计算最优Tag组合
                    //     await _recruitService.HandleIdleState(slot);
                    //     hasAction = true;
                    //     break;
                    
                    case RecruitSlotStatus.InProgress:
                        // 进行中状态：检查是否加急（isUrgent 参数默认为 false，可根据需求修改）
                        bool isUrgent = true;
                        bool actionExecuted = await _recruitService.HandleInProgressState(slot, isUrgent);
                        if (actionExecuted)
                        {
                            hasAction = true;
                        }
                        break;
                    
                    default:
                        Console.WriteLine($"槽位 {slot.Index + 1}: 状态未知");
                        break;
                }
            }

            return hasAction;
        }

        #endregion
    }
}