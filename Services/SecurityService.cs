using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic; // Added for Dictionary
using System.IO; // Added for MemoryStream and StreamWriter/StreamReader
using System.Security.Cryptography.Pkcs; // Added for CryptoStream
using LogisticManager.Constants;
using LogisticManager.Services;

namespace LogisticManager.Services
{
    /// <summary>
    /// 보안 관련 기능을 처리하는 서비스 클래스
    /// 환경 변수 처리, 암호화, 복호화 기능 제공
    /// </summary>
    public class SecurityService
    {
        private static readonly string _encryptionKey = "MySecretKey123!";
        
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
        /// 암호화된 문자열을 복호화하는 메서드
        /// </summary>
        /// <param name="encryptedText">암호화된 텍스트</param>
        /// <returns>복호화된 텍스트</returns>
        public static string DecryptString(string encryptedText)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                    return string.Empty;

                var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32, '0'));
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                
                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 복호화 실패: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 과거 형식(CBC + space padding 키)까지 호환하여 복호화 시도
        /// </summary>
        /// <param name="encryptedText">암호화된 텍스트</param>
        /// <returns>복호화된 텍스트 또는 빈 문자열</returns>
        public static string DecryptStringCompat(string encryptedText)
        {
            // 1차: 현재 형식(ECB + '0' 패딩 키)
            var ecb = DecryptString(encryptedText);
            if (!string.IsNullOrEmpty(ecb))
            {
                return ecb;
            }

            // 2차: 레거시 형식(CBC + space 패딩 키 + IV=0)
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                    return string.Empty;

                var keyBytesLegacy = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32)); // space pad
                var iv = new byte[16];
                var encryptedBytes = Convert.FromBase64String(encryptedText);

                using var aes = Aes.Create();
                aes.Key = keyBytesLegacy;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
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
                // Application.StartupPath를 사용하여 settings.json 파일 찾기
                var startupPath = Application.StartupPath;
                var configSettingsPath = Path.Combine(startupPath, "config", "settings.json");
                var rootSettingsPath = Path.Combine(startupPath, "settings.json");
                
                string settingsPath;
                
                // config/settings.json을 우선적으로 사용, 없으면 루트의 settings.json 사용
                if (File.Exists(configSettingsPath))
                {
                    settingsPath = configSettingsPath;
                }
                else if (File.Exists(rootSettingsPath))
                {
                    settingsPath = rootSettingsPath;
                }
                else
                {
                    throw new FileNotFoundException("settings.json 파일을 찾을 수 없습니다.");
                }
                
                var backupPath = Path.Combine(Path.GetDirectoryName(settingsPath) ?? startupPath, "settings.backup.json");
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
                // Application.StartupPath를 사용하여 settings.json 파일 찾기
                var startupPath = Application.StartupPath;
                var configSettingsPath = Path.Combine(startupPath, "config", "settings.json");
                var rootSettingsPath = Path.Combine(startupPath, "settings.json");
                
                string settingsPath;
                
                // config/settings.json을 우선적으로 사용, 없으면 루트의 settings.json 사용
                if (File.Exists(configSettingsPath))
                {
                    settingsPath = configSettingsPath;
                }
                else if (File.Exists(rootSettingsPath))
                {
                    settingsPath = rootSettingsPath;
                }
                else
                {
                    throw new FileNotFoundException("settings.json 파일을 찾을 수 없습니다.");
                }
                
                var backupPath = Path.Combine(Path.GetDirectoryName(settingsPath) ?? startupPath, "settings.backup.json");
                
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
                // DecryptString과 동일한 방식(ECB + PKCS7, 동일 키 패딩)으로 암호화하여 상호 운용 보장
                var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32, '0'));
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"암호화 실패: {ex.Message}");
                return plainText;
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
            
            var connectionString = string.Format(DatabaseConstants.CONNECTION_STRING_TEMPLATE, server, database, user, password, port);
            
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
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseConstants.SETTINGS_FILE_NAME);
                
                // 설정 파일 경로 검증
                var (pathValid, pathMessage) = SettingsValidationService.ValidateSettingsFilePath(settingsPath);
                if (!pathValid)
                {
                    Console.WriteLine($"❌ {pathMessage}");
                    Console.WriteLine(DatabaseConstants.ERROR_SETTINGS_FILE_NOT_FOUND);
                    throw new InvalidOperationException(DatabaseConstants.ERROR_SETTINGS_FILE_COMPLETELY_MISSING);
                }
                
                var jsonContent = File.ReadAllText(settingsPath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    Console.WriteLine("⚠️ SecurityService: settings.json 파일이 비어있음");
                    Console.WriteLine(DatabaseConstants.ERROR_SETTINGS_FILE_READ_FAILED);
                    throw new InvalidOperationException("설정 파일이 비어있습니다.");
                }
                
                //Console.WriteLine($"📄 SecurityService: settings.json 파일 내용: {jsonContent}");
                
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                if (settings == null)
                {
                    Console.WriteLine("❌ SecurityService: settings.json 파싱 실패");
                    Console.WriteLine(DatabaseConstants.ERROR_SETTINGS_FILE_PARSE_FAILED);
                    throw new InvalidOperationException("설정 파일 파싱에 실패했습니다.");
                }
                
                // 설정값 추출 (null 체크 포함)
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_SERVER, out var server) || string.IsNullOrWhiteSpace(server))
                {
                    Console.WriteLine("❌ SecurityService: DB_SERVER 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_NAME, out var database) || string.IsNullOrWhiteSpace(database))
                {
                    Console.WriteLine("❌ SecurityService: DB_NAME 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_USER, out var user) || string.IsNullOrWhiteSpace(user))
                {
                    Console.WriteLine("❌ SecurityService: DB_USER 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_PASSWORD, out var password) || string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("❌ SecurityService: DB_PASSWORD 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_PORT, out var port) || string.IsNullOrWhiteSpace(port))
                {
                    Console.WriteLine("❌ SecurityService: DB_PORT 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                // 설정값 엄격한 검증 (이제 null이 아님을 보장)
                var (isValid, validationMessages) = SettingsValidationService.ValidateDatabaseSettings(server, database, user, password, port);
                if (!isValid)
                {
                    Console.WriteLine("❌ SecurityService: 설정값 유효성 검증 실패:");
                    foreach (var message in validationMessages)
                    {
                        Console.WriteLine($"   {message}");
                    }
                    
                    // 필수값이 누락된 경우 프로그램 중단
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                Console.WriteLine($"✅ SecurityService: settings.json에서 데이터베이스 설정을 성공적으로 읽어왔습니다.");
                Console.WriteLine(DatabaseConstants.SUCCESS_SETTINGS_LOADED);
                return (server, database, user, password, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SecurityService: settings.json 읽기 실패: {ex.Message}");
                Console.WriteLine(DatabaseConstants.ERROR_SETTINGS_FILE_READ_FAILED);
                throw new InvalidOperationException($"설정 파일 읽기 실패: {ex.Message}", ex);
            }
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