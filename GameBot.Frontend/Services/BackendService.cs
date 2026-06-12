using System;
using System.Reflection;

namespace GameBot.Frontend.Services
{
    /// <summary>
    /// BackendService 类 - 简单的后端 DLL 加载器和调用器
    /// </summary>
    public class BackendService
    {
        #region 单例模式

        private static BackendService? _instance;

        public static BackendService Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new BackendService();
                }
                return _instance;
            }
        }

        #endregion

        #region 私有字段

        private Assembly? _assembly;
        private object? _service;
        private Type? _serviceType;

        #endregion

        #region 加载 DLL

        public bool Load(string dllPath)
        {
            try
            {
                _assembly = Assembly.LoadFrom(dllPath);
                _serviceType = _assembly.GetType("GameBot.Backend.Services.GameBotService");
                if (_serviceType == null)
                    return false;
                _service = Activator.CreateInstance(_serviceType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool LoadFromAppDirectory()
        {
            // DLL 已通过项目配置自动复制到输出目录
            string[] paths = {
                "GameBot.Backend.dll"
            };

            foreach (string path in paths)
            {
                if (Load(path))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsLoaded => _assembly != null && _service != null;

        #endregion

        #region 方法调用

        public string InvokeString(string methodName, params object[] args)
        {
            if (!IsLoaded) return string.Empty;
            
            try
            {
                var method = _serviceType.GetMethod(methodName);
                if (method == null) return string.Empty;
                var result = method.Invoke(_service, args);
                return result as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public int InvokeInt(string methodName, params object[] args)
        {
            if (!IsLoaded) return -1;
            
            try
            {
                var method = _serviceType.GetMethod(methodName);
                if (method == null) return -1;
                var result = method.Invoke(_service, args);
                return result is int ? (int)result : -1;
            }
            catch
            {
                return -1;
            }
        }

        public bool InvokeBool(string methodName, params object[] args)
        {
            if (!IsLoaded) return false;
            
            try
            {
                var method = _serviceType.GetMethod(methodName);
                if (method == null) return false;
                var result = method.Invoke(_service, args);
                return result is bool ? (bool)result : false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 清理

        public void Cleanup()
        {
            if (_service is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _assembly = null;
            _service = null;
            _serviceType = null;
        }

        #endregion
    }
}
