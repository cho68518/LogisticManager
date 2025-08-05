using MySqlConnector;
using System.Data;
using System.Configuration;

namespace LogisticManager.Services
{
    /// <summary>
    /// 데이터베이스 연결 및 쿼리 실행을 담당하는 서비스 클래스
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // JSON 파일에서 설정을 읽어서 연결 문자열 생성
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            var settings = new Dictionary<string, string>();
            
            Console.WriteLine($"🔍 DatabaseService: 설정 파일 경로 = {settingsPath}");
            
            try
            {
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    Console.WriteLine($"📄 DatabaseService: JSON 파일 내용 = {jsonContent}");
                    
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        try
                        {
                            // Newtonsoft.Json을 사용하여 더 안전하게 역직렬화
                            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                            Console.WriteLine($"✅ DatabaseService: JSON에서 {settings.Count}개 설정 로드");
                            
                            // 각 설정값 로깅
                            foreach (var setting in settings)
                            {
                                Console.WriteLine($"📋 DatabaseService: {setting.Key} = {setting.Value}");
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            Console.WriteLine($"❌ DatabaseService: JSON 역직렬화 실패: {jsonEx.Message}");
                            Console.WriteLine($"🔍 DatabaseService: JSON 예외 상세: {jsonEx}");
                            
                            // JSON 역직렬화 실패 시 기본값 사용
                            Console.WriteLine("⚠️ DatabaseService: 기본값을 사용합니다.");
                            settings = new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ DatabaseService: JSON 파일이 비어있음");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ DatabaseService: 설정 파일이 존재하지 않음 = {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: JSON 파일 읽기 실패: {ex.Message}");
                Console.WriteLine($"🔍 DatabaseService: 예외 상세: {ex}");
            }
            
            // JSON에서 설정을 읽어오거나 기본값 사용 (안전한 기본값)
            var server = settings.GetValueOrDefault("DB_SERVER", "gramwonlogis.mycafe24.com");
            var database = settings.GetValueOrDefault("DB_NAME", "gramwonlogis");
            var user = settings.GetValueOrDefault("DB_USER", "gramwonlogis");
            var password = settings.GetValueOrDefault("DB_PASSWORD", "jung5516!");
            var port = settings.GetValueOrDefault("DB_PORT", "3306");
            
            // 설정값 검증 및 로깅
            Console.WriteLine($"�� DatabaseService: 설정값 검증");
            Console.WriteLine($"   DB_SERVER: '{server}' (길이: {server?.Length ?? 0})");
            Console.WriteLine($"   DB_NAME: '{database}' (길이: {database?.Length ?? 0})");
            Console.WriteLine($"   DB_USER: '{user}' (길이: {user?.Length ?? 0})");
            Console.WriteLine($"   DB_PASSWORD: '{password}' (길이: {password?.Length ?? 0})");
            Console.WriteLine($"   DB_PORT: '{port}' (길이: {port?.Length ?? 0})");
            
            // 설정값 검증
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user))
            {
                Console.WriteLine("⚠️ DatabaseService: 필수 설정값이 누락되어 기본값을 사용합니다.");
                server = "gramwonlogis.mycafe24.com";
                database = "gramwonlogis";
                user = "gramwonlogis";
                password = "jung5516!";
                port = "3306";
            }
            
            Console.WriteLine($"🔗 DatabaseService: 최종 설정값");
            Console.WriteLine($"   서버: {server}");
            Console.WriteLine($"   데이터베이스: {database}");
            Console.WriteLine($"   사용자: {user}");
            Console.WriteLine($"   포트: {port}");
            
            _connectionString = $"Server={server};Database={database};User ID={user};Password={password};Port={port};CharSet=utf8mb4;SslMode=none;AllowPublicKeyRetrieval=true;Convert Zero Datetime=True;ConnectionTimeout=30;";
            
            Console.WriteLine($"🔗 DatabaseService: 연결 문자열 생성 완료");
            Console.WriteLine($"🔗 DatabaseService: 연결 문자열 = {_connectionString}");
        }

        /// <summary>
        /// 쿼리를 실행하여 DataTable을 반환하는 비동기 메서드
        /// </summary>
        /// <param name="query">실행할 SQL 쿼리</param>
        /// <returns>쿼리 결과 DataTable</returns>
        public async Task<DataTable> GetDataTableAsync(string query)
        {
            using var connection = new MySqlConnector.MySqlConnection(_connectionString);
            using var command = new MySqlConnector.MySqlCommand(query, connection);
            using var adapter = new MySqlConnector.MySqlDataAdapter(command);
            
            var dataTable = new DataTable();
            
            try
            {
                await connection.OpenAsync();
                adapter.Fill(dataTable);
                return dataTable;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"데이터베이스 쿼리 실행 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// INSERT, UPDATE, DELETE 등의 쿼리를 실행하는 비동기 메서드
        /// </summary>
        /// <param name="query">실행할 SQL 쿼리</param>
        /// <returns>영향받은 행의 수</returns>
        public async Task<int> ExecuteNonQueryAsync(string query)
        {
            using var connection = new MySqlConnector.MySqlConnection(_connectionString);
            using var command = new MySqlConnector.MySqlCommand(query, connection);
            
            try
            {
                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"데이터베이스 명령 실행 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 트랜잭션을 사용하여 여러 쿼리를 실행하는 메서드
        /// </summary>
        /// <param name="queries">실행할 쿼리 목록</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> ExecuteTransactionAsync(IEnumerable<string> queries)
        {
            using var connection = new MySqlConnector.MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                foreach (var query in queries)
                {
                    using var command = new MySqlConnector.MySqlCommand(query, connection, transaction);
                    await command.ExecuteNonQueryAsync();
                }
                
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"트랜잭션 실행 중 오류 발생: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 데이터베이스 연결 정보를 반환하는 메서드
        /// </summary>
        /// <returns>DB 연결 정보</returns>
        public (string Server, string Database, string User, string Port, string ConnectionString) GetConnectionInfo()
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            var settings = new Dictionary<string, string>();
            
            try
            {
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        try
                        {
                            // Newtonsoft.Json을 사용하여 더 안전하게 역직렬화
                            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                        }
                        catch (Exception jsonEx)
                        {
                            Console.WriteLine($"❌ DatabaseService: JSON 역직렬화 실패: {jsonEx.Message}");
                            settings = new Dictionary<string, string>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: JSON 파일 읽기 실패: {ex.Message}");
            }
            
            // JSON에서 설정을 읽어오거나 기본값 사용
            var server = settings.GetValueOrDefault("DB_SERVER", "gramwonlogis.mycafe24.com");
            var database = settings.GetValueOrDefault("DB_NAME", "gramwonlogis");
            var user = settings.GetValueOrDefault("DB_USER", "gramwonlogis");
            var password = settings.GetValueOrDefault("DB_PASSWORD", "jung5516!");
            var port = settings.GetValueOrDefault("DB_PORT", "3306");
            
            var connectionString = $"Server={server};Database={database};User ID={user};Password={password};Port={port};CharSet=utf8mb4;SslMode=none;AllowPublicKeyRetrieval=true;Convert Zero Datetime=True;";
            
            return (server, database, user, port, connectionString);
        }

        /// <summary>
        /// 연결 상태를 확인하는 메서드
        /// </summary>
        /// <returns>연결 가능 여부</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine($"🔍 연결 테스트 시작: {_connectionString}");
                
                using var connection = new MySqlConnector.MySqlConnection(_connectionString);
                Console.WriteLine("📡 연결 객체 생성 완료");
                
                await connection.OpenAsync();
                Console.WriteLine("✅ 데이터베이스 연결 성공!");
                
                // 연결 상태 확인
                Console.WriteLine($"📊 연결 정보: Server={connection.DataSource}, Database={connection.Database}, State={connection.State}");
                
                return true;
            }
            catch (Exception ex)
            {
                // 연결 실패 시 상세 오류 정보 로깅
                Console.WriteLine($"❌ 데이터베이스 연결 테스트 실패: {ex.Message}");
                Console.WriteLine($"🔍 연결 문자열: {_connectionString}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"🔍 상세 오류: {ex.InnerException.Message}");
                }
                
                // 오류 타입별 상세 정보
                if (ex is MySqlConnector.MySqlException mySqlEx)
                {
                    Console.WriteLine($"🔍 MySQL 오류 코드: {mySqlEx.Number}");
                    Console.WriteLine($"🔍 MySQL 오류 메시지: {mySqlEx.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// 연결 상태를 확인하고 상세 오류 정보를 반환하는 메서드
        /// </summary>
        /// <returns>연결 결과와 오류 메시지</returns>
        public async Task<(bool IsConnected, string ErrorMessage)> TestConnectionWithDetailsAsync()
        {
            try
            {
                Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: 연결 테스트 시작");
                Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: 연결 문자열 = {_connectionString}");
                
                using var connection = new MySqlConnector.MySqlConnection(_connectionString);
                Console.WriteLine("📡 TestConnectionWithDetailsAsync: MySqlConnection 객체 생성 완료");
                
                Console.WriteLine("📡 TestConnectionWithDetailsAsync: 데이터베이스 연결 시도 중...");
                await connection.OpenAsync();
                Console.WriteLine("✅ TestConnectionWithDetailsAsync: 데이터베이스 연결 성공!");
                
                // 연결 정보 확인
                Console.WriteLine($"📊 TestConnectionWithDetailsAsync: 연결 정보");
                Console.WriteLine($"   서버: {connection.DataSource}");
                Console.WriteLine($"   데이터베이스: {connection.Database}");
                Console.WriteLine($"   상태: {connection.State}");
                Console.WriteLine($"   연결 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                // 간단한 쿼리 테스트
                using var command = new MySqlConnector.MySqlCommand("SELECT 1 as test_result", connection);
                var result = await command.ExecuteScalarAsync();
                Console.WriteLine($"📊 TestConnectionWithDetailsAsync: 테스트 쿼리 결과 = {result}");
                
                return (true, "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ TestConnectionWithDetailsAsync: 연결 실패");
                Console.WriteLine($"❌ TestConnectionWithDetailsAsync: 오류 메시지 = {ex.Message}");
                Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: 예외 타입 = {ex.GetType().Name}");
                Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: 예외 상세 = {ex}");
                
                var errorMessage = $"데이터베이스 연결 실패: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: 내부 오류 = {ex.InnerException.Message}");
                    Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: 내부 예외 타입 = {ex.InnerException.GetType().Name}");
                    errorMessage += $"\n상세 오류: {ex.InnerException.Message}";
                }
                
                // MySQL 특정 오류 정보
                if (ex is MySqlConnector.MySqlException mySqlEx)
                {
                    Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: MySQL 오류 코드 = {mySqlEx.Number}");
                    Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: MySQL 오류 메시지 = {mySqlEx.Message}");
                    errorMessage += $"\nMySQL 오류 코드: {mySqlEx.Number}";
                }
                
                Console.WriteLine($"🔍 TestConnectionWithDetailsAsync: 최종 오류 메시지 = {errorMessage}");
                
                return (false, errorMessage);
            }
        }
    }
} 