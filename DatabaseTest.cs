using System;
using MySqlConnector;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

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
                
                var connectionString = $"Server={server};Database={database};User ID={user};Password={password};Port={port};CharSet=utf8mb4;SslMode=none;AllowPublicKeyRetrieval=true;Convert Zero Datetime=True;";
                
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
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        Console.WriteLine($"📄 DatabaseTest: settings.json 파일 내용: {jsonContent}");
                        
                        var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                        if (settings != null)
                        {
                            var server = settings.GetValueOrDefault("DB_SERVER", "gramwonlogis2.mycafe24.com");
                            var database = settings.GetValueOrDefault("DB_NAME", "gramwonlogis2");
                            var user = settings.GetValueOrDefault("DB_USER", "gramwonlogis2");
                            var password = settings.GetValueOrDefault("DB_PASSWORD", "jung5516!");
                            var port = settings.GetValueOrDefault("DB_PORT", "3306");
                            
                            Console.WriteLine($"✅ DatabaseTest: settings.json에서 데이터베이스 설정을 성공적으로 읽어왔습니다.");
                            return (server, database, user, password, port);
                        }
                        else
                        {
                            Console.WriteLine("❌ DatabaseTest: settings.json 파싱 실패");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ DatabaseTest: settings.json 파일이 비어있음");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ DatabaseTest: settings.json 파일이 존재하지 않음: {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseTest: settings.json 읽기 실패: {ex.Message}");
            }
            
            // 기본값 반환
            Console.WriteLine("🔄 DatabaseTest: 기본값을 사용합니다.");
            return ("gramwonlogis2.mycafe24.com", "gramwonlogis2", "gramwonlogis2", "jung5516!", "3306");
        }
    }
} 