using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LogisticManager.Services
{
    /// <summary>
    /// 설정 파일들을 리소스로 읽는 서비스 클래스
    /// 
    /// 주요 기능:
    /// - App.config를 리소스에서 읽기
    /// - settings.json을 리소스에서 읽기
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
        /// settings.json을 리소스에서 읽기
        /// </summary>
        /// <returns>JSON 내용</returns>
        public static string ReadSettingsJson()
        {
            return ReadResourceFile("settings.json");
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