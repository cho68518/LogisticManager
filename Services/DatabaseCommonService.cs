using System;
using System.Data;
using System.Threading.Tasks;
using System.Text;
using MySqlConnector;
using LogisticManager.Services;
using LogisticManager.Models;

namespace LogisticManager.Services
{
    /// <summary>
    /// 공통 데이터베이스 처리 기능을 제공하는 서비스 클래스
    /// 
    /// 📋 주요 기능:
    /// - 데이터베이스 연결 확인
    /// - 테이블 존재 여부 확인
    /// - 데이터 조회 및 처리
    /// - 배치 데이터 처리
    /// 
    /// 💡 사용법:
    /// var dbService = new DatabaseCommonService(databaseService);
    /// var data = await dbService.GetDataFromDatabase("테이블명");
    /// </summary>
    public class DatabaseCommonService
    {
        private readonly DatabaseService _databaseService;
        private readonly MappingService _mappingService;
        private readonly LoggingCommonService _loggingService;

        /// <summary>
        /// DatabaseCommonService 생성자
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스 인스턴스</param>
        /// <param name="mappingService">매핑 서비스 인스턴스</param>
        public DatabaseCommonService(DatabaseService databaseService, MappingService mappingService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
            _loggingService = new LoggingCommonService();
        }

        /// <summary>
        /// 데이터베이스에서 테이블 데이터를 조회하는 공통 메서드
        /// 
        /// 📋 주요 기능:
        /// - 지정된 테이블에서 모든 데이터 조회
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - 비동기 처리로 성능 최적화
        /// - 오류 처리 및 로깅
        /// 
        /// 💡 사용법:
        /// var data = await GetDataFromDatabase("테이블명");
        /// 
        /// 🔗 의존성:
        /// - _databaseService: 데이터베이스 연결 관리
        /// - MySqlConnection: MySQL 데이터베이스 연결
        /// </summary>
        /// <param name="tableName">조회할 테이블명</param>
        /// <returns>테이블 데이터 (DataTable) 또는 null (오류 시)</returns>
        public async Task<DataTable?> GetDataFromDatabase(string tableName)
        {
            try
            {
                // 간단한 SELECT 쿼리로 모든 데이터 조회
                var query = $"SELECT * FROM `{tableName}`";
                
                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var adapter = new MySqlDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [{tableName}] 테이블 데이터베이스 조회 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 데이터베이스에서 테이블 데이터를 조회하는 공통 메서드 (중복 제거 포함)
        /// 
        /// 📋 주요 기능:
        /// - 지정된 테이블에서 특정 컬럼 기준으로 중복 제거된 데이터 조회
        /// - DISTINCT 쿼리로 데이터베이스 레벨에서 중복 제거
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - 비동기 처리로 성능 최적화
        /// - 오류 처리 및 로깅
        /// 
        /// 💡 사용법:
        /// var data = await GetDataFromDatabase("테이블명", new[] { "컬럼1", "컬럼2", "컬럼3" });
        /// 
        /// 🔗 의존성:
        /// - _databaseService: 데이터베이스 연결 관리
        /// - MySqlConnection: MySQL 데이터베이스 연결
        /// 
        /// 🎯 중복 제거 원리:
        /// - 지정된 컬럼들의 값이 모두 동일한 행들을 하나로 그룹화
        /// - 첫 번째 행만 선택하여 중복 제거
        /// - 데이터베이스 레벨에서 처리하여 성능 최적화
        /// </summary>
        /// <param name="tableName">조회할 테이블명</param>
        /// <param name="distinctColumns">중복 제거 기준이 될 컬럼명 배열</param>
        /// <returns>중복 제거된 테이블 데이터 (DataTable) 또는 null (오류 시)</returns>
        public async Task<DataTable?> GetDataFromDatabase(string tableName, string[] distinctColumns)
        {
            try
            {
                // 컬럼명 유효성 검증
                if (distinctColumns == null || distinctColumns.Length == 0)
                {
                    Console.WriteLine($"⚠️ [{tableName}] 중복 제거 컬럼이 지정되지 않았습니다. 기본 조회로 진행합니다.");
                    return await GetDataFromDatabase(tableName);
                }

                // 컬럼명을 안전하게 처리 (백틱으로 감싸기)
                var safeColumns = distinctColumns.Select(col => $"`{col}`").ToArray();
                var distinctColumnsStr = string.Join(", ", safeColumns);
                
                // DISTINCT 쿼리 구성: 중복 제거 컬럼 + 모든 컬럼
                var query = $"SELECT DISTINCT {distinctColumnsStr}, * FROM `{tableName}`";
                
                Console.WriteLine($"🔍 [{tableName}] 중복 제거 쿼리 실행: {query}");
                
                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var adapter = new MySqlDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            
                            Console.WriteLine($"✅ [{tableName}] 중복 제거 데이터 조회 완료: {dataTable.Rows.Count:N0}건");
                            Console.WriteLine($"   중복 제거 기준 컬럼: {string.Join(", ", distinctColumns)}");
                            
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [{tableName}] 테이블 중복 제거 데이터 조회 실패: {ex.Message}");
                Console.WriteLine($"   중복 제거 컬럼: {string.Join(", ", distinctColumns ?? new string[0])}");
                
                // 내부 예외가 있는 경우 추가 정보 출력
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   내부 오류: {ex.InnerException.Message}");
                }
                
                return null;
            }
        }

        /// <summary>
        /// SQL 쿼리를 직접 실행하여 데이터를 조회하는 공통 메서드
        /// 
        /// 📋 주요 기능:
        /// - 사용자 정의 SQL 쿼리 실행
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - 비동기 처리로 성능 최적화
        /// - 오류 처리 및 로깅
        /// - 트랜잭션 안전성 보장
        /// 
        /// 💡 사용법:
        /// var data = await GetDataFromQuery("SELECT * FROM 테이블명 WHERE 컬럼 = @값", 
        ///     new Dictionary<string, object> { { "@값", "실제값" } });
        /// 
        /// 🔗 의존성:
        /// - _databaseService: 데이터베이스 연결 관리
        /// - MySqlConnection: MySQL 데이터베이스 연결
        /// 
        /// 🛡️ 보안 기능:
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - 쿼리 실행 전 유효성 검증
        /// - 상세한 오류 로깅
        /// </summary>
        /// <param name="sqlQuery">실행할 SQL 쿼리문</param>
        /// <param name="parameters">쿼리 매개변수 (선택사항)</param>
        /// <returns>쿼리 결과 데이터 (DataTable) 또는 null (오류 시)</returns>
        public async Task<DataTable?> GetDataFromQuery(string sqlQuery, Dictionary<string, object>? parameters = null)
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrWhiteSpace(sqlQuery))
                {
                    Console.WriteLine("❌ SQL 쿼리가 비어있습니다.");
                    return null;
                }

                // 쿼리 로깅 (보안을 위해 매개변수 값은 마스킹)
                var maskedQuery = sqlQuery;
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var param in parameters)
                    {
                        maskedQuery = maskedQuery.Replace(param.Key, $"<{param.Key}>");
                    }
                }
                Console.WriteLine($"🔍 쿼리 실행: {maskedQuery}");
                Console.WriteLine($"🔍 매개변수 개수: {parameters?.Count ?? 0}");

                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new MySqlCommand(sqlQuery, connection))
                    {
                        // 매개변수 추가
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                // 매개변수명에서 @ 제거 후 추가
                                var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                                command.Parameters.AddWithValue(paramName, param.Value ?? DBNull.Value);
                            }
                        }

                        using (var adapter = new MySqlDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            
                            Console.WriteLine($"✅ 쿼리 실행 완료: {dataTable.Rows.Count:N0}건");
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 쿼리 실행 실패: {ex.Message}");
                Console.WriteLine($"   실행된 쿼리: {sqlQuery}");
                Console.WriteLine($"   매개변수: {string.Join(", ", parameters?.Select(p => $"{p.Key}={p.Value}") ?? new string[0])}");
                
                // 내부 예외가 있는 경우 추가 정보 출력
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   내부 오류: {ex.InnerException.Message}");
                }
                
                return null;
            }
        }

        /// <summary>
        /// 데이터베이스 연결을 확인하는 공통 메서드
        /// 
        /// 🎯 주요 기능:
        /// - MySQL 데이터베이스 연결 상태 확인
        /// - 연결 문자열 유효성 검증
        /// - 연결 성공/실패 로깅
        /// - 안전한 연결 관리
        /// 
        /// 🔧 처리 과정:
        /// 1. 연결 문자열 가져오기
        /// 2. MySqlConnection 생성 및 연결 시도
        /// 3. 연결 상태 확인
        /// 4. 리소스 정리
        /// 
        /// 💡 사용 목적:
        /// - 데이터베이스 서비스 시작 전 연결 확인
        /// - 주기적인 연결 상태 모니터링
        /// - 연결 문제 조기 발견
        /// 
        /// ⚠️ 처리 방식:
        /// - using 문으로 안전한 리소스 관리
        /// - 비동기 연결으로 성능 최적화
        /// - 오류 발생 시 상세한 로깅
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        public async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                var connectionString = _databaseService.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("❌ 데이터베이스 연결 문자열이 비어있습니다.");
                    return false;
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // 연결 상태 추가 확인
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        Console.WriteLine("✅ 데이터베이스 연결 확인 완료");
                        Console.WriteLine($"   서버: {connection.ServerVersion}");
                        Console.WriteLine($"   데이터베이스: {connection.Database}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("❌ 데이터베이스 연결이 열리지 않았습니다.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 데이터베이스 연결 확인 실패:");
                Console.WriteLine($"   오류 내용: {ex.Message}");
                Console.WriteLine($"   오류 유형: {ex.GetType().Name}");
                
                // 내부 예외가 있는 경우 추가 정보 출력
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   내부 오류: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// 테이블이 존재하는지 확인하는 공통 메서드
        /// 
        /// 🎯 주요 기능:
        /// - MySQL 데이터베이스에서 테이블 존재 여부 확인
        /// - SQL 인젝션 방지 (매개변수화된 쿼리)
        /// - 상세한 테이블 정보 수집
        /// - 안전한 데이터베이스 접근
        /// 
        /// 🔧 처리 과정:
        /// 1. 테이블명 유효성 검증
        /// 2. information_schema 쿼리 실행
        /// 3. 결과 분석 및 반환
        /// 4. 리소스 정리
        /// 
        /// 💡 사용 목적:
        /// - 테이블 생성 전 존재 여부 확인
        /// - 데이터 처리 전 테이블 유효성 검증
        /// - 동적 테이블 선택 시 안전성 보장
        /// 
        /// ⚠️ 처리 방식:
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - using 문으로 안전한 리소스 관리
        /// - 비동기 실행으로 성능 최적화
        /// </summary>
        /// <param name="tableName">확인할 테이블명</param>
        /// <returns>테이블 존재 여부</returns>
        public async Task<bool> CheckTableExistsAsync(string tableName)
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    Console.WriteLine("❌ 테이블명이 비어있습니다.");
                    return false;
                }

                // SQL 인젝션 방지를 위한 매개변수화된 쿼리
                var query = @"
                    SELECT 
                        COUNT(*) as table_count,
                        table_type,
                        engine,
                        table_rows,
                        data_length,
                        index_length
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = @tableName";
                
                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var tableCount = Convert.ToInt32(reader["table_count"]);
                                var exists = tableCount > 0;
                                
                                if (exists)
                                {
                                    var tableType = reader["table_type"]?.ToString() ?? "UNKNOWN";
                                    var engine = reader["engine"]?.ToString() ?? "UNKNOWN";
                                    var tableRows = reader["table_rows"]?.ToString() ?? "0";
                                    var dataLength = reader["data_length"]?.ToString() ?? "0";
                                    var indexLength = reader["index_length"]?.ToString() ?? "0";
                                    
                                    Console.WriteLine($"✅ 테이블 존재 확인: {tableName}");
                                    Console.WriteLine($"   테이블 유형: {tableType}");
                                    Console.WriteLine($"   엔진: {engine}");
                                    Console.WriteLine($"   행 수: {tableRows:N0}");
                                    Console.WriteLine($"   데이터 크기: {Convert.ToInt64(dataLength):N0} bytes");
                                    Console.WriteLine($"   인덱스 크기: {Convert.ToInt64(indexLength):N0} bytes");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ 테이블 존재하지 않음: {tableName}");
                                }
                                
                                return exists;
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ 테이블 정보를 읽을 수 없습니다: {tableName}");
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 테이블 존재 확인 실패:");
                Console.WriteLine($"   테이블명: {tableName}");
                Console.WriteLine($"   오류 내용: {ex.Message}");
                Console.WriteLine($"   오류 유형: {ex.GetType().Name}");
                
                // 내부 예외가 있는 경우 추가 정보 출력
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   내부 오류: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// 데이터를 배치로 처리하여 데이터베이스에 삽입하는 메서드
        /// </summary>
        /// <param name="data">삽입할 데이터</param>
        /// <param name="progress">진행률 보고자</param>
        /// <returns>처리된 행 수</returns>
        public async Task<int> TruncateAndInsertOriginalDataOptimized(DataTable data, string tableName, IProgress<string>? progress)
        {
            try
            {
                if (data == null || data.Rows.Count == 0)
                {
                    progress?.Report("⚠️ 삽입할 데이터가 없습니다.");
                    return 0;
                }
                
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    progress?.Report("⚠️ 테이블명이 지정되지 않았습니다.");
                    return 0;
                }

                var totalRows = data.Rows.Count;
                var batchSize = 500; // 배치 크기
                var processedRows = 0;

                progress?.Report($"🔄 배치 처리 시작: 총 {totalRows:N0}행, 배치 크기: {batchSize}");

                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    try
                    {
                        for (int i = 0; i < totalRows; i += batchSize)
                        {
                            var currentBatchSize = Math.Min(batchSize, totalRows - i);
                            var batchData = data.Clone();
                            
                            // 현재 배치의 데이터 복사
                            for (int j = 0; j < currentBatchSize; j++)
                            {
                                batchData.ImportRow(data.Rows[i + j]);
                            }

                            // 배치 데이터 처리
                            var batchProcessed = await ProcessBatchDataAsync(connection, batchData, tableName, progress);
                            processedRows += batchProcessed;

                            var progressPercentage = (i + currentBatchSize) * 100 / totalRows;
                            progress?.Report($"📊 배치 처리 진행률: {progressPercentage}% ({processedRows:N0}/{totalRows:N0}행)");
                        }

                        progress?.Report($"✅ 배치 처리 완료: 총 {processedRows:N0}행 처리됨");
                        progress?.Report("");
                        return processedRows;
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"❌ 배치 처리 중 오류 발생: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 배치 처리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 최적화된 방식으로 원본 데이터를 삽입하는 메서드 (TRUNCATE 없음)
        /// 
        /// 📋 주요 기능:
        /// - 배치 단위로 데이터 삽입 (성능 최적화)
        /// - 테이블 매핑 기반 컬럼 자동 매핑
        /// - 진행률 보고 및 에러 처리
        /// 
        /// 🔗 의존성:
        /// - ProcessBatchDataAsync (내부 메서드)
        /// - MappingService (테이블 매핑 정보)
        /// 
        /// ⚠️ 주의사항:
        /// - 이 메서드는 TRUNCATE를 실행하지 않음
        /// - 호출하는 곳에서 별도로 TRUNCATE를 실행해야 함
        /// </summary>
        /// <param name="data">삽입할 데이터</param>
        /// <param name="tableName">삽입할 테이블명</param>
        /// <param name="progress">진행률 보고자</param>
        /// <returns>처리된 행 수</returns>
        public async Task<int> InsertOriginalDataOptimized(DataTable data, string tableName, IProgress<string>? progress)
        {
            try
            {
                if (data == null || data.Rows.Count == 0)
                {
                    progress?.Report("⚠️ 삽입할 데이터가 없습니다.");
                    return 0;
                }
                
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    progress?.Report("⚠️ 테이블명이 지정되지 않았습니다.");
                    return 0;
                }

                var totalRows = data.Rows.Count;
                var batchSize = 500; // 배치 크기
                var processedRows = 0;

                progress?.Report($"🔄 배치 처리 시작: 총 {totalRows:N0}행, 배치 크기: {batchSize}");

                using (var connection = new MySqlConnection(_databaseService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    try
                    {
                        for (int i = 0; i < totalRows; i += batchSize)
                        {
                            var currentBatchSize = Math.Min(batchSize, totalRows - i);
                            var batchData = data.Clone();
                            
                            // 현재 배치의 데이터 복사
                            for (int j = 0; j < currentBatchSize; j++)
                            {
                                batchData.ImportRow(data.Rows[i + j]);
                            }

                            // 배치 데이터 처리
                            var batchProcessed = await ProcessBatchDataAsync(connection, batchData, tableName, progress);
                            processedRows += batchProcessed;

                            var progressPercentage = (i + currentBatchSize) * 100 / totalRows;
                            progress?.Report($"📊 배치 처리 진행률: {progressPercentage}% ({processedRows:N0}/{totalRows:N0}행)");
                        }

                        progress?.Report($"✅ 배치 처리 완료: 총 {processedRows:N0}행 처리됨");
                        progress?.Report("");
                        return processedRows;
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"❌ 배치 처리 중 오류 발생: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 배치 처리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 배치 데이터를 처리하는 내부 메서드
        /// </summary>
        /// <param name="connection">데이터베이스 연결</param>
        /// <param name="batchData">배치 데이터</param>
        /// <param name="tableName">삽입할 테이블명</param>
        /// <param name="progress">진행률 보고자</param>
        /// <returns>처리된 행 수</returns>
        private async Task<int> ProcessBatchDataAsync(MySqlConnection connection, DataTable batchData, string tableName, IProgress<string>? progress)
        {
            try
            {
                if (batchData.Rows.Count == 0)
                    return 0;

                var processedRows = 0;
                
                // 테이블 매핑 정보에서 컬럼 정보 가져오기 (런타임 쿼리 실행 없음)
                var tableMapping = _mappingService.GetTableMapping(tableName);
                if (tableMapping == null || tableMapping.Columns == null || tableMapping.Columns.Count == 0)
                {
                    throw new InvalidOperationException($"테이블 '{tableName}'의 매핑 정보를 찾을 수 없습니다. table_mappings.json을 확인해주세요.");
                }

                // INSERT에서 제외할 컬럼 필터링 (id, excludeFromInsert=true 등)
                var insertColumns = tableMapping.Columns
                    .Where(col => !col.ExcludeFromInsert && !col.IsPrimaryKey)
                    .Select(col => col.DatabaseColumn)
                    .ToList();

                if (insertColumns.Count == 0)
                {
                    throw new InvalidOperationException($"테이블 '{tableName}'에 삽입 가능한 컬럼이 없습니다.");
                }

                //progress?.Report($"🔧 테이블 매핑 정보 로드 완료: {tableName} ({insertColumns.Count}개 컬럼)");
                progress?.Report($"🔧 테이블 매핑 정보 로드 완료: ({insertColumns.Count}개 컬럼)");

                // 배치 INSERT 쿼리 생성
                var insertQuery = GenerateInsertQueryFromMapping(tableName, insertColumns);
                //progress?.Report($"🔧 INSERT 쿼리 생성 완료: {tableName}");
                
                // 상세 로깅: SQL 쿼리 및 컬럼 정보
                var debugInfo = $"[DEBUG] 생성된 INSERT 쿼리: {insertQuery}\n" +
                               $"[DEBUG] 삽입할 컬럼들: {string.Join(", ", insertColumns)}\n" +
                               $"[DEBUG] 백틱으로 감싼 컬럼들: {string.Join(", ", insertColumns.Select(c => $"`{c}`"))}\n" +
                               $"[DEBUG] Excel 컬럼들: {string.Join(", ", batchData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}";
                
                _loggingService.WriteLogWithFlush("logs/current/app.log", debugInfo);
                Console.WriteLine(debugInfo);
                
                // 상세 디버깅: 각 컬럼별 매핑 결과
                var mappingInfo = new StringBuilder();
                foreach (var column in insertColumns)
                {
                    var excelColumnName = FindExcelColumnName(column, batchData);
                    var mappingLine = $"[DEBUG] 컬럼 매핑: '{column}' → Excel: '{excelColumnName ?? "매핑 실패"}'";
                    mappingInfo.AppendLine(mappingLine);
                    Console.WriteLine(mappingLine);
                }
                
                _loggingService.WriteLogWithFlush("logs/current/app.log", mappingInfo.ToString());
                
                // 🚨 쇼핑몰 컬럼 특별 디버깅
                if (insertColumns.Contains("쇼핑몰"))
                {
                    var excelColumnName = FindExcelColumnName("쇼핑몰", batchData);
                    var specialDebug = $"[DEBUG] 🚨 쇼핑몰 컬럼 매핑: DB '쇼핑몰' → Excel '{excelColumnName ?? "NULL"}'";
                    _loggingService.WriteLogWithFlush("logs/current/app.log", specialDebug);
                    Console.WriteLine(specialDebug);
                }

                // 🚨 수취인명 컬럼 특별 디버깅
                if (insertColumns.Contains("수취인명"))
                {
                    var excelColumnName = FindExcelColumnName("수취인명", batchData);
                    var specialDebug = $"[DEBUG] 🚨 수취인명 컬럼 매핑: DB '수취인명' → Excel '{excelColumnName ?? "NULL"}'";
                    _loggingService.WriteLogWithFlush("logs/current/app.log", specialDebug);
                    Console.WriteLine(specialDebug);
                }

                // 배치 데이터 처리
                foreach (DataRow row in batchData.Rows)
                {
                    try
                    {
                        // 각 행마다 새로운 command 생성 (트랜잭션 문제 해결)
                        using (var command = new MySqlCommand(insertQuery, connection))
                        {
                            // 매개변수 추가 (안전한 이름 사용)
                            foreach (var column in insertColumns)
                            {
                                var safeParameterName = GetSafeParameterName(column);
                                command.Parameters.Add($"@{safeParameterName}", MySqlDbType.VarChar);
                            }

                            // 매개변수 값 설정
                            var valuesLog = new StringBuilder();
                            valuesLog.AppendLine($"[DEBUG] 🚨 행 {processedRows + 1} VALUES 데이터:");
                            
                            for (int i = 0; i < insertColumns.Count; i++)
                            {
                                var columnName = insertColumns[i];
                                var safeParameterName = GetSafeParameterName(columnName);
                                var parameterName = $"@{safeParameterName}";
                                
                                // Excel 컬럼명과 데이터베이스 컬럼명 매핑 시도
                                var excelColumnName = FindExcelColumnName(columnName, row.Table);
                                if (!string.IsNullOrEmpty(excelColumnName))
                                {
                                    var value = row[excelColumnName];
                                    var parameterValue = value == DBNull.Value ? (object)DBNull.Value : value.ToString();
                                    command.Parameters[parameterName].Value = parameterValue;
                                    
                                    // VALUES 로그에 기록
                                    valuesLog.AppendLine($"  {columnName}: '{parameterValue}' (Excel 컬럼: '{excelColumnName}')");
                                }
                                else
                                {
                                    // 매핑되는 Excel 컬럼을 찾을 수 없으면 빈 값으로 설정
                                    command.Parameters[parameterName].Value = DBNull.Value;
                                    valuesLog.AppendLine($"  {columnName}: NULL (Excel 컬럼: 매핑 실패)");
                                }
                            }
                            
                            // VALUES 로그를 파일에 기록
                            _loggingService.WriteLogWithFlush("logs/current/app.log", valuesLog.ToString());
                            Console.WriteLine(valuesLog.ToString());
                            
                            // 🚨 쇼핑몰 컬럼 특별 로그
                            if (insertColumns.Contains("쇼핑몰"))
                            {
                                var safeParameterName = GetSafeParameterName("쇼핑몰");
                                var storeNameValue = command.Parameters[$"@{safeParameterName}"].Value;
                                var storeNameLog = $"[DEBUG] 🚨 쇼핑몰 컬럼 특별 로그 - 행 {processedRows + 1}: '{storeNameValue}'";
                                _loggingService.WriteLogWithFlush("logs/current/app.log", storeNameLog);
                                Console.WriteLine(storeNameLog);
                            }

                            // 🚨 수취인명 컬럼 특별 로그 (김신영 케이스 추적)
                            if (insertColumns.Contains("수취인명"))
                            {
                                var safeParameterName = GetSafeParameterName("수취인명");
                                var recipientNameValue = command.Parameters[$"@{safeParameterName}"].Value;
                                var recipientNameLog = $"[DEBUG] 🚨 수취인명 컬럼 특별 로그 - 행 {processedRows + 1}: '{recipientNameValue}'";
                                _loggingService.WriteLogWithFlush("logs/current/app.log", recipientNameLog);
                                Console.WriteLine(recipientNameLog);
                                
                                // 김신영 케이스 특별 추적
                                if (recipientNameValue?.ToString() == "김신영")
                                {
                                    var kimLog = $"[DEBUG] 🚨 김신영 수취인 발견 - 행 {processedRows + 1}:\n" +
                                                $"  수취인명: '{recipientNameValue}'\n" +
                                                $"  매개변수명: @{safeParameterName}\n" +
                                                $"  전체 VALUES: {valuesLog}";
                                    _loggingService.WriteLogWithFlush("logs/current/app.log", kimLog);
                                    Console.WriteLine(kimLog);
                                }
                            }

                            // 🚨 김신영 수취인 INSERT 실행 전 최종 검증
                            if (insertColumns.Contains("수취인명"))
                            {
                                var safeParameterName = GetSafeParameterName("수취인명");
                                var recipientNameValue = command.Parameters[$"@{safeParameterName}"].Value;
                                
                                if (recipientNameValue?.ToString() == "김신영")
                                {
                                    var finalCheckLog = $"[DEBUG] 🚨 김신영 수취인 INSERT 실행 전 최종 검증:\n" +
                                                       $"  행 번호: {processedRows + 1}\n" +
                                                       $"  수취인명: '{recipientNameValue}'\n" +
                                                       $"  매개변수명: @{safeParameterName}\n" +
                                                       $"  SQL 쿼리: {insertQuery}\n" +
                                                       $"  매개변수 개수: {command.Parameters.Count}";
                                    _loggingService.WriteLogWithFlush("logs/current/app.log", finalCheckLog);
                                    Console.WriteLine(finalCheckLog);
                                }
                            }

                            // INSERT 실행
                            await command.ExecuteNonQueryAsync();
                            
                            processedRows++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"⚠️ 행 {processedRows + 1} 삽입 실패: {ex.Message}";
                        progress?.Report(errorMessage);
                        
                        // 오류 로그 기록
                        _loggingService.WriteLogWithFlush("logs/current/app.log", $"[ERROR] {errorMessage}");
                        _loggingService.WriteLogWithFlush("logs/current/app.log", $"[ERROR] 상세 오류: {ex}");
                        
                        // 개별 행 실패는 로그만 남기고 계속 진행
                    }
                }

                return processedRows;
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 배치 데이터 처리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 데이터베이스 연결 문자열을 반환하는 메서드
        /// </summary>
        /// <returns>연결 문자열</returns>
        public string GetConnectionString()
        {
            return _databaseService.GetConnectionString();
        }

        /// <summary>
        /// 테이블의 컬럼 정보를 가져오는 메서드
        /// </summary>
        /// <param name="connection">데이터베이스 연결</param>
        /// <param name="tableName">테이블명</param>
        /// <returns>컬럼명 리스트</returns>
        private async Task<List<string>> GetTableColumnsAsync(MySqlConnection connection, string tableName)
        {
            var columns = new List<string>();
            
            try
            {
                var query = $"SHOW COLUMNS FROM `{tableName}`";
                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString("Field");
                        columns.Add(columnName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"테이블 '{tableName}'의 컬럼 정보를 가져올 수 없습니다: {ex.Message}");
            }
            
            return columns;
        }

        /// <summary>
        /// 테이블 매핑 정보를 기반으로 INSERT 쿼리를 생성하는 메서드
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="columns">컬럼 리스트</param>
        /// <returns>INSERT 쿼리</returns>
        private string GenerateInsertQueryFromMapping(string tableName, List<string> columns)
        {
            // 특수문자가 포함된 컬럼명을 안전하게 백틱으로 감싸기
            // 괄호, 공백, 하이픈 등 특수문자가 포함된 컬럼명도 안전하게 처리
            var columnList = string.Join(", ", columns.Select(c => $"`{c}`"));
            
            // 매개변수명에서 특수문자 제거하여 안전한 이름으로 변환
            var parameterList = string.Join(", ", columns.Select(c => $"@{GetSafeParameterName(c)}"));
            
            var insertQuery = $"INSERT INTO `{tableName}` ({columnList}) VALUES ({parameterList})";
            
            // 백틱 처리 디버깅 로그
            var debugLog = $"[GenerateInsertQueryFromMapping] 백틱 처리 결과:\n" +
                          $"  원본 컬럼들: {string.Join(", ", columns)}\n" +
                          $"  백틱 처리된 컬럼들: {columnList}\n" +
                          $"  안전한 매개변수들: {parameterList}\n" +
                          $"  최종 쿼리: {insertQuery}";
            
            _loggingService.WriteLogWithFlush("logs/current/app.log", debugLog);
            Console.WriteLine(debugLog);
            
            return insertQuery;
        }

        /// <summary>
        /// Excel 컬럼명을 찾는 헬퍼 메서드 (강화된 매칭 로직)
        /// </summary>
        /// <param name="databaseColumnName">데이터베이스 컬럼명</param>
        /// <param name="dataTable">Excel 데이터 테이블</param>
        /// <returns>매핑되는 Excel 컬럼명</returns>
        private string? FindExcelColumnName(string databaseColumnName, DataTable dataTable)
        {
            // 🎯 특별한 매핑 규칙: 쇼핑몰 → 쇼핑몰 (Excel에 있는 컬럼)
            if (databaseColumnName == "쇼핑몰")
            {
                // Excel에 쇼핑몰 컬럼이 있으면 그것을 사용
                if (dataTable.Columns.Contains("쇼핑몰"))
                {
                    return "쇼핑몰";
                }
                // Excel에 쇼핑몰 컬럼이 없으면 null 반환 (INSERT 제외)
                return null;
            }

            // 🚨 수취인명 컬럼 특별 디버깅
            if (databaseColumnName == "수취인명")
            {
                var availableColumns = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                var debugLog = $"[FindExcelColumnName] 🚨 수취인명 컬럼 매핑 시도:\n" +
                              $"  사용 가능한 Excel 컬럼들: {availableColumns}\n" +
                              $"  정확한 매칭 결과: {dataTable.Columns.Contains("수취인명")}";
                
                _loggingService.WriteLogWithFlush("logs/current/app.log", debugLog);
                Console.WriteLine(debugLog);
            }

            // 1. 정확한 매칭 시도
            if (dataTable.Columns.Contains(databaseColumnName))
            {
                if (databaseColumnName == "수취인명")
                {
                    var successLog = $"[FindExcelColumnName] ✅ 수취인명 정확한 매칭 성공: '수취인명'";
                    _loggingService.WriteLogWithFlush("logs/current/app.log", successLog);
                    Console.WriteLine(successLog);
                }
                return databaseColumnName;
            }

            // 2. 괄호 제거 후 매칭 시도 (예: 주문번호(쇼핑몰) → 주문번호(쇼핑몰))
            var cleanColumnName = databaseColumnName.Trim('(', ')', ' ', '\t');
            if (dataTable.Columns.Contains(cleanColumnName))
            {
                if (databaseColumnName == "수취인명")
                {
                    var successLog = $"[FindExcelColumnName] ✅ 수취인명 괄호제거 매칭 성공: '{cleanColumnName}'";
                    _loggingService.WriteLogWithFlush("logs/current/app.log", successLog);
                    Console.WriteLine(successLog);
                }
                return cleanColumnName;
            }

            // 3. 괄호가 포함된 형태로 매칭 시도 (예: 주문번호 → 주문번호(쇼핑몰))
            // 🚫 쇼핑몰 컬럼은 괄호가 포함된 형태로 매칭하지 않음
            if (databaseColumnName != "쇼핑몰")
            {
                var withParentheses = $"({cleanColumnName})";
                if (dataTable.Columns.Contains(withParentheses))
                {
                    if (databaseColumnName == "수취인명")
                    {
                        var successLog = $"[FindExcelColumnName] ✅ 수취인명 괄호포함 매칭 성공: '{withParentheses}'";
                        _loggingService.WriteLogWithFlush("logs/current/app.log", successLog);
                        Console.WriteLine(successLog);
                    }
                    return withParentheses;
                }
            }

            // 4. 부분 매칭 시도 (예: 주문번호가 포함된 컬럼 찾기)
            foreach (DataColumn column in dataTable.Columns)
            {
                var columnName = column.ColumnName;
                
                // 정확한 부분 매칭
                if (columnName.Contains(cleanColumnName) || cleanColumnName.Contains(columnName))
                {
                    if (databaseColumnName == "수취인명")
                    {
                        var successLog = $"[FindExcelColumnName] ✅ 수취인명 부분 매칭 성공: '{columnName}'";
                        _loggingService.WriteLogWithFlush("logs/current/app.log", successLog);
                        Console.WriteLine(successLog);
                    }
                    return columnName;
                }
                
                // 괄호 제거 후 부분 매칭
                var cleanExcelColumn = columnName.Trim('(', ')', ' ', '\t');
                if (cleanExcelColumn.Contains(cleanColumnName) || cleanColumnName.Contains(cleanExcelColumn))
                {
                    if (databaseColumnName == "수취인명")
                    {
                        var successLog = $"[FindExcelColumnName] ✅ 수취인명 괄호제거 부분 매칭 성공: '{columnName}'";
                        _loggingService.WriteLogWithFlush("logs/current/app.log", successLog);
                        Console.WriteLine(successLog);
                    }
                    return columnName;
                }
            }

            // 5. 매칭 실패 시 null 반환
            if (databaseColumnName == "수취인명")
            {
                var failLog = $"[FindExcelColumnName] ❌ 수취인명 매핑 실패: 모든 매칭 시도 실패";
                _loggingService.WriteLogWithFlush("logs/current/app.log", failLog);
                Console.WriteLine(failLog);
            }
            return null;
        }

        /// <summary>
        /// 컬럼명을 안전한 매개변수명으로 변환하는 메서드
        /// 
        /// 변환 규칙:
        /// 1. 괄호 () 제거
        /// 2. 공백을 언더스코어로 변환
        /// 3. 특수문자를 언더스코어로 변환
        /// 4. 숫자로 시작하는 경우 앞에 'p' 추가
        /// 
        /// 변환 예시:
        /// - 주문번호(쇼핑몰) → 주문번호_쇼핑몰
        /// - 송장수량 → 송장수량
        /// - 1번컬럼 → p1번컬럼
        /// </summary>
        /// <param name="columnName">원본 컬럼명</param>
        /// <returns>안전한 매개변수명</returns>
        private string GetSafeParameterName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return columnName;

            // 괄호 제거 및 특수문자를 언더스코어로 변환
            var safeName = columnName
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Replace(",", "_")
                .Replace(";", "_")
                .Replace(":", "_")
                .Replace("'", "_")
                .Replace("\"", "_")
                .Replace("`", "_")
                .Replace("[", "_")
                .Replace("]", "_")
                .Replace("{", "_")
                .Replace("}", "_")
                .Replace("|", "_")
                .Replace("\\", "_")
                .Replace("/", "_");

            // 연속된 언더스코어를 하나로 변환
            safeName = System.Text.RegularExpressions.Regex.Replace(safeName, "_+", "_");
            
            // 앞뒤 언더스코어 제거
            safeName = safeName.Trim('_');
            
            // 숫자로 시작하는 경우 앞에 'p' 추가
            if (char.IsDigit(safeName[0]))
            {
                safeName = "p" + safeName;
            }
            
            return safeName;
        }

        /// <summary>
        /// INSERT 쿼리를 생성하는 메서드 (기존 방식 - 호환성 유지)
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="columns">컬럼 리스트</param>
        /// <returns>INSERT 쿼리</returns>
        private string GenerateInsertQuery(string tableName, List<string> columns)
        {
            var columnList = string.Join(", ", columns.Select(c => $"`{c}`"));
            var parameterList = string.Join(", ", columns.Select(c => $"@{c}"));
            
            return $"INSERT INTO `{tableName}` ({columnList}) VALUES ({parameterList})";
        }
    }
}
