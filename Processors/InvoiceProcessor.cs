using System.Data;
using System.Configuration;
using LogisticManager.Services;
using LogisticManager.Models;

namespace LogisticManager.Processors
{
    /// <summary>
    /// 전체 송장 처리 로직을 담당하는 메인 프로세서 클래스
    /// 
    /// 주요 기능:
    /// - Excel 파일 읽기 및 데이터 검증
    /// - 1차 데이터 가공 (주소 정리, 수취인명 정리, 결제방법 정리)
    /// - 출고지별 데이터 분류
    /// - 각 출고지별 특화 처리
    /// - 최종 파일 생성 및 Dropbox 업로드
    /// - Kakao Work 알림 전송
    /// 
    /// 처리 단계:
    /// 1. Excel 파일 읽기 (10%)
    /// 2. 1차 데이터 가공 (20%)
    /// 3. 출고지별 분류 (30%)
    /// 4. 각 출고지별 처리 (30-80%)
    /// 5. 최종 파일 생성 및 업로드 (80-90%)
    /// 6. Kakao Work 알림 전송 (90-100%)
    /// 
    /// 의존성:
    /// - FileService: Excel 파일 읽기/쓰기
    /// - DatabaseService: 데이터베이스 연동
    /// - ApiService: Dropbox 업로드, Kakao Work 알림
    /// - ShipmentProcessor: 출고지별 세부 처리
    /// </summary>
    public class InvoiceProcessor
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 파일 처리 서비스 - Excel 파일 읽기/쓰기 담당
        /// </summary>
        private readonly FileService _fileService;
        
        /// <summary>
        /// 데이터베이스 서비스 - MySQL 연결 및 쿼리 실행 담당
        /// </summary>
        private readonly DatabaseService _databaseService;
        
        /// <summary>
        /// API 서비스 - Dropbox 업로드, Kakao Work 알림 담당
        /// </summary>
        private readonly ApiService _apiService;
        
        /// <summary>
        /// 진행 상황 메시지 콜백 - 실시간 로그 메시지 전달
        /// </summary>
        private readonly IProgress<string>? _progress;
        
        /// <summary>
        /// 진행률 콜백 - 0-100% 진행률 전달
        /// </summary>
        private readonly IProgress<int>? _progressReporter;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// InvoiceProcessor 생성자
        /// 
        /// 의존성 주입:
        /// - FileService: Excel 파일 처리
        /// - DatabaseService: 데이터베이스 연동
        /// - ApiService: 외부 API 연동
        /// - progress: 진행 상황 메시지 콜백 (선택사항)
        /// - progressReporter: 진행률 콜백 (선택사항)
        /// </summary>
        /// <param name="fileService">파일 처리 서비스</param>
        /// <param name="databaseService">데이터베이스 서비스</param>
        /// <param name="apiService">API 서비스</param>
        /// <param name="progress">진행 상황 메시지 콜백 (선택사항)</param>
        /// <param name="progressReporter">진행률 콜백 (선택사항)</param>
        public InvoiceProcessor(FileService fileService, DatabaseService databaseService, ApiService apiService, 
            IProgress<string>? progress = null, IProgress<int>? progressReporter = null)
        {
            _fileService = fileService;
            _databaseService = databaseService;
            _apiService = apiService;
            _progress = progress;
            _progressReporter = progressReporter;
        }

        #endregion

        #region 메인 처리 메서드 (Main Processing Method)

        /// <summary>
        /// 송장 처리의 메인 메서드
        /// 
        /// 전체 처리 과정:
        /// 1. Excel 파일 읽기 (0-10%)
        /// 2. 1차 데이터 가공 (10-20%)
        /// 3. 출고지별 분류 (20-30%)
        /// 4. 각 출고지별 처리 (30-80%)
        /// 5. 최종 파일 생성 및 업로드 (80-90%)
        /// 6. Kakao Work 알림 전송 (90-100%)
        /// 
        /// 예외 처리:
        /// - 파일 읽기 오류
        /// - 데이터 가공 오류
        /// - API 연동 오류
        /// - 네트워크 오류
        /// </summary>
        /// <param name="filePath">입력 Excel 파일의 전체 경로</param>
        /// <param name="progress">진행 상황 메시지 콜백 (선택사항)</param>
        /// <param name="progressReporter">진행률 콜백 (선택사항)</param>
        /// <returns>처리 완료 여부 (true: 성공, false: 실패)</returns>
        /// <exception cref="InvalidOperationException">처리 중 오류가 발생한 경우</exception>
        public async Task<bool> ProcessAsync(string filePath, IProgress<string>? progress = null, IProgress<int>? progressReporter = null)
        {
            try
            {
                // 진행 상황 및 진행률 콜백 설정
                _progress?.Report("송장 처리 작업을 시작합니다...");
                _progressReporter?.Report(0);

                // 1단계: Excel 파일 읽기 (0-10%)
                _progress?.Report("Excel 파일을 읽는 중...");
                var originalData = _fileService.ReadExcelToDataTable(filePath);
                _progressReporter?.Report(10);
                _progress?.Report($"총 {originalData.Rows.Count}건의 데이터를 읽었습니다.");

                // 2단계: 1차 데이터 가공 (10-20%)
                _progress?.Report("1차 데이터 가공을 시작합니다...");
                var processedData = ProcessFirstStageData(originalData);
                _progressReporter?.Report(20);
                _progress?.Report("1차 데이터 가공이 완료되었습니다.");

                // 3단계: 출고지별 분류 (20-30%)
                _progress?.Report("출고지별 데이터 분류를 시작합니다...");
                var shipmentGroups = ClassifyByShipmentCenter(processedData);
                _progressReporter?.Report(30);
                _progress?.Report($"총 {shipmentGroups.Count}개 출고지로 분류되었습니다.");

                // 4단계: 각 출고지별 처리 (30-80%)
                var processedResults = new List<(string centerName, DataTable data)>();
                var totalCenters = shipmentGroups.Count;
                var currentCenter = 0;

                foreach (var group in shipmentGroups)
                {
                    currentCenter++;
                    // 진행률 계산: 30% + (현재 출고지 / 전체 출고지) * 50%
                    var progressPercentage = 30 + (int)((double)currentCenter / totalCenters * 50);
                    _progressReporter?.Report(progressPercentage);

                    _progress?.Report($"{group.centerName} 출고지 처리 중... ({currentCenter}/{totalCenters})");
                    
                    // 각 출고지별 세부 처리
                    var centerProcessedData = ProcessShipmentCenter(group.centerName, group.data);
                    processedResults.Add((group.centerName, centerProcessedData));
                    
                    _progress?.Report($"{group.centerName} 출고지 처리 완료");
                }

                // 5단계: 최종 파일 생성 및 업로드 (80-90%)
                _progress?.Report("최종 파일 생성을 시작합니다...");
                _progressReporter?.Report(80);
                
                var uploadResults = await GenerateAndUploadFiles(processedResults);
                
                _progress?.Report("최종 파일 생성 및 업로드 완료");
                _progressReporter?.Report(90);

                // 6단계: 카카오워크 알림 전송 (90-100%)
                _progress?.Report("카카오워크 알림을 전송합니다...");
                await SendKakaoWorkNotifications(uploadResults);
                
                _progress?.Report("카카오워크 알림 전송 완료");
                _progressReporter?.Report(100);

                _progress?.Report("모든 송장 처리 작업이 완료되었습니다!");
                return true;
            }
            catch (Exception ex)
            {
                _progress?.Report($"송장 처리 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 데이터 가공 (Data Processing)

        /// <summary>
        /// 1차 데이터 가공 처리
        /// 
        /// 가공 내용:
        /// - 주소 정리 (연속 공백 제거, 빈 괄호 제거, 상세주소 분리)
        /// - 수취인명 정리 (특수문자 제거)
        /// - 결제방법 정리 (표준화)
        /// - 유효한 데이터만 필터링
        /// 
        /// 처리 과정:
        /// 1. 원본 DataTable 복제
        /// 2. 각 행을 Order 객체로 변환
        /// 3. 데이터 유효성 검사
        /// 4. 각 필드별 정리 작업 수행
        /// 5. 정리된 데이터를 새로운 DataTable에 추가
        /// </summary>
        /// <param name="data">원본 데이터가 담긴 DataTable</param>
        /// <returns>가공된 데이터가 담긴 DataTable</returns>
        private DataTable ProcessFirstStageData(DataTable data)
        {
            // 원본 데이터 구조를 복제하여 새로운 DataTable 생성
            var processedData = data.Clone();

            // 각 행을 순회하며 가공 처리
            foreach (DataRow row in data.Rows)
            {
                // DataRow를 Order 객체로 변환
                var order = Order.FromDataRow(row);
                
                // 데이터 유효성 검사
                if (order.IsValid())
                {
                    // 주소 정리 (연속 공백, 빈 괄호 제거, 상세주소 분리)
                    CleanAddress(order);
                    
                    // 수취인명 정리 (특수문자 제거)
                    CleanRecipientName(order);
                    
                    // 결제방법 정리 (표준화)
                    CleanPaymentMethod(order);
                    
                    // 정리된 데이터를 새로운 DataTable에 추가
                    processedData.Rows.Add(order.ToDataRow(processedData));
                }
            }

            return processedData;
        }

        /// <summary>
        /// 주소 정리 처리
        /// 
        /// 정리 작업:
        /// - 연속 공백을 단일 공백으로 변경
        /// - 빈 괄호 "()" 제거
        /// - 앞뒤 공백 제거
        /// - 상세주소가 주소에 포함된 경우 분리
        /// 
        /// 상세주소 분리 규칙:
        /// - 주소에 "(" 와 ")" 가 포함된 경우
        /// - 괄호 안의 내용을 상세주소로 분리
        /// - 괄호 앞의 내용을 기본 주소로 설정
        /// </summary>
        /// <param name="order">정리할 주문 정보</param>
        private void CleanAddress(Order order)
        {
            if (string.IsNullOrEmpty(order.Address))
                return;

            // 특정 패턴 정리
            order.Address = order.Address
                .Replace("  ", " ") // 연속 공백을 단일 공백으로 변경
                .Replace("()", "") // 빈 괄호 제거
                .Trim(); // 앞뒤 공백 제거

            // 상세주소가 주소에 포함되어 있으면 분리
            if (order.Address.Contains("(") && order.Address.Contains(")"))
            {
                var startIndex = order.Address.IndexOf("(");
                var endIndex = order.Address.IndexOf(")");
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    // 괄호 안의 내용을 상세주소로 분리
                    var detailAddress = order.Address.Substring(startIndex + 1, endIndex - startIndex - 1);
                    order.DetailAddress = detailAddress;
                    
                    // 괄호 앞의 내용을 기본 주소로 설정
                    order.Address = order.Address.Substring(0, startIndex).Trim();
                }
            }
        }

        /// <summary>
        /// 수취인명 정리 처리
        /// 
        /// 제거하는 특수문자:
        /// - 괄호: "(", ")"
        /// - 대괄호: "[", "]"
        /// - 앞뒤 공백
        /// 
        /// 목적:
        /// - 송장 출력 시 깔끔한 수취인명 표시
        /// - 데이터 일관성 확보
        /// </summary>
        /// <param name="order">정리할 주문 정보</param>
        private void CleanRecipientName(Order order)
        {
            if (string.IsNullOrEmpty(order.RecipientName))
                return;

            // 특수문자 제거 및 정리
            order.RecipientName = order.RecipientName
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "")
                .Trim();
        }

        /// <summary>
        /// 결제방법 정리 처리
        /// 
        /// 표준화 규칙:
        /// - "카드" 또는 "card" 포함 → "카드결제"
        /// - "현금" 또는 "cash" 포함 → "현금결제"
        /// - "무통장" 또는 "bank" 포함 → "무통장입금"
        /// - 기타 → 원본 유지
        /// 
        /// 목적:
        /// - 결제방법 표준화로 송장 출력 일관성 확보
        /// - 데이터 분석 시 카테고리화 용이
        /// </summary>
        /// <param name="order">정리할 주문 정보</param>
        private void CleanPaymentMethod(Order order)
        {
            if (string.IsNullOrEmpty(order.PaymentMethod))
                return;

            // 결제방법 표준화 (소문자로 변환 후 비교)
            var paymentMethod = order.PaymentMethod.ToLower();
            if (paymentMethod.Contains("카드") || paymentMethod.Contains("card"))
            {
                order.PaymentMethod = "카드결제";
            }
            else if (paymentMethod.Contains("현금") || paymentMethod.Contains("cash"))
            {
                order.PaymentMethod = "현금결제";
            }
            else if (paymentMethod.Contains("무통장") || paymentMethod.Contains("bank"))
            {
                order.PaymentMethod = "무통장입금";
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
                var order = Order.FromDataRow(row);
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
                var specialType = GetSpecialType(centerName);
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
            var specialCenters = new[] { "감천", "카카오", "부산외부" };
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
                    var dropboxUrl = await _apiService.UploadFileToDropboxAsync(filePath, centerName);
                    uploadResults.Add((centerName, filePath, dropboxUrl));
                    _progress?.Report($"{centerName} Dropbox 업로드 완료");
                }
                catch (Exception ex)
                {
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
                    _progress?.Report($"{centerName} 카카오워크 알림 전송 실패: {ex.Message}");
                }
            }
        }

        #endregion
    }
} 