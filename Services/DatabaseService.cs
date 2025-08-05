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

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// DatabaseService 생성자
        /// 
        /// 초기화 작업:
        /// 1. settings.json에서 데이터베이스 설정 읽기
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
            // JSON 파일에서 설정을 읽어서 연결 문자열 생성
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            var settings = new Dictionary<string, string>();
            
            Console.WriteLine($"🔍 DatabaseService: 설정 파일 경로 = {settingsPath}");
            
            try
            {
                // 설정 파일 존재 여부 확인
                if (File.Exists(settingsPath))
                {
                    // JSON 파일 내용 읽기
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
                            // JSON 역직렬화 실패 시 상세한 오류 정보 기록
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
                // 설정 파일 읽기 실패 시 상세한 오류 정보 기록
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
            Console.WriteLine($"🔍 DatabaseService: 설정값 검증");
            Console.WriteLine($"   DB_SERVER: '{server}' (길이: {server?.Length ?? 0})");
            Console.WriteLine($"   DB_NAME: '{database}' (길이: {database?.Length ?? 0})");
            Console.WriteLine($"   DB_USER: '{user}' (길이: {user?.Length ?? 0})");
            Console.WriteLine($"   DB_PASSWORD: '{password}' (길이: {password?.Length ?? 0})");
            Console.WriteLine($"   DB_PORT: '{port}' (길이: {port?.Length ?? 0})");
            
            // 설정값 검증 (필수 값이 누락된 경우 기본값 사용)
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user))
            {
                Console.WriteLine("⚠️ DatabaseService: 필수 설정값이 누락되어 기본값을 사용합니다.");
                server = "gramwonlogis.mycafe24.com";
                database = "gramwonlogis";
                user = "gramwonlogis";
                password = "jung5516!";
                port = "3306";
            }
            
            // 최종 설정값 로깅
            Console.WriteLine($"🔗 DatabaseService: 최종 설정값");
            Console.WriteLine($"   서버: {server}");
            Console.WriteLine($"   데이터베이스: {database}");
            Console.WriteLine($"   사용자: {user}");
            Console.WriteLine($"   포트: {port}");
            
            // MySQL 연결 문자열 생성
            _connectionString = $"Server={server};Database={database};User={user};Password={password};Port={port};CharSet=utf8;";
            Console.WriteLine($"🔗 DatabaseService: 연결 문자열 생성 완료 (길이: {_connectionString.Length})");
            
            // MappingService 인스턴스 생성
            _mappingService = new MappingService();
            Console.WriteLine("✅ DatabaseService: MappingService 초기화 완료");
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
            // MySQL 연결 생성
            using var connection = new MySqlConnection(_connectionString);
            
            try
            {
                // 데이터베이스 연결
                await connection.OpenAsync();
                Console.WriteLine("✅ DatabaseService: 매개변수화된 트랜잭션 시작");
                
                // 트랜잭션 시작
                using var transaction = await connection.BeginTransactionAsync();
                
                try
                {
                    var totalAffectedRows = 0;
                    
                    // 각 쿼리를 순차적으로 실행
                    foreach (var (sql, parameters) in queriesWithParameters)
                    {
                        using var command = new MySqlCommand(sql, connection, transaction);
                        
                        // 매개변수 추가
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                        
                        var affectedRows = await command.ExecuteNonQueryAsync();
                        totalAffectedRows += affectedRows;
                        Console.WriteLine($"✅ DatabaseService: 매개변수화 쿼리 실행 완료 - {affectedRows}행 영향받음");
                    }
                    
                    // 모든 쿼리가 성공하면 커밋
                    await transaction.CommitAsync();
                    Console.WriteLine($"✅ DatabaseService: 매개변수화 트랜잭션 커밋 완료 - 총 {totalAffectedRows}행 처리됨");
                    return true;
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 롤백
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ DatabaseService: 매개변수화 트랜잭션 롤백 - {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DatabaseService: 매개변수화 트랜잭션 실행 실패: {ex.Message}");
                return false;
            }
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
            var user = ExtractValue(connectionString, "User=", ";");
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
    }
} 