using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameBot.Backend.Services
{
    /// <summary>
    /// 招募服务类 - 处理招募槽位的各种操作逻辑
    /// </summary>
    public class RecruitService
    {
        #region 私有字段

        private AdbService _adbService;
        private GameBotService _gameBotService;

        #endregion

        #region 构造函数

        public RecruitService(AdbService adbService, GameBotService gameBotService)
        {
            _adbService = adbService;
            _gameBotService = gameBotService;
        }

        #endregion

        #region 已完成状态处理

        /// <summary>
        /// 处理已完成状态 - 点击聘用按钮直到进入空闲状态
        /// </summary>
        /// <param name="slot">槽位信息</param>
        /// <returns>是否成功完成操作</returns>
        public async Task<bool> HandleCompletedState(GameBotService.RecruitSlot slot)
        {
            Console.WriteLine($"处理槽位 {slot.Index + 1} 已完成状态");
            
            // 已完成状态需要点击聘用按钮，然后可能需要点击确认等后续操作
            // 直到识别到空闲状态
            
            try
            {
                // 步骤1：点击聘用按钮区域（使用StatusRegion2作为按钮区域）
                int centerX = slot.StatusRegion1.X + slot.StatusRegion1.Width / 2;
                int centerY = slot.StatusRegion1.Y + slot.StatusRegion1.Height / 2;
                
                await TapAsync(centerX, centerY);
                Console.WriteLine($"槽位 {slot.Index + 1}: 点击聘用按钮 ({centerX}, {centerY})");
                await Task.Delay(1000);
                
                // 步骤2：可能需要点击确认领取按钮（假设在屏幕中央偏下位置）
                // 这是一个通用位置，实际可能需要根据界面调整
                await TapAsync(2410, 65);
                Console.WriteLine($"槽位 {slot.Index + 1}: 跳过");
                await Task.Delay(2000);

                await TapAsync(2410, 65);
                Console.WriteLine($"槽位 {slot.Index + 1}: 跳过");
                await Task.Delay(1000);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理已完成状态异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 空闲状态处理

        /// <summary>
        /// 处理空闲状态 - 开始招募流程
        /// </summary>
        /// <param name="slot">槽位信息</param>
        /// <returns>是否成功完成操作</returns>
        public async Task<bool> HandleIdleState(GameBotService.RecruitSlot slot)
        {
            Console.WriteLine($"处理槽位 {slot.Index + 1} 空闲状态");
            
            try
            {
                // 步骤1：点击开始招募按钮
                int centerX = slot.StatusRegion2.X + slot.StatusRegion2.Width / 2;
                int centerY = slot.StatusRegion2.Y + slot.StatusRegion2.Height / 2;
                
                await TapAsync(centerX, centerY);
                Console.WriteLine($"槽位 {slot.Index + 1}: 点击开始招募按钮 ({centerX}, {centerY})");
                await Task.Delay(800);
                
                // 步骤2：识别并计算最优Tag组合
                await CalculateOptimalTags();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理空闲状态异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算最优Tag组合
        /// </summary>
        private async Task CalculateOptimalTags()
        {
            Console.WriteLine("开始计算最优Tag组合...");
            
            // 截图获取当前界面
            string? screenshotBase64 = await _gameBotService.CaptureScreenAsync();
            if (screenshotBase64 == null)
            {
                Console.WriteLine("截图失败，无法识别Tag");
                return;
            }
            
            // 定义Tag识别区域（假设在屏幕中间区域显示5个Tag）
            Rect[] tagRegions = {
                new Rect(480, 700, 300, 80),   // Tag 1
                new Rect(780, 700, 300, 80),   // Tag 2
                new Rect(1080, 700, 300, 80),  // Tag 3
                new Rect(630, 850, 300, 80),   // Tag 4
                new Rect(930, 850, 300, 80)    // Tag 5
            };
            
            // 识别所有Tag
            string[]? tags = await _gameBotService.RecognizeTextAsync(screenshotBase64, tagRegions);
            
            if (tags == null || tags.Length == 0)
            {
                Console.WriteLine("未识别到任何Tag");
                return;
            }
            
            // 输出识别到的Tag
            Console.WriteLine("识别到的Tag:");
            for (int i = 0; i < tags.Length; i++)
            {
                Console.WriteLine($"  Tag {i + 1}: {tags[i]}");
            }
            
            // 计算最优组合（简单示例：选择第一个Tag）
            List<int> optimalCombination = CalculateBestTagCombination(tags);
            
            if (optimalCombination.Count > 0)
            {
                Console.WriteLine($"选择的Tag组合: {string.Join(", ", optimalCombination.Select(i => i + 1))}");
                
                // 点击选中的Tag
                foreach (int index in optimalCombination)
                {
                    if (index < tagRegions.Length)
                    {
                        int x = tagRegions[index].X + tagRegions[index].Width / 2;
                        int y = tagRegions[index].Y + tagRegions[index].Height / 2;
                        await TapAsync(x, y);
                        Console.WriteLine($"点击 Tag {index + 1} ({x}, {y})");
                        await Task.Delay(300);
                    }
                }
                
                // 点击确认按钮（假设在底部）
                await TapAsync(960, 1800);
                Console.WriteLine("点击确认按钮");
                await Task.Delay(500);
                
                // 点击开始招募按钮
                await TapAsync(960, 1650);
                Console.WriteLine("点击开始招募");
            }
            else
            {
                // 无满意标签，刷新Tag
                await RefreshTags();
            }
        }

        /// <summary>
        /// 计算最优Tag组合（简单实现：选择第一个非空Tag）
        /// </summary>
        private List<int> CalculateBestTagCombination(string[] tags)
        {
            List<int> combination = new List<int>();
            
            // 示例：选择第一个非空Tag
            for (int i = 0; i < tags.Length; i++)
            {
                if (!string.IsNullOrEmpty(tags[i]) && tags[i].Trim().Length > 0)
                {
                    combination.Add(i);
                    break; // 只选一个Tag作为示例
                }
            }
            
            return combination;
        }

        /// <summary>
        /// 刷新Tag
        /// </summary>
        private async Task RefreshTags()
        {
            Console.WriteLine("刷新Tag...");
            
            // 点击刷新按钮（假设在Tag区域附近）
            await TapAsync(1400, 700);
            Console.WriteLine("点击刷新按钮");
            await Task.Delay(500);
            
            // 点击确认刷新（假设弹窗确认）
            await TapAsync(960, 1500);
            Console.WriteLine("确认刷新");
            await Task.Delay(500);
        }

        #endregion

        #region 进行中状态处理

        /// <summary>
        /// 处理进行中状态
        /// </summary>
        /// <param name="slot">槽位信息</param>
        /// <param name="isUrgent">是否为加急状态（手动设置）</param>
        /// <returns>是否执行了操作</returns>
        public async Task<bool> HandleInProgressState(GameBotService.RecruitSlot slot, bool isUrgent)
        {
            Console.WriteLine($"处理槽位 {slot.Index + 1} 进行中状态");
            
            if (isUrgent)
            {
                // 加急状态：点击立即招募按钮
                Console.WriteLine($"槽位 {slot.Index + 1}: 加急状态，执行立即招募");
                await ExecuteInstantRecruit(slot);
                return true;
            }
            else
            {
                // 非加急状态：识别并输出招募完成倒计时
                Console.WriteLine($"槽位 {slot.Index + 1}: 正常进行中，识别倒计时");
                await RecognizeAndOutputCountdown(slot);
                return false;
            }
        }

        /// <summary>
        /// 识别并输出招募完成倒计时
        /// </summary>
        private async Task RecognizeAndOutputCountdown(GameBotService.RecruitSlot slot)
        {
            string? screenshotBase64 = await _gameBotService.CaptureScreenAsync();
            if (screenshotBase64 == null)
            {
                Console.WriteLine($"槽位 {slot.Index + 1}: 截图失败，无法识别倒计时");
                return;
            }
            
            // 使用独立的倒计时区域
            
            string? results = await _gameBotService.RecognizeTextAsync(screenshotBase64, slot.CountdownRegion);
            
            if (results != null && results.Length > 0 && !string.IsNullOrEmpty(results))
            {
                Console.WriteLine($"槽位 {slot.Index + 1}: 剩余时间: {results}");
            }
            else
            {
                Console.WriteLine($"槽位 {slot.Index + 1}: 无法识别剩余时间");
            }
        }

        /// <summary>
        /// 执行立即招募
        /// </summary>
        private async Task ExecuteInstantRecruit(GameBotService.RecruitSlot slot)
        {
            try
            {
                // 点击立即招募按钮（使用StatusRegion3作为按钮区域）
                int centerX = slot.StatusRegion3.X + slot.StatusRegion3.Width / 2;
                int centerY = slot.StatusRegion3.Y + slot.StatusRegion3.Height / 2;
                
                await TapAsync(centerX, centerY);
                Console.WriteLine($"点击立即招募 ({centerX}, {centerY})");
                await Task.Delay(1000);
                
                // 点击确认消耗加急许可立即招募
                await TapAsync(1700, 1000);
                Console.WriteLine("确认立即招募");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行立即招募异常: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 执行点击操作
        /// </summary>
        private async Task TapAsync(int x, int y)
        {
            if (_adbService != null)
            {
                _adbService.Tap(x, y);
            }
            await Task.CompletedTask;
        }

        #endregion
    }
}
