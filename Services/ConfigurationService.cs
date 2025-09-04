using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LogisticManager.Services
{
    /// <summary>
    /// 설정 파일들을 읽는 서비스 클래스
    /// 
    /// 주요 기능:
    /// - App.config를 리소스에서 읽기
    /// - settings.json을 파일 시스템에서 읽기 (Application.StartupPath 사용)
    /// - column_mapping.json을 리소스에서 읽기
    /// - 설정 파일이 없을 때 기본값 제공
    /// </summary>
    public static class ConfigurationService
    {
        /// <summary>
        /// 리소스에서 설정 파일을 읽는 메서드
        /// </summary>
        /// <param name="resourceName">리소스 이름</param>
        /// <returns>설정 파일 내용</returns>
        public static string ReadResourceFile(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fullResourceName = $"{assembly.GetName().Name}.{resourceName}";
                
                using var stream = assembly.GetManifestResourceStream(fullResourceName);
                if (stream == null)
                {
                    Console.WriteLine($"⚠️ 리소스 파일을 찾을 수 없습니다: {fullResourceName}");
                    return string.Empty;
                }
                
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 리소스 파일 읽기 중 오류 발생: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// App.config를 리소스에서 읽기
        /// </summary>
        /// <returns>App.config 내용</returns>
        public static string ReadAppConfig()
        {
            return ReadResourceFile("App.config");
        }

        /// <summary>
        /// settings.json을 파일 시스템에서 읽기 (Application.StartupPath 사용)
        /// </summary>
        /// <returns>JSON 내용</returns>
        public static string ReadSettingsJson()
        {
            try
            {
                // Application.StartupPath를 사용하여 settings.json 파일 찾기
                var startupPath = Application.StartupPath;
                var configSettingsPath = Path.Combine(startupPath, "config", "settings.json");
                var rootSettingsPath = Path.Combine(startupPath, "settings.json");
                
                string settingsPath;
                
                // config/settings.json을 우선적으로 사용, 없으면 루트의 settings.json 사용
                if (File.Exists(configSettingsPath))
                {
                    settingsPath = configSettingsPath;
                    Console.WriteLine($"🔍 ConfigurationService: config/settings.json 사용: {settingsPath}");
                }
                else if (File.Exists(rootSettingsPath))
                {
                    settingsPath = rootSettingsPath;
                    Console.WriteLine($"🔍 ConfigurationService: 루트 settings.json 사용: {settingsPath}");
                }
                else
                {
                    Console.WriteLine($"⚠️ ConfigurationService: settings.json 파일을 찾을 수 없습니다.");
                    Console.WriteLine($"   config 폴더: {configSettingsPath}");
                    Console.WriteLine($"   루트 폴더: {rootSettingsPath}");
                    return string.Empty;
                }
                
                // 파일 내용 읽기
                var jsonContent = File.ReadAllText(settingsPath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    Console.WriteLine("⚠️ ConfigurationService: settings.json 파일이 비어있음");
                    return string.Empty;
                }
                
                Console.WriteLine($"✅ ConfigurationService: settings.json 파일을 성공적으로 읽었습니다.");
                return jsonContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ConfigurationService: settings.json 파일 읽기 실패: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// column_mapping.json을 리소스에서 읽기
        /// </summary>
        /// <returns>JSON 내용</returns>
        public static string ReadColumnMappingJson()
        {
            return ReadResourceFile("column_mapping.json");
        }
    }
} 