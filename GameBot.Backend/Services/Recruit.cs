using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
                await Task.Delay(2000);
                
                // 步骤2：可能需要点击确认领取按钮（假设在屏幕中央偏下位置）
                // 这是一个通用位置，实际可能需要根据界面调整
                await TapAsync(2410, 65);
                Console.WriteLine($"槽位 {slot.Index + 1}: 跳过");
                await Task.Delay(5000);

                await TapAsync(2410, 65);
                Console.WriteLine($"槽位 {slot.Index + 1}: 跳过");
                await Task.Delay(2000);
                
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
                await Task.Delay(2000);
                
                // 步骤2：识别并计算最优Tag组合      
                return await CalculateOptimalTags();
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
        private async Task<bool> CalculateOptimalTags()
        {
            Console.WriteLine("开始计算最优Tag组合...");
            
            // 截图获取当前界面
            string? screenshotBase64 = await _gameBotService.CaptureScreenAsync();
            if (screenshotBase64 == null)
            {
                Console.WriteLine("截图失败，无法识别Tag");
                return false;
            }
            
            // 定义Tag识别区域（假设在屏幕中间区域显示5个Tag）
            Rect[] tagRegions = {
                new Rect(750, 730, 275, 80),   // Tag 1
                new Rect(1100, 730, 275, 80),   // Tag 2
                new Rect(1425, 730, 275, 80),  // Tag 3
                new Rect(750, 870, 275, 80),   // Tag 4
                new Rect(1100, 870, 275, 80)    // Tag 5
            };
            
            // 识别所有Tag
            string[]? tags = await _gameBotService.RecognizeTextAsync(screenshotBase64, tagRegions);
            
            if (tags == null || tags.Length == 0)
            {
                Console.WriteLine("未识别到任何Tag");
                return false;
            }
            
            // 输出识别到的Tag
            Console.WriteLine("识别到的Tag:");
            for (int i = 0; i < tags.Length; i++)
            {
                Console.WriteLine($"  Tag {i + 1}: {tags[i]}");
            }
            
            // 计算最优组合
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
                        await Task.Delay(1000);
                    }
                }
                // 调整时间点击确认按钮
                await TapAsync(900, 600);
                await Task.Delay(2000);
                await TapAsync(1950, 1150);
                await Task.Delay(2000);
                Console.WriteLine("点击调整时间 确认按钮");

                 return true;
            }
            else
            {
                // 存在六星tag，不选择任何tag
                Console.WriteLine("tag组合为空（异常或是六星tag）");
                return false;
            }
        }

        /// <summary>
        /// 计算最优Tag组合（四星优先逻辑）
        /// </summary>
        private List<int> CalculateBestTagCombination(string[] tags)
        {
            // 1. 检查是否存在高级资深干员标签（6星）或资深干员标签（5星）
            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i]?.Trim() ?? string.Empty;
                if (tag.Contains("高级资深干员") || tag.Contains("资深干员"))
                {
                    Console.WriteLine($"警告：检测到高级资深干员或资深干员标签（Tag {i + 1}: {tag}）");
                    Console.WriteLine("请人工处理此招募");
                    return new List<int>(); // 返回空组合，由调用者处理退出
                }
            }
            
            // 2. 加载干员数据（提前加载用于验证Tag有效性）
            // JSON文件位于当前工作目录下的 GameBot.Backend/Resource 文件夹
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "recruitment.json");
            
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"干员数据文件不存在: {jsonPath}");
                return new List<int>();
            }
            
            string jsonContent = File.ReadAllText(jsonPath);
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            
            // 3. 获取所有有效Tag集合（从json的tags字段中获取）
            HashSet<string> validTagSet = new HashSet<string>();
            foreach (var tagEntry in data.tags)
            {
                validTagSet.Add(tagEntry.Value.ToString());
            }
            
            Console.WriteLine($"JSON中定义的Tag数量: {validTagSet.Count}");
            
            // 4. 过滤有效Tag（非空且在json中定义）
            List<string> validTags = new List<string>();
            List<int> validTagIndices = new List<int>();
            
            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i]?.Trim();
                if (!string.IsNullOrEmpty(tag))
                {
                    if (validTagSet.Contains(tag))
                    {
                        validTags.Add(tag);
                        validTagIndices.Add(i);
                    }
                    else
                    {
                        Console.WriteLine($"警告：识别到无效的Tag: '{tag}'，已跳过（可能是OCR识别错误）");
                    }
                }
            }
            
            if (validTags.Count == 0)
            {
                Console.WriteLine("没有有效的Tag可选");
                return new List<int>();
            }
            
            Console.WriteLine($"有效Tag数量: {validTags.Count}");
            Console.WriteLine($"有效Tag列表: {string.Join(", ", validTags)}");
            
            // 4. 四星干员特殊词条（必出四星）
            HashSet<string> specialTags = new HashSet<string>
            {
                "爆发", "特种干员", "快速复活", "控场", "支援", "召唤", "削弱", "位移"
            };
            
            // 检查是否有特殊词条
            for (int i = 0; i < validTags.Count; i++)
            {
                if (specialTags.Contains(validTags[i]))
                {
                    Console.WriteLine($"检测到四星特殊词条: {validTags[i]}");
                    Console.WriteLine("选择该词条（保底四星）");
                    
                    // 保存截图到log文件夹
                    SaveScreenshotToLog(validTags[i]);
                    
                    return new List<int> { validTagIndices[i] };
                }
            }
            
            // 5. 检查三词条四星干员组合
            Console.WriteLine("检查三词条四星干员组合...");
            List<List<string>> fourStarTriples = GetFourStarTripleCombinations(data);
            
            if (fourStarTriples.Count > 0)
            {
                Console.WriteLine($"共有 {fourStarTriples.Count} 个三词条四星组合");
                
                // 生成所有3-tag组合并比对
                if (validTags.Count >= 3)
                {
                    List<List<int>> combinations = GenerateCombinations(validTags.Count, 3);
                    
                    foreach (var combo in combinations)
                    {
                        List<string> selectedTags = combo.Select(i => validTags[i]).OrderBy(t => t).ToList();
                        
                        foreach (var triple in fourStarTriples)
                        {
                            List<string> sortedTriple = triple.OrderBy(t => t).ToList();
                            
                            // 检查是否完全匹配
                            if (selectedTags.SequenceEqual(sortedTriple))
                            {
                                Console.WriteLine($"找到三词条四星组合: {string.Join(" + ", triple)}");
                                // 将三词条组合成一个字符串作为截图标识
                                string tagCombination = string.Join("+", triple);
                                SaveScreenshotToLog(tagCombination);
                                return combo.Select(i => validTagIndices[i]).ToList();
                            }
                        }
                    }
                }
            }
            
            // 6. 未找到匹配组合，选择第一个有效Tag
            Console.WriteLine("未找到精确匹配，选择第一个有效Tag");
            if (validTags.Count > 0)
            {
                return new List<int> { validTagIndices[0] };
            }
            
            return new List<int>();
        }

        /// <summary>
        /// 获取所有三词条四星干员的Tag组合
        /// </summary>
        private List<List<string>> GetFourStarTripleCombinations(dynamic data)
        {
            List<List<string>> triples = new List<List<string>>();
            
            foreach (var op in data.operators)
            {
                if (op.rarity == 4)
                {
                    List<string> tags = new List<string>();
                    foreach (var tag in op.tags)
                    {
                        tags.Add(tag.ToString());
                    }
                    
                    // 只保留恰好3个Tag的组合
                    if (tags.Count == 3)
                    {
                        triples.Add(tags);
                    }
                }
            }
            
            return triples;
        }

        /// <summary>
        /// 保存截图到log文件夹
        /// </summary>
        /// <param name="tagName">触发截图的词条名称</param>
        private void SaveScreenshotToLog(string tagName)
        {
            try
            {
                // 创建log文件夹
                string logDir = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory,
                    "log"
                );
                
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                
                // 生成带时间戳和词条名的文件名
                string fileName = $"screenshot_{tagName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(logDir, fileName);
                
                // 获取当前截图
                string? screenshotBase64 = _gameBotService.CaptureScreenAsync().Result;
                
                if (!string.IsNullOrEmpty(screenshotBase64))
                {
                    // 解码Base64并保存为PNG文件
                    byte[] imageBytes = Convert.FromBase64String(screenshotBase64);
                    File.WriteAllBytes(filePath, imageBytes);
                    
                    Console.WriteLine($"截图已保存到: {filePath}");
                }
                else
                {
                    Console.WriteLine("截图失败，无法保存到log文件夹");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存截图到log文件夹时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成组合索引（从n个元素中选k个的所有组合）
        /// </summary>
        private List<List<int>> GenerateCombinations(int n, int k)
        {
            List<List<int>> result = new List<List<int>>();
            GenerateCombinationsHelper(0, n, k, new List<int>(), result);
            return result;
        }

        private void GenerateCombinationsHelper(int start, int n, int k, List<int> current, List<List<int>> result)
        {
            if (current.Count == k)
            {
                result.Add(new List<int>(current));
                return;
            }
            
            for (int i = start; i < n; i++)
            {
                current.Add(i);
                GenerateCombinationsHelper(i + 1, n, k, current, result);
                current.RemoveAt(current.Count - 1);
            }
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
                await Task.Delay(2000);
                
                // 点击确认消耗加急许可立即招募
                await TapAsync(1700, 1000);
                Console.WriteLine("确认立即招募");
                await Task.Delay(2000);
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
