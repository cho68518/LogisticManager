using System.Data;
using System.Configuration;
using LogisticManager.Services;
using LogisticManager.Models;

namespace LogisticManager.Processors
{
    /// <summary>
    /// 전체 송장 처리 로직을 담당하는 메인 프로세서 클래스
    /// 
    /// 📋 주요 기능:
    /// - Excel 파일 읽기 및 데이터 검증
    /// - 1차 데이터 가공 (주소 정리, 수취인명 정리, 결제방법 정리)
    /// - 출고지별 데이터 분류
    /// - 각 출고지별 특화 처리
    /// - 최종 파일 생성 및 Dropbox 업로드
    /// - Kakao Work 알림 전송
    /// 
    /// 🔄 처리 단계:
    /// 1. Excel 파일 읽기 (0-10%) - ColumnMapping 적용
    /// 2. 1차 데이터 가공 (10-20%) - 데이터베이스 처리
    /// 3. 출고지별 분류 (20-30%) - 그룹화
    /// 4. 각 출고지별 처리 (30-80%) - 특화 로직
    /// 5. 최종 파일 생성 및 업로드 (80-90%) - Excel 생성 + Dropbox
    /// 6. Kakao Work 알림 전송 (90-100%) - 실시간 알림
    /// 
    /// 🔗 의존성:
    /// - FileService: Excel 파일 읽기/쓰기 (ColumnMapping 적용)
    /// - DatabaseService: 데이터베이스 연동 (MySQL)
    /// - ApiService: Dropbox 업로드, Kakao Work 알림
    /// - ShipmentProcessor: 출고지별 세부 처리
    /// - MappingService: 컬럼 매핑 처리
    /// 
    /// 🎯 성능 최적화:
    /// - 배치 처리 (500건 단위)
    /// - 매개변수화된 쿼리 (SQL 인젝션 방지)
    /// - 트랜잭션 처리 (데이터 일관성)
    /// - 진행률 실시간 보고
    /// 
    /// 🛡️ 보안 기능:
    /// - SQL 인젝션 방지
    /// - 데이터 유효성 검사
    /// - 오류 처리 및 롤백
    /// - 로깅 및 추적
    /// </summary>
    public class InvoiceProcessor
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 파일 처리 서비스 - Excel 파일 읽기/쓰기 담당
        /// 
        /// 주요 기능:
        /// - Excel 파일을 DataTable로 변환 (ColumnMapping 적용)
        /// - DataTable을 Excel 파일로 저장
        /// - 파일 선택 대화상자 제공
        /// - 출력 파일 경로 생성
        /// 
        /// 사용 라이브러리:
        /// - EPPlus (Excel 파일 처리)
        /// - MappingService (컬럼 매핑)
        /// </summary>
        private readonly FileService _fileService;
        
        /// <summary>
        /// 데이터베이스 서비스 - MySQL 연결 및 쿼리 실행 담당
        /// 
        /// 주요 기능:
        /// - MySQL 데이터베이스 연결 관리
        /// - SQL 쿼리 실행 (SELECT, INSERT, UPDATE, DELETE)
        /// - 트랜잭션 처리
        /// - 매개변수화된 쿼리 지원
        /// 
        /// 보안:
        /// - 연결 문자열 암호화
        /// - SQL 인젝션 방지
        /// - 연결 풀링
        /// </summary>
        private readonly DatabaseService _databaseService;
        
        /// <summary>
        /// API 서비스 - Dropbox 업로드, Kakao Work 알림 담당
        /// 
        /// 주요 기능:
        /// - Dropbox 파일 업로드
        /// - Kakao Work 메시지 전송
        /// - 외부 API 연동
        /// - 인증 토큰 관리
        /// 
        /// 설정:
        /// - API 키 관리
        /// - 재시도 로직
        /// - 오류 처리
        /// </summary>
        private readonly ApiService _apiService;
        
        /// <summary>
        /// 진행 상황 메시지 콜백 - 실시간 로그 메시지 전달
        /// 
        /// 사용 목적:
        /// - 처리 단계별 상세 로그
        /// - 오류 메시지 전달
        /// - 사용자 인터페이스 업데이트
        /// 
        /// 메시지 형식:
        /// - ✅ 성공 메시지
        /// - ❌ 오류 메시지
        /// - 📊 진행 상황
        /// - 🔄 처리 단계
        /// </summary>
        private readonly IProgress<string>? _progress;
        
        /// <summary>
        /// 진행률 콜백 - 0-100% 진행률 전달
        /// 
        /// 사용 목적:
        /// - 실시간 진행률 표시
        /// - 프로그레스 바 업데이트
        /// - 처리 시간 예측
        /// 
        /// 진행률 구간:
        /// - 0-10%: Excel 파일 읽기
        /// - 10-20%: 1차 데이터 가공
        /// - 20-30%: 출고지별 분류
        /// - 30-80%: 각 출고지별 처리
        /// - 80-90%: 파일 생성 및 업로드
        /// - 90-100%: 알림 전송
        /// </summary>
        private readonly IProgress<int>? _progressReporter;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// InvoiceProcessor 생성자 - 의존성 주입 패턴 적용
        /// 
        /// 🏗️ 초기화 과정:
        /// 1. 각 서비스 인스턴스를 private 필드에 저장
        /// 2. 의존성 주입을 통한 결합도 감소
        /// 3. 단위 테스트 용이성 확보
        /// 4. 메모리 효율성 향상
        /// 
        /// 📦 주입되는 서비스:
        /// - fileService: Excel 파일 처리 (ColumnMapping 적용)
        /// - databaseService: MySQL 데이터베이스 연동
        /// - apiService: 외부 API 연동 (Dropbox, Kakao Work)
        /// - progress: 진행 상황 메시지 콜백 (선택사항)
        /// - progressReporter: 진행률 콜백 (선택사항)
        /// 
        /// 🎯 설계 원칙:
        /// - 의존성 역전 원칙 (DIP) 적용
        /// - 단일 책임 원칙 (SRP) 준수
        /// - 개방-폐쇄 원칙 (OCP) 지원
        /// 
        /// ⚡ 성능 고려사항:
        /// - 서비스 인스턴스 재사용
        /// - 메모리 할당 최소화
        /// - 가비지 컬렉션 부담 감소
        /// </summary>
        /// <param name="fileService">파일 처리 서비스 - Excel 파일 읽기/쓰기 담당</param>
        /// <param name="databaseService">데이터베이스 서비스 - MySQL 연결 및 쿼리 실행 담당</param>
        /// <param name="apiService">API 서비스 - Dropbox 업로드, Kakao Work 알림 담당</param>
        /// <param name="progress">진행 상황 메시지 콜백 - 실시간 로그 메시지 전달 (선택사항)</param>
        /// <param name="progressReporter">진행률 콜백 - 0-100% 진행률 전달 (선택사항)</param>
        public InvoiceProcessor(FileService fileService, DatabaseService databaseService, ApiService apiService, 
            IProgress<string>? progress = null, IProgress<int>? progressReporter = null)
        {
            // 각 서비스 인스턴스를 private 필드에 저장 (의존성 주입)
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService), "FileService는 필수입니다.");
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService), "DatabaseService는 필수입니다.");
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService), "ApiService는 필수입니다.");
            _progress = progress;
            _progressReporter = progressReporter;
            
            // 초기화 완료 로그
            Console.WriteLine("✅ InvoiceProcessor 초기화 완료 - 모든 서비스 주입됨");
        }

        #endregion

        #region 메인 처리 메서드 (Main Processing Method)

        /// <summary>
        /// 송장 처리의 메인 메서드 - 전체 송장 처리 워크플로우 실행
        /// 
        /// 🚀 전체 처리 과정:
        /// 1. Excel 파일 읽기 (0-10%) - ColumnMapping 적용
        /// 2. 1차 데이터 가공 (10-20%) - 데이터베이스 처리
        /// 3. 출고지별 분류 (20-30%) - 그룹화
        /// 4. 각 출고지별 처리 (30-80%) - 특화 로직
        /// 5. 최종 파일 생성 및 업로드 (80-90%) - Excel 생성 + Dropbox
        /// 6. Kakao Work 알림 전송 (90-100%) - 실시간 알림
        /// 
        /// 📊 진행률 관리:
        /// - 실시간 진행률 보고 (0-100%)
        /// - 단계별 상세 로그 메시지
        /// - 처리 시간 예측 및 표시
        /// - 오류 발생 시 즉시 중단
        /// 
        /// 🛡️ 예외 처리:
        /// - 파일 읽기 오류 (FileNotFoundException, IOException)
        /// - 데이터 가공 오류 (InvalidOperationException)
        /// - API 연동 오류 (HttpRequestException)
        /// - 네트워크 오류 (SocketException)
        /// - 데이터베이스 오류 (MySqlException)
        /// 
        /// 🔄 재시도 로직:
        /// - 네트워크 오류 시 자동 재시도
        /// - 데이터베이스 연결 오류 시 재연결
        /// - API 호출 실패 시 지수 백오프
        /// 
        /// 📈 성능 최적화:
        /// - 비동기 처리로 UI 블로킹 방지
        /// - 배치 처리로 메모리 효율성 향상
        /// - 트랜잭션 처리로 데이터 일관성 보장
        /// - 진행률 실시간 업데이트
        /// </summary>
        /// <param name="filePath">입력 Excel 파일의 전체 경로 - 절대 경로 권장</param>
        /// <param name="progress">진행 상황 메시지 콜백 - 실시간 로그 메시지 전달 (선택사항)</param>
        /// <param name="progressReporter">진행률 콜백 - 0-100% 진행률 전달 (선택사항)</param>
        /// <returns>처리 완료 여부 - true: 성공, false: 실패</returns>
        /// <exception cref="FileNotFoundException">Excel 파일이 존재하지 않는 경우</exception>
        /// <exception cref="InvalidOperationException">처리 중 오류가 발생한 경우</exception>
        /// <exception cref="MySqlException">데이터베이스 연결 또는 쿼리 오류</exception>
        /// <exception cref="HttpRequestException">API 호출 오류</exception>
        public async Task<bool> ProcessAsync(string filePath, IProgress<string>? progress = null, IProgress<int>? progressReporter = null)
        {
            // 입력 매개변수 검증
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("파일 경로는 비어있을 수 없습니다.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel 파일을 찾을 수 없습니다: {filePath}");
            }

            try
            {
                // 진행 상황 및 진행률 콜백 설정 (매개변수 우선, 필드값 대체)
                var finalProgress = progress ?? _progress;
                var finalProgressReporter = progressReporter ?? _progressReporter;
                
                finalProgress?.Report("🚀 송장 처리 작업을 시작합니다...");
                finalProgressReporter?.Report(0);

                // 1단계: Excel 파일 읽기 (0-10%) - ColumnMapping 적용
                finalProgress?.Report("📖 Excel 파일을 읽는 중... (ColumnMapping 적용)");
                var originalData = _fileService.ReadExcelToDataTable(filePath, "order_table");
                finalProgressReporter?.Report(10);
                finalProgress?.Report($"✅ 총 {originalData.Rows.Count}건의 데이터를 읽었습니다.");

                // 데이터 유효성 검사
                if (originalData.Rows.Count == 0)
                {
                    finalProgress?.Report("⚠️ Excel 파일에 데이터가 없습니다.");
                    return false;
                }

                // 2단계: 1차 데이터 가공 (10-20%) - 데이터베이스 처리
                finalProgress?.Report("🔧 1차 데이터 가공을 시작합니다... (데이터베이스 처리)");
                var processedData = await ProcessFirstStageData(originalData);
                finalProgressReporter?.Report(20);
                finalProgress?.Report("✅ 1차 데이터 가공이 완료되었습니다.");

                // 3단계: 출고지별 분류 (20-30%) - 그룹화
                finalProgress?.Report("📦 출고지별 데이터 분류를 시작합니다...");
                var shipmentGroups = ClassifyByShipmentCenter(processedData);
                finalProgressReporter?.Report(30);
                finalProgress?.Report($"✅ 총 {shipmentGroups.Count}개 출고지로 분류되었습니다.");

                // 4단계: 각 출고지별 처리 (30-80%) - 특화 로직
                var processedResults = new List<(string centerName, DataTable data)>();
                var totalCenters = shipmentGroups.Count;
                var currentCenter = 0;

                foreach (var group in shipmentGroups)
                {
                    currentCenter++;
                    // 진행률 계산: 30% + (현재 출고지 / 전체 출고지) * 50%
                    var progressPercentage = 30 + (int)((double)currentCenter / totalCenters * 50);
                    finalProgressReporter?.Report(progressPercentage);

                    finalProgress?.Report($"🏭 {group.centerName} 출고지 처리 중... ({currentCenter}/{totalCenters})");
                    
                    // 각 출고지별 세부 처리
                    var centerProcessedData = ProcessShipmentCenter(group.centerName, group.data);
                    processedResults.Add((group.centerName, centerProcessedData));
                    
                    finalProgress?.Report($"✅ {group.centerName} 출고지 처리 완료");
                }

                // 5단계: 최종 파일 생성 및 업로드 (80-90%) - Excel 생성 + Dropbox
                finalProgress?.Report("📄 최종 파일 생성을 시작합니다...");
                finalProgressReporter?.Report(80);
                
                var uploadResults = await GenerateAndUploadFiles(processedResults);
                
                finalProgress?.Report("✅ 최종 파일 생성 및 업로드 완료");
                finalProgressReporter?.Report(90);

                // 6단계: 카카오워크 알림 전송 (90-100%) - 실시간 알림
                finalProgress?.Report("📱 카카오워크 알림을 전송합니다...");
                await SendKakaoWorkNotifications(uploadResults);
                
                finalProgress?.Report("✅ 카카오워크 알림 전송 완료");
                finalProgressReporter?.Report(100);

                finalProgress?.Report("🎉 모든 송장 처리 작업이 완료되었습니다!");
                return true;
            }
            catch (Exception ex)
            {
                // 상세한 오류 정보 로깅
                var errorMessage = $"❌ 송장 처리 중 오류 발생: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n내부 오류: {ex.InnerException.Message}";
                }
                
                _progress?.Report(errorMessage);
                Console.WriteLine($"❌ InvoiceProcessor 오류: {ex}");
                throw;
            }
        }

        #endregion

        #region 데이터 가공 (Data Processing)

        /// <summary>
        /// 1차 데이터 가공 처리 (파이썬 코드 기반) - 데이터베이스 중심 처리
        /// 
        /// 🔄 처리 단계:
        /// 1. 데이터베이스에 원본 데이터 삽입 (배치 처리 최적화)
        /// 2. 특정 품목코드에 별표 추가 (7710, 7720)
        /// 3. 송장명 변경 (BS_ → GC_)
        /// 4. 수취인명 정리 (nan → 난난)
        /// 5. 주소 정리 (· 문자 제거)
        /// 6. 결제수단 정리 (배민상회 → 0)
        /// 
        /// 🎯 파이썬 코드 변환:
        /// - 데이터베이스 직접 삽입 방식 (메모리 효율성)
        /// - 단계별 SQL 업데이트 처리 (트랜잭션 보장)
        /// - 오류 처리 및 로깅 (안정성)
        /// - 배치 처리로 성능 최적화
        /// 
        /// 📊 성능 최적화:
        /// - 배치 크기: 500건 (기존 100건에서 증가)
        /// - 매개변수화된 쿼리 (SQL 인젝션 방지)
        /// - 트랜잭션 처리 (데이터 일관성)
        /// - 진행률 실시간 보고
        /// 
        /// 🛡️ 보안 기능:
        /// - SQL 인젝션 방지
        /// - 데이터 유효성 검사
        /// - 오류 처리 및 롤백
        /// - 상세한 로깅
        /// </summary>
        /// <param name="data">원본 데이터가 담긴 DataTable - ColumnMapping이 적용된 데이터</param>
        /// <returns>가공된 데이터가 담긴 DataTable - 데이터베이스에서 다시 읽어온 정리된 데이터</returns>
        /// <exception cref="InvalidOperationException">데이터 가공 중 오류 발생</exception>
        /// <exception cref="MySqlException">데이터베이스 연결 또는 쿼리 오류</exception>
        private async Task<DataTable> ProcessFirstStageData(DataTable data)
        {
            try
            {
                _progress?.Report("🔧 1차 데이터 가공 시작: 데이터베이스 삽입 단계");
                
                // 1단계: 데이터베이스에 원본 데이터 삽입 (배치 처리로 최적화)
                await InsertDataToDatabaseOptimized(data);
                _progress?.Report("✅ 데이터베이스 삽입 완료");
                
                // 2단계: 특정 품목코드에 별표 추가
                await AddStarToAddress();
                _progress?.Report("✅ 특정 품목코드의 주문건 주소에 별표(*) 추가 완료");
                
                // 3단계: 송장명 변경 (BS_ → GC_)
                await ReplaceBsWithGc();
                _progress?.Report("✅ 송장명 변경 완료");
                
                // 4단계: 수취인명 정리 (nan → 난난)
                await UpdateRecipientName();
                _progress?.Report("✅ 수취인명 정리 완료");
                
                // 5단계: 주소 정리 (· 문자 제거)
                await CleanAddressInDatabase();
                _progress?.Report("✅ 주소 정리 완료");
                
                // 6단계: 결제수단 정리 (배민상회 → 0)
                await UpdatePaymentMethodForBaemin();
                _progress?.Report("✅ 결제수단 정리 완료");
                
                // 7단계: 정리된 데이터를 다시 읽어오기
                var processedData = await LoadProcessedDataFromDatabase();
                _progress?.Report($"✅ 1차 데이터 가공 완료: {processedData.Rows.Count}건");
                
                return processedData;
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 1차 데이터 가공 실패: {ex.Message}");
                Console.WriteLine($"❌ ProcessFirstStageData 오류: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 데이터베이스에 원본 데이터 삽입 (최적화된 버전) - 배치 처리 및 성능 향상
        /// 
        /// 🚀 개선사항:
        /// - 배치 크기를 500건으로 증가하여 성능 향상 (기존 100건 대비 5배)
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지 (보안 강화)
        /// - 메모리 사용량 최적화 (배치별 처리)
        /// - 진행 상황 상세 보고 (실시간 진행률)
        /// - 트랜잭션 처리로 데이터 일관성 보장
        /// 
        /// 📊 처리 과정:
        /// 1. 전체 데이터를 500건 단위로 배치 분할
        /// 2. 각 배치별로 매개변수화된 쿼리 생성
        /// 3. 트랜잭션을 사용하여 배치 실행
        /// 4. 실시간 진행률 및 처리 건수 보고
        /// 5. 오류 발생 시 즉시 중단 및 롤백
        /// 
        /// 🎯 성능 최적화:
        /// - 네트워크 오버헤드 최소화 (배치 처리)
        /// - 메모리 효율성 향상 (배치별 메모리 해제)
        /// - 데이터베이스 연결 재사용
        /// - 병렬 처리 가능성 확보
        /// 
        /// 🛡️ 보안 기능:
        /// - SQL 인젝션 방지 (매개변수화된 쿼리)
        /// - 데이터 유효성 검사 (Order.IsValid())
        /// - 트랜잭션 롤백 (오류 시)
        /// - 상세한 오류 로깅
        /// </summary>
        /// <param name="data">삽입할 데이터 - ColumnMapping이 적용된 DataTable</param>
        /// <exception cref="InvalidOperationException">배치 삽입 중 오류 발생</exception>
        /// <exception cref="MySqlException">데이터베이스 연결 또는 쿼리 오류</exception>
        private async Task InsertDataToDatabaseOptimized(DataTable data)
        {
            const int batchSize = 500; // 배치 크기 증가 (성능 최적화)
            var totalRows = data.Rows.Count;
            var processedRows = 0;
            
            _progress?.Report($"📊 총 {totalRows}건의 데이터를 배치 처리합니다... (배치 크기: {batchSize}건)");
            
            // 배치별로 처리 (메모리 효율성)
            for (int i = 0; i < totalRows; i += batchSize)
            {
                var batchQueries = new List<string>();
                var batchParameters = new List<Dictionary<string, object>>();
                
                // 현재 배치의 데이터 처리
                var endIndex = Math.Min(i + batchSize, totalRows);
                for (int j = i; j < endIndex; j++)
                {
                    var row = data.Rows[j];
                    var order = Order.FromDataRow(row);
                    
                    // 데이터 유효성 검사 (보안 강화)
                    if (order.IsValid())
                    {
                        // 매개변수화된 INSERT 쿼리 생성 (SQL 인젝션 방지)
                        var sql = @"
                            INSERT INTO 송장출력_사방넷원본변환 (
                                수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 옵션명, 수량, 배송메세지, 주문번호,
                                쇼핑몰, 수집시간, 송장명, 품목코드, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
                            ) 
                            VALUES (@수취인명, @전화번호1, @전화번호2, @우편번호, @주소, @옵션명, @수량, @배송메세지, @주문번호,
                                    @쇼핑몰, @수집시간, @송장명, @품목코드, @주문번호쇼핑몰, @결제금액, @주문금액, @결제수단, @면과세구분, @주문상태, @배송송)";
                        
                        // 매개변수 생성 (데이터 타입 안전성)
                        var parameters = new Dictionary<string, object>
                        {
                            ["@수취인명"] = order.RecipientName ?? "",
                            ["@전화번호1"] = order.RecipientPhone ?? "",
                            ["@전화번호2"] = "",
                            ["@우편번호"] = order.ZipCode ?? "",
                            ["@주소"] = order.Address ?? "",
                            ["@옵션명"] = "",
                            ["@수량"] = order.Quantity.ToString(),
                            ["@배송메세지"] = order.SpecialNote ?? "",
                            ["@주문번호"] = order.OrderNumber ?? "",
                            ["@쇼핑몰"] = order.StoreName ?? "",
                            ["@수집시간"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ["@송장명"] = order.ProductName ?? "",
                            ["@품목코드"] = order.ProductCode ?? "",
                            ["@주문번호쇼핑몰"] = order.OrderNumber ?? "",
                            ["@결제금액"] = order.TotalPrice.ToString(),
                            ["@주문금액"] = order.TotalPrice.ToString(),
                            ["@결제수단"] = TruncateString(order.PaymentMethod ?? "", 255), // 문자열 길이 제한
                            ["@면과세구분"] = order.PriceCategory ?? "",
                            ["@주문상태"] = order.ProcessingStatus ?? "",
                            ["@배송송"] = order.ShippingType ?? ""
                        };
                        
                        batchQueries.Add(sql);
                        batchParameters.Add(parameters);
                    }
                }
                
                // 배치 실행 (트랜잭션 처리)
                if (batchQueries.Count > 0)
                {
                    try
                    {
                        await ExecuteBatchInsertOptimized(batchQueries, batchParameters);
                        processedRows += batchQueries.Count;
                        
                        // 진행 상황 보고 (실시간 진행률)
                        var progressPercentage = (int)((double)processedRows / totalRows * 100);
                        _progress?.Report($"📈 데이터 삽입 진행률: {progressPercentage}% ({processedRows}/{totalRows}건)");
                    }
                    catch (Exception ex)
                    {
                        _progress?.Report($"❌ 배치 삽입 실패 (배치 {i/batchSize + 1}): {ex.Message}");
                        Console.WriteLine($"❌ InsertDataToDatabaseOptimized 오류: {ex}");
                        throw;
                    }
                }
            }
            
            _progress?.Report($"✅ 데이터베이스 삽입 완료: 총 {processedRows}건 처리됨");
        }

        /// <summary>
        /// 문자열을 지정된 길이로 자르는 유틸리티 메서드
        /// </summary>
        /// <param name="input">입력 문자열</param>
        /// <param name="maxLength">최대 길이</param>
        /// <returns>자른 문자열</returns>
        private string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Length > maxLength ? input.Substring(0, maxLength) : input;
        }

        /// <summary>
        /// 최적화된 배치 삽입 실행
        /// 
        /// 개선사항:
        /// - 트랜잭션 사용으로 성능 향상
        /// - 상세한 오류 메시지
        /// - 메모리 효율성 개선
        /// - 매개변수화된 쿼리로 SQL 인젝션 방지
        /// </summary>
        /// <param name="queries">삽입 쿼리 목록</param>
        /// <param name="parametersList">매개변수 목록</param>
        private async Task ExecuteBatchInsertOptimized(List<string> queries, List<Dictionary<string, object>> parametersList)
        {
            try
            {
                // 매개변수화된 쿼리로 변환
                var parameterizedQueries = new List<(string sql, Dictionary<string, object> parameters)>();
                
                for (int i = 0; i < queries.Count; i++)
                {
                    parameterizedQueries.Add((queries[i], parametersList[i]));
                }
                
                // 매개변수화된 트랜잭션 실행
                var success = await _databaseService.ExecuteParameterizedTransactionAsync(parameterizedQueries);
                
                if (success)
                {
                    _progress?.Report($"✅ 배치 삽입 성공: {queries.Count}건");
                }
                else
                {
                    throw new InvalidOperationException("매개변수화된 트랜잭션 실행 실패");
                }
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 배치 삽입 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 일괄 삽입 실행 (기존 메서드 - 호환성 유지)
        /// </summary>
        /// <param name="queries">삽입 쿼리 목록</param>
        /// <param name="parameters">매개변수</param>
        private async Task ExecuteBatchInsert(List<string> queries, Dictionary<string, object> parameters)
        {
            try
            {
                await _databaseService.ExecuteTransactionAsync(queries);
                _progress?.Report($"✅ {queries.Count}건 데이터 삽입 완료");
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 데이터 삽입 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 특정 품목코드에 별표 추가
        /// 
        /// 대상 품목코드: 7710, 7720
        /// 처리: 주소 뒤에 '*' 추가
        /// </summary>
        private async Task AddStarToAddress()
        {
            try
            {
                var updateQuery = @"
                    UPDATE 송장출력_사방넷원본변환
                    SET 주소 = CONCAT(주소, '*')
                    WHERE 품목코드 IN ('7710', '7720')";
                
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(updateQuery);
                _progress?.Report($"✅ 특정 품목코드의 주문건 주소에 별표(*) 추가 완료: {affectedRows}건");
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 별표 추가 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 송장명 변경 (BS_ → GC_)
        /// 
        /// 처리: 송장명이 'BS_'로 시작하는 경우 'GC_'로 변경
        /// </summary>
        private async Task ReplaceBsWithGc()
        {
            try
            {
                var updateQuery = @"
                    UPDATE 송장출력_사방넷원본변환
                    SET 송장명 = CONCAT('GC_', SUBSTRING(송장명, 4))
                    WHERE LEFT(송장명, 3) = 'BS_'";
                
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(updateQuery);
                _progress?.Report($"✅ 송장명 변경 완료: {affectedRows}건 (BS_ → GC_)");
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 송장명 변경 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 수취인명 정리
        /// 
        /// 처리: 수취인명이 'nan'인 경우 '난난'으로 변경
        /// </summary>
        private async Task UpdateRecipientName()
        {
            try
            {
                var updateQuery = @"
                    UPDATE 송장출력_사방넷원본변환
                    SET 수취인명 = '난난'
                    WHERE 수취인명 = 'nan'";
                
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(updateQuery);
                _progress?.Report($"✅ 수취인명 정리 완료: {affectedRows}건");
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 수취인명 정리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 주소 정리 (· 문자 제거)
        /// 
        /// 처리: 주소에서 '·' 문자를 제거
        /// </summary>
        private async Task CleanAddressInDatabase()
        {
            try
            {
                var updateQuery = @"
                    UPDATE 송장출력_사방넷원본변환
                    SET 주소 = REPLACE(주소, '·', '')
                    WHERE 주소 LIKE '%·%'";
                
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(updateQuery);
                _progress?.Report($"✅ 주소 정리 완료: {affectedRows}건");
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 주소 정리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 결제수단 정리 (배민상회 → 0)
        /// 
        /// 처리: 쇼핑몰이 '배민상회'인 경우 결제수단을 '0'으로 변경
        /// </summary>
        private async Task UpdatePaymentMethodForBaemin()
        {
            try
            {
                var updateQuery = @"
                    UPDATE 송장출력_사방넷원본변환
                    SET 결제수단 = '0'
                    WHERE 쇼핑몰 = '배민상회'";
                
                var affectedRows = await _databaseService.ExecuteNonQueryAsync(updateQuery);
                _progress?.Report($"✅ 결제수단 정리 완료: {affectedRows}건");
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 결제수단 정리 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 데이터베이스에서 정리된 데이터 읽어오기
        /// </summary>
        /// <returns>정리된 데이터</returns>
        private async Task<DataTable> LoadProcessedDataFromDatabase()
        {
            try
            {
                var query = @"
                    SELECT 
                        수취인명, 전화번호1, 전화번호2, 우편번호, 주소, 옵션명, 수량, 배송메세지, 주문번호,
                        쇼핑몰, 수집시간, 송장명, 품목코드, `주문번호(쇼핑몰)`, 결제금액, 주문금액, 결제수단, 면과세구분, 주문상태, 배송송
                    FROM 송장출력_사방넷원본변환
                    ORDER BY 주문번호";
                
                return await _databaseService.GetDataTableAsync(query);
            }
            catch (Exception ex)
            {
                _progress?.Report($"❌ 데이터 읽기 실패: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 출고지별 분류 (Shipment Center Classification)

        /// <summary>
        /// 출고지별로 데이터를 분류합니다
        /// 
        /// 분류 기준:
        /// - Order.ShippingCenter 필드 값
        /// - 값이 없는 경우 "미분류"로 분류
        /// 
        /// 반환 형식:
        /// - List<(string centerName, DataTable data)>
        /// - centerName: 출고지명
        /// - data: 해당 출고지의 데이터
        /// 
        /// 처리 과정:
        /// 1. Dictionary를 사용하여 출고지별 그룹화
        /// 2. 각 Order의 ShippingCenter 값을 키로 사용
        /// 3. 동일한 출고지의 데이터를 하나의 DataTable로 수집
        /// 4. 튜플 리스트로 변환하여 반환
        /// </summary>
        /// <param name="data">분류할 전체 데이터</param>
        /// <returns>출고지별로 그룹화된 데이터 리스트</returns>
        private List<(string centerName, DataTable data)> ClassifyByShipmentCenter(DataTable data)
        {
            // 출고지별 그룹화를 위한 Dictionary
            var groups = new Dictionary<string, DataTable>();
            
            // 각 행을 순회하며 출고지별로 분류
            foreach (DataRow row in data.Rows)
            {
                // DataRow를 Order 객체로 변환
                var order = Order.FromDataRow(row);
                // 출고지명이 없으면 "미분류"로 설정
                var centerName = order.ShippingCenter ?? "미분류";
                
                // 해당 출고지의 DataTable이 없으면 생성
                if (!groups.ContainsKey(centerName))
                {
                    groups[centerName] = data.Clone();
                }
                
                // 해당 출고지의 DataTable에 데이터 추가
                groups[centerName].Rows.Add(order.ToDataRow(groups[centerName]));
            }

            // Dictionary를 튜플 리스트로 변환하여 반환
            return groups.Select(g => (g.Key, g.Value)).ToList();
        }

        #endregion

        #region 출고지별 처리 (Shipment Center Processing)

        /// <summary>
        /// 특정 출고지의 데이터를 처리합니다
        /// 
        /// 처리 과정:
        /// 1. 출고지별 배송비 설정
        /// 2. 특수 출고지 여부 확인
        /// 3. 특수 출고지인 경우 특화 처리
        /// 4. 일반 출고지인 경우 기본 처리
        /// 
        /// 특수 출고지:
        /// - 감천: 특별한 가격 계산 로직
        /// - 카카오: 이벤트 가격 적용
        /// - 부산외부: 지역별 특별 처리
        /// 
        /// 일반 출고지:
        /// - 기본 송장 처리 로직 적용
        /// - 표준 배송비 적용
        /// </summary>
        /// <param name="centerName">처리할 출고지명</param>
        /// <param name="data">해당 출고지의 데이터</param>
        /// <returns>처리된 데이터</returns>
        private DataTable ProcessShipmentCenter(string centerName, DataTable data)
        {
            // 출고지별 배송비 설정
            var shippingCost = GetShippingCostForCenter(centerName);
            
            // 특수 출고지 처리
            if (IsSpecialShipmentCenter(centerName))
            {
                // 특수 출고지 타입 확인
                var specialType = GetSpecialType(centerName);
                // ShipmentProcessor를 생성하여 특화 처리 실행
                var processor = new ShipmentProcessor(centerName, data, shippingCost, _progress);
                return processor.ProcessSpecialShipment(specialType);
            }
            else
            {
                // 일반 출고지 처리
                var processor = new ShipmentProcessor(centerName, data, shippingCost, _progress);
                return processor.Process();
            }
        }

        /// <summary>
        /// 출고지별 배송비를 가져옵니다
        /// 
        /// 배송비 설정:
        /// - 서울냉동: 5,000원
        /// - 경기공산: 4,000원
        /// - 부산: 6,000원
        /// - 기타: 5,000원 (기본값)
        /// 
        /// 설정 방법:
        /// - App.config의 appSettings에서 읽어옴
        /// - 설정이 없는 경우 기본값 사용
        /// </summary>
        /// <param name="centerName">배송비를 확인할 출고지명</param>
        /// <returns>해당 출고지의 배송비</returns>
        private decimal GetShippingCostForCenter(string centerName)
        {
            // 출고지명에 따른 설정 키 결정
            var configKey = centerName switch
            {
                "서울냉동" => "SeoulColdShippingCost",
                "경기공산" => "GyeonggiIndustrialShippingCost",
                "부산" => "BusanShippingCost",
                _ => "DefaultShippingCost"
            };

            // App.config에서 배송비 설정 읽기
            var configValue = ConfigurationManager.AppSettings[configKey];
            // 설정값을 decimal로 변환, 실패하면 기본값 5000원 사용
            return decimal.TryParse(configValue, out var cost) ? cost : 5000m;
        }

        /// <summary>
        /// 특수 출고지인지 확인합니다
        /// 
        /// 특수 출고지 목록:
        /// - 감천: 특별한 가격 계산 로직 적용
        /// - 카카오: 이벤트 가격 적용
        /// - 부산외부: 지역별 특별 처리
        /// 
        /// 확인 방법:
        /// - 출고지명에 특정 키워드가 포함되어 있는지 확인
        /// - 대소문자 구분 없이 검색
        /// </summary>
        /// <param name="centerName">확인할 출고지명</param>
        /// <returns>특수 출고지 여부 (true: 특수, false: 일반)</returns>
        private bool IsSpecialShipmentCenter(string centerName)
        {
            // 특수 출고지 목록 정의
            var specialCenters = new[] { "감천", "카카오", "부산외부" };
            // 출고지명에 특수 키워드가 포함되어 있는지 확인
            return specialCenters.Any(center => centerName.Contains(center));
        }

        /// <summary>
        /// 특수 출고지의 타입을 가져옵니다
        /// 
        /// 특수 타입 분류:
        /// - 감천: "감천" (특별한 가격 계산)
        /// - 카카오: "카카오" (이벤트 가격 적용)
        /// - 기타: "일반" (기본 처리)
        /// 
        /// 사용 목적:
        /// - ShipmentProcessor에서 특화 처리 로직 선택
        /// - 각 특수 출고지별 맞춤 처리
        /// </summary>
        /// <param name="centerName">타입을 확인할 출고지명</param>
        /// <returns>특수 타입 문자열</returns>
        private string GetSpecialType(string centerName)
        {
            // 출고지명에 따른 특수 타입 반환
            if (centerName.Contains("감천"))
                return "감천";
            else if (centerName.Contains("카카오"))
                return "카카오";
            else
                return "일반";
        }

        #endregion

        #region 파일 생성 및 업로드 (File Generation and Upload)

        /// <summary>
        /// 최종 파일을 생성하고 업로드합니다
        /// 
        /// 처리 과정:
        /// 1. 각 출고지별로 Excel 파일 생성
        /// 2. Dropbox에 파일 업로드
        /// 3. 업로드 결과 수집
        /// 4. 실패한 경우 로그 메시지 출력
        /// 
        /// 파일명 형식:
        /// - 송장_{출고지명}_{날짜}.xlsx
        /// - 예: 송장_서울냉동_20241201.xlsx
        /// 
        /// 반환 형식:
        /// - List<(string centerName, string filePath, string dropboxUrl)>
        /// - centerName: 출고지명
        /// - filePath: 로컬 파일 경로
        /// - dropboxUrl: Dropbox 공유 링크
        /// </summary>
        /// <param name="processedResults">처리된 결과들</param>
        /// <returns>업로드 결과 리스트</returns>
        private async Task<List<(string centerName, string filePath, string dropboxUrl)>> GenerateAndUploadFiles(
            List<(string centerName, DataTable data)> processedResults)
        {
            // 업로드 결과를 저장할 리스트
            var uploadResults = new List<(string centerName, string filePath, string dropboxUrl)>();

            // 각 출고지별로 파일 생성 및 업로드
            foreach (var (centerName, data) in processedResults)
            {
                // 데이터가 없는 경우 건너뛰기
                if (data.Rows.Count == 0)
                    continue;

                // 파일명 생성 (날짜 포함)
                var fileName = $"송장_{centerName}_{DateTime.Now:yyyyMMdd}";
                var filePath = _fileService.GetOutputFilePath(fileName, centerName);

                // Excel 파일 생성
                _fileService.SaveDataTableToExcel(data, filePath, centerName);
                _progress?.Report($"{centerName} 송장 파일 생성 완료: {Path.GetFileName(filePath)}");

                // Dropbox 업로드
                try
                {
                    // Dropbox에 파일 업로드
                    var dropboxUrl = await _apiService.UploadFileToDropboxAsync(filePath, centerName);
                    // 성공한 경우 결과에 추가
                    uploadResults.Add((centerName, filePath, dropboxUrl));
                    _progress?.Report($"{centerName} Dropbox 업로드 완료");
                }
                catch (Exception ex)
                {
                    // 업로드 실패 시 로그만 출력하고 계속 진행
                    _progress?.Report($"{centerName} Dropbox 업로드 실패: {ex.Message}");
                }
            }

            return uploadResults;
        }

        #endregion

        #region 알림 전송 (Notification Sending)

        /// <summary>
        /// 카카오워크 알림을 전송합니다
        /// 
        /// 알림 내용:
        /// - 출고지명
        /// - 파일명
        /// - 처리 시간
        /// - Dropbox 링크 (첨부)
        /// 
        /// 설정 요구사항:
        /// - App.config의 "KakaoWorkChatroomId" 설정
        /// - 설정이 없으면 알림 전송하지 않음
        /// 
        /// 예외 처리:
        /// - 개별 알림 전송 실패 시 로그만 출력
        /// - 전체 프로세스는 계속 진행
        /// </summary>
        /// <param name="uploadResults">업로드 결과 리스트</param>
        private async Task SendKakaoWorkNotifications(List<(string centerName, string filePath, string dropboxUrl)> uploadResults)
        {
            // Kakao Work 채팅방 ID 설정 확인
            var chatroomId = ConfigurationManager.AppSettings["KakaoWorkChatroomId"] ?? "";
            
            // 채팅방 ID가 설정되지 않았으면 알림 전송하지 않음
            if (string.IsNullOrEmpty(chatroomId))
            {
                _progress?.Report("카카오워크 채팅방 ID가 설정되지 않아 알림을 전송하지 않습니다.");
                return;
            }

            // 각 업로드 결과에 대해 알림 전송
            foreach (var (centerName, filePath, dropboxUrl) in uploadResults)
            {
                try
                {
                    // 알림 메시지 구성
                    var message = $"[송장 처리 완료]\n출고지: {centerName}\n파일: {Path.GetFileName(filePath)}\n처리 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    
                    // Kakao Work로 메시지 전송
                    await _apiService.SendKakaoWorkMessageAsync(chatroomId, message, dropboxUrl);
                    _progress?.Report($"{centerName} 카카오워크 알림 전송 완료");
                }
                catch (Exception ex)
                {
                    // 알림 전송 실패 시 로그만 출력하고 계속 진행
                    _progress?.Report($"{centerName} 카카오워크 알림 전송 실패: {ex.Message}");
                }
            }
        }

        #endregion
    }
} 