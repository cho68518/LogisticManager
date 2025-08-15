using System;
using MySqlConnector;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using LogisticManager.Constants;
using LogisticManager.Services;

namespace LogisticManager
{
    public class DatabaseTest
    {
        public static void TestConnection()
        {
            // 로그 파일 경로
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            
            try
            {
                Console.WriteLine("🔍 DatabaseTest: 연결 테스트 시작");
                File.AppendAllText(logPath, "🔍 DatabaseTest: 연결 테스트 시작\n");
                
                // settings.json에서 직접 데이터베이스 설정 읽기
                var (server, database, user, password, port) = LoadDatabaseSettingsFromJson();
                
                // 설정값 엄격한 검증 (필수 값이 누락된 경우 테스트 중단)
                var (isValid, validationMessages) = SettingsValidationService.ValidateDatabaseSettings(server, database, user, password, port);
                if (!isValid)
                {
                    Console.WriteLine("❌ DatabaseTest: 설정값 유효성 검증 실패:");
                    foreach (var message in validationMessages)
                    {
                        Console.WriteLine($"   {message}");
                    }
                    
                    // 필수값이 누락된 경우 테스트 중단
                    Console.WriteLine(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                    return;
                }
                
                Console.WriteLine($"🔍 DatabaseTest: 설정값 검증");
                File.AppendAllText(logPath, "🔍 DatabaseTest: 설정값 검증\n");
                Console.WriteLine($"   DB_SERVER: '{server}' (길이: {server?.Length ?? 0})");
                File.AppendAllText(logPath, $"   DB_SERVER: '{server}' (길이: {server?.Length ?? 0})\n");
                Console.WriteLine($"   DB_NAME: '{database}' (길이: {database?.Length ?? 0})");
                File.AppendAllText(logPath, $"   DB_NAME: '{database}' (길이: {database?.Length ?? 0})\n");
                Console.WriteLine($"   DB_USER: '{user}' (길이: {user?.Length ?? 0})");
                File.AppendAllText(logPath, $"   DB_USER: '{user}' (길이: {user?.Length ?? 0})\n");
                Console.WriteLine($"   DB_PASSWORD: '{password}' (길이: {password?.Length ?? 0})");
                File.AppendAllText(logPath, $"   DB_PASSWORD: '{password}' (길이: {password?.Length ?? 0})\n");
                Console.WriteLine($"   DB_PORT: '{port}' (길이: {port?.Length ?? 0})");
                File.AppendAllText(logPath, $"   DB_PORT: '{port}' (길이: {port?.Length ?? 0})\n");
                
                var connectionString = string.Format(DatabaseConstants.CONNECTION_STRING_UTF8MB4_TEMPLATE, server, database, user, password, port);
                
                Console.WriteLine($"🔗 DatabaseTest: 연결 문자열 생성 완료");
                File.AppendAllText(logPath, "🔗 DatabaseTest: 연결 문자열 생성 완료\n");
                Console.WriteLine($"   서버: {server}");
                File.AppendAllText(logPath, $"   서버: {server}\n");
                Console.WriteLine($"   데이터베이스: {database}");
                File.AppendAllText(logPath, $"   데이터베이스: {database}\n");
                Console.WriteLine($"   사용자: {user}");
                File.AppendAllText(logPath, $"   사용자: {user}\n");
                Console.WriteLine($"   포트: {port}");
                File.AppendAllText(logPath, $"   포트: {port}\n");
                
                try
                {
                    Console.WriteLine("🌐 DatabaseTest: MySqlConnection 객체 생성 시도...");
                    File.AppendAllText(logPath, "🌐 DatabaseTest: MySqlConnection 객체 생성 시도...\n");
                    using (var connection = new MySqlConnection(connectionString))
                    {
                        Console.WriteLine("✅ DatabaseTest: MySqlConnection 객체 생성 성공");
                        File.AppendAllText(logPath, "✅ DatabaseTest: MySqlConnection 객체 생성 성공\n");
                        Console.WriteLine("🌐 DatabaseTest: 데이터베이스 연결 시도...");
                        File.AppendAllText(logPath, "🌐 DatabaseTest: 데이터베이스 연결 시도...\n");
                        Console.WriteLine($"서버: {server}");
                        File.AppendAllText(logPath, $"서버: {server}\n");
                        Console.WriteLine($"데이터베이스: {database}");
                        File.AppendAllText(logPath, $"데이터베이스: {database}\n");
                        Console.WriteLine($"사용자: {user}");
                        File.AppendAllText(logPath, $"사용자: {user}\n");
                        Console.WriteLine($"포트: {port}");
                        File.AppendAllText(logPath, $"포트: {port}\n");
                        
                        connection.Open();
                        Console.WriteLine("✅ DatabaseTest: 데이터베이스 연결 성공!");
                        File.AppendAllText(logPath, "✅ DatabaseTest: 데이터베이스 연결 성공!\n");
                        
                        // 간단한 쿼리 테스트
                        using (var command = new MySqlCommand("SELECT 1 as test_result", connection))
                        {
                            var result = command.ExecuteScalar();
                            Console.WriteLine($"📊 DatabaseTest: 테스트 쿼리 결과: {result}");
                            File.AppendAllText(logPath, $"📊 DatabaseTest: 테스트 쿼리 결과: {result}\n");
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"❌ DatabaseTest: 데이터베이스 연결 실패: {dbEx.Message}");
                    File.AppendAllText(logPath, $"❌ DatabaseTest: 데이터베이스 연결 실패: {dbEx.Message}\n");
                    Console.WriteLine($"🔍 DatabaseTest: DB 예외 타입: {dbEx.GetType().Name}");
                    File.AppendAllText(logPath, $"🔍 DatabaseTest: DB 예외 타입: {dbEx.GetType().Name}\n");
                    Console.WriteLine($"🔍 DatabaseTest: DB 예외 상세: {dbEx}");
                    File.AppendAllText(logPath, $"🔍 DatabaseTest: DB 예외 상세: {dbEx}\n");
                    
                    if (dbEx.InnerException != null)
                    {
                        Console.WriteLine($"🔍 DatabaseTest: 내부 예외: {dbEx.InnerException.Message}");
                        File.AppendAllText(logPath, $"🔍 DatabaseTest: 내부 예외: {dbEx.InnerException.Message}\n");
                        Console.WriteLine($"🔍 DatabaseTest: 내부 예외 타입: {dbEx.InnerException.GetType().Name}");
                        File.AppendAllText(logPath, $"🔍 DatabaseTest: 내부 예외 타입: {dbEx.InnerException.GetType().Name}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseTest: 일반 오류: {ex.Message}");
                File.AppendAllText(logPath, $"❌ DatabaseTest: 일반 오류: {ex.Message}\n");
            }
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
                    Console.WriteLine("⚠️ DatabaseTest: settings.json 파일이 비어있음");
                    Console.WriteLine(DatabaseConstants.ERROR_SETTINGS_FILE_READ_FAILED);
                    throw new InvalidOperationException("설정 파일이 비어있습니다.");
                }
                
                Console.WriteLine($"📄 DatabaseTest: settings.json 파일 내용: {jsonContent}");
                
                var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                if (settings == null)
                {
                    Console.WriteLine("❌ DatabaseTest: settings.json 파싱 실패");
                    Console.WriteLine(DatabaseConstants.ERROR_SETTINGS_FILE_PARSE_FAILED);
                    throw new InvalidOperationException("설정 파일 파싱에 실패했습니다.");
                }
                
                // 설정값 추출 (null 체크 포함)
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_SERVER, out var server) || string.IsNullOrWhiteSpace(server))
                {
                    Console.WriteLine("❌ DatabaseTest: DB_SERVER 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_NAME, out var database) || string.IsNullOrWhiteSpace(database))
                {
                    Console.WriteLine("❌ DatabaseTest: DB_NAME 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_USER, out var user) || string.IsNullOrWhiteSpace(user))
                {
                    Console.WriteLine("❌ DatabaseTest: DB_USER 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_PASSWORD, out var password) || string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("❌ DatabaseTest: DB_PASSWORD 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_PORT, out var port) || string.IsNullOrWhiteSpace(port))
                {
                    Console.WriteLine("❌ DatabaseTest: DB_PORT 설정값이 누락되었습니다.");
                    throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                }
                
                Console.WriteLine($"✅ DatabaseTest: settings.json에서 데이터베이스 설정을 성공적으로 읽어왔습니다.");
                Console.WriteLine(DatabaseConstants.SUCCESS_SETTINGS_LOADED);
                return (server, database, user, password, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseTest: settings.json 읽기 실패: {ex.Message}");
                Console.WriteLine(DatabaseConstants.ERROR_SETTINGS_FILE_READ_FAILED);
                throw new InvalidOperationException($"설정 파일 읽기 실패: {ex.Message}", ex);
            }
        }
        

    }
} 