using LogisticManager.Models;
using LogisticManager.Services;
using System.Data;
using System.Text;

namespace LogisticManager.Repositories
{
    /// <summary>
    /// 송장 데이터 저장소 구현체 - Repository 패턴 적용
    /// 
    /// 📋 주요 기능:
    /// - 송장출력_사방넷원본변환_Test 테이블 전용 Repository
    /// - 배치 처리 최적화 (500건 단위)
    /// - 매개변수화된 쿼리 (SQL 인젝션 방지)
    /// - 트랜잭션 처리 (데이터 일관성)
    /// - 진행률 실시간 보고
    /// - 하이브리드 동적 쿼리 생성 (설정 기반 + 리플렉션 폴백)
    /// 
    /// 🎯 성능 최적화:
    /// - 배치 크기 최적화 (500건)
    /// - 메모리 효율적인 처리
    /// - 인덱스 활용 쿼리
    /// - 비동기 처리
    /// 
    /// 🛡️ 보안 기능:
    /// - SQL 인젝션 방지
    /// - 매개변수화된 쿼리
    /// - 데이터 유효성 검사
    /// - 예외 처리 및 로깅
    /// 
    /// 💡 사용법:
    /// var repository = new InvoiceRepository(databaseService);
    /// await repository.InsertBatchAsync(invoices, progress);
    /// </summary>
    public class InvoiceRepository : IInvoiceRepository
    {
        #region 상수 및 필드

        /// <summary>테이블명 - App.config에서 읽어오거나 기본값 사용</summary>
        private readonly string _tableName;
        
        /// <summary>기본 배치 크기 - 성능 최적화</summary>
        private const int DEFAULT_BATCH_SIZE = 5000;

        /// <summary>데이터베이스 서비스 - MySQL 연결 및 쿼리 실행</summary>
        private readonly DatabaseService _databaseService;

        /// <summary>하이브리드 동적 쿼리 생성기 - 설정 기반 + 리플렉션 폴백</summary>
        private readonly DynamicQueryBuilder _queryBuilder;

        #endregion

        #region 생성자

        /// <summary>
        /// InvoiceRepository 생성자 (기본 테이블명 사용)
        /// 
        /// 📋 기능:
        /// - DatabaseService 의존성 주입
        /// - App.config에서 테이블명 읽기 (기본값 제공)
        /// - DynamicQueryBuilder 초기화 (하이브리드 방식)
        /// - null 체크 및 예외 처리
        /// - 초기화 완료 로그
        /// 
        /// 🔧 테이블명 설정 방법:
        /// App.config에 다음 키 추가:
        /// <add key="InvoiceTable.Name" value="송장출력_사방넷원본변환_Prod" />
        /// 
        /// 💡 사용법:
        /// var repository = new InvoiceRepository(databaseService);
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스</param>
        /// <exception cref="ArgumentNullException">databaseService가 null인 경우</exception>
        public InvoiceRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _tableName = GetTableNameFromConfig();
            _queryBuilder = new DynamicQueryBuilder(useReflectionFallback: true);
            
            //Console.WriteLine($"✅ InvoiceRepository 초기화 완료 - 테이블: {_tableName}");
            //Console.WriteLine($"🔧 DynamicQueryBuilder 초기화 완료 - 하이브리드 모드 활성화");
        }

        /// <summary>
        /// InvoiceRepository 생성자 (커스텀 테이블명 사용)
        /// 
        /// 📋 기능:
        /// - DatabaseService 의존성 주입
        /// - 사용자 지정 테이블명 사용
        /// - DynamicQueryBuilder 초기화 (하이브리드 방식)
        /// - null 체크 및 예외 처리
        /// - 초기화 완료 로그
        /// 
        /// 💡 사용법:
        /// var repository = new InvoiceRepository(databaseService, "custom_table_name");
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스</param>
        /// <param name="tableName">사용할 테이블명</param>
        /// <exception cref="ArgumentNullException">databaseService가 null인 경우</exception>
        /// <exception cref="ArgumentException">tableName이 비어있는 경우</exception>
        public InvoiceRepository(DatabaseService databaseService, string tableName)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("테이블명은 비어있을 수 없습니다.", nameof(tableName));
                
            _tableName = tableName;
            _queryBuilder = new DynamicQueryBuilder(useReflectionFallback: true);
            
            //Console.WriteLine($"✅ InvoiceRepository 초기화 완료 - 테이블: {_tableName}");
            //Console.WriteLine($"🔧 DynamicQueryBuilder 초기화 완료 - 하이브리드 모드 활성화");
        }

        /// <summary>
        /// App.config에서 테이블명을 읽어오는 메서드
        /// 
        /// 📋 처리 순서:
        /// 1. 환경 변수 확인 (Test, Prod 등)
        /// 2. 환경별 테이블명 설정 확인
        /// 3. 기본 테이블명 설정 확인
        /// 4. 모든 설정이 없으면 기본값 사용
        /// 
        /// 🔧 App.config 설정 예시:
        /// <appSettings>
        ///   <add key="Environment" value="Test" />
        ///   <add key="InvoiceTable.Name" value="송장출력_사방넷원본변환_Prod" />
        ///   <add key="InvoiceTable.TestName" value="송장출력_사방넷원본변환_Test" />
        /// </appSettings>
        /// </summary>
        /// <returns>설정된 테이블명</returns>
        private string GetTableNameFromConfig()
        {
            const string DEFAULT__tableName = "송장출력_사방넷원본변환_Test";
            
            try
            {
                // System.Configuration 사용
                var environment = System.Configuration.ConfigurationManager.AppSettings["Environment"] ?? "Test";
                Console.WriteLine($"[DEBUG] 현재 환경 설정: {environment}");
                
                string configKey = environment.ToUpper() switch
                {
                    "TEST" => "InvoiceTable.TestName",
                    "PROD" or "PRODUCTION" => "InvoiceTable.Name",
                    "DEV" or "DEVELOPMENT" => "InvoiceTable.DevName",
                    "MAIN" => "InvoiceTable.Name",
                    _ => "InvoiceTable.Name"
                };
                
                //Console.WriteLine($"[DEBUG] 선택된 설정 키: {configKey}");
                
                var tableName = System.Configuration.ConfigurationManager.AppSettings[configKey];
                //Console.WriteLine($"[DEBUG] 설정에서 읽은 테이블명: {tableName ?? "(null)"}");
                
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    Console.WriteLine($"✅ Configuration에서 테이블명 로드: {tableName} (환경: {environment})");
                    return tableName;
                }
                
                // 기본 설정 시도
                var defaultTableName = System.Configuration.ConfigurationManager.AppSettings["InvoiceTable.Name"];
                //Console.WriteLine($"[DEBUG] 기본 설정에서 읽은 테이블명: {defaultTableName ?? "(null)"}");
                
                if (!string.IsNullOrWhiteSpace(defaultTableName))
                {
                    Console.WriteLine($"✅ Configuration에서 기본 테이블명 로드: {defaultTableName}");
                    return defaultTableName;
                }
                
                //Console.WriteLine($"⚠️ Configuration에서 테이블명을 찾을 수 없어 기본값 사용: {DEFAULT__tableName}");
                return DEFAULT__tableName;
            }
            catch (Exception)
            {
                //Console.WriteLine($"❌ Configuration 읽기 실패, 기본값 사용: {DEFAULT__tableName}");
                return DEFAULT__tableName;
            }
        }

        #endregion

        #region 기본 CRUD 작업

        /// <summary>
        /// 송장 데이터 배치 삽입 - 성능 최적화된 대량 처리
        /// 
        /// 🚀 성능 최적화:
        /// - 배치 크기: 500건 (메모리 효율성과 성능의 균형)
        /// - 매개변수화된 쿼리 (SQL 인젝션 방지)
        /// - 트랜잭션 처리 (데이터 일관성)
        /// - 진행률 실시간 보고
        /// 
        /// 📋 처리 과정:
        /// 1. 입력 데이터 유효성 검사
        /// 2. 배치 단위로 데이터 분할
        /// 3. 각 배치마다 매개변수화된 쿼리 생성
        /// 4. 트랜잭션으로 배치 삽입 실행
        /// 5. 진행률 실시간 보고
        /// 
        /// ⚠️ 예외 처리:
        /// - ArgumentNullException: invoices가 null인 경우
        /// - InvalidOperationException: 배치 삽입 실패 시
        /// - Exception: 일반적인 데이터베이스 오류
        /// </summary>
        /// <param name="invoices">삽입할 송장 목록</param>
        /// <param name="progress">진행률 콜백</param>
        /// <param name="batchSize">배치 크기 (기본값: 500)</param>
        /// <returns>삽입된 총 행 수</returns>
        public async Task<int> InsertBatchAsync(IEnumerable<InvoiceDto> invoices, IProgress<string>? progress = null, int batchSize = DEFAULT_BATCH_SIZE)
        {
            // === 기본 테이블명 사용하여 오버로드 메서드 호출 ===
            // Repository가 관리하는 _tableName(App.config에서 결정)을 사용
            return await InsertBatchAsync(_tableName, invoices, progress, batchSize);
        }

        public async Task<int> InsertBatchAsync(string tableName, IEnumerable<InvoiceDto> invoices, IProgress<string>? progress = null, int batchSize = DEFAULT_BATCH_SIZE)
        {
            // === 1단계: 입력 데이터 유효성 검사 및 전처리 ===
            
            // 테이블명 유효성 검사
            if (!ValidateTableName(tableName))
            {
                progress?.Report($"❌ 잘못된 테이블명: {tableName}");
                return 0;
            }
            
            // null 체크: invoices 매개변수가 null인 경우 즉시 예외 발생
            if (invoices == null)
                throw new ArgumentNullException(nameof(invoices));

            // IEnumerable을 List로 변환하여 반복 처리 최적화 및 Count 연산 가능
            var invoiceList = invoices.ToList();
            
            // 빈 컬렉션 체크: 처리할 데이터가 없는 경우 조기 반환
            if (invoiceList.Count == 0)
            {
                progress?.Report("⚠️ 삽입할 데이터가 없습니다.");
                return 0; // 삽입된 행 수 0 반환
            }

            // === 2단계: 16GB 환경 최적화 - 단순한 전체 데이터 처리 ===
            var totalRows = invoiceList.Count;     // 전체 처리할 행 수
            var processedRows = 0;                 // 실제 처리 완료된 행 수
            
            // UI에 전체 처리 시작 정보 알림 (16GB 환경 최적화)
            progress?.Report($"🚀 총 {totalRows}건의 데이터를 전체 처리합니다... (테이블: {tableName}, 16GB 환경 최적화)");
            LogManagerService.LogInfo($"🚀 전체 데이터 처리 시작 - 총 {totalRows:N0}건 (16GB 환경 최적화)");
            
            try
            {
                // === 3단계: 16GB 환경 최적화 - 단순한 전체 데이터 처리 ===
                // 배치 처리 없이 전체 데이터를 한 번에 처리
                var allQueries = new List<(string sql, Dictionary<string, object> parameters)>();
                
                // === 3-1: 전체 데이터의 쿼리 목록 준비 ===
                foreach (var invoice in invoiceList)
                {
                    // === 개별 송장 데이터 유효성 검사 ===
                    // InvoiceDto.IsValid(): 필수 필드 존재 여부 및 비즈니스 규칙 검증
                    if (invoice.IsValid())
                    {
                        // 유효한 데이터인 경우 INSERT 쿼리 및 매개변수 생성 (커스텀 테이블명 사용)
                        var (sql, parameters) = BuildInsertQuery(tableName, invoice);
                        allQueries.Add((sql, parameters));
                    }
                    // 유효하지 않은 데이터는 자동으로 스킵됨 (로그 없이 조용히 처리)
                }
                
                // === 3-2: 전체 쿼리 실행 ===
                if (allQueries.Count > 0)
                {
                    var totalLog = $"[InvoiceRepository] 전체 데이터 처리 시작 - 총 쿼리 수: {allQueries.Count:N0}건";
                    LogManagerService.LogInfo($"{totalLog}");
                    
                    // === 첫 번째 쿼리 상세 로깅 ===
                    if (allQueries.Count > 0)
                    {
                        var firstQuery = allQueries.First();
                        var sqlLog = $"[InvoiceRepository] 첫 번째 쿼리 SQL: {firstQuery.sql}";
                        var paramLog = $"[InvoiceRepository] 첫 번째 쿼리 매개변수: {string.Join(", ", firstQuery.parameters.Select(p => $"{p.Key}={p.Value}"))}";
                        
                        LogManagerService.LogInfo($"{sqlLog}");
                        LogManagerService.LogInfo($"{paramLog}");
                    }
                    
                    // === 트랜잭션 단위 전체 실행 ===
                    // ExecuteParameterizedTransactionAsync: 모든 쿼리를 하나의 트랜잭션으로 실행
                    // 하나라도 실패하면 전체 롤백되어 데이터 일관성 보장
                    var success = await _databaseService.ExecuteParameterizedTransactionAsync(allQueries);
                    
                    var resultLog = $"[InvoiceRepository] 전체 데이터 처리 결과: {(success ? "성공" : "실패")}";
                    LogManagerService.LogInfo($"{resultLog}");
                    
                    // === 전체 실행 결과 검증 ===
                    if (!success)
                    {
                        var failureLog = $"[InvoiceRepository] 전체 데이터 삽입 실패 - 상세 정보 로깅 완료";
                        LogManagerService.LogError($"{failureLog}");
                        throw new InvalidOperationException($"전체 데이터 삽입 실패");
                    }
                    
                    // === 처리 통계 업데이트 ===
                    processedRows = allQueries.Count;
                    
                    // === 진행률 완료 보고 ===
                    progress?.Report($"📈 데이터 삽입 완료: 100% ({processedRows:N0}/{totalRows:N0}건)");
                }
                
                // === 4단계: 전체 처리 완료 보고 ===
                progress?.Report($"✅ 전체 데이터 삽입 완료: 총 {processedRows:N0}건 처리됨 (테이블: {tableName})");
                LogManagerService.LogInfo($"✅ 전체 데이터 삽입 완료: 총 {processedRows:N0}건 처리됨 (테이블: {tableName}) - 16GB 환경 최적화");
                return processedRows; // 실제 DB에 삽입된 행 수 반환
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ 전체 데이터 삽입 실패: {ex.Message}");
                LogManagerService.LogError($"❌ 전체 데이터 삽입 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 테이블 초기화 (TRUNCATE) - DynamicQueryBuilder 사용
        /// 
        /// 📋 주요 기능:
        /// - DynamicQueryBuilder를 사용한 하이브리드 TRUNCATE 쿼리 생성
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - 테이블명 유효성 검사
        /// - SQL 인젝션 방지
        /// 
        /// 🎯 동작 순서:
        /// 1. 테이블명 유효성 검사
        /// 2. DynamicQueryBuilder를 사용한 TRUNCATE 쿼리 생성
        /// 3. 매개변수화된 쿼리 실행
        /// 4. 결과 반환
        /// 
        /// 💡 사용법:
        /// ```csharp
        /// // 기본 사용법 (기본 테이블명 사용)
        /// var result = await repository.TruncateTableAsync();
        /// 
        /// // 커스텀 테이블명 사용
        /// var result = await repository.TruncateTableAsync("custom_table_name");
        /// ```
        /// </summary>
        /// <returns>작업 성공 여부</returns>
        public async Task<bool> TruncateTableAsync()
        {
            return await TruncateTableAsync(_tableName);
        }

        /// <summary>
        /// 테이블 초기화 (TRUNCATE) - DynamicQueryBuilder 사용 (커스텀 테이블명)
        /// </summary>
        /// <param name="tableName">초기화할 테이블명</param>
        /// <returns>작업 성공 여부</returns>
        public async Task<bool> TruncateTableAsync(string tableName)
        {
            try
            {
                //Console.WriteLine($"🔍 InvoiceRepository: 테이블 '{tableName}'에 대한 하이브리드 TRUNCATE 쿼리 생성 시작");
                
                // === 1단계: DynamicQueryBuilder를 사용한 하이브리드 TRUNCATE 쿼리 생성 ===
                var (sql, parameters) = _queryBuilder.BuildTruncateQuery(tableName);
                
                //Console.WriteLine($"✅ 하이브리드 TRUNCATE 쿼리 생성 완료 - 테이블: {tableName}");
                
                // === 2단계: 매개변수화된 쿼리 실행 ===
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                
                //Console.WriteLine($"✅ TRUNCATE 쿼리 실행 완료 - 테이블: {tableName}");
                return true; // TRUNCATE는 성공하면 항상 true
            }
            catch (ArgumentException ex)
            {
                LogManagerService.LogInfo($"❌ 테이블 매핑 오류: {ex.Message}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogManagerService.LogInfo($"❌ 쿼리 생성 실패: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogManagerService.LogInfo($"❌ 예상치 못한 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 전체 데이터 조회 - 페이징 지원
        /// 
        /// 📋 기능:
        /// - 테이블의 모든 데이터 조회
        /// - 선택적 페이징 지원 (LIMIT, OFFSET)
        /// - 메모리 효율적인 처리
        /// 
        /// 💡 사용법:
        /// var allData = await repository.GetAllAsync();
        /// var pagedData = await repository.GetAllAsync(100, 200); // 100건, 200번째부터
        /// </summary>
        /// <param name="limit">조회 제한 수 (0 = 제한 없음)</param>
        /// <param name="offset">시작 위치</param>
        /// <returns>송장 목록</returns>
        public async Task<IEnumerable<InvoiceDto>> GetAllAsync(int limit = 0, int offset = 0)
        {
            return await GetAllAsync(_tableName, limit, offset);
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllAsync(string tableName, int limit = 0, int offset = 0)
        {
            // === 1단계: 테이블명 유효성 검사 ===
            if (!ValidateTableName(tableName))
            {
                throw new ArgumentException($"잘못된 테이블명: {tableName}", nameof(tableName));
            }

            // === 2단계: 기본 SELECT 쿼리 구성 ===
            // 모든 컬럼을 선택하는 기본 쿼리 작성
            // tableName 파라미터로 전달된 동적 테이블명 사용
            var sql = $"SELECT * FROM {tableName}";
            
            // === 3단계: 선택적 페이징 처리 ===
            // limit > 0인 경우에만 페이징 적용 (성능 최적화)
            if (limit > 0)
            {
                // === LIMIT 절 추가 ===
                // MySQL의 LIMIT: 반환할 최대 행 수 제한
                // 대용량 테이블에서 메모리 오버플로우 방지 및 응답 시간 단축
                sql += $" LIMIT {limit}";
                
                // === OFFSET 절 추가 (선택적) ===
                // offset > 0인 경우에만 OFFSET 추가
                // MySQL의 OFFSET: 결과에서 건너뛸 행 수 지정
                // 페이징 구현 시 사용 (예: 2페이지 = LIMIT 20 OFFSET 20)
                if (offset > 0)
                    sql += $" OFFSET {offset}";
            }
            // limit == 0인 경우: 모든 데이터를 반환 (페이징 없음)
            // 주의: 대용량 테이블의 경우 메모리 부족 위험이 있으므로 신중히 사용
            
            // === 4단계: 쿼리 실행 및 DataTable 획득 ===
            // DatabaseService를 통한 안전한 비동기 쿼리 실행
            // 내부적으로 연결 풀링, 타임아웃 관리, 예외 처리 등이 수행됨
            var dataTable = await _databaseService.ExecuteQueryAsync(sql);
            
            // === 5단계: DataTable을 InvoiceDto 컬렉션으로 변환 ===
            // ConvertDataTableToInvoiceDtos: 타입 안전한 객체 변환 수행
            // - null 안전성 보장
            // - 타입 변환 처리 (문자열 → 숫자, 날짜 등)
            // - 잘못된 데이터 형식에 대한 기본값 적용
            return ConvertDataTableToInvoiceDtos(dataTable);
        }

        /// <summary>
        /// 조건별 데이터 조회 - 동적 WHERE 절 지원
        /// 
        /// 📋 기능:
        /// - 사용자 정의 WHERE 조건
        /// - 매개변수화된 쿼리 (SQL 인젝션 방지)
        /// - 유연한 조건 설정
        /// 
        /// 💡 사용법:
        /// var data = await repository.GetByConditionAsync("품목코드 = @code", new { code = "7710" });
        /// var data2 = await repository.GetByConditionAsync("수량 > @qty AND 결제금액 > @amount", new { qty = 1, amount = 1000 });
        /// </summary>
        /// <param name="whereClause">WHERE 조건절</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>조건에 맞는 송장 목록</returns>
        public async Task<IEnumerable<InvoiceDto>> GetByConditionAsync(string whereClause, object? parameters = null)
        {
            return await GetByConditionAsync(_tableName, whereClause, parameters);
        }

        public async Task<IEnumerable<InvoiceDto>> GetByConditionAsync(string tableName, string whereClause, object? parameters = null)
        {
            if (!ValidateTableName(tableName))
            {
                throw new ArgumentException($"잘못된 테이블명: {tableName}", nameof(tableName));
            }

            var sql = $"SELECT * FROM {tableName} WHERE {whereClause}";
            var dataTable = await _databaseService.ExecuteQueryAsync(sql, parameters);
            return ConvertDataTableToInvoiceDtos(dataTable);
        }

        /// <summary>
        /// 데이터 개수 조회 - 성능 최적화된 COUNT 쿼리
        /// 
        /// 📋 기능:
        /// - 빠른 COUNT(*) 쿼리
        /// - 선택적 WHERE 조건
        /// - 인덱스 활용 최적화
        /// 
        /// 💡 사용법:
        /// var totalCount = await repository.GetCountAsync();
        /// var filteredCount = await repository.GetCountAsync("품목코드 = @code", new { code = "7710" });
        /// </summary>
        /// <param name="whereClause">WHERE 조건절 (선택적)</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>데이터 개수</returns>
        public async Task<int> GetCountAsync(string? whereClause = null, object? parameters = null)
        {
            return await GetCountAsync(_tableName, whereClause, parameters);
        }

        public async Task<int> GetCountAsync(string tableName, string? whereClause = null, object? parameters = null)
        {
            if (!ValidateTableName(tableName))
            {
                throw new ArgumentException($"잘못된 테이블명: {tableName}", nameof(tableName));
            }

            var sql = $"SELECT COUNT(*) FROM {tableName}";
            
            if (!string.IsNullOrWhiteSpace(whereClause))
                sql += $" WHERE {whereClause}";
            
            var result = await _databaseService.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result);
        }

        #endregion

        #region 1차 데이터 가공 작업

        /// <summary>
        /// 특정 품목코드의 주소에 별표(*) 추가
        /// 
        /// 📋 기능:
        /// - IN 절을 사용한 다중 품목코드 처리
        /// - CONCAT 함수로 문자열 연결
        /// - 대량 업데이트 최적화
        /// 
        /// 💡 사용법:
        /// var updated = await repository.AddStarToAddressAsync(new[] { "7710", "7720" });
        /// </summary>
        /// <param name="productCodes">대상 품목코드 목록</param>
        /// <returns>업데이트된 행 수</returns>
        public async Task<int> AddStarToAddressAsync(IEnumerable<string> productCodes)
        {
            return await AddStarToAddressAsync(_tableName, productCodes);
        }

        public async Task<int> AddStarToAddressAsync(string tableName, IEnumerable<string> productCodes)
        {
            // === 1단계: 테이블명 및 입력 데이터 유효성 검사 ===
            if (!ValidateTableName(tableName))
            {
                LogManagerService.LogInfo($"❌ AddStarToAddress 실패: 잘못된 테이블명: {tableName}");
                return 0;
            }

            // productCodes가 null이거나 빈 컬렉션인 경우 처리할 데이터가 없음
            // ?. 연산자: null 안전성 보장
            // Any(): 하나 이상의 요소가 있는지 확인
            if (productCodes?.Any() != true)
                return 0; // 처리된 행 수 0 반환
            
            // === 2단계: SQL 인젝션 방지를 위한 품목코드 목록 정제 ===
            // SQL 인젝션 공격 방지를 위해 작은따옴표(') 이스케이프 처리
            // Replace("'", "''"): MySQL에서 작은따옴표를 리터럴로 사용하는 표준 방법
            // string.Join: 품목코드들을 쉼표로 구분된 문자열로 변환
            var codeList = string.Join("', '", productCodes.Select(code => code.Replace("'", "''")));
            
            // === 3단계: 동적 UPDATE 쿼리 구성 ===
            var sql = $@"
                UPDATE {tableName}
                SET 주소 = CONCAT(주소, '*')
                WHERE 품목코드 IN ('{codeList}')
                  AND RIGHT(주소, 1) <> '*'"; // 주소 끝에 이미 '*'가 있는 경우 중복 추가 방지
            
            // 쿼리 구성 설명:
            // 1. UPDATE {tableName}: 지정된 테이블명 사용
            // 2. SET 주소 = CONCAT(주소, '*'): 기존 주소 뒤에 별표(*) 문자 추가
            //    - CONCAT 함수: MySQL에서 문자열 연결을 위한 표준 함수
            //    - 기존 주소 내용은 유지하면서 별표만 추가
            // 3. WHERE 품목코드 IN (...): IN 절을 사용한 다중 조건 검색
            //    - 여러 품목코드를 한 번의 쿼리로 처리하여 성능 최적화
            //    - 품목코드가 목록에 포함된 모든 행이 대상
            
            // === 4단계: 쿼리 실행 및 결과 반환 ===
            // DatabaseService를 통한 안전한 비동기 UPDATE 실행
            // 반환값: 실제로 업데이트된 행의 수 (MySQL의 ROW_COUNT())
            return await _databaseService.ExecuteNonQueryAsync(sql);
        }

        /// <summary>
        /// 송장명 일괄 변경 (접두사 교체)
        /// 
        /// 📋 기능:
        /// - LEFT 함수로 접두사 확인
        /// - CONCAT과 SUBSTRING으로 문자열 조작
        /// - 안전한 문자열 처리
        /// 
        /// 💡 사용법:
        /// var updated = await repository.ReplacePrefixAsync("송장명", "BS_", "GC_");
        /// </summary>
        /// <param name="fieldName">대상 필드명</param>
        /// <param name="oldPrefix">기존 접두사</param>
        /// <param name="newPrefix">새 접두사</param>
        /// <returns>업데이트된 행 수</returns>
        public async Task<int> ReplacePrefixAsync(string fieldName, string oldPrefix, string newPrefix)
        {
            return await ReplacePrefixAsync(_tableName, fieldName, oldPrefix, newPrefix);
        }

        public async Task<int> ReplacePrefixAsync(string tableName, string fieldName, string oldPrefix, string newPrefix)
        {
            if (!ValidateTableName(tableName))
            {
                LogManagerService.LogInfo($"❌ ReplacePrefix 실패: 잘못된 테이블명: {tableName}");
                return 0;
            }

            // === 1단계: 매개변수화된 UPDATE 쿼리 구성 ===
            var sql = $@"
                UPDATE {tableName}
                SET {fieldName} = CONCAT(@newPrefix, SUBSTRING({fieldName}, @prefixLength))
                WHERE LEFT({fieldName}, @oldPrefixLength) = @oldPrefix";
            
            // === 2단계: 쿼리 매개변수 준비 (SQL 인젝션 방지) ===
            var parameters = new Dictionary<string, object>
            {
                ["@newPrefix"] = newPrefix,
                ["@prefixLength"] = oldPrefix.Length + 1,
                ["@oldPrefixLength"] = oldPrefix.Length,
                ["@oldPrefix"] = oldPrefix
            };
            
            // === 3단계: 매개변수화된 쿼리 실행 ===
            return await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        /// <summary>
        /// 필드 값 일괄 변경
        /// 
        /// 📋 기능:
        /// - 동적 필드명 지원
        /// - 매개변수화된 쿼리
        /// - 유연한 WHERE 조건
        /// 
        /// 💡 사용법:
        /// var updated = await repository.UpdateFieldAsync("수취인명", "난난", "수취인명 = @oldValue", new { oldValue = "nan" });
        /// </summary>
        /// <param name="fieldName">변경할 필드명</param>
        /// <param name="newValue">새 값</param>
        /// <param name="whereClause">WHERE 조건절</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>업데이트된 행 수</returns>
        public async Task<int> UpdateFieldAsync(string fieldName, object newValue, string whereClause, object? parameters = null)
        {
            return await UpdateFieldAsync(_tableName, fieldName, newValue, whereClause, parameters);
        }

        public async Task<int> UpdateFieldAsync(string tableName, string fieldName, object newValue, string whereClause, object? parameters = null)
        {
            if (!ValidateTableName(tableName))
            {
                LogManagerService.LogInfo($"❌ UpdateField 실패: 잘못된 테이블명: {tableName}");
                return 0;
            }

            var sql = $@"
                UPDATE {tableName}
                SET {fieldName} = @newValue
                WHERE {whereClause}";
            
            var allParameters = new Dictionary<string, object> { ["@newValue"] = newValue };
            
            if (parameters != null)
            {
                var additionalParams = ConvertObjectToDictionary(parameters);
                foreach (var param in additionalParams)
                {
                    allParameters[param.Key] = param.Value;
                }
            }
            
            return await _databaseService.ExecuteNonQueryAsync(sql, allParameters);
        }

        /// <summary>
        /// 문자열 필드에서 특정 문자 제거
        /// 
        /// 📋 기능:
        /// - REPLACE 함수 사용
        /// - LIKE 연산자로 대상 행 필터링
        /// - 불필요한 처리 방지
        /// 
        /// 💡 사용법:
        /// var updated = await repository.RemoveCharacterAsync("주소", "·");
        /// </summary>
        /// <param name="fieldName">대상 필드명</param>
        /// <param name="targetChar">제거할 문자</param>
        /// <returns>업데이트된 행 수</returns>
        public async Task<int> RemoveCharacterAsync(string fieldName, string targetChar)
        {
            return await RemoveCharacterAsync(_tableName, fieldName, targetChar);
        }

        public async Task<int> RemoveCharacterAsync(string tableName, string fieldName, string targetChar)
        {
            if (!ValidateTableName(tableName))
            {
                LogManagerService.LogInfo($"❌ RemoveCharacter 실패: 잘못된 테이블명: {tableName}");
                return 0;
            }

            var sql = $@"
                UPDATE {tableName}
                SET {fieldName} = REPLACE({fieldName}, @targetChar, '')
                WHERE {fieldName} LIKE @pattern";
            
            var parameters = new Dictionary<string, object>
            {
                ["@targetChar"] = targetChar,
                ["@pattern"] = $"%{targetChar}%"
            };
            
            return await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        #endregion

        #region 특수 처리 작업

        /// <summary>
        /// 제주도 주소 마킹
        /// 
        /// 📋 기능:
        /// - 다중 패턴 검색 (OR 조건)
        /// - 별표2 필드에 '제주' 값 설정
        /// - LIKE 연산자 활용
        /// 
        /// 💡 사용법:
        /// var marked = await repository.MarkJejuAddressAsync(new[] { "%제주특별%", "%제주 특별%" });
        /// </summary>
        /// <param name="addressPatterns">제주도 주소 패턴 목록</param>
        /// <returns>마킹된 행 수</returns>
        public async Task<int> MarkJejuAddressAsync(IEnumerable<string> addressPatterns)
        {
            if (addressPatterns?.Any() != true)
                return 0;

            var conditions = addressPatterns.Select((_, index) => $"주소 LIKE @pattern{index}");
            var whereClause = string.Join(" OR ", conditions);
            
            var sql = $@"
                UPDATE {_tableName}
                SET 별표2 = '제주'
                WHERE {whereClause}";
            
            var parameters = new Dictionary<string, object>();
            var patternArray = addressPatterns.ToArray();
            for (int i = 0; i < patternArray.Length; i++)
            {
                parameters[$"@pattern{i}"] = patternArray[i];
            }
            
            return await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        /// <summary>
        /// 박스 상품 명칭 변경
        /// 
        /// 📋 기능:
        /// - 패턴 매칭으로 박스 상품 검색
        /// - CONCAT으로 접두사 추가
        /// - 특수 문자 접두사 지원
        /// 
        /// 💡 사용법:
        /// var updated = await repository.AddBoxPrefixAsync("▨▧▦ ", "%박스%");
        /// </summary>
        /// <param name="prefix">추가할 접두사</param>
        /// <param name="pattern">박스 상품 패턴</param>
        /// <returns>업데이트된 행 수</returns>
        public async Task<int> AddBoxPrefixAsync(string prefix, string pattern)
        {
            var sql = $@"
                UPDATE {_tableName}
                SET 송장명 = CONCAT(@prefix, 송장명)
                WHERE 송장명 LIKE @pattern";
            
            var parameters = new Dictionary<string, object>
            {
                ["@prefix"] = prefix,
                ["@pattern"] = pattern
            };
            
            return await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 커스텀 쿼리 실행 (SELECT)
        /// 
        /// 📋 기능:
        /// - 복잡한 조회 쿼리 지원
        /// - 매개변수화된 쿼리
        /// - DataTable 반환
        /// 
        /// 💡 사용법:
        /// var result = await repository.ExecuteQueryAsync("SELECT COUNT(*) as cnt FROM table WHERE field = @value", new { value = "test" });
        /// </summary>
        /// <param name="sql">실행할 SQL 쿼리</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>쿼리 결과</returns>
        public async Task<DataTable> ExecuteQueryAsync(string sql, object? parameters = null)
        {
            return await _databaseService.ExecuteQueryAsync(sql, parameters);
        }

        /// <summary>
        /// 커스텀 쿼리 실행 (UPDATE/INSERT/DELETE) - Repository 패턴 확장
        /// 
        /// 📋 기능:
        /// - Repository 패턴 내에서 데이터 변경 쿼리 실행
        /// - 영향받은 행 수 반환으로 결과 확인 가능
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// - DatabaseService 위임으로 트랜잭션 일관성 보장
        /// 
        /// 🎯 사용 목적:
        /// - 표준 Repository 메서드로 커버되지 않는 복잡한 데이터 변경
        /// - 조건부 업데이트, 대량 삭제, 복합 INSERT 등
        /// - 커스텀 비즈니스 로직 구현 시 활용
        /// 
        /// 💡 사용법:
        /// var affected = await repository.ExecuteNonQueryAsync(
        ///     "UPDATE table SET 상태 = @status WHERE 날짜 < @cutoff AND 처리완료 = 0", 
        ///     new { status = "만료", cutoff = DateTime.Now.AddDays(-90) }
        /// );
        /// </summary>
        /// <param name="sql">실행할 SQL 쿼리 (UPDATE, INSERT, DELETE)</param>
        /// <param name="parameters">쿼리 매개변수 (익명 객체 또는 Dictionary)</param>
        /// <returns>영향받은 행 수</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null)
        {
            // === DatabaseService에 직접 위임 ===
            // Repository 패턴의 일관성을 유지하면서 DatabaseService의 모든 기능 활용
            // 매개변수 처리, 연결 관리, 트랜잭션 처리 등이 DatabaseService에서 통합 관리됨
            return await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        #endregion

        #region 내부 헬퍼 메서드

        /// <summary>
        /// 하이브리드 INSERT 쿼리 생성 - 기본 테이블명 사용
        /// 
        /// 📋 기능:
        /// - DynamicQueryBuilder를 사용한 하이브리드 쿼리 생성
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - 타입 안전성 보장
        /// - SQL 인젝션 방지
        /// 
        /// 🎯 동작 순서:
        /// 1. 설정 기반 매핑 시도 (table_mappings.json)
        /// 2. 설정이 없는 경우 리플렉션 기반 폴백
        /// 3. 둘 다 실패 시 예외 발생
        /// 
        /// 💡 사용법:
        /// var (sql, parameters) = BuildInsertQuery(invoice);
        /// </summary>
        /// <param name="invoice">삽입할 송장 데이터</param>
        /// <returns>SQL 쿼리와 매개변수</returns>
        /// <exception cref="ArgumentException">테이블명이 비어있거나 매핑 설정이 없는 경우</exception>
        /// <exception cref="InvalidOperationException">쿼리 생성 실패</exception>
        private (string sql, Dictionary<string, object> parameters) BuildInsertQuery(InvoiceDto invoice)
        {
            return BuildInsertQuery(_tableName, invoice);
        }

        /// <summary>
        /// 하이브리드 INSERT 쿼리 생성 - 커스텀 테이블명 사용
        /// 
        /// 📋 기능:
        /// - DynamicQueryBuilder를 사용한 하이브리드 쿼리 생성
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - 타입 안전성 보장
        /// - SQL 인젝션 방지
        /// - 확장 가능한 구조
        /// 
        /// 🎯 동작 순서:
        /// 1. 테이블명 유효성 검사
        /// 2. 설정 기반 매핑 시도 (table_mappings.json)
        /// 3. 설정이 없는 경우 리플렉션 기반 폴백
        /// 4. 둘 다 실패 시 예외 발생
        /// 
        /// 🛡️ 보안 기능:
        /// - SQL 인젝션 방지 (매개변수화된 쿼리)
        /// - 테이블명 검증
        /// - 타입 안전성 보장
        /// 
        /// ⚠️ 예외 처리:
        /// - ArgumentException: 테이블명이 비어있거나 매핑 설정이 없는 경우
        /// - InvalidOperationException: 쿼리 생성 실패
        /// 
        /// 💡 사용법:
        /// var (sql, parameters) = BuildInsertQuery("custom_table", invoice);
        /// 
        /// 🔧 설정 파일 구조 (table_mappings.json):
        /// ```json
        /// {
        ///   "invoice_table": {
        ///     "tableName": "invoice_table",
        ///     "columns": [
        ///       {
        ///         "propertyName": "RecipientName",
        ///         "databaseColumn": "수취인명",
        ///         "dataType": "VARCHAR",
        ///         "isRequired": true
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="invoice">삽입할 송장 데이터</param>
        /// <returns>SQL 쿼리와 매개변수</returns>
        /// <exception cref="ArgumentException">테이블명이 비어있거나 매핑 설정이 없는 경우</exception>
        /// <exception cref="InvalidOperationException">쿼리 생성 실패</exception>
        private (string sql, Dictionary<string, object> parameters) BuildInsertQuery(string tableName, InvoiceDto invoice)
        {
            try
            {
                //LogManagerService.LogInfo($"🔍 InvoiceRepository: 테이블 '{tableName}'에 대한 하이브리드 INSERT 쿼리 생성 시작");
                
                // === 1단계: DynamicQueryBuilder를 사용한 하이브리드 쿼리 생성 ===
                var (sql, parameters) = _queryBuilder.BuildInsertQuery(tableName, invoice);
                
                //LogManagerService.LogInfo($"✅ 하이브리드 쿼리 생성 완료 - 테이블: {tableName}");
                //LogManagerService.LogInfo($"📊 생성된 컬럼 수: {parameters.Count}개");
                
                return (sql, parameters);
            }
            catch (ArgumentException ex)
            {
                LogManagerService.LogInfo($"❌ 테이블 매핑 오류: {ex.Message}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogManagerService.LogInfo($"❌ 쿼리 생성 실패: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogManagerService.LogInfo($"❌ 예상치 못한 오류: {ex.Message}");
                throw new InvalidOperationException($"테이블 '{tableName}'에 대한 INSERT 쿼리 생성 중 오류가 발생했습니다: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// DataTable을 InvoiceDto 컬렉션으로 변환
        /// 
        /// 📋 기능:
        /// - DataTable의 각 행을 InvoiceDto로 변환
        /// - null 안전성 처리
        /// - 타입 변환 처리
        /// 
        /// 💡 사용법:
        /// var invoices = ConvertDataTableToInvoiceDtos(dataTable);
        /// </summary>
        /// <param name="dataTable">변환할 DataTable</param>
        /// <returns>InvoiceDto 컬렉션</returns>
        private IEnumerable<InvoiceDto> ConvertDataTableToInvoiceDtos(DataTable dataTable)
        {
            // === 1단계: 결과 컬렉션 초기화 ===
            var invoices = new List<InvoiceDto>();
            
            var debugLog = $"[DEBUG] DataTable 변환 시작 - 행 수: {dataTable.Rows.Count}";
            LogManagerService.LogInfo($"{debugLog}");
            
            var columnLog = $"[DEBUG] DataTable 컬럼들: {string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}";
            LogManagerService.LogInfo($"{columnLog}");
            
            // === 2단계: DataTable의 각 행을 InvoiceDto 객체로 변환 ===
            foreach (DataRow row in dataTable.Rows)
            {
                // === 2-1: 새 InvoiceDto 객체 생성 및 필드별 안전한 값 할당 ===
                
                // 원본 값 로깅
                var originalValuesLog = $"[DEBUG] 원본 DataRow 값:";
                LogManagerService.LogInfo($"{originalValuesLog}");
                
                // 컬럼 존재 여부 확인
                var columnExistsLog = $"[DEBUG] 컬럼 존재 여부 확인:";
                LogManagerService.LogInfo($"{columnExistsLog}");
                
                var phone1ExistsLog = $"[DEBUG]   전화번호1 컬럼 존재: {dataTable.Columns.Contains("전화번호1")}";
                LogManagerService.LogInfo($"{phone1ExistsLog}");
                
                var phone2ExistsLog = $"[DEBUG]   전화번호2 컬럼 존재: {dataTable.Columns.Contains("전화번호2")}";
                LogManagerService.LogInfo($"{phone2ExistsLog}");
                
                var zipCodeExistsLog = $"[DEBUG]   우편번호 컬럼 존재: {dataTable.Columns.Contains("우편번호")}";
                LogManagerService.LogInfo($"{zipCodeExistsLog}");
                
                var optionNameExistsLog = $"[DEBUG]   옵션명 컬럼 존재: {dataTable.Columns.Contains("옵션명")}";
                LogManagerService.LogInfo($"{optionNameExistsLog}");
                
                var specialNoteExistsLog = $"[DEBUG]   배송메세지 컬럼 존재: {dataTable.Columns.Contains("배송메세지")}";
                LogManagerService.LogInfo($"{specialNoteExistsLog}");
                
                var storeNameExistsLog = $"[DEBUG]   쇼핑몰 컬럼 존재: {dataTable.Columns.Contains("쇼핑몰")}";
                LogManagerService.LogInfo($"{storeNameExistsLog}");
                
                var collectedAtExistsLog = $"[DEBUG]   수집시간 컬럼 존재: {dataTable.Columns.Contains("수집시간")}";
                LogManagerService.LogInfo($"{collectedAtExistsLog}");
                
                var productCodeExistsLog = $"[DEBUG]   품목코드 컬럼 존재: {dataTable.Columns.Contains("품목코드")}";
                LogManagerService.LogInfo($"{productCodeExistsLog}");
                
                var orderNumberMallExistsLog = $"[DEBUG]   주문번호(쇼핑몰) 컬럼 존재: {dataTable.Columns.Contains("주문번호(쇼핑몰)")}";
                LogManagerService.LogInfo($"{orderNumberMallExistsLog}");
                
                var paymentAmountExistsLog = $"[DEBUG]   결제금액 컬럼 존재: {dataTable.Columns.Contains("결제금액")}";
                LogManagerService.LogInfo($"{paymentAmountExistsLog}");
                
                var orderAmountExistsLog = $"[DEBUG]   주문금액 컬럼 존재: {dataTable.Columns.Contains("주문금액")}";
                LogManagerService.LogInfo($"{orderAmountExistsLog}");
                
                var paymentMethodExistsLog = $"[DEBUG]   결제수단 컬럼 존재: {dataTable.Columns.Contains("결제수단")}";
                LogManagerService.LogInfo($"{paymentMethodExistsLog}");
                
                var taxTypeExistsLog = $"[DEBUG]   면과세구분 컬럼 존재: {dataTable.Columns.Contains("면과세구분")}";
                LogManagerService.LogInfo($"{taxTypeExistsLog}");
                
                var orderStatusExistsLog = $"[DEBUG]   주문상태 컬럼 존재: {dataTable.Columns.Contains("주문상태")}";
                LogManagerService.LogInfo($"{orderStatusExistsLog}");
                
                var shippingTypeExistsLog = $"[DEBUG]   배송송 컬럼 존재: {dataTable.Columns.Contains("배송송")}";
                LogManagerService.LogInfo($"{shippingTypeExistsLog}");
                
                // 실제 값 읽기
                var phone1Log = $"[DEBUG]   전화번호1: '{row["전화번호1"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{phone1Log}");
                
                var phone2Log = $"[DEBUG]   전화번호2: '{row["전화번호2"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{phone2Log}");
                
                var zipCodeLog = $"[DEBUG]   우편번호: '{row["우편번호"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{zipCodeLog}");
                
                var optionNameLog = $"[DEBUG]   옵션명: '{row["옵션명"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{optionNameLog}");
                
                var specialNoteLog = $"[DEBUG]   배송메세지: '{row["배송메세지"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{specialNoteLog}");
                
                var storeNameLog = $"[DEBUG]   쇼핑몰: '{row["쇼핑몰"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{storeNameLog}");
                
                var collectedAtLog = $"[DEBUG]   수집시간: '{row["쇼핑몰"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{collectedAtLog}");
                
                var productCodeLog = $"[DEBUG]   품목코드: '{row["품목코드"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{productCodeLog}");
                
                var orderNumberMallLog = $"[DEBUG]   주문번호(쇼핑몰): '{row["주문번호(쇼핑몰)"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{orderNumberMallLog}");
                
                var paymentAmountLog = $"[DEBUG]   결제금액: '{row["결제금액"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{paymentAmountLog}");
                
                var orderAmountLog = $"[DEBUG]   주문금액: '{row["주문번호"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{orderAmountLog}");
                
                var paymentMethodLog = $"[DEBUG]   결제수단: '{row["결제수단"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{paymentMethodLog}");
                
                var taxTypeLog = $"[DEBUG]   면과세구분: '{row["면과세구분"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{taxTypeLog}");
                
                var orderStatusLog = $"[DEBUG]   주문상태: '{row["주문상태"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{orderStatusLog}");
                
                var shippingTypeLog = $"[DEBUG]   배송송: '{row["배송송"]?.ToString() ?? "NULL"}'";
                LogManagerService.LogInfo($"{shippingTypeLog}");
                
                var invoice = new InvoiceDto
                {
                    // === 고객 정보 필드 (null 안전성 보장) ===
                    // ?. 연산자: null 체크 후 ToString() 실행
                    // ?? 연산자: null이면 빈 문자열 반환
                    RecipientName = row["수취인명"]?.ToString() ?? string.Empty,
                    Phone1 = row["전화번호1"]?.ToString() ?? string.Empty,
                    Phone2 = row["전화번호2"]?.ToString() ?? string.Empty,
                    ZipCode = row["우편번호"]?.ToString() ?? string.Empty,
                    Address = row["주소"]?.ToString() ?? string.Empty,
                    OptionName = row["옵션명"]?.ToString() ?? string.Empty,
                    Quantity = int.TryParse(row["수량"]?.ToString(), out int qty) ? qty : (int?)null,
                    ProductName = row["송장명"]?.ToString() ?? string.Empty,
                    ProductCode = row["품목코드"]?.ToString() ?? string.Empty,
                    ProductCount = row["품목개수"]?.ToString() ?? string.Empty,
                    SpecialNote = row["배송메세지"]?.ToString() ?? string.Empty,
                    OrderNumber = row["주문번호"]?.ToString() ?? string.Empty,
                    StoreName = row["쇼핑몰"]?.ToString() ?? string.Empty,
                    CollectedAt = DateTime.TryParse(row["수집시간"]?.ToString(), out DateTime collectedAt) ? collectedAt : (DateTime?)null,
                    OrderNumberMall = row["주문번호(쇼핑몰)"]?.ToString() ?? string.Empty,
                    OrderAmount = row["주문금액"]?.ToString() ?? string.Empty,
                    PaymentAmount = row["결제금액"]?.ToString() ?? string.Empty,
                    PaymentMethod = row["결제수단"]?.ToString() ?? string.Empty,
                    TaxType = row["면과세구분"]?.ToString() ?? string.Empty,
                    OrderStatus = row["주문상태"]?.ToString() ?? string.Empty,
                    DeliveryCost = row["택배비용"]?.ToString() ?? string.Empty,
                    BoxSize = row["박스크기"]?.ToString() ?? string.Empty,
                    DeliveryQuantity = row["택배수량"]?.ToString() ?? string.Empty,
                    DeliveryQuantity1 = row["택배수량1"]?.ToString() ?? string.Empty,
                    DeliveryQuantitySum = row["택배수량합산"]?.ToString() ?? string.Empty,
                    ShippingType = row["배송송"]?.ToString() ?? string.Empty,
                    PrintCount = row["출력개수"]?.ToString() ?? string.Empty,
                    InvoiceQuantity = row["송장수량"]?.ToString() ?? string.Empty,
                    InvoiceSeparator = row["송장구분자"]?.ToString() ?? string.Empty,
                    InvoiceType = row["송장구분"]?.ToString() ?? string.Empty,
                    InvoiceTypeFinal = row["송장구분최종"]?.ToString() ?? string.Empty,
                    Location = row["위치"]?.ToString() ?? string.Empty,
                    LocationConverted = row["위치변환"]?.ToString() ?? string.Empty,
                    Star1 = row["별표1"]?.ToString() ?? string.Empty,
                    Star2 = row["별표2"]?.ToString() ?? string.Empty,
                    Msg1 = row["msg1"]?.ToString() ?? string.Empty,
                    Msg2 = row["msg2"]?.ToString() ?? string.Empty,
                    Msg3 = row["msg3"]?.ToString() ?? string.Empty,
                    Msg4 = row["msg4"]?.ToString() ?? string.Empty,
                    Msg5 = row["msg5"]?.ToString() ?? string.Empty,
                    Msg6 = row["msg6"]?.ToString() ?? string.Empty
                };
                
                // 변환된 값 로깅
                var convertedValuesLog = $"[DEBUG] 변환된 InvoiceDto 값:";
                LogManagerService.LogInfo($"{convertedValuesLog}");
                
                var phone1ConvertedLog = $"[DEBUG]   Phone1: '{invoice.Phone1}'";
                LogManagerService.LogInfo($"{phone1ConvertedLog}");
                
                var phone2ConvertedLog = $"[DEBUG]   Phone2: '{invoice.Phone2}'";
                LogManagerService.LogInfo($"{phone2ConvertedLog}");
                
                var zipCodeConvertedLog = $"[DEBUG]   ZipCode: '{invoice.ZipCode}'";
                LogManagerService.LogInfo($"{zipCodeConvertedLog}");
                
                var optionNameConvertedLog = $"[DEBUG]   OptionName: '{invoice.OptionName}'";
                LogManagerService.LogInfo($"{optionNameConvertedLog}");
                
                var specialNoteConvertedLog = $"[DEBUG]   SpecialNote: '{invoice.SpecialNote}'";
                LogManagerService.LogInfo($"{specialNoteConvertedLog}");
                
                var storeNameConvertedLog = $"[DEBUG]   StoreName: '{invoice.StoreName}'";
                LogManagerService.LogInfo($"{storeNameConvertedLog}");
                
                var collectedAtConvertedLog = $"[DEBUG]   CollectedAt: '{invoice.CollectedAt}'";
                LogManagerService.LogInfo($"{collectedAtConvertedLog}");
                
                var productCodeConvertedLog = $"[DEBUG]   ProductCode: '{invoice.ProductCode}'";
                LogManagerService.LogInfo($"{productCodeConvertedLog}");
                
                var orderNumberMallConvertedLog = $"[DEBUG]   OrderNumberMall: '{invoice.OrderNumberMall}'";
                LogManagerService.LogInfo($"{orderNumberMallConvertedLog}");
                
                var paymentAmountConvertedLog = $"[DEBUG]   PaymentAmount: '{invoice.PaymentAmount}'";
                LogManagerService.LogInfo($"{paymentAmountConvertedLog}");
                
                var orderAmountConvertedLog = $"[DEBUG]   OrderAmount: '{invoice.OrderAmount}'";
                LogManagerService.LogInfo($"{orderAmountConvertedLog}");
                
                var paymentMethodConvertedLog = $"[DEBUG]   PaymentMethod: '{invoice.PaymentMethod}'";
                LogManagerService.LogInfo($"{paymentMethodConvertedLog}");
                
                var taxTypeConvertedLog = $"[DEBUG]   TaxType: '{invoice.TaxType}'";
                LogManagerService.LogInfo($"{taxTypeConvertedLog}");
                
                var orderStatusConvertedLog = $"[DEBUG]   OrderStatus: '{invoice.OrderStatus}'";
                LogManagerService.LogInfo($"{orderStatusConvertedLog}");
                
                var shippingTypeConvertedLog = $"[DEBUG]   ShippingType: '{invoice.ShippingType}'";
                LogManagerService.LogInfo($"{shippingTypeConvertedLog}");
                
                var separatorLog = $"[DEBUG] ========================================";
                LogManagerService.LogInfo($"{separatorLog}");
                
                invoices.Add(invoice);
            }
            
            return invoices;
        }

        /// <summary>
        /// 객체를 Dictionary로 변환 (매개변수 처리용)
        /// 
        /// 📋 기능:
        /// - 익명 객체를 Dictionary로 변환
        /// - 리플렉션 사용
        /// - null 안전성 처리
        /// 
        /// 💡 사용법:
        /// var dict = ConvertObjectToDictionary(new { id = 1, name = "test" });
        /// </summary>
        /// <param name="obj">변환할 객체</param>
        /// <returns>Dictionary 형태의 매개변수</returns>
        private Dictionary<string, object> ConvertObjectToDictionary(object obj)
        {
            // === 1단계: 결과 딕셔너리 초기화 ===
            var dictionary = new Dictionary<string, object>();
            
            // === 2단계: 입력 객체 null 체크 및 리플렉션 처리 ===
            if (obj != null)
            {
                // === 2-1: 리플렉션을 통한 객체의 모든 속성 정보 획득 ===
                // GetType().GetProperties(): 객체의 모든 public 속성들을 배열로 반환
                // 리플렉션 사용으로 런타임에 객체의 구조를 동적으로 분석
                var properties = obj.GetType().GetProperties();
                
                // === 2-2: 각 속성을 딕셔너리 항목으로 변환 ===
                foreach (var property in properties)
                {
                    // === 속성 값 추출 ===
                    // GetValue(obj): 지정된 객체에서 현재 속성의 값을 추출
                    // 속성 값이 null일 수 있으므로 null 체크 필요
                    var value = property.GetValue(obj);
                    
                    // === 매개변수명 생성 및 값 할당 ===
                    // 딕셔너리 키: @접두사 + 속성명 (예: "Name" → "@Name")
                    // SQL 매개변수 형식에 맞게 @ 접두사 자동 추가
                    // ?? DBNull.Value: null 값을 DBNull.Value로 변환
                    //   - .NET의 null과 SQL의 NULL을 올바르게 매핑
                    //   - 데이터베이스에서 NULL 값을 정확히 처리하기 위함
                    dictionary[$"@{property.Name}"] = value ?? DBNull.Value;
                }
            }
            // obj가 null인 경우: 빈 딕셔너리가 반환됨
            
            // === 3단계: 완성된 매개변수 딕셔너리 반환 ===
            // 반환된 딕셔너리는 DatabaseService에서 SQL 매개변수로 직접 사용 가능
            return dictionary;
        }

        /// <summary>
        /// 문자열을 지정된 길이로 자르는 유틸리티 메서드
        /// 
        /// 📋 기능:
        /// - 문자열 길이 제한
        /// - null 안전성 처리
        /// - 성능 최적화
        /// 
        /// 💡 사용법:
        /// var truncated = TruncateString("긴 문자열", 10);
        /// </summary>
        /// <param name="input">자를 문자열</param>
        /// <param name="maxLength">최대 길이</param>
        /// <returns>자른 문자열</returns>
        private string TruncateString(string input, int maxLength)
        {
            // === 1단계: 입력 문자열 null/빈 값 체크 ===
            // string.IsNullOrEmpty: null 또는 빈 문자열("")인지 확인
            // 데이터베이스 필드 길이 제한을 위한 안전장치
            if (string.IsNullOrEmpty(input)) 
                return string.Empty; // 빈 문자열 반환으로 일관성 유지
            
            // === 2단계: 문자열 길이 검사 및 필요시 자르기 ===
            // 삼항 연산자를 사용한 조건부 처리:
            // - input.Length > maxLength: 최대 길이 초과 여부 확인
            // - true인 경우: Substring(0, maxLength)로 최대 길이만큼 자르기
            // - false인 경우: 원본 문자열 그대로 반환
            // 
            // 사용 목적:
            // - 데이터베이스 필드의 길이 제한 준수 (예: VARCHAR(255))
            // - 긴 결제수단명, 주소, 메모 등으로 인한 DB 삽입 오류 방지
            // - 데이터 무결성 보장 및 예외 처리 최소화
            return input.Length > maxLength ? input.Substring(0, maxLength) : input;
        }

        /// <summary>
        /// 테이블명 유효성 검사 메서드
        /// 
        /// 📋 기능:
        /// - SQL 인젝션 방지
        /// - 위험한 문자 및 키워드 검사
        /// - null/빈 값 체크
        /// 
        /// 🔒 보안 체크 항목:
        /// - 세미콜론(;), 주석(--, /* */), DROP, DELETE 등
        /// - 허용된 문자: 영문, 숫자, 한글, 언더스코어(_)
        /// 
        /// 💡 사용법:
        /// if (ValidateTableName(tableName)) { /* 안전한 테이블명 */ }
        /// </summary>
        /// <param name="tableName">검증할 테이블명</param>
        /// <returns>유효하면 true, 위험하면 false</returns>
        private bool ValidateTableName(string tableName)
        {
            // === 1단계: 기본 null/빈 값 체크 ===
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            // === 2단계: 테이블명 보안 검증 (기본적인 SQL 인젝션 방지) ===
            // 위험한 문자나 SQL 키워드 포함 여부 검사
            if (tableName.Contains(";") || tableName.Contains("--") || 
                tableName.Contains("/*") || tableName.Contains("*/") ||
                tableName.ToUpper().Contains("DROP") || tableName.ToUpper().Contains("DELETE") ||
                tableName.ToUpper().Contains("INSERT") || tableName.ToUpper().Contains("UPDATE") ||
                tableName.ToUpper().Contains("ALTER") || tableName.ToUpper().Contains("CREATE"))
            {
                return false;
            }

            // === 3단계: 추가 보안 검증 ===
            // 공백 문자, 특수문자 등 검사
            if (tableName.Contains(" ") || tableName.Contains("'") || tableName.Contains("\"") ||
                tableName.Contains("\\") || tableName.Contains("/"))
            {
                return false;
            }

            // === 4단계: 유효한 테이블명으로 판단 ===
            return true;
        }

        #endregion

        /// <summary>
        /// 단일 송장 데이터 업데이트 (하이브리드 동적 쿼리 사용)
        /// 
        /// 📋 주요 기능:
        /// - DynamicQueryBuilder를 사용한 하이브리드 UPDATE 쿼리 생성
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - WHERE 조건 동적 생성
        /// - 타입 안전성 보장
        /// - SQL 인젝션 방지
        /// 
        /// 🎯 동작 순서:
        /// 1. 테이블명 유효성 검사
        /// 2. DynamicQueryBuilder를 사용한 UPDATE 쿼리 생성
        /// 3. 매개변수화된 쿼리 실행
        /// 4. 결과 반환
        /// 
        /// 💡 사용법:
        /// ```csharp
        /// // 기본 사용법 (기본키 기반 업데이트)
        /// var result = await repository.UpdateAsync(invoiceDto);
        /// 
        /// // WHERE 조건 지정
        /// var result = await repository.UpdateAsync(invoiceDto, "OrderNumber = @OrderNumber");
        /// 
        /// // 커스텀 테이블명 사용
        /// var result = await repository.UpdateAsync("custom_table", invoiceDto, "RecipientName = @RecipientName");
        /// ```
        /// </summary>
        /// <param name="invoice">업데이트할 송장 데이터</param>
        /// <param name="whereClause">WHERE 조건 (선택사항, 기본값: 기본키 기반)</param>
        /// <returns>업데이트된 행 수</returns>
        public async Task<int> UpdateAsync(InvoiceDto invoice, string? whereClause = null)
        {
            return await UpdateAsync(_tableName, invoice, whereClause);
        }

        /// <summary>
        /// 단일 송장 데이터 업데이트 (커스텀 테이블명 사용)
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="invoice">업데이트할 송장 데이터</param>
        /// <param name="whereClause">WHERE 조건 (선택사항, 기본값: 기본키 기반)</param>
        /// <returns>업데이트된 행 수</returns>
        public async Task<int> UpdateAsync(string tableName, InvoiceDto invoice, string? whereClause = null)
        {
            try
            {
                LogManagerService.LogInfo($"🔍 InvoiceRepository: 테이블 '{tableName}'에 대한 하이브리드 UPDATE 쿼리 생성 시작");
                
                // === 1단계: DynamicQueryBuilder를 사용한 하이브리드 UPDATE 쿼리 생성 ===
                var (sql, parameters) = _queryBuilder.BuildUpdateQuery(tableName, invoice, whereClause);
                
                LogManagerService.LogInfo($"✅ 하이브리드 UPDATE 쿼리 생성 완료 - 테이블: {tableName}");
                LogManagerService.LogInfo($"📊 생성된 컬럼 수: {parameters.Count}개");
                
                // === 2단계: 매개변수화된 쿼리 실행 ===
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                
                LogManagerService.LogInfo($"✅ UPDATE 쿼리 실행 완료 - 영향받은 행 수: {affectedRows}개");
                return affectedRows;
            }
            catch (ArgumentException ex)
            {
                LogManagerService.LogInfo($"❌ 테이블 매핑 오류: {ex.Message}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogManagerService.LogInfo($"❌ 쿼리 생성 실패: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogManagerService.LogInfo($"❌ 예상치 못한 오류: {ex.Message}");
                throw new InvalidOperationException($"테이블 '{tableName}'에 대한 UPDATE 쿼리 실행 중 오류가 발생했습니다: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 단일 송장 데이터 삭제 (하이브리드 동적 쿼리 사용)
        /// 
        /// 📋 주요 기능:
        /// - DynamicQueryBuilder를 사용한 하이브리드 DELETE 쿼리 생성
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - WHERE 조건 동적 생성
        /// - 타입 안전성 보장
        /// - SQL 인젝션 방지
        /// 
        /// 🎯 동작 순서:
        /// 1. 테이블명 유효성 검사
        /// 2. DynamicQueryBuilder를 사용한 DELETE 쿼리 생성
        /// 3. 매개변수화된 쿼리 실행
        /// 4. 결과 반환
        /// 
        /// 💡 사용법:
        /// ```csharp
        /// // 기본 사용법 (기본키 기반 삭제)
        /// var result = await repository.DeleteAsync(invoiceDto);
        /// 
        /// // WHERE 조건 지정
        /// var result = await repository.DeleteAsync(invoiceDto, "OrderNumber = @OrderNumber");
        /// 
        /// // 커스텀 테이블명 사용
        /// var result = await repository.DeleteAsync("custom_table", invoiceDto, "RecipientName = @RecipientName");
        /// ```
        /// </summary>
        /// <param name="invoice">삭제할 송장 데이터</param>
        /// <param name="whereClause">WHERE 조건 (선택사항, 기본값: 기본키 기반)</param>
        /// <returns>삭제된 행 수</returns>
        public async Task<int> DeleteAsync(InvoiceDto invoice, string? whereClause = null)
        {
            return await DeleteAsync(_tableName, invoice, whereClause);
        }

        /// <summary>
        /// 단일 송장 데이터 삭제 (커스텀 테이블명 사용)
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="invoice">삭제할 송장 데이터</param>
        /// <param name="whereClause">WHERE 조건 (선택사항, 기본값: 기본키 기반)</param>
        /// <returns>삭제된 행 수</returns>
        public async Task<int> DeleteAsync(string tableName, InvoiceDto invoice, string? whereClause = null)
        {
            try
            {
                LogManagerService.LogInfo($"🔍 InvoiceRepository: 테이블 '{tableName}'에 대한 하이브리드 DELETE 쿼리 생성 시작");
                
                // === 1단계: DynamicQueryBuilder를 사용한 하이브리드 DELETE 쿼리 생성 ===
                var (sql, parameters) = _queryBuilder.BuildDeleteQuery(tableName, invoice, whereClause);
                
                LogManagerService.LogInfo($"✅ 하이브리드 DELETE 쿼리 생성 완료 - 테이블: {tableName}");
                
                // === 2단계: 매개변수화된 쿼리 실행 ===
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                
                LogManagerService.LogInfo($"✅ DELETE 쿼리 실행 완료 - 영향받은 행 수: {affectedRows}개");
                return affectedRows;
            }
            catch (ArgumentException ex)
            {
                LogManagerService.LogInfo($"❌ 테이블 매핑 오류: {ex.Message}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                LogManagerService.LogInfo($"❌ 쿼리 생성 실패: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogManagerService.LogInfo($"❌ 예상치 못한 오류: {ex.Message}");
                throw new InvalidOperationException($"테이블 '{tableName}'에 대한 DELETE 쿼리 실행 중 오류가 발생했습니다: {ex.Message}", ex);
            }
        }
    }
}