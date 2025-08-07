using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic; // Added for Dictionary
using System.IO; // Added for MemoryStream and StreamWriter/StreamReader
using System.Security.Cryptography.Pkcs; // Added for CryptoStream

namespace LogisticManager.Services
{
    /// <summary>
    /// 보안 관련 기능을 처리하는 서비스 클래스
    /// 환경 변수 처리, 암호화, 복호화 기능 제공
    /// </summary>
    public class SecurityService
    {
        private static readonly string _encryptionKey = "LogisticManager2024!@#";
        
        /// <summary>
        /// 환경 변수로 설정된 값을 가져오는 메서드 (JSON 파일에서도 로드)
        /// </summary>
        /// <param name="key">환경 변수 키</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns>환경 변수 값 또는 기본값</returns>
        public static string GetEnvironmentVariable(string key, string defaultValue = "")
        {
            try
            {
                // 시스템 환경 변수에서 먼저 찾기
                var value = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"✅ 시스템 환경 변수에서 '{key}' = '{value}' 찾음");
                    return value?.ToString() ?? defaultValue;
                }
                
                // 사용자 환경 변수에서 찾기
                value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
                if (!string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"✅ 사용자 환경 변수에서 '{key}' = '{value}' 찾음");
                    return value?.ToString() ?? defaultValue;
                }
                
                // JSON 파일에서 찾기
                value = LoadFromJsonFile(key);
                if (!string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"✅ JSON 파일에서 '{key}' = '{value}' 찾음");
                    return value;
                }
                
                Console.WriteLine($"⚠️ '{key}' 값을 찾을 수 없음, 기본값 '{defaultValue}' 사용");
                return defaultValue;
            }
            catch (Exception ex)
            {
                // 로그 기록 (실제 구현에서는 로깅 서비스 사용)
                Console.WriteLine($"❌ 환경 변수 '{key}' 읽기 실패: {ex.Message}");
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 환경 변수를 설정하는 메서드 (JSON 파일과 환경 변수 모두 저장)
        /// </summary>
        /// <param name="key">환경 변수 키</param>
        /// <param name="value">설정할 값</param>
        /// <param name="target">환경 변수 대상 (기본값: User)</param>
        public static void SetEnvironmentVariable(string key, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.User)
        {
            try
            {
                // 환경 변수에 저장
                Environment.SetEnvironmentVariable(key, value, target);
                
                // JSON 파일에도 저장 (영구 보존)
                SaveToJsonFile(key, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"환경 변수 '{key}' 설정 실패: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// App.config의 설정값에서 환경 변수를 치환하는 메서드
        /// </summary>
        /// <param name="configValue">설정값</param>
        /// <returns>환경 변수가 치환된 값</returns>
        public static string ResolveEnvironmentVariables(string configValue)
        {
            if (string.IsNullOrEmpty(configValue))
                return configValue;
            
            try
            {
                // %VARIABLE_NAME% 패턴을 찾아서 환경 변수로 치환
                var pattern = @"%([^%]+)%";
                var result = Regex.Replace(configValue, pattern, match =>
                {
                    var variableName = match.Groups[1].Value;
                    var envValue = GetEnvironmentVariable(variableName, match.Value);
                    return envValue;
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"환경 변수 치환 중 오류 발생: {ex.Message}");
                return configValue; // 오류 발생 시 원본 값 반환
            }
        }
        
        /// <summary>
        /// JSON 파일에 설정을 저장하는 메서드 (성능 최적화 + 백업)
        /// </summary>
        /// <param name="key">설정 키</param>
        /// <param name="value">설정 값</param>
        private static void SaveToJsonFile(string key, string value)
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.backup.json");
                var settings = new Dictionary<string, string>();
                
                // 기존 설정 로드 (파일이 존재하고 크기가 0이 아닌 경우에만)
                if (File.Exists(settingsPath) && new FileInfo(settingsPath).Length > 0)
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(settingsPath);
                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ 기존 JSON 파일 읽기 실패, 백업에서 복구 시도: {ex.Message}");
                        
                        // 백업 파일에서 복구 시도
                        if (File.Exists(backupPath))
                        {
                            try
                            {
                                var backupContent = File.ReadAllText(backupPath);
                                settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(backupContent) ?? new Dictionary<string, string>();
                                Console.WriteLine("✅ 백업 파일에서 복구 성공");
                            }
                            catch
                            {
                                settings = new Dictionary<string, string>();
                                Console.WriteLine("❌ 백업 파일 복구도 실패, 새로 시작");
                            }
                        }
                        else
                        {
                            settings = new Dictionary<string, string>();
                        }
                    }
                }
                
                // 새 설정 추가/업데이트
                settings[key] = value;
                
                // 백업 파일 생성
                var backupJson = System.Text.Json.JsonSerializer.Serialize(settings);
                File.WriteAllText(backupPath, backupJson);
                
                // JSON 파일에 저장 (성능 최적화: 들여쓰기 없이 저장)
                var jsonString = System.Text.Json.JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsPath, jsonString);
                
                Console.WriteLine($"✅ 설정 '{key}' = '{value}' 저장 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JSON 파일 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// JSON 파일에서 설정을 로드하는 메서드 (백업 파일 지원)
        /// </summary>
        /// <param name="key">설정 키</param>
        /// <returns>설정 값 또는 null</returns>
        private static string? LoadFromJsonFile(string key)
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.backup.json");
                
                // 메인 파일에서 로드 시도
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        Console.WriteLine($"📄 메인 JSON 파일 내용: {jsonContent}");
                        
                        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                        if (settings != null && settings.ContainsKey(key))
                        {
                            Console.WriteLine($"✅ 메인 JSON에서 '{key}' = '{settings[key]}' 찾음");
                            return settings[key];
                        }
                        else if (settings != null)
                        {
                            Console.WriteLine($"⚠️ 메인 JSON에서 '{key}' 키를 찾을 수 없음. 사용 가능한 키: {string.Join(", ", settings.Keys)}");
                        }
                        else
                        {
                            Console.WriteLine("❌ 메인 JSON 파싱 실패");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ 메인 JSON 파일이 비어있음");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ 메인 JSON 파일이 존재하지 않음: {settingsPath}");
                }
                
                // 백업 파일에서 로드 시도
                if (File.Exists(backupPath))
                {
                    Console.WriteLine("🔄 백업 파일에서 로드 시도...");
                    var backupContent = File.ReadAllText(backupPath);
                    if (!string.IsNullOrEmpty(backupContent))
                    {
                        Console.WriteLine($"📄 백업 JSON 파일 내용: {backupContent}");
                        
                        var backupSettings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(backupContent);
                        if (backupSettings != null && backupSettings.ContainsKey(key))
                        {
                            Console.WriteLine($"✅ 백업 JSON에서 '{key}' = '{backupSettings[key]}' 찾음");
                            return backupSettings[key];
                        }
                        else if (backupSettings != null)
                        {
                            Console.WriteLine($"⚠️ 백업 JSON에서 '{key}' 키를 찾을 수 없음. 사용 가능한 키: {string.Join(", ", backupSettings.Keys)}");
                        }
                        else
                        {
                            Console.WriteLine("❌ 백업 JSON 파싱 실패");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ 백업 JSON 파일이 비어있음");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ 백업 JSON 파일도 존재하지 않음");
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JSON 파일 로드 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 문자열을 암호화하는 메서드
        /// </summary>
        /// <param name="plainText">암호화할 텍스트</param>
        /// <returns>암호화된 Base64 문자열</returns>
        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;
            
            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];
                
                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);
                
                swEncrypt.Write(plainText);
                swEncrypt.Flush();
                csEncrypt.FlushFinalBlock();
                
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"암호화 실패: {ex.Message}");
                return plainText;
            }
        }
        
        /// <summary>
        /// 암호화된 문자열을 복호화하는 메서드
        /// </summary>
        /// <param name="cipherText">복호화할 Base64 문자열</param>
        /// <returns>복호화된 텍스트</returns>
        public static string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;
            
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];
                
                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipherBytes);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                
                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"복호화 실패: {ex.Message}");
                return cipherText;
            }
        }
        
        /// <summary>
        /// 안전한 연결 문자열을 생성하는 메서드
        /// </summary>
        /// <returns>JSON 파일에서 읽은 연결 문자열</returns>
        public static string GetSecureConnectionString()
        {
            // settings.json에서 직접 데이터베이스 설정 읽기
            var (server, database, user, password, port) = LoadDatabaseSettingsFromJson();
            
            var connectionString = $"Server={server};Database={database};User Id={user};Password={password};Port={port};CharSet=utf8;Convert Zero Datetime=True;Allow User Variables=True;";
            
            Console.WriteLine($"🔗 SecurityService: 연결 문자열 생성 완료 (서버: {server}, DB: {database}, 사용자: {user})");
            
            return connectionString;
        }

        /// <summary>
        /// settings.json에서 직접 데이터베이스 설정을 읽어오는 메서드
        /// </summary>
        /// <returns>데이터베이스 설정 튜플</returns>
        private static (string server, string database, string user, string password, string port) LoadDatabaseSettingsFromJson()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        Console.WriteLine($"📄 SecurityService: settings.json 파일 내용: {jsonContent}");
                        
                        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                        if (settings != null)
                        {
                            var server = settings.GetValueOrDefault("DB_SERVER", "gramwonlogis2.mycafe24.com");
                            var database = settings.GetValueOrDefault("DB_NAME", "gramwonlogis2");
                            var user = settings.GetValueOrDefault("DB_USER", "gramwonlogis2");
                            var password = settings.GetValueOrDefault("DB_PASSWORD", "jung5516!");
                            var port = settings.GetValueOrDefault("DB_PORT", "3306");
                            
                            Console.WriteLine($"✅ SecurityService: settings.json에서 데이터베이스 설정을 성공적으로 읽어왔습니다.");
                            return (server, database, user, password, port);
                        }
                        else
                        {
                            Console.WriteLine("❌ SecurityService: settings.json 파싱 실패");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ SecurityService: settings.json 파일이 비어있음");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ SecurityService: settings.json 파일이 존재하지 않음: {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SecurityService: settings.json 읽기 실패: {ex.Message}");
            }
            
            // 기본값 반환
            Console.WriteLine("🔄 SecurityService: 기본값을 사용합니다.");
            return ("gramwonlogis2.mycafe24.com", "gramwonlogis2", "gramwonlogis2", "jung5516!", "3306");
        }
        
        /// <summary>
        /// 모든 필수 환경 변수가 설정되어 있는지 확인하는 메서드
        /// </summary>
        /// <returns>모든 필수 환경 변수가 설정되어 있으면 true</returns>
        public static bool ValidateEnvironmentVariables()
        {
            var requiredVariables = new[]
            {
                "DB_PASSWORD",
                "DROPBOX_API_KEY",
                "KAKAO_WORK_API_KEY",
                "KAKAO_CHATROOM_ID"
            };
            
            foreach (var variable in requiredVariables)
            {
                var value = GetEnvironmentVariable(variable, "");
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 환경 변수 설정 상태를 확인하는 메서드
        /// </summary>
        /// <returns>설정 상태 정보</returns>
        public static Dictionary<string, bool> GetEnvironmentVariableStatus()
        {
            var status = new Dictionary<string, bool>();
            
            var variables = new[]
            {
                "DB_SERVER", "DB_NAME", "DB_USER", "DB_PASSWORD", "DB_PORT",
                "DROPBOX_API_KEY", "KAKAO_WORK_API_KEY", "KAKAO_CHATROOM_ID"
            };
            
            foreach (var variable in variables)
            {
                var value = GetEnvironmentVariable(variable, "");
                status[variable] = !string.IsNullOrEmpty(value);
            }
            
            return status;
        }
    }
} 