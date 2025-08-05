using System;
using MySql.Data.MySqlClient;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json;

namespace LogisticManager
{
    public class DatabaseTest
    {
        public static void TestConnection()
        {
            // settings.json에서 설정을 읽어서 연결 문자열 생성
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            var settings = new Dictionary<string, string>();
            
            Console.WriteLine($"🔍 DatabaseTest: 설정 파일 경로 = {settingsPath}");
            
            try
            {
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    Console.WriteLine($"📄 DatabaseTest: JSON 파일 내용 = {jsonContent}");
                    
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        try
                        {
                            // Newtonsoft.Json을 사용하여 더 안전하게 역직렬화
                            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                            Console.WriteLine($"✅ DatabaseTest: JSON에서 {settings.Count}개 설정 로드");
                            
                            // 각 설정값 로깅
                            foreach (var setting in settings)
                            {
                                Console.WriteLine($"📋 DatabaseTest: {setting.Key} = {setting.Value}");
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            Console.WriteLine($"❌ DatabaseTest: JSON 역직렬화 실패: {jsonEx.Message}");
                            
                            // JSON 역직렬화 실패 시 기본값 사용
                            Console.WriteLine("⚠️ DatabaseTest: 기본값을 사용합니다.");
                            settings = new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ DatabaseTest: JSON 파일이 비어있음");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ DatabaseTest: 설정 파일이 존재하지 않음 = {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseTest: JSON 파일 읽기 실패: {ex.Message}");
                Console.WriteLine($"🔍 DatabaseTest: 예외 상세: {ex}");
            }
            
            // JSON에서 설정을 읽어오거나 기본값 사용
            var server = settings.GetValueOrDefault("DB_SERVER", "gramwonlogis.mycafe24.com");
            var database = settings.GetValueOrDefault("DB_NAME", "gramwonlogis");
            var user = settings.GetValueOrDefault("DB_USER", "gramwonlogis");
            var password = settings.GetValueOrDefault("DB_PASSWORD", "jung5516!");
            var port = settings.GetValueOrDefault("DB_PORT", "3306");
            
            var connectionString = $"Server={server};Database={database};Uid={user};Pwd={password};CharSet=utf8mb4;Port={port};SslMode=none;AllowPublicKeyRetrieval=true;";
            
            Console.WriteLine($"🔗 DatabaseTest: 연결 문자열 생성 완료");
            Console.WriteLine($"   서버: {server}");
            Console.WriteLine($"   데이터베이스: {database}");
            Console.WriteLine($"   사용자: {user}");
            Console.WriteLine($"   포트: {port}");
            
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    Console.WriteLine("🌐 데이터베이스에 연결을 시도합니다...");
                    Console.WriteLine($"서버: {server}");
                    Console.WriteLine($"데이터베이스: {database}");
                    Console.WriteLine($"사용자: {user}");
                    Console.WriteLine($"포트: {port}");
                    
                    connection.Open();
                    Console.WriteLine("✅ 데이터베이스 연결 성공!");
                    
                    // 간단한 쿼리 테스트
                    using (var command = new MySqlCommand("SELECT 1 as test_result", connection))
                    {
                        var result = command.ExecuteScalar();
                        Console.WriteLine($"테스트 쿼리 결과: {result}");
                    }
                    
                    // 서버 버전 확인
                    using (var command = new MySqlCommand("SELECT VERSION() as version", connection))
                    {
                        var version = command.ExecuteScalar();
                        Console.WriteLine($"MySQL 서버 버전: {version}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 데이터베이스 연결 실패: {ex.Message}");
                Console.WriteLine($"상세 오류: {ex}");
                
                if (ex is MySqlException mySqlEx)
                {
                    Console.WriteLine($"🔍 MySQL 오류 코드: {mySqlEx.Number}");
                    Console.WriteLine($"🔍 MySQL 오류 메시지: {mySqlEx.Message}");
                }
            }
        }
    }
} 