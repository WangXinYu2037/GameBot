using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Tesseract;

namespace GameBot.Backend.Services
{
    /// <summary>
    /// OcrService 类 - 基于 Tesseract 的文字识别服务
    /// </summary>
    public class OcrService : IDisposable
    {
        private static OcrService? _instance;
        private static readonly object _lock = new object();
        private TesseractEngine? _engine;
        private bool _isInitialized;

        private OcrService()
        {
            InitializeEngine();
        }

        /// <summary>
        /// 单例实例
        /// </summary>
        public static OcrService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new OcrService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化 Tesseract 引擎
        /// </summary>
        private void InitializeEngine()
        {
            try
            {
                // 获取 tessdata 目录路径
                string tessdataPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "tessdata"
                );

                // 如果不存在 tessdata 目录，创建并使用默认配置
                if (!Directory.Exists(tessdataPath))
                {
                    Directory.CreateDirectory(tessdataPath);
                    Console.WriteLine($"tessdata 目录创建于: {tessdataPath}");
                }

                // 创建 Tesseract 引擎
                // 使用中文+英文语言包
                _engine = new TesseractEngine(tessdataPath, "chi_sim+eng", EngineMode.Default);
                
                // 启用自动页面分割模式
                _engine.SetVariable("tessedit_pageseg_mode", "6");
                
                _isInitialized = true;
                Console.WriteLine("Tesseract OCR 引擎初始化成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tesseract OCR 引擎初始化失败: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// 识别全图文字
        /// </summary>
        public async Task<string?> RecognizeTextAsync(string imageBase64)
        {
            return await RecognizeTextInRegionAsync(imageBase64, null);
        }

        /// <summary>
        /// 识别指定区域文字
        /// </summary>
        public async Task<string?> RecognizeTextInRegionAsync(string imageBase64, Rect? region)
        {
            if (string.IsNullOrEmpty(imageBase64))
            {
                Console.WriteLine("图像数据为空");
                return null;
            }

            if (!_isInitialized || _engine == null)
            {
                Console.WriteLine("OCR 引擎未初始化");
                return null;
            }

            try
            {
                // 解码 Base64 图像
                byte[] imageBytes = Convert.FromBase64String(imageBase64);
                byte[] targetImageBytes = imageBytes;

                // 如果指定了区域，裁剪图像
                if (region.HasValue)
                {
                    Rect r = region.Value;
                    Console.WriteLine($"识别区域: X={r.X}, Y={r.Y}, Width={r.Width}, Height={r.Height}");

                    targetImageBytes = CropImage(imageBytes, r);
                    if (targetImageBytes == null)
                    {
                        return "区域无效";
                    }
                }

                // 使用 Tesseract 识别
                using (MemoryStream ms = new MemoryStream(targetImageBytes))
                using (Bitmap bitmap = new Bitmap(ms))
                using (Pix pix = Pix.LoadFromMemory(targetImageBytes))
                using (Page page = _engine.Process(pix))
                {
                    string result = page.GetText();
                    
                    // 移除所有空白字符（包括空格、制表符、换行符等）
                    string cleanedResult = new string(result.Where(c => !char.IsWhiteSpace(c)).ToArray());
                    
                    Console.WriteLine($"OCR 识别成功，识别到 {cleanedResult.Length} 个字符");
                    return await Task.FromResult(cleanedResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR 识别异常: {ex.Message}");
                return await Task.FromResult($"OCR识别异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量识别多个区域
        /// </summary>
        public async Task<string?[]> RecognizeTextInRegionsAsync(string imageBase64, Rect[] regions)
        {
            if (regions == null || regions.Length == 0)
            {
                return new[] { await RecognizeTextAsync(imageBase64) };
            }

            string?[] results = new string[regions.Length];

            for (int i = 0; i < regions.Length; i++)
            {
                results[i] = await RecognizeTextInRegionAsync(imageBase64, regions[i]);
            }

            return results;
        }

        /// <summary>
        /// 裁剪图像到指定区域
        /// </summary>
        private byte[]? CropImage(byte[] imageBytes, Rect region)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(imageBytes))
                using (Bitmap originalBitmap = new Bitmap(ms))
                {
                    // 检查区域是否在图像范围内
                    if (region.X < 0 || region.Y < 0 ||
                        region.X + region.Width > originalBitmap.Width ||
                        region.Y + region.Height > originalBitmap.Height)
                    {
                        Console.WriteLine($"区域超出图像范围！图像尺寸: {originalBitmap.Width}x{originalBitmap.Height}");
                        return null;
                    }

                    // 裁剪图像
                    using (Bitmap croppedBitmap = originalBitmap.Clone(
                        new System.Drawing.Rectangle(region.X, region.Y, region.Width, region.Height),
                        originalBitmap.PixelFormat))
                    {
                        // 保存裁剪后的图像用于调试
                        string debugPath = Path.Combine(
                            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory,
                            "Test"
                        );
                        if (!Directory.Exists(debugPath))
                        {
                            Directory.CreateDirectory(debugPath);
                        }
                        croppedBitmap.Save(Path.Combine(debugPath, $"tesseract_region_{region.X}_{region.Y}.png"), System.Drawing.Imaging.ImageFormat.Png);

                        // 转换为 byte[]
                        using (MemoryStream croppedMs = new MemoryStream())
                        {
                            croppedBitmap.Save(croppedMs, System.Drawing.Imaging.ImageFormat.Png);
                            return croppedMs.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"裁剪图像失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
        }

        /// <summary>
        /// 强制释放资源
        /// </summary>
        public static void ForceDispose()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    _instance._engine?.Dispose();
                    _instance._engine = null;
                    _instance = null;
                }
            }
        }
    }

    /// <summary>
    /// 区域结构体
    /// </summary>
    public struct Rect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
