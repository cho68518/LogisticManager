using MySqlConnector;
using System.Data;
using System.Configuration;
using LogisticManager.Models;

namespace LogisticManager.Services
{
    /// <summary>
    /// 데이터베이스 연결 및 쿼리 실행을 담당하는 서비스 클래스
    /// 
    /// 주요 기능:
    /// - MySQL 데이터베이스 연결 관리
    /// - SQL 쿼리 실행 (SELECT, INSERT, UPDATE, DELETE)
    /// - 트랜잭션 처리
    /// - Excel 데이터를 데이터베이스에 삽입
    /// - 연결 상태 테스트
    /// 
    /// 설정 파일:
    /// - settings.json에서 데이터베이스 연결 정보 읽기
    /// - DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD, DB_PORT
    /// 
    /// 의존성:
    /// - MySqlConnector: MySQL 연결 및 쿼리 실행
    /// - MappingService: 컬럼 매핑 설정 관리
    /// 
    /// 보안:
    /// - 연결 문자열에 민감한 정보 포함
    /// - 설정 파일 접근 권한 관리 필요
    /// </summary>
    public class DatabaseService
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// MySQL 데이터베이스 연결 문자열
        /// 서버, 데이터베이스, 사용자, 비밀번호, 포트 정보 포함
        /// </summary>
        private readonly string _connectionString;
        
        /// <summary>
        /// 컬럼 매핑 설정을 관리하는 서비스
        /// Excel 컬럼명과 데이터베이스 컬럼명 간의 매핑 처리
        /// </summary>
        private readonly MappingService _mappingService;
        
        /// <summary>
        /// 로그 파일 관리를 위한 서비스
        /// 로그 파일 크기 자동 관리 및 클리어 기능
        /// </summary>
        private readonly LogManagementService _logManagementService;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// DatabaseService 생성자
        /// 
        /// 초기화 작업:
        /// 1. settings.json에서 직접 데이터베이스 설정 읽기 (무조건 JSON 파일 우선)
        /// 2. 연결 문자열 생성
        /// 3. MappingService 인스턴스 생성
        /// 4. 설정값 검증 및 로깅
        /// 
        /// 설정 파일 구조:
        /// - DB_SERVER: 데이터베이스 서버 주소
        /// - DB_NAME: 데이터베이스 이름
        /// - DB_USER: 데이터베이스 사용자명
        /// - DB_PASSWORD: 데이터베이스 비밀번호
        /// - DB_PORT: 데이터베이스 포트 번호
        /// 
        /// 예외 처리:
        /// - 설정 파일 읽기 실패 시 기본값 사용
        /// - JSON 파싱 오류 시 기본값 사용
        /// - 필수 설정값 누락 시 기본값 사용
        /// </summary>
        public DatabaseService()
        {
            Console.WriteLine("🔍 DatabaseService: settings.json에서 직접 데이터베이스 설정을 읽어옵니다.");
            
            // settings.json에서 직접 데이터베이스 설정 읽기 (무조건 JSON 파일 우선)
            var (server, database, user, password, port) = LoadDatabaseSettingsFromJson();
            
            // 설정값 검증 및 로깅
            Console.WriteLine($"🔍 DatabaseService: settings.json에서 읽어온 설정값");
            Console.WriteLine($"   DB_SERVER: '{server}' (길이: {server?.Length ?? 0})");
            Console.WriteLine($"   DB_NAME: '{database}' (길이: {database?.Length ?? 0})");
            Console.WriteLine($"   DB_USER: '{user}' (길이: {user?.Length ?? 0})");
            Console.WriteLine($"   DB_PASSWORD: '{password}' (길이: {password?.Length ?? 0})");
            Console.WriteLine($"   DB_PORT: '{port}' (길이: {port?.Length ?? 0})");
            
            // 설정값 검증 (필수 값이 누락된 경우 기본값 사용)
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user))
            {
                Console.WriteLine("⚠️ DatabaseService: 필수 설정값이 누락되어 기본값을 사용합니다.");
                server = "gramwonlogis2.mycafe24.com";
                database = "gramwonlogis2";
                user = "gramwonlogis2";
                password = "jung5516!";
                port = "3306";
            }
            
            // 최종 설정값 로깅
            Console.WriteLine($"🔗 DatabaseService: 최종 설정값");
            Console.WriteLine($"   서버: {server}");
            Console.WriteLine($"   데이터베이스: {database}");
            Console.WriteLine($"   사용자: {user}");
            Console.WriteLine($"   포트: {port}");
            
            // 연결 문자열 생성
            _connectionString = $"Server={server};Database={database};User Id={user};Password={password};Port={port};CharSet=utf8;Convert Zero Datetime=True;Allow User Variables=True;";
            
            // MappingService 인스턴스 생성
            _mappingService = new MappingService();
            
            // 로그 관리 서비스 초기화
            _logManagementService = new LogManagementService();
            
            Console.WriteLine("✅ DatabaseService 초기화 완료");
        }

        #endregion

        #region 설정 로드 메서드 (Settings Loading Methods)

        /// <summary>
        /// settings.json에서 직접 데이터베이스 설정을 읽어오는 메서드
        /// 
        /// 읽기 순서:
        /// 1. settings.json 파일에서 직접 읽기
        /// 2. 파일이 없거나 읽기 실패 시 기본값 사용
        /// 
        /// 반환값:
        /// - (server, database, user, password, port) 튜플
        /// </summary>
        /// <returns>데이터베이스 설정 튜플</returns>
        private (string server, string database, string user, string password, string port) LoadDatabaseSettingsFromJson()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        Console.WriteLine($"📄 settings.json 파일 내용: {jsonContent}");
                        
                        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                        if (settings != null)
                        {
                            var server = settings.GetValueOrDefault("DB_SERVER", "gramwonlogis2.mycafe24.com");
                            var database = settings.GetValueOrDefault("DB_NAME", "gramwonlogis2");
                            var user = settings.GetValueOrDefault("DB_USER", "gramwonlogis2");
                            var password = settings.GetValueOrDefault("DB_PASSWORD", "jung5516!");
                            var port = settings.GetValueOrDefault("DB_PORT", "3306");
                            
                            Console.WriteLine($"✅ settings.json에서 데이터베이스 설정을 성공적으로 읽어왔습니다.");
                            return (server, database, user, password, port);
                        }
                        else
                        {
                            Console.WriteLine("❌ settings.json 파싱 실패");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ settings.json 파일이 비어있음");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ settings.json 파일이 존재하지 않음: {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ settings.json 읽기 실패: {ex.Message}");
            }
            
            // 기본값 반환
            Console.WriteLine("🔄 기본값을 사용합니다.");
            return ("gramwonlogis2.mycafe24.com", "gramwonlogis2", "gramwonlogis2", "jung5516!", "3306");
        }

        #endregion

        #region 로그 관리 헬퍼 메서드

        /// <summary>
        /// 로그 파일에 안전하게 메시지 작성 (크기 관리 포함)
        /// 
        /// 🎯 주요 기능:
        /// - 로그 파일 크기 자동 체크 및 필요시 클리어
        /// - 스레드 안전한 로그 작성
        /// - 예외 발생 시 안전한 처리
        /// 
        /// 💡 사용 목적:
        /// - 로그 파일 크기 자동 관리
        /// - 시스템 안정성 보장
        /// - 로그 작성 성능 최적화
        /// </summary>
        /// <param name="message">작성할 로그 메시지</param>
        private void WriteLogSafely(string message)
        {
            try
            {
                // 로그 파일 크기 체크 및 필요시 클리어
                _logManagementService.CheckAndClearLogFileIfNeeded();
                
                // 로그 파일에 메시지 작성
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\n");
            }
            catch (Exception ex)
            {
                // 로그 작성 실패 시 콘솔에만 출력 (시스템 안정성 보장)
                Console.WriteLine($"[DatabaseService] 로그 작성 실패: {ex.Message}");
            }
        }

        #endregion

        #region 데이터 조회 메서드 (Data Retrieval Methods)

        /// <summary>
        /// SQL 쿼리를 실행하여 DataTable을 반환하는 비동기 메서드
        /// 
        /// 처리 과정:
        /// 1. MySQL 연결 생성
        /// 2. SQL 쿼리 실행
        /// 3. 결과를 DataTable로 변환
        /// 4. 연결 해제 및 리소스 정리
        /// 
        /// 사용 목적:
        /// - SELECT 쿼리 실행
        /// - 데이터 조회 및 분석
        /// - 테이블 구조 확인
        /// 
        /// 예외 처리:
        /// - MySqlException: 데이터베이스 연결 또는 쿼리 오류
        /// - InvalidOperationException: 연결 실패
        /// - TimeoutException: 쿼리 실행 시간 초과
        /// </summary>
        /// <param name="query">실행할 SQL 쿼리</param>
        /// <returns>쿼리 결과가 담긴 DataTable</returns>
        /// <exception cref="MySqlException">데이터베이스 오류</exception>
        /// <exception cref="InvalidOperationException">연결 실패</exception>
        public async Task<DataTable> GetDataTableAsync(string query)
        {
            // MySQL 연결 생성
            using var connection = new MySqlConnection(_connectionString);
            
            try
            {
                // 데이터베이스 연결
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 성공");
                
                // SQL 쿼리 실행 및 DataTable로 변환
                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();
                
                // 데이터를 DataTable에 채움
                adapter.Fill(dataTable);
                
                Console.WriteLine($"✅ DatabaseService: 쿼리 실행 완료 - {dataTable.Rows.Count}행 반환");
                return dataTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 쿼리 실행 실패: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 데이터 수정 메서드 (Data Modification Methods)

        /// <summary>
        /// INSERT, UPDATE, DELETE 쿼리를 실행하는 비동기 메서드
        /// 
        /// 처리 과정:
        /// 1. MySQL 연결 생성
        /// 2. SQL 쿼리 실행
        /// 3. 영향받은 행 수 반환
        /// 4. 연결 해제 및 리소스 정리
        /// 
        /// 사용 목적:
        /// - 데이터 삽입 (INSERT)
        /// - 데이터 수정 (UPDATE)
        /// - 데이터 삭제 (DELETE)
        /// - 테이블 생성/수정 (CREATE, ALTER)
        /// 
        /// 반환 값:
        /// - 영향받은 행의 수
        /// - INSERT: 삽입된 행 수
        /// - UPDATE: 수정된 행 수
        /// - DELETE: 삭제된 행 수
        /// </summary>
        /// <param name="query">실행할 SQL 쿼리</param>
        /// <returns>영향받은 행의 수</returns>
        /// <exception cref="MySqlException">데이터베이스 오류</exception>
        public async Task<int> ExecuteNonQueryAsync(string query)
        {
            // MySQL 연결 생성
            using var connection = new MySqlConnection(_connectionString);
            
            try
            {
                // 데이터베이스 연결
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 성공");
                
                // SQL 쿼리 실행
                using var command = new MySqlCommand(query, connection);
                var affectedRows = await command.ExecuteNonQueryAsync();
                
                Console.WriteLine($"✅ DatabaseService: 쿼리 실행 완료 - {affectedRows}행 영향받음");
                return affectedRows;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 쿼리 실행 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 매개변수를 지원하는 데이터 변경 쿼리 실행 (INSERT, UPDATE, DELETE)
        /// 
        /// 📋 기능:
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - 트랜잭션 지원 준비
        /// - 영향받은 행 수 반환
        /// 
        /// 💡 사용법:
        /// await ExecuteNonQueryAsync("UPDATE table SET field = @value WHERE id = @id", new { value = "test", id = 1 });
        /// </summary>
        /// <param name="query">실행할 SQL 쿼리</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>영향받은 행 수</returns>
        public async Task<int> ExecuteNonQueryAsync(string query, object? parameters = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 성공 (ExecuteNonQueryAsync with parameters)");
                
                using var command = new MySqlCommand(query, connection);
                
                // 매개변수가 있는 경우 바인딩
                if (parameters != null)
                {
                    var paramDict = ConvertObjectToDictionary(parameters);
                    foreach (var param in paramDict)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }
                
                var affectedRows = await command.ExecuteNonQueryAsync();
                
                Console.WriteLine($"✅ DatabaseService: ExecuteNonQueryAsync 완료 - {affectedRows}행 영향받음");
                return affectedRows;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: ExecuteNonQueryAsync 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// SELECT 쿼리를 실행하여 DataTable 반환 (매개변수 지원)
        /// 
        /// 📋 기능:
        /// - 복잡한 조회 쿼리 실행
        /// - 매개변수화된 쿼리 지원
        /// - DataTable 형태로 결과 반환
        /// 
        /// 💡 사용법:
        /// var result = await ExecuteQueryAsync("SELECT * FROM table WHERE field = @value", new { value = "test" });
        /// </summary>
        /// <param name="query">실행할 SQL 쿼리</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>쿼리 결과 DataTable</returns>
        public async Task<DataTable> ExecuteQueryAsync(string query, object? parameters = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 성공 (ExecuteQueryAsync with parameters)");
                
                using var command = new MySqlCommand(query, connection);
                
                // 매개변수가 있는 경우 바인딩
                if (parameters != null)
                {
                    var paramDict = ConvertObjectToDictionary(parameters);
                    foreach (var param in paramDict)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }
                
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                
                Console.WriteLine($"✅ DatabaseService: ExecuteQueryAsync 완료 - {dataTable.Rows.Count}행 조회됨");
                return dataTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: ExecuteQueryAsync 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 단일 값을 반환하는 SELECT 쿼리를 실행하는 비동기 메서드 (매개변수 지원)
        /// 
        /// 📋 주요 기능:
        /// - COUNT, MAX, MIN, SUM 등의 집계 함수 결과 조회
        /// - 단일 컬럼 값 조회
        /// - 매개변수화된 쿼리 지원 (SQL 인젝션 방지)
        /// - null 안전성 보장
        /// 
        /// 🔄 처리 과정:
        /// 1. MySQL 연결 생성
        /// 2. 매개변수가 있는 경우 바인딩
        /// 3. SQL 쿼리 실행
        /// 4. 단일 값 반환
        /// 5. 연결 해제 및 리소스 정리
        /// 
        /// 💡 사용 목적:
        /// - 데이터 개수 조회 (COUNT)
        /// - 최대/최소값 조회 (MAX/MIN)
        /// - 합계 조회 (SUM)
        /// - 단일 값 존재 여부 확인
        /// 
        /// ⚠️ 예외 처리:
        /// - MySqlException: 데이터베이스 오류
        /// - InvalidOperationException: 쿼리 결과가 없는 경우
        /// - ArgumentNullException: 쿼리가 null인 경우
        /// 
        /// 🎯 반환 값:
        /// - object: 쿼리 결과 값 (null 가능)
        /// - DBNull.Value인 경우 null 반환
        /// - 결과가 없는 경우 null 반환
        /// 
        /// 💡 사용법:
        /// var count = await ExecuteScalarAsync("SELECT COUNT(*) FROM table");
        /// var maxId = await ExecuteScalarAsync("SELECT MAX(id) FROM table WHERE name = @name", new { name = "test" });
        /// </summary>
        /// <param name="query">실행할 SQL 쿼리</param>
        /// <param name="parameters">쿼리 매개변수 (선택적)</param>
        /// <returns>쿼리 결과 단일 값</returns>
        /// <exception cref="MySqlException">데이터베이스 오류</exception>
        /// <exception cref="ArgumentNullException">쿼리가 null인 경우</exception>
        public async Task<object?> ExecuteScalarAsync(string query, object? parameters = null)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentNullException(nameof(query), "쿼리는 비어있을 수 없습니다.");

            // MySQL 연결 생성
            using var connection = new MySqlConnection(_connectionString);
            
            try
            {
                // 데이터베이스 연결
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 성공 (ExecuteScalarAsync)");
                
                // SQL 명령 생성
                using var command = new MySqlCommand(query, connection);
                
                // 매개변수가 있는 경우 바인딩
                if (parameters != null)
                {
                    var paramDict = ConvertObjectToDictionary(parameters);
                    foreach (var param in paramDict)
                    {
                        var value = param.Value ?? DBNull.Value;
                        command.Parameters.AddWithValue(param.Key, value);
                    }
                    Console.WriteLine($"✅ DatabaseService: 매개변수 바인딩 완료 - {paramDict.Count}개");
                }
                
                // SQL 쿼리 실행
                var result = await command.ExecuteScalarAsync();
                
                // DBNull 처리
                if (result == DBNull.Value)
                    result = null;
                
                Console.WriteLine($"✅ DatabaseService: ExecuteScalarAsync 완료 - 결과: {result ?? "NULL"}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: ExecuteScalarAsync 실패: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 트랜잭션 처리 (Transaction Processing)

        /// <summary>
        /// 여러 SQL 쿼리를 트랜잭션으로 실행하는 비동기 메서드
        /// 
        /// 트랜잭션 처리:
        /// 1. 트랜잭션 시작
        /// 2. 모든 쿼리를 순차적으로 실행
        /// 3. 성공 시 커밋, 실패 시 롤백
        /// 4. 연결 해제 및 리소스 정리
        /// 
        /// 사용 목적:
        /// - 데이터 일관성 보장
        /// - 여러 테이블 동시 수정
        /// - 복잡한 데이터 처리 작업
        /// 
        /// 예외 처리:
        /// - 하나라도 실패하면 전체 롤백
        /// - 트랜잭션 중단 시 자동 롤백
        /// </summary>
        /// <param name="queries">실행할 SQL 쿼리 목록</param>
        /// <returns>모든 쿼리가 성공하면 true, 아니면 false</returns>
        public async Task<bool> ExecuteTransactionAsync(IEnumerable<string> queries)
        {
            // MySQL 연결 생성
            using var connection = new MySqlConnection(_connectionString);
            
            try
            {
                // 데이터베이스 연결
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 성공");
                
                // 트랜잭션 시작
                using var transaction = await connection.BeginTransactionAsync();
                Console.WriteLine("🔄 DatabaseService: 트랜잭션 시작");
                
                try
                {
                    // 각 쿼리를 순차적으로 실행
                    foreach (var query in queries)
                    {
                        using var command = new MySqlCommand(query, connection, transaction);
                        var affectedRows = await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ DatabaseService: 쿼리 실행 완료 - {affectedRows}행 영향받음");
                    }
                    
                    // 모든 쿼리가 성공하면 커밋
                    await transaction.CommitAsync();
                    Console.WriteLine("✅ DatabaseService: 트랜잭션 커밋 완료");
                    return true;
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 롤백
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ DatabaseService: 트랜잭션 롤백 - {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 트랜잭션 실행 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 매개변수화된 쿼리로 트랜잭션을 실행하는 비동기 메서드 (새로운 최적화 버전)
        /// 
        /// 개선사항:
        /// - SQL 인젝션 방지를 위한 매개변수화된 쿼리
        /// - 성능 향상을 위한 배치 처리
        /// - 메모리 효율성 개선
        /// - 상세한 오류 처리
        /// 
        /// 사용 예시:
        /// var queries = new List<(string sql, Dictionary<string, object> parameters)>
        /// {
        ///     ("INSERT INTO table (col1, col2) VALUES (@val1, @val2)", 
        ///      new Dictionary<string, object> { ["@val1"] = "value1", ["@val2"] = "value2" })
        /// };
        /// </summary>
        /// <param name="queriesWithParameters">SQL 쿼리와 매개변수의 튜플 목록</param>
        /// <returns>모든 쿼리가 성공하면 true, 아니면 false</returns>
        public async Task<bool> ExecuteParameterizedTransactionAsync(IEnumerable<(string sql, Dictionary<string, object> parameters)> queriesWithParameters)
        {
            const int maxRetries = 3;
            var retryDelays = new[] { 1000, 2000, 4000 }; // 지수 백오프 (밀리초)
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            
            for (int retry = 0; retry <= maxRetries; retry++)
            {
                // MySQL 연결 생성
                using var connection = new MySqlConnection(_connectionString);
                
                try
                {
                    // 데이터베이스 연결
                    await connection.OpenAsync();
                    var startLog = $"[DatabaseService] 매개변수화된 트랜잭션 시작 (시도 {retry + 1}/{maxRetries + 1})";
                    Console.WriteLine(startLog);
                    WriteLogSafely(startLog);
                    
                    // 트랜잭션 시작
                    using var transaction = await connection.BeginTransactionAsync();
                    
                    try
                    {
                        var totalAffectedRows = 0;
                        var queryCount = 0;
                        
                        foreach (var (sql, parameters) in queriesWithParameters)
                        {
                            queryCount++;
                            var queryLog = $"[DatabaseService] 쿼리 {queryCount} 실행 시작";
                            var sqlLog = $"[DatabaseService] SQL: {sql}";
                            var paramLog = $"[DatabaseService] 매개변수: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}";
                            
                            Console.WriteLine(queryLog);
                            Console.WriteLine(sqlLog);
                            Console.WriteLine(paramLog);
                            WriteLogSafely(queryLog);
                            WriteLogSafely(sqlLog);
                            WriteLogSafely(paramLog);
                            
                            try
                            {
                                using var command = new MySqlCommand(sql, connection, transaction);
                                command.CommandTimeout = 300; // 5분 타임아웃 설정
                                
                                // 매개변수 추가
                                foreach (var param in parameters)
                                {
                                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                                }
                                
                                // 쿼리 실행
                                var affectedRows = await command.ExecuteNonQueryAsync();
                                totalAffectedRows += affectedRows;
                                
                                var successLog = $"[DatabaseService] 쿼리 {queryCount} 성공 - 영향받은 행: {affectedRows}";
                                Console.WriteLine(successLog);
                                WriteLogSafely(successLog);
                            }
                            catch (MySqlException ex) when (ex.Number == 1205 || // 데드락
                                                          ex.Number == 1213 || // 데드락 감지
                                                          ex.Number == 1037 || // 메모리 부족
                                                          ex.Number == 2006 || // 서버 연결 끊김
                                                          ex.Number == 2013)   // 연결 유실
                            {
                                var errorLog = $"[DatabaseService] 쿼리 {queryCount} 실패 (일시적 오류): {ex.Message}";
                                var detailLog = $"[DatabaseService] 상세 오류 (MySQL 오류 번호: {ex.Number}): {ex}";
                                
                                Console.WriteLine(errorLog);
                                Console.WriteLine(detailLog);
                                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {errorLog}\n");
                                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {detailLog}\n");
                                
                                // 트랜잭션 롤백
                                await transaction.RollbackAsync();
                                
                                if (retry < maxRetries)
                                {
                                    var retryLog = $"[DatabaseService] 일시적 오류로 인한 재시도 준비 - {retryDelays[retry]}ms 후 재시도";
                                    Console.WriteLine(retryLog);
                                    File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {retryLog}\n");
                                    
                                    await Task.Delay(retryDelays[retry]);
                                    throw; // 외부 catch 블록으로 전파하여 전체 트랜잭션 재시도
                                }
                                
                                var maxRetriesLog = $"[DatabaseService] 최대 재시도 횟수 초과 - 트랜잭션 실패";
                                Console.WriteLine(maxRetriesLog);
                                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {maxRetriesLog}\n");
                                return false;
                            }
                            catch (Exception ex)
                            {
                                var errorLog = $"[DatabaseService] 쿼리 {queryCount} 실패 (영구적 오류): {ex.Message}";
                                var detailLog = $"[DatabaseService] 상세 오류: {ex}";
                                
                                Console.WriteLine(errorLog);
                                Console.WriteLine(detailLog);
                                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {errorLog}\n");
                                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {detailLog}\n");
                                
                                // 트랜잭션 롤백
                                await transaction.RollbackAsync();
                                return false; // 영구적 오류는 재시도하지 않음
                            }
                        }
                        
                        // 모든 쿼리 성공 시 커밋
                        await transaction.CommitAsync();
                        
                        var commitLog = $"[DatabaseService] 트랜잭션 커밋 완료 - 총 {queryCount}개 쿼리, {totalAffectedRows}개 행 영향받음";
                        Console.WriteLine(commitLog);
                        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {commitLog}\n");
                        
                        return true;
                    }
                    catch (Exception ex) when (retry < maxRetries)
                    {
                        var retryLog = $"[DatabaseService] 트랜잭션 실패 - 재시도 {retry + 1}/{maxRetries}: {ex.Message}";
                        Console.WriteLine(retryLog);
                        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {retryLog}\n");
                        
                        await Task.Delay(retryDelays[retry]);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    if (retry < maxRetries)
                    {
                        var retryLog = $"[DatabaseService] 데이터베이스 연결 실패 - 재시도 {retry + 1}/{maxRetries}";
                        Console.WriteLine(retryLog);
                        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {retryLog}\n");
                        
                        await Task.Delay(retryDelays[retry]);
                        continue;
                    }
                    
                    var errorLog = $"[DatabaseService] 데이터베이스 연결 실패 (최대 재시도 횟수 초과): {ex.Message}";
                    Console.WriteLine(errorLog);
                    File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {errorLog}\n");
                    return false;
                }
            }
            
            return false;
        }

        #endregion

        #region 연결 문자열 관리 (Connection String Management)

        /// <summary>
        /// 현재 데이터베이스 연결 문자열을 반환하는 메서드
        /// </summary>
        /// <returns>데이터베이스 연결 문자열</returns>
        public string GetConnectionString()
        {
            return _connectionString;
        }

        #endregion

        #region Excel 데이터 삽입 (Excel Data Insertion)

        /// <summary>
        /// Excel 데이터를 데이터베이스 테이블에 삽입하는 비동기 메서드
        /// 
        /// 처리 과정:
        /// 1. Excel 데이터를 데이터베이스 형식으로 변환
        /// 2. 매핑 설정을 사용하여 컬럼 매핑
        /// 3. 데이터 유효성 검사
        /// 4. INSERT 쿼리 생성 및 실행
        /// 5. 삽입된 행 수 반환
        /// 
        /// 매핑 처리:
        /// - Excel 컬럼명을 데이터베이스 컬럼명으로 변환
        /// - 데이터 타입 변환 (문자열, 숫자, 날짜)
        /// - 기본값 설정 및 null 처리
        /// 
        /// 사용 목적:
        /// - 송장 데이터를 데이터베이스에 저장
        /// - Excel 파일의 데이터를 영구 저장
        /// - 데이터 분석 및 백업
        /// </summary>
        /// <param name="dataTable">삽입할 Excel 데이터</param>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="tableMappingKey">테이블 매핑 키 (기본값: "order_table")</param>
        /// <returns>삽입된 행의 수</returns>
        public async Task<int> InsertExcelDataAsync(DataTable dataTable, string tableName, string tableMappingKey = "order_table")
        {
            try
            {
                Console.WriteLine($"🔍 DatabaseService: Excel 데이터 삽입 시작 - {dataTable.Rows.Count}행");
                
                // Excel 데이터를 데이터베이스 형식으로 변환
                var transformedData = _mappingService.TransformExcelData(dataTable, tableMappingKey);
                Console.WriteLine($"✅ DatabaseService: 데이터 변환 완료 - {transformedData.Count}행");
                
                if (transformedData.Count == 0)
                {
                    Console.WriteLine("⚠️ DatabaseService: 변환된 데이터가 없습니다.");
                    return 0;
                }
                
                // 각 행에 대해 INSERT 쿼리 생성 및 실행
                var insertedRows = 0;
                foreach (var rowData in transformedData)
                {
                    // 데이터 유효성 검사
                    var (isValid, errors) = _mappingService.ValidateData(rowData, tableMappingKey);
                    if (!isValid)
                    {
                        Console.WriteLine($"⚠️ DatabaseService: 데이터 유효성 검사 실패: {string.Join(", ", errors)}");
                        continue;
                    }
                    
                    // INSERT 쿼리 생성
                    var insertQuery = _mappingService.GenerateInsertQuery(tableName, rowData);
                    
                    // 쿼리 실행
                    var affectedRows = await ExecuteNonQueryAsync(insertQuery);
                    insertedRows += affectedRows;
                }
                
                Console.WriteLine($"✅ DatabaseService: Excel 데이터 삽입 완료 - {insertedRows}행 삽입됨");
                return insertedRows;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: Excel 데이터 삽입 실패: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 설정 관리 (Configuration Management)

        /// <summary>
        /// 매핑 설정을 다시 로드하는 메서드
        /// 
        /// 사용 목적:
        /// - 설정 파일 변경 시 동적 반영
        /// - 매핑 설정 업데이트
        /// - 런타임 설정 변경
        /// </summary>
        public void ReloadMappingConfiguration()
        {
            _mappingService.ReloadConfiguration();
            Console.WriteLine("✅ DatabaseService: 매핑 설정 다시 로드 완료");
        }

        /// <summary>
        /// 현재 매핑 설정을 가져오는 메서드
        /// 
        /// 반환 값:
        /// - MappingConfiguration: 현재 매핑 설정
        /// - null: 설정이 로드되지 않은 경우
        /// </summary>
        /// <returns>현재 매핑 설정</returns>
        public MappingConfiguration? GetMappingConfiguration()
        {
            return _mappingService.GetConfiguration();
        }

        #endregion

        #region 연결 정보 관리 (Connection Information Management)

        /// <summary>
        /// 데이터베이스 연결 정보를 가져오는 메서드
        /// 
        /// 반환 정보:
        /// - Server: 데이터베이스 서버 주소
        /// - Database: 데이터베이스 이름
        /// - User: 데이터베이스 사용자명
        /// - Port: 데이터베이스 포트 번호
        /// - ConnectionString: 전체 연결 문자열
        /// 
        /// 보안 주의사항:
        /// - 비밀번호는 연결 문자열에 포함되어 있음
        /// - 로깅 시 비밀번호 노출 주의
        /// </summary>
        /// <returns>데이터베이스 연결 정보</returns>
        public (string Server, string Database, string User, string Port, string ConnectionString) GetConnectionInfo()
        {
            // 연결 문자열에서 정보 추출
            var connectionString = _connectionString;
            
            // 간단한 파싱을 통해 정보 추출
            var server = ExtractValue(connectionString, "Server=", ";");
            var database = ExtractValue(connectionString, "Database=", ";");
            var user = ExtractValue(connectionString, "User Id=", ";");
            var port = ExtractValue(connectionString, "Port=", ";");
            
            return (server, database, user, port, connectionString);
        }

        /// <summary>
        /// 연결 문자열에서 특정 값을 추출하는 헬퍼 메서드
        /// </summary>
        /// <param name="connectionString">연결 문자열</param>
        /// <param name="key">찾을 키</param>
        /// <param name="delimiter">구분자</param>
        /// <returns>추출된 값</returns>
        private string ExtractValue(string connectionString, string key, string delimiter)
        {
            var startIndex = connectionString.IndexOf(key);
            if (startIndex == -1) return string.Empty;
            
            startIndex += key.Length;
            var endIndex = connectionString.IndexOf(delimiter, startIndex);
            if (endIndex == -1) endIndex = connectionString.Length;
            
            return connectionString.Substring(startIndex, endIndex - startIndex);
        }

        #endregion

        #region 연결 테스트 (Connection Testing)

        /// <summary>
        /// 데이터베이스 연결을 테스트하는 비동기 메서드
        /// 
        /// 테스트 과정:
        /// 1. MySQL 연결 생성
        /// 2. 연결 시도
        /// 3. 간단한 쿼리 실행 (SELECT 1)
        /// 4. 연결 해제
        /// 
        /// 사용 목적:
        /// - 애플리케이션 시작 시 연결 상태 확인
        /// - 네트워크 연결 상태 확인
        /// - 데이터베이스 서버 상태 확인
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // MySQL 연결 생성
                using var connection = new MySqlConnection(_connectionString);
                
                // 연결 시도
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 연결 테스트 성공");
                
                // 간단한 쿼리 실행으로 연결 확인
                using var command = new MySqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                Console.WriteLine("✅ DatabaseService: 쿼리 테스트 성공");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 연결 테스트 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 데이터베이스 연결을 상세하게 테스트하는 비동기 메서드
        /// 
        /// 테스트 내용:
        /// 1. 연결 문자열 유효성 검사
        /// 2. 네트워크 연결 확인
        /// 3. 인증 정보 확인
        /// 4. 데이터베이스 접근 권한 확인
        /// 
        /// 반환 정보:
        /// - IsConnected: 연결 성공 여부
        /// - ErrorMessage: 오류 발생 시 상세 메시지
        /// </summary>
        /// <returns>(연결 성공 여부, 오류 메시지)</returns>
        public async Task<(bool IsConnected, string ErrorMessage)> TestConnectionWithDetailsAsync()
        {
            try
            {
                // 연결 문자열 유효성 검사
                if (string.IsNullOrEmpty(_connectionString))
                {
                    return (false, "연결 문자열이 비어있습니다.");
                }
                
                // MySQL 연결 생성
                using var connection = new MySqlConnection(_connectionString);
                
                // 연결 시도
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 상세 연결 테스트 성공");
                
                // 데이터베이스 정보 확인
                var serverVersion = connection.ServerVersion;
                var database = connection.Database;
                
                Console.WriteLine($"📊 DatabaseService: 서버 버전 = {serverVersion}");
                Console.WriteLine($"📊 DatabaseService: 데이터베이스 = {database}");
                
                return (true, "연결 성공");
            }
            catch (MySqlException mysqlEx)
            {
                // MySQL 특정 오류 처리
                var errorMessage = mysqlEx.Number switch
                {
                    1045 => "인증 실패: 사용자명 또는 비밀번호가 잘못되었습니다.",
                    1049 => "데이터베이스가 존재하지 않습니다.",
                    2003 => "서버에 연결할 수 없습니다. 서버 주소와 포트를 확인하세요.",
                    _ => $"MySQL 오류 ({mysqlEx.Number}): {mysqlEx.Message}"
                };
                
                Console.WriteLine($"❌ DatabaseService: 상세 연결 테스트 실패: {errorMessage}");
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                // 일반적인 오류 처리
                var errorMessage = $"연결 오류: {ex.Message}";
                Console.WriteLine($"❌ DatabaseService: 상세 연결 테스트 실패: {errorMessage}");
                return (false, errorMessage);
            }
        }

        #endregion

        #region 유틸리티 메서드 (Utility Methods)

        /// <summary>
        /// 객체를 Dictionary로 변환하는 유틸리티 메서드 (매개변수 처리용)
        /// 
        /// 📋 주요 기능:
        /// - 익명 객체를 Dictionary&lt;string, object&gt;로 변환
        /// - 이미 Dictionary인 경우 그대로 반환
        /// - 리플렉션을 사용한 프로퍼티 추출
        /// - null 안전성 보장
        /// - 매개변수 접두사 자동 추가 (@)
        /// 
        /// 🔄 처리 과정:
        /// 1. 입력 객체가 이미 Dictionary인지 확인
        /// 2. Dictionary인 경우 그대로 반환
        /// 3. 그렇지 않은 경우 리플렉션으로 프로퍼티 추출
        /// 4. 각 프로퍼티 이름에 @ 접두사 추가
        /// 5. null 값을 DBNull.Value로 변환
        /// 6. Dictionary 형태로 반환
        /// 
        /// 💡 사용 목적:
        /// - 익명 객체를 SQL 매개변수로 변환
        /// - 이미 Dictionary인 경우 처리
        /// - 매개변수화된 쿼리 지원
        /// - SQL 인젝션 방지
        /// - 타입 안전성 보장
        /// 
        /// ⚠️ 예외 처리:
        /// - null 입력 시 빈 Dictionary 반환
        /// - 리플렉션 오류 시 해당 프로퍼티 스킵
        /// - 프로퍼티 값이 null인 경우 DBNull.Value로 변환
        /// 
        /// 💡 사용법:
        /// var dict = ConvertObjectToDictionary(new { id = 1, name = "test" });
        /// // 결과: { "@id": 1, "@name": "test" }
        /// 
        /// var dict2 = ConvertObjectToDictionary(new { value = (string?)null });
        /// // 결과: { "@value": DBNull.Value }
        /// 
        /// var dict3 = ConvertObjectToDictionary(new Dictionary<string, object> { { "@key", "value" } });
        /// // 결과: { "@key": "value" } (그대로 반환)
        /// </summary>
        /// <param name="obj">변환할 객체 (익명 객체, Dictionary 등)</param>
        /// <returns>Dictionary 형태의 매개변수 (키: @프로퍼티명, 값: 프로퍼티값)</returns>
        private Dictionary<string, object> ConvertObjectToDictionary(object obj)
        {
            var dictionary = new Dictionary<string, object>();
            
            // null 체크
            if (obj == null)
                return dictionary;
            
            try
            {
                // 이미 Dictionary<string, object>인 경우 그대로 반환
                if (obj is Dictionary<string, object> existingDict)
                {
                    // 기존 Dictionary의 값들을 복사하면서 null 값을 DBNull.Value로 변환
                    foreach (var kvp in existingDict)
                    {
                        dictionary[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                    Console.WriteLine($"✅ DatabaseService: 기존 Dictionary 사용 - {dictionary.Count}개 매개변수");
                    return dictionary;
                }
                
                // 리플렉션을 사용하여 객체의 프로퍼티들 추출
                var properties = obj.GetType().GetProperties();
                
                foreach (var property in properties)
                {
                    try
                    {
                        // 프로퍼티 값 추출
                        var value = property.GetValue(obj);
                        
                        // 매개변수 이름 생성 (@ 접두사 추가)
                        var parameterName = $"@{property.Name}";
                        
                        // null 값을 DBNull.Value로 변환
                        dictionary[parameterName] = value ?? DBNull.Value;
                    }
                    catch (Exception ex)
                    {
                        // 개별 프로퍼티 처리 실패 시 로그 출력 후 스킵
                        Console.WriteLine($"⚠️ DatabaseService: 프로퍼티 '{property.Name}' 처리 실패: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"✅ DatabaseService: 객체를 Dictionary로 변환 완료 - {dictionary.Count}개 매개변수");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 객체 변환 실패: {ex.Message}");
            }
            
            return dictionary;
        }

        #endregion

        #region 연결 관리 (Connection Management)

        /// <summary>
        /// 데이터베이스 연결 객체 반환 (동기)
        /// </summary>
        /// <returns>MySQL 연결 객체</returns>
        public MySqlConnection GetConnection()
        {
            try
            {
                var connection = new MySqlConnection(_connectionString);
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 객체 생성 완료");
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 데이터베이스 연결 객체 생성 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 데이터베이스 연결 객체 반환 (비동기)
        /// </summary>
        /// <returns>MySQL 연결 객체</returns>
        public async Task<MySqlConnection> GetConnectionAsync()
        {
            try
            {
                var connection = new MySqlConnection(_connectionString);
                Console.WriteLine("✅ DatabaseService: 데이터베이스 연결 객체 생성 완료 (비동기)");
                return await Task.FromResult(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 데이터베이스 연결 객체 생성 실패 (비동기): {ex.Message}");
                throw;
            }
        }

        #endregion
    }
} 