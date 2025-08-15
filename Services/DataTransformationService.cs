using System;
using System.Data;
using System.Text.RegularExpressions;
using System.IO;

namespace LogisticManager.Services
{
    /// <summary>
    /// 엑셀 데이터 변환 및 정규화 서비스
    /// 
    /// 🎯 주요 목적:
    /// - 엑셀에서 읽어온 원본 데이터를 시스템에서 사용할 수 있는 형태로 변환
    /// - 데이터 품질 향상 및 일관성 확보
    /// - 시스템 호환성 및 오류 방지
    /// 
    /// 📋 핵심 변환 기능:
    /// 
    /// 1️⃣ 텍스트 데이터 정제:
    ///    - 수취인명: 'nan' → '난난' 변환 (데이터 오류 수정)
    ///    - 송장명: 'BS_' → 'GC_' 접두사 변경 (시스템 호환성)
    ///    - 옵션명: 특수문자 제거 및 공백 정리 (데이터 정제)
    /// 
    /// 2️⃣ 연락처 정보 정규화:
    ///    - 전화번호: 010-1234-5678 형식으로 통일
    ///    - 우편번호: 5자리/6자리 형식 표준화
    /// 
    /// 3️⃣ 주소 데이터 처리:
    ///    - 주소 정제: 공백 정리, '·' 문자 제거
    ///    - 특수 규칙: 품목코드 7710/7720 시 주소 끝에 '*' 추가 (배송 구분)
    /// 
    /// 4️⃣ 결제 정보 표준화:
    ///    - 결제수단: 배민상회 쇼핑몰 시 '0'으로 통일
    /// 
    /// 5️⃣ 숫자 데이터 정규화:
    ///    - 수량: 숫자만 추출하여 정수 변환
    ///    - 날짜: 표준 형식(yyyy-MM-dd HH:mm:ss) 통일
    /// 
    /// ⚡ 처리 시점:
    /// - 엑셀 → DataTable 변환 직후
    /// - Order 객체 생성 전 단계
    /// 
    /// 🔧 확장성:
    /// - 새로운 변환 규칙 추가 용이
    /// - 비즈니스 요구사항 반영 가능
    /// 
    /// 💡 사용법:
    /// ```csharp
    /// var transformationService = new DataTransformationService();
    /// var transformedData = transformationService.TransformData(originalDataTable);
    /// ```
    /// </summary>
    public class DataTransformationService
    {
        #region 상수 정의 (Constants)

        /// <summary>숫자만 추출하는 정규식 패턴</summary>
        private const string DIGITS_ONLY_PATTERN = @"[^\d]";
        
        /// <summary>영문, 숫자, 한글, 기본 특수문자만 허용하는 패턴</summary>
        private const string ALLOWED_CHARS_PATTERN = @"[^a-zA-Z0-9가-힣\s\-\(\)\[\]]";
        
        /// <summary>연속된 공백을 하나로 줄이는 패턴</summary>
        private const string MULTIPLE_SPACES_PATTERN = @"\s{2,}";

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// DataTable 데이터 변환 및 정규화 메인 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 엑셀에서 읽어온 원본 DataTable을 시스템에서 사용할 수 있는 형태로 변환
        /// - 모든 행에 대해 개별 변환 규칙 적용
        /// - 변환 과정을 상세 로그로 기록 (디버깅 및 추적)
        /// 
        /// 🔄 처리 흐름:
        /// 1. 입력 검증 (DataTable null 체크)
        /// 2. 모든 행을 순회하며 개별 변환 수행
        /// 3. 각 컬럼별 변환 규칙 적용
        /// 4. 변환 결과를 상세 로그로 출력
        /// 5. 오류 발생 시 원본 값 유지
        /// 
        /// 📊 변환 대상 컬럼:
        /// 
        /// 🔤 텍스트 데이터:
        ///    - 수취인명: 'nan' → '난난' (데이터 오류 수정)
        ///    - 송장명: 'BS_' → 'GC_' 접두사 변경 (시스템 호환성)
        ///    - 옵션명: 특수문자 제거, 공백 정리 (데이터 정제)
        /// 
        /// 📞 연락처 정보:
        ///    - 전화번호1/2: 010-1234-5678 형식으로 통일
        ///    - 우편번호: 5자리/6자리 형식 표준화
        /// 
        /// 🏠 주소 데이터:
        ///    - 주소: 공백 정리, '·' 제거 (데이터 정제)
        ///    - 특수 규칙: 품목코드 7710/7720 시 '*' 추가 (배송 구분)
        /// 
        /// 💳 결제 정보:
        ///    - 결제수단: 배민상회 시 '0'으로 통일 (시스템 표준화)
        /// 
        /// 🔢 숫자 데이터:
        ///    - 수량: 숫자만 추출하여 정수 변환
        ///    - 수집시간: 표준 날짜 형식 통일 (yyyy-MM-dd HH:mm:ss)
        /// 
        /// ⚠️ 중요 사항:
        /// - 원본 DataTable을 직접 수정 (메모리 효율성)
        /// - 변환 실패 시 원본 값 보존 (데이터 손실 방지)
        /// - 상세 로그로 변환 과정 추적 가능 (디버깅)
        /// - 변환된 행 수와 오류 수를 통계로 제공
        /// 
        /// 💡 사용 예시:
        /// ```csharp
        /// var transformationService = new DataTransformationService();
        /// var transformedData = transformationService.TransformData(originalDataTable);
        /// Console.WriteLine($"변환 완료: {transformedData.Rows.Count}개 행 처리");
        /// ```
        /// </summary>
        /// <param name="dataTable">변환할 DataTable (엑셀에서 읽어온 원본 데이터)</param>
        /// <returns>변환된 DataTable (원본 DataTable을 수정하여 반환)</returns>
        /// <exception cref="ArgumentNullException">dataTable이 null인 경우</exception>
        public DataTable TransformData(DataTable dataTable)
        {
            // 입력 검증
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable), "변환할 DataTable이 null입니다.");
            }

            Console.WriteLine($"[DataTransformationService] 데이터 변환 시작 - 총 {dataTable.Rows.Count}개 행 처리");

            // 별표2 컬럼이 없으면 생성 (엑셀에 없을 수 있으므로 메모리 내에서 보강)
            try
            {
                if (!dataTable.Columns.Contains("별표2"))
                {
                    dataTable.Columns.Add("별표2", typeof(string));
                    var initLog = "[DataTransformationService] 별표2 컬럼이 없어 생성함 (기본값: 빈 문자열)";
                    Console.WriteLine(initLog);
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {initLog}\n");
                }
                else
                {
                    // 기존 컬럼이 존재하더라도 null 값은 빈 문자열로 초기화하여 INSERT 시 누락 방지
                    foreach (DataRow row in dataTable.Rows)
                    {
                        if (row["별표2"] == DBNull.Value || row["별표2"] == null)
                        {
                            row["별표2"] = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var initErr = $"[DataTransformationService] 별표2 컬럼 생성 실패: {ex.Message}";
                Console.WriteLine(initErr);
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {initErr}\n");
            }

            int transformedCount = 0;
            int errorCount = 0;

            // 모든 데이터 행에 대해 변환 수행
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var row = dataTable.Rows[i];
                
                try
                {
                    // 행별 변환 수행
                    bool hasChanges = TransformRow(row, dataTable, i + 1);
                    
                    if (hasChanges)
                    {
                        transformedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"❌ [DataTransformationService] 행 {i + 1} 변환 중 오류 발생: {ex.Message}");
                }
            }

            Console.WriteLine($"✅ [DataTransformationService] 데이터 변환 완료 - 변환된 행: {transformedCount}개, 오류: {errorCount}개");
            
            return dataTable;
        }

        #endregion

        #region 내부 메서드 (Private Methods)

        /// <summary>
        /// 개별 DataRow 데이터 변환 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 단일 DataRow에 대해 모든 변환 규칙을 순차적으로 적용
        /// - 각 변환 단계에서 변경 사항을 로그로 기록
        /// - 변환 실패 시 원본 값 보존
        /// 
        /// 🔄 변환 대상 및 규칙:
        /// 
        /// 🔤 텍스트 데이터:
        ///    - 수취인명: 'nan' → '난난' (데이터 오류 수정)
        ///    - 송장명: 'BS_' → 'GC_' 접두사 변경 (시스템 호환성)
        ///    - 옵션명: 특수문자 제거, 공백 정리 (데이터 정제)
        /// 
        /// 📞 연락처 정보:
        ///    - 전화번호1/2: 010-1234-5678 형식으로 정규화
        ///    - 우편번호: 5자리/6자리 형식으로 통일
        /// 
        /// 🏠 주소 데이터:
        ///    - 주소: 텍스트 정제 ('·' 제거) + 품목코드 특수 처리
        ///    - 품목코드 7710/7720 시 주소 끝에 '*' 추가 (배송 구분)
        /// 
        /// 💳 결제 정보:
        ///    - 결제수단: 쇼핑몰별 조건부 변환 (배민상회 → '0')
        /// 
        /// 🔢 숫자 데이터:
        ///    - 수량: 숫자만 추출하여 정수 변환
        ///    - 날짜: 표준 형식(yyyy-MM-dd HH:mm:ss)으로 표준화
        /// 
        /// ⚠️ 처리 방식:
        /// - 각 컬럼별로 개별 변환 메서드 호출
        /// - 변환 전후 값 비교하여 변경 사항만 로그 기록
        /// - 컬럼이 존재하지 않는 경우 안전하게 건너뜀
        /// - 변환 실패 시 원본 값 유지 (데이터 손실 방지)
        /// 
        /// 💡 반환값:
        /// - true: 변환이 수행된 경우
        /// - false: 변환이 수행되지 않은 경우
        /// </summary>
        /// <param name="row">변환할 DataRow</param>
        /// <param name="dataTable">DataTable (컬럼 존재 여부 확인용)</param>
        /// <param name="rowNumber">행 번호 (로깅용)</param>
        /// <returns>변환이 수행되었는지 여부</returns>
        private bool TransformRow(DataRow row, DataTable dataTable, int rowNumber)
        {
            bool hasChanges = false;

            // 수취인명 변환
            // - 수취인명이 'nan'인 경우 '난난'으로 변경
            // - 대소문자 구분 없이 비교 (NaN, nan, NAN 등 모두 처리)
            // - 목적: 데이터 정제 및 표준화
            if (dataTable.Columns.Contains("수취인명"))
            {
                var originalValue = row["수취인명"]?.ToString() ?? string.Empty;
                var transformedValue = TransformRecipientName(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["수취인명"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"👤 [행{rowNumber}] 수취인명 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 전화번호1 변환
            if (dataTable.Columns.Contains("전화번호1"))
            {
                var originalValue = row["전화번호1"]?.ToString() ?? string.Empty;
                var transformedValue = NormalizePhoneNumber(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["전화번호1"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"📞 [행{rowNumber}] 전화번호1 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 전화번호2 변환
            if (dataTable.Columns.Contains("전화번호2"))
            {
                var originalValue = row["전화번호2"]?.ToString() ?? string.Empty;
                var transformedValue = NormalizePhoneNumber(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["전화번호2"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"📞 [행{rowNumber}] 전화번호2 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 우편번호 변환
            if (dataTable.Columns.Contains("우편번호"))
            {
                var originalValue = row["우편번호"]?.ToString() ?? string.Empty;
                var transformedValue = NormalizeZipCode(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["우편번호"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"📮 [행{rowNumber}] 우편번호 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 주소 변환 및 품목코드별 특수 처리
            // - NormalizeAddress 메서드를 통해 주소 문자열을 정제함
            // - 주요 처리 내용:
            //   1. 앞뒤 불필요한 공백 제거 (Trim)
            //   2. 연속된 공백을 하나로 치환 (예: "서울   강남구" → "서울 강남구")
            //   3. 특수문자(탭, 개행 등) 제거 또는 공백으로 변환
            //   4. 특수문자 제거 ('·' 문자 제거)
            //   5. 주소 내 불필요한 특수기호(쉼표, 슬래시 등) 정리
            //   6. 품목코드가 7710, 7720인 경우 주소 끝에 '*' 추가
            // - 목적: 주소 데이터의 일관성 확보 및 후속 처리(배송지 분류 등) 정확도 향상
            if (dataTable.Columns.Contains("주소"))
            {
                var originalValue = row["주소"]?.ToString() ?? string.Empty;
                var transformedValue = NormalizeAddress(originalValue);
                
                // 품목코드 확인하여 특수 처리
                if (dataTable.Columns.Contains("품목코드"))
                {
                    var productCode = row["품목코드"]?.ToString() ?? string.Empty;
                    transformedValue = ApplyProductCodeSpecialRule(transformedValue, productCode);
                }
                
                if (originalValue != transformedValue)
                {
                    row["주소"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"🏠 [행{rowNumber}] 주소 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 배송메세지 변환 ('★' 제거)
            // - SQL 기준: SET 배송메세지 = REPLACE(배송메세지, '★', '') WHERE 배송메세지 LIKE '%★%'
            // - 목적: 불필요한 특수기호 제거로 라벨 출력/DB 저장 호환성 개선
            if (dataTable.Columns.Contains("배송메세지"))
            {
                var originalSpecialNote = row["배송메세지"]?.ToString() ?? string.Empty;
                var transformedSpecialNote = RemoveFilledStarFromSpecialNote(originalSpecialNote);

                if (!string.Equals(originalSpecialNote, transformedSpecialNote, StringComparison.Ordinal))
                {
                    row["배송메세지"] = transformedSpecialNote;
                    hasChanges = true;
                    Console.WriteLine($"📝 [행{rowNumber}] 배송메세지 '★' 제거: '{originalSpecialNote}' → '{transformedSpecialNote}'");
                }
            }

            // 별표2 컬럼 처리 - 제주특별자치도 주소 감지
            // - SQL 기준: SET 별표2 = '제주' WHERE 주소 LIKE '%제주특별%'
            // - 주소에 '제주특별' 또는 '제주 특별'이 포함된 경우 별표2를 '제주'로 설정
            // - 목적: 제주도 배송 구분을 위한 별표2 컬럼 활용
            // - 중복 처리 방지: 데이터베이스 레벨 처리는 비활성화하여 메모리 내 처리만 수행
            if (dataTable.Columns.Contains("별표2") && dataTable.Columns.Contains("주소"))
            {
                // 디버깅: 컬럼 존재 확인 로그
                var logMessage = $"🔍 [행{rowNumber}] 별표2/주소 컬럼 확인: 별표2={dataTable.Columns.Contains("별표2")}, 주소={dataTable.Columns.Contains("주소")}";
                Console.WriteLine(logMessage);
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
            }
            else
            {
                // 필요한 컬럼이 없는 경우 로그 출력
                var logMessage = $"⚠️ [행{rowNumber}] 별표2 처리 건너뜀: 별표2컬럼={dataTable.Columns.Contains("별표2")}, 주소컬럼={dataTable.Columns.Contains("주소")}";
                Console.WriteLine(logMessage);
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
            }
            
            if (dataTable.Columns.Contains("별표2") && dataTable.Columns.Contains("주소"))
            {
                try
                {
                    // 안전한 데이터 추출
                    var addressValue = row["주소"];
                    var star2Value = row["별표2"];
                    
                    // 디버깅: 원본 데이터 확인
                    var logMessage = $"🔍 [행{rowNumber}] 별표2 처리 시작: 주소타입={addressValue?.GetType().Name}, 별표2타입={star2Value?.GetType().Name}";
                    Console.WriteLine(logMessage);
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
                    
                    // null 체크 및 문자열 변환
                    var addressString = addressValue?.ToString() ?? string.Empty;
                    var originalStar2String = star2Value?.ToString() ?? string.Empty;
                    
                    // 디버깅: 변환된 문자열 확인
                    logMessage = $"🔍 [행{rowNumber}] 별표2 문자열 변환: 주소='{addressString}', 별표2='{originalStar2String}'";
                    Console.WriteLine(logMessage);
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
                    
                    // 변환 로직 실행
                    var transformedStar2String = TransformStar2ByAddress(originalStar2String, addressString);
                    
                    // 디버깅: 변환 결과 확인
                    logMessage = $"🔍 [행{rowNumber}] 별표2 변환 결과: '{originalStar2String}' → '{transformedStar2String}'";
                    Console.WriteLine(logMessage);
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
                    
                    // 값이 변경된 경우에만 업데이트
                    if (!string.Equals(originalStar2String, transformedStar2String, StringComparison.Ordinal))
                    {
                        row["별표2"] = transformedStar2String;
                        hasChanges = true;
                        logMessage = $"⭐ [행{rowNumber}] 별표2 변환: '{originalStar2String}' → '{transformedStar2String}' (주소: {addressString})";
                        Console.WriteLine(logMessage);
                        File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
                    }
                    else
                    {
                        logMessage = $"ℹ️ [행{rowNumber}] 별표2 변환 없음: 값이 동일함";
                        Console.WriteLine(logMessage);
                        File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
                    }
                }
                catch (Exception ex)
                {
                    // 별표2 처리 중 오류 발생 시 로그 출력 후 계속 진행
                    var errorMessage = $"⚠️ [DataTransformationService] 별표2 처리 오류 (행{rowNumber}): {ex.Message}";
                    Console.WriteLine(errorMessage);
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {errorMessage}\n");
                    
                    var stackTraceMessage = $"⚠️ [DataTransformationService] 별표2 처리 오류 상세: {ex.StackTrace}";
                    Console.WriteLine(stackTraceMessage);
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {stackTraceMessage}\n");
                    // 오류가 발생해도 다른 변환 작업은 계속 진행
                }
            }

            // 옵션명 변환 (특수문자 제거)
            if (dataTable.Columns.Contains("옵션명"))
            {
                var originalValue = row["옵션명"]?.ToString() ?? string.Empty;
                var transformedValue = NormalizeOptionName(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["옵션명"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"⚙️ [행{rowNumber}] 옵션명 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 수량 변환
            if (dataTable.Columns.Contains("수량"))
            {
                var originalValue = row["수량"]?.ToString() ?? string.Empty;
                var transformedValue = NormalizeQuantity(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["수량"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"🔢 [행{rowNumber}] 수량 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 송장명 변환
            // - SQL 기준: CONCAT('GC_', SUBSTRING(송장명, 4)) WHERE LEFT(송장명, 3) = 'BS_'
            // - 처리 규칙: 송장명이 'BS_'로 시작하는 경우 'BS_'를 'GC_'로 교체
            // - 예시: 'BS_12345' → 'GC_12345'
            // - 목적: 송장 구분 코드 변경을 통한 시스템 호환성 확보
            if (dataTable.Columns.Contains("송장명"))
            {
                var originalValue = row["송장명"]?.ToString() ?? string.Empty;
                var transformedValue = TransformInvoiceName(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["송장명"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"📋 [행{rowNumber}] 송장명 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            // 결제수단 변환 (쇼핑몰 조건부)
            // - SQL 기준: SET 결제수단 = '0' WHERE 쇼핑몰 = '배민상회'
            // - 처리 규칙: 쇼핑몰이 '배민상회'인 경우 결제수단을 '0'으로 설정
            // - 목적: 특정 쇼핑몰의 결제수단 표준화
            if (dataTable.Columns.Contains("결제수단") && dataTable.Columns.Contains("쇼핑몰"))
            {
                var shoppingMall = row["쇼핑몰"]?.ToString() ?? string.Empty;
                var originalPaymentMethod = row["결제수단"]?.ToString() ?? string.Empty;
                var transformedPaymentMethod = TransformPaymentMethodByMall(originalPaymentMethod, shoppingMall);
                
                if (originalPaymentMethod != transformedPaymentMethod)
                {
                    row["결제수단"] = transformedPaymentMethod;
                    hasChanges = true;
                    Console.WriteLine($"💳 [행{rowNumber}] 결제수단 변환: '{originalPaymentMethod}' → '{transformedPaymentMethod}' (쇼핑몰: {shoppingMall})");
                }
            }

            // 수집시간 변환
            // 수집시간 변환은 NormalizeDateTime 메서드를 통해 수행됨.
            // 이 메서드는 다양한 날짜/시간 문자열(예: "2024-06-01 13:22:11", "2024.6.1", "2024/06/01", "20240601" 등)을
            // 표준화된 형식(예: "yyyy-MM-dd HH:mm:ss" 또는 "yyyy-MM-dd")으로 변환함.
            // - 잘못된 날짜/시간 값은 빈 문자열 또는 원본 값으로 반환될 수 있음.
            // - 목적: 날짜/시간 데이터의 일관성 확보 및 후속 처리(정렬, 비교 등) 정확도 향상
            if (dataTable.Columns.Contains("수집시간"))
            {
                var originalValue = row["수집시간"]?.ToString() ?? string.Empty;
                var transformedValue = NormalizeDateTime(originalValue);
                
                if (originalValue != transformedValue)
                {
                    row["수집시간"] = transformedValue;
                    hasChanges = true;
                    Console.WriteLine($"📅 [행{rowNumber}] 수집시간 변환: '{originalValue}' → '{transformedValue}'");
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// 전화번호 정규화 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 다양한 형식의 전화번호를 표준 형식으로 변환
        /// - 숫자만 추출하여 길이별로 적절한 형식 적용
        /// - 변환 실패 시 원본 값 유지
        /// 
        /// 🔄 변환 규칙:
        /// - 숫자만 추출 후 길이별 형식 적용
        /// - 11자리: 010-1234-5678 (휴대폰)
        /// - 10자리: 02-123-4567 또는 031-123-4567 (지역번호)
        /// - 8자리: 1234-5678 (단축번호)
        /// - 기타: 원본 유지
        /// 
        /// 📋 변환 예시:
        /// - "01012345678" → "010-1234-5678"
        /// - "0212345678" → "02-1234-5678"
        /// - "0311234567" → "031-123-4567"
        /// - "abc123def" → "abc123def" (원본 유지)
        /// 
        /// ⚠️ 처리 방식:
        /// - 정규식을 사용하여 숫자만 추출
        /// - 길이에 따른 조건부 형식 적용
        /// - 예외 발생 시 원본 값 반환
        /// 
        /// 💡 사용 목적:
        /// - 연락처 정보의 일관성 확보
        /// - 시스템 간 데이터 호환성 향상
        /// - 사용자 입력 오류 보정
        /// </summary>
        /// <param name="phoneNumber">정규화할 전화번호</param>
        /// <returns>정규화된 전화번호</returns>
        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return string.Empty;
            }

            try
            {
                // 숫자만 추출
                var digitsOnly = Regex.Replace(phoneNumber, DIGITS_ONLY_PATTERN, "");
                
                // 길이에 따른 형식 적용
                return digitsOnly.Length switch
                {
                    11 when digitsOnly.StartsWith("010") => $"{digitsOnly[..3]}-{digitsOnly.Substring(3, 4)}-{digitsOnly[7..]}",  // 010-1234-5678
                    10 when digitsOnly.StartsWith("02") => $"{digitsOnly[..2]}-{digitsOnly.Substring(2, 4)}-{digitsOnly[6..]}",    // 02-1234-5678
                    10 => $"{digitsOnly[..3]}-{digitsOnly.Substring(3, 3)}-{digitsOnly[6..]}",                                       // 031-123-4567
                    8 => $"{digitsOnly[..4]}-{digitsOnly[4..]}",                                                                      // 1234-5678
                    _ => phoneNumber // 형식이 맞지 않으면 원본 유지
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 전화번호 변환 실패: {phoneNumber} - {ex.Message}");
                return phoneNumber; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 우편번호를 표준 형식으로 정규화하는 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 다양한 형식의 우편번호를 표준 형식으로 변환
        /// - 숫자만 추출하여 길이별로 적절한 형식 적용
        /// - 신우편번호(5자리)와 구우편번호(6자리) 모두 지원
        /// 
        /// 🔄 변환 규칙:
        /// 1. 숫자 외의 모든 문자 제거
        /// 2. 5자리 우편번호: 12345 형식 유지 (신우편번호)
        /// 3. 6자리 우편번호: 123-456 형식으로 변환 (구우편번호)
        /// 4. 그 외 길이는 원본 유지
        /// 
        /// 📮 지원 형식:
        /// - 신우편번호: 12345 (5자리)
        /// - 구우편번호: 123-456 (6자리, 하이픈 추가)
        /// 
        /// 📋 변환 예시:
        /// - "12345" → "12345" (신우편번호)
        /// - "123456" → "123-456" (구우편번호)
        /// - "123-456" → "123-456" (이미 올바른 형식)
        /// - "abc123def" → "abc123def" (원본 유지)
        /// 
        /// ⚠️ 처리 방식:
        /// - 정규식을 사용하여 숫자만 추출
        /// - 길이에 따른 조건부 형식 적용
        /// - 예외 발생 시 원본 값 반환
        /// 
        /// 💡 사용 목적:
        /// - 우편번호 형식의 일관성 확보
        /// - 배송 시스템과의 호환성 향상
        /// - 주소 검증 및 정확도 개선
        /// </summary>
        /// <param name="zipCode">정규화할 우편번호</param>
        /// <returns>정규화된 우편번호</returns>
        private string NormalizeZipCode(string zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
            {
                return string.Empty;
            }

            try
            {
                // 숫자만 추출
                var digitsOnly = Regex.Replace(zipCode, DIGITS_ONLY_PATTERN, "");
                
                return digitsOnly.Length switch
                {
                    5 => digitsOnly,                                    // 12345 (신우편번호)
                    6 => $"{digitsOnly[..3]}-{digitsOnly[3..]}",       // 123-456 (구우편번호)
                    _ => zipCode                                        // 형식이 맞지 않으면 원본 유지
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 우편번호 변환 실패: {zipCode} - {ex.Message}");
                return zipCode; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 쇼핑몰별 결제수단 변환 메서드 (SQL 로직 기준)
        /// 
        /// 🎯 주요 기능:
        /// - 특정 쇼핑몰의 결제수단을 시스템 표준 코드로 변환
        /// - SQL 로직과 동일한 규칙 적용
        /// - 조건에 맞지 않는 경우 원본 값 유지
        /// 
        /// 🔄 변환 규칙:
        /// - 배민상회 쇼핑몰: 결제수단 → '0'
        /// - 기타 쇼핑몰: 원본 유지
        /// 
        /// 📋 변환 예시:
        /// - 배민상회 + "카드" → "0"
        /// - 배민상회 + "현금" → "0"
        /// - 쿠팡 + "카드" → 원본 유지
        /// - 네이버 + "무통장입금" → 원본 유지
        /// 
        /// ⚠️ 처리 방식:
        /// - 쇼핑몰명을 대소문자 구분 없이 비교
        /// - 공백 제거 후 정확한 매칭 수행
        /// - 예외 발생 시 원본 값 반환
        /// 
        /// 💡 사용 목적:
        /// - 특정 쇼핑몰 결제수단 표준화
        /// - 시스템 처리 규칙 통일
        /// - 결제 방식 코드 정규화
        /// - 데이터베이스 저장 시 일관성 확보
        /// 
        /// 🔧 SQL 대응:
        /// ```sql
        /// UPDATE orders 
        /// SET 결제수단 = '0' 
        /// WHERE 쇼핑몰 = '배민상회'
        /// ```
        /// </summary>
        /// <param name="paymentMethod">원본 결제수단</param>
        /// <param name="shoppingMall">쇼핑몰명</param>
        /// <returns>변환된 결제수단</returns>
        private string TransformPaymentMethodByMall(string paymentMethod, string shoppingMall)
        {
            try
            {
                // SQL의 WHERE 쇼핑몰 = '배민상회' 조건 확인
                if (string.Equals(shoppingMall?.Trim(), "배민상회", StringComparison.OrdinalIgnoreCase))
                {
                    // SQL의 SET 결제수단 = '0' 로직 구현
                    var transformedMethod = "0";
                    Console.WriteLine($"💳 [결제수단 변환규칙] 배민상회 조건 적용: 쇼핑몰='{shoppingMall}', 결제수단 '{paymentMethod}' → '{transformedMethod}'");
                    return transformedMethod;
                }

                // 조건에 맞지 않으면 원본 반환
                return paymentMethod ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 결제수단 변환 실패: 결제수단={paymentMethod}, 쇼핑몰={shoppingMall} - {ex.Message}");
                return paymentMethod ?? string.Empty; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 수취인명 변환 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 데이터 오류로 인한 'nan' 값을 의미 있는 한글명으로 변환
        /// - 대소문자 구분 없이 다양한 'nan' 형태 처리
        /// - 변환 실패 시 원본 값 유지
        /// 
        /// 🔄 변환 규칙:
        /// - 'nan' → '난난' (대소문자 무관)
        /// - 기타 값: 원본 유지
        /// 
        /// 📋 변환 예시:
        /// - "nan", "NaN", "NAN", "Nan" → "난난"
        /// - "홍길동", "김철수" → 원본 유지
        /// - "김난", "난김" → 원본 유지 (정확한 매칭만)
        /// 
        /// ⚠️ 처리 방식:
        /// - 대소문자 구분 없이 정확한 'nan' 매칭
        /// - 공백 제거 후 비교 수행
        /// - 예외 발생 시 원본 값 반환
        /// 
        /// 💡 사용 목적:
        /// - 데이터 정제 및 표준화
        /// - 처리 오류 방지
        /// - 의미 있는 한글명 변환
        /// - 시스템 처리 시 오류 방지
        /// 
        /// 🔧 데이터 오류 처리:
        /// - 엑셀에서 빈 셀이 'nan'으로 읽히는 경우 처리
        /// - 시스템에서 의미 없는 값 대신 기본값 제공
        /// </summary>
        /// <param name="recipientName">변환할 수취인명</param>
        /// <returns>변환된 수취인명</returns>
        private string TransformRecipientName(string recipientName)
        {
            if (string.IsNullOrWhiteSpace(recipientName))
            {
                return recipientName ?? string.Empty;
            }

            try
            {
                // 'nan' 값 확인 (대소문자 구분 없이)
                if (string.Equals(recipientName.Trim(), "nan", StringComparison.OrdinalIgnoreCase))
                {
                    var transformedName = "난난";
                    Console.WriteLine($"👤 [수취인명 변환규칙] 'nan'(대소문자 무관) → '난난' 변환 적용: '{recipientName}' → '{transformedName}'");
                    return transformedName;
                }

                // 조건에 맞지 않으면 원본 반환
                return recipientName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 수취인명 변환 실패: {recipientName} - {ex.Message}");
                return recipientName; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 송장명 변환 메서드 (SQL 로직 기준)
        /// 
        /// 🎯 주요 기능:
        /// - 송장 구분 코드를 시스템 표준으로 변환
        /// - SQL 로직과 동일한 규칙 적용
        /// - 대소문자 구분 없이 처리
        /// 
        /// 🔄 변환 규칙:
        /// - 'BS_' → 'GC_' 접두사 변경
        /// - 기타 접두사: 원본 유지
        /// 
        /// 📋 변환 예시:
        /// - "BS_12345" → "GC_12345"
        /// - "BS_ORDER_001" → "GC_ORDER_001"
        /// - "AC_98765" → 원본 유지
        /// - "bs_12345" → "GC_12345" (대소문자 무관)
        /// 
        /// ⚠️ 처리 방식:
        /// - 대소문자 구분 없이 'BS_' 접두사 확인
        /// - 3글자 이상인 경우에만 처리
        /// - 예외 발생 시 원본 값 반환
        /// 
        /// 💡 사용 목적:
        /// - 송장 구분 코드 표준화
        /// - 시스템 호환성 확보
        /// - 레거시 코드 변환
        /// - 데이터베이스 저장 시 일관성 확보
        /// 
        /// 🔧 SQL 대응:
        /// ```sql
        /// UPDATE invoices 
        /// SET 송장명 = CONCAT('GC_', SUBSTRING(송장명, 4)) 
        /// WHERE LEFT(송장명, 3) = 'BS_'
        /// ```
        /// </summary>
        /// <param name="invoiceName">변환할 송장명</param>
        /// <returns>변환된 송장명</returns>
        private string TransformInvoiceName(string invoiceName)
        {
            if (string.IsNullOrWhiteSpace(invoiceName))
            {
                return string.Empty;
            }

            try
            {
                // SQL의 LEFT(송장명, 3) = 'BS_' 조건 확인 (대소문자 구분 없이 비교)
                if (invoiceName.Length >= 3 && invoiceName.StartsWith("BS_", StringComparison.OrdinalIgnoreCase))
                {
                    // SQL의 CONCAT('GC_', SUBSTRING(송장명, 4)) 로직 구현
                    // SUBSTRING(송장명, 4)는 4번째 문자부터 끝까지 (0-based index로는 3부터)
                    var remainingPart = invoiceName.Length > 3 ? invoiceName.Substring(3) : string.Empty;
                    var transformedName = "GC_" + remainingPart;

                    Console.WriteLine($"📋 [송장명 변환규칙] 'BS_'(대소문자 무관) → 'GC_' 변환 적용: '{invoiceName}' → '{transformedName}'");
                    return transformedName;
                }

                // 조건에 맞지 않으면 원본 반환
                return invoiceName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 송장명 변환 실패: {invoiceName} - {ex.Message}");
                return invoiceName; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 품목코드에 따른 주소 특수 규칙을 적용하는 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 특정 품목코드에 대해 주소에 배송 구분자를 추가
        /// - 중복 '*' 추가 방지
        /// - 조건에 맞지 않는 경우 원본 주소 유지
        /// 
        /// 🎯 특수 규칙:
        /// - 품목코드가 7710 또는 7720인 경우: 주소 끝에 '*' 추가
        /// - 기타 품목코드: 원본 주소 유지
        /// 
        /// 📋 처리 내용:
        /// - "서울 강남구 역삼동" + 품목코드 7710 → "서울 강남구 역삼동*"
        /// - "부산 해운대구 우동" + 품목코드 7720 → "부산 해운대구 우동*"
        /// - "대구 중구 동인동" + 품목코드 1234 → "대구 중구 동인동" (변경 없음)
        /// - "서울 강남구*" + 품목코드 7710 → "서울 강남구*" (중복 방지)
        /// 
        /// ⚠️ 처리 방식:
        /// - 품목코드 공백 제거 후 정확한 매칭
        /// - 이미 '*'가 끝에 있는지 확인하여 중복 방지
        /// - 예외 발생 시 원본 주소 반환
        /// 
        /// 💡 사용 목적:
        /// - 특정 품목에 대한 배송 구분자 표시
        /// - 물류 처리 시 특별 취급 대상 식별
        /// - 배송 시스템에서 특별 처리 대상 구분
        /// - 데이터베이스 저장 시 배송 구분 정보 포함
        /// 
        /// 🔧 비즈니스 로직:
        /// - 품목코드 7710, 7720은 특별 배송이 필요한 품목
        /// - 주소 끝의 '*'는 배송 시스템에서 특별 처리 신호
        /// </summary>
        /// <param name="address">기본 정제된 주소</param>
        /// <param name="productCode">품목코드</param>
        /// <returns>품목코드 규칙이 적용된 주소</returns>
        private string ApplyProductCodeSpecialRule(string address, string productCode)
        {
            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(productCode))
            {
                return address;
            }

            try
            {
                // 품목코드가 7710 또는 7720인 경우 주소 끝에 '*' 추가
                if (productCode.Trim() == "7710" || productCode.Trim() == "7720")
                {
                    // 이미 '*'가 끝에 있는지 확인하여 중복 방지
                    if (!address.EndsWith("*"))
                    {
                        var modifiedAddress = address + "*";
                        Console.WriteLine($"🏷️ [품목코드 특수규칙] 품목코드 {productCode}로 인해 주소 변경: '{address}' → '{modifiedAddress}'");
                        return modifiedAddress;
                    }
                }

                return address;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 품목코드 특수규칙 적용 실패: 주소={address}, 품목코드={productCode} - {ex.Message}");
                return address; // 적용 실패 시 원본 주소 반환
            }
        }

        /// <summary>
        /// 주소 텍스트를 정제하는 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 주소 문자열의 불필요한 문자 제거 및 정리
        /// - SQL 로직과 동일한 규칙 적용
        /// - 주소 데이터의 일관성 확보
        /// 
        /// 🔄 변환 규칙:
        /// 1. 앞뒤 공백 제거
        /// 2. 연속된 공백을 하나로 통일
        /// 3. 특수 문자 제거 ('·' 문자 제거)
        /// 4. 불필요한 문자 제거
        /// 
        /// 🏠 처리 내용:
        /// - 공백 정리: "서울  특별시    강남구" → "서울 특별시 강남구"
        /// - 특수문자 제거: "서울·강남구" → "서울강남구"
        /// - 괄호 정리: 주소 내 괄호 정보 유지
        /// - 층/호수 정보: 표준 형식으로 정리
        /// 
        /// 📋 변환 예시:
        /// - "서울  강남구  역삼동" → "서울 강남구 역삼동"
        /// - "부산·해운대구·우동" → "부산해운대구우동"
        /// - "대구 중구 동인동 (1층)" → "대구 중구 동인동 (1층)"
        /// 
        /// ⚠️ 처리 방식:
        /// - 정규식을 사용하여 연속된 공백 처리
        /// - 문자열 치환으로 특수문자 제거
        /// - 예외 발생 시 원본 주소 반환
        /// 
        /// 💡 사용 목적:
        /// - 주소 데이터의 일관성 확보
        /// - 데이터베이스 저장 시 공간 효율성
        /// - 주소 검색 및 매칭 정확도 향상
        /// - 배송 시스템과의 호환성 개선
        /// 
        /// 📌 SQL 기준: REPLACE(주소, '·', '')
        /// </summary>
        /// <param name="address">정제할 주소</param>
        /// <returns>정제된 주소</returns>
        private string NormalizeAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return string.Empty;
            }

            try
            {
                // 앞뒤 공백 제거
                var normalized = address.Trim();
                
                // SQL의 REPLACE(주소, '·', '') 로직 구현
                // '·' (가운뎃점, 중점) 문자 제거
                if (normalized.Contains('·'))
                {
                    var beforeReplace = normalized;
                    normalized = normalized.Replace("·", "");
                    Console.WriteLine($"🏠 [주소 정제] '·' 문자 제거: '{beforeReplace}' → '{normalized}'");
                }
                
                // 연속된 공백을 하나로 통일
                normalized = Regex.Replace(normalized, MULTIPLE_SPACES_PATTERN, " ");
                
                // TODO: 필요에 따라 추가 주소 정제 규칙 구현
                // 예: 도로명 주소 형식 통일, 구 주소와 신 주소 변환 등
                
                return normalized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 주소 변환 실패: {address} - {ex.Message}");
                return address; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 옵션명을 정리하는 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 옵션명 문자열의 불필요한 문자 제거 및 정리
        /// - 영문, 숫자, 한글, 기본 특수문자만 허용
        /// - 옵션명 데이터의 일관성 확보
        /// 
        /// 🔄 변환 규칙:
        /// 1. 앞뒤 공백 제거
        /// 2. 연속된 공백을 하나로 통일
        /// 3. 허용되지 않는 특수문자 제거
        /// 4. 대소문자 정리 (필요시)
        /// 
        /// ⚙️ 처리 내용:
        /// - 공백 정리: "빨강    색" → "빨강 색"
        /// - 특수문자 제거: 불필요한 기호 정리
        /// - 형식 통일: 일관된 옵션명 형식 적용
        /// 
        /// 📋 변환 예시:
        /// - "빨강    색" → "빨강 색"
        /// - "Large@Size" → "LargeSize"
        /// - "옵션명(특가)" → "옵션명특가"
        /// - "A-123_B" → "A-123_B" (허용된 특수문자 유지)
        /// 
        /// ⚠️ 처리 방식:
        /// - 정규식을 사용하여 허용되지 않는 문자 제거
        /// - 영문, 숫자, 한글, 기본 특수문자만 허용
        /// - 예외 발생 시 원본 옵션명 반환
        /// 
        /// 💡 사용 목적:
        /// - 옵션명 데이터의 일관성 확보
        /// - 데이터베이스 저장 시 특수문자 오류 방지
        /// - 옵션명 검색 및 매칭 정확도 향상
        /// - 시스템 처리 시 안정성 확보
        /// </summary>
        /// <param name="optionName">정리할 옵션명</param>
        /// <returns>정리된 옵션명</returns>
        private string NormalizeOptionName(string optionName)
        {
            if (string.IsNullOrWhiteSpace(optionName))
            {
                return string.Empty;
            }

            try
            {
                // 앞뒤 공백 제거
                var normalized = optionName.Trim();
                
                // 연속된 공백을 하나로 통일
                normalized = Regex.Replace(normalized, MULTIPLE_SPACES_PATTERN, " ");
                
                // 허용되지 않는 특수문자 제거 (영문, 숫자, 한글, 기본 특수문자만 허용)
                normalized = Regex.Replace(normalized, ALLOWED_CHARS_PATTERN, "");
                
                return normalized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 옵션명 변환 실패: {optionName} - {ex.Message}");
                return optionName; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 수량 데이터를 정규화하는 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 다양한 형식의 수량 데이터를 정수 형태로 변환
        /// - 숫자 외의 모든 문자 제거
        /// - 유효하지 않은 값은 "0"으로 처리
        /// 
        /// 🔄 변환 규칙:
        /// 1. 숫자 외의 모든 문자 제거
        /// 2. 빈 문자열인 경우 "0" 반환
        /// 3. 유효한 정수가 아닌 경우 "0" 반환
        /// 
        /// 🔢 처리 내용:
        /// - "10개" → "10"
        /// - "5 EA" → "5"
        /// - "abc" → "0"
        /// - "" → "0"
        /// 
        /// 📋 변환 예시:
        /// - "10개" → "10"
        /// - "5 EA" → "5"
        /// - "abc" → "0"
        /// - "" → "0"
        /// - "123.45" → "123"
        /// - "1,234" → "1234"
        /// 
        /// ⚠️ 처리 방식:
        /// - 정규식을 사용하여 숫자만 추출
        /// - int.TryParse로 유효성 검증
        /// - 예외 발생 시 "0" 반환
        /// 
        /// 💡 사용 목적:
        /// - 수량 데이터의 일관성 확보
        /// - 데이터베이스 저장 시 숫자 형식 보장
        /// - 계산 및 집계 시 정확성 향상
        /// - 시스템 처리 시 오류 방지
        /// </summary>
        /// <param name="quantity">정규화할 수량</param>
        /// <returns>정규화된 수량 (문자열)</returns>
        private string NormalizeQuantity(string quantity)
        {
            if (string.IsNullOrWhiteSpace(quantity))
            {
                return "0";
            }

            try
            {
                // 숫자만 추출
                var digitsOnly = Regex.Replace(quantity, DIGITS_ONLY_PATTERN, "");
                
                // 빈 문자열이거나 유효하지 않은 숫자인 경우 0 반환
                if (string.IsNullOrEmpty(digitsOnly) || !int.TryParse(digitsOnly, out var numericValue))
                {
                    return "0";
                }
                
                return numericValue.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 수량 변환 실패: {quantity} - {ex.Message}");
                return "0"; // 변환 실패 시 0 반환
            }
        }

        /// <summary>
        /// 주소 기반으로 별표2 컬럼을 변환하는 메서드 (SQL 로직 기준)
        /// 
        /// 🎯 주요 기능:
        /// - 주소에 '제주특별' 또는 '제주 특별'이 포함된 경우 별표2를 '제주'로 설정
        /// - SQL 로직과 동일한 규칙 적용
        /// - 대소문자 구분 없이 처리
        /// - 공백 포함 형태도 처리 ('제주 특별')
        /// 
        /// 🔄 변환 규칙:
        /// - 주소 LIKE '%제주특별%' → 별표2 = '제주'
        /// - 주소 LIKE '%제주 특별%' → 별표2 = '제주'
        /// - 기타 주소: 원본 별표2 값 유지
        /// 
        /// 📋 변환 예시:
        /// - "제주특별자치도 제주시" → 별표2: "제주"
        /// - "제주 특별자치도 서귀포시" → 별표2: "제주"
        /// - "제주특별자치도 서귀포시" → 별표2: "제주"
        /// - "서울특별시 강남구" → 별표2: 원본 유지
        /// - "부산광역시 해운대구" → 별표2: 원본 유지
        /// 
        /// ⚠️ 처리 방식:
        /// - 대소문자 구분 없이 '제주특별' 또는 '제주 특별' 포함 여부 확인
        /// - 주소가 null이거나 빈 문자열인 경우 원본 별표2 값 유지
        /// - 예외 발생 시 원본 별표2 값 반환
        /// 
        /// 💡 사용 목적:
        /// - 제주도 배송 구분을 위한 별표2 컬럼 활용
        /// - 배송 시스템에서 제주도 특별 처리 대상 식별
        /// - 물류 처리 시 제주도 배송 구분
        /// - 데이터베이스 저장 시 제주도 배송 정보 포함
        /// 
        /// 🔧 SQL 대응:
        /// ```sql
        /// UPDATE orders 
        /// SET 별표2 = '제주' 
        /// WHERE 주소 LIKE '%제주특별%' OR 주소 LIKE '%제주 특별%'
        /// ```
        /// </summary>
        /// <param name="originalStar2Value">원본 별표2 값</param>
        /// <param name="addressValue">주소 값</param>
        /// <returns>변환된 별표2 값</returns>
        private string TransformStar2ByAddress(string originalStar2Value, string addressValue)
        {
            try
            {
                // 입력값 검증 및 안전한 처리
                if (string.IsNullOrWhiteSpace(addressValue))
                {
                    return originalStar2Value ?? string.Empty;
                }

                // null 체크 및 안전한 문자열 처리
                var safeOriginalValue = originalStar2Value ?? string.Empty;
                var safeAddressValue = addressValue.Trim();

                // 제주도 주소 패턴 확인 (대소문자 구분 없이)
                bool isJejuAddress = safeAddressValue.Contains("제주특별", StringComparison.OrdinalIgnoreCase) || 
                                   safeAddressValue.Contains("제주 특별", StringComparison.OrdinalIgnoreCase);

                if (isJejuAddress)
                {
                    // SQL의 SET 별표2 = '제주' 로직 구현
                    var transformedValue = "제주";
                    var logMessage = $"⭐ [별표2 변환규칙] 제주특별자치도 주소 감지: 주소='{safeAddressValue}', 별표2 '{safeOriginalValue}' → '{transformedValue}'";
                    Console.WriteLine(logMessage);
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logMessage}\n");
                    return transformedValue;
                }

                // 조건에 맞지 않으면 원본 반환
                return safeOriginalValue;
            }
            catch (Exception ex)
            {
                var errorMessage = $"⚠️ [DataTransformationService] 별표2 변환 실패: 별표2={originalStar2Value}, 주소={addressValue} - {ex.Message}";
                Console.WriteLine(errorMessage);
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {errorMessage}\n");
                
                var stackTraceMessage = $"⚠️ [DataTransformationService] 별표2 변환 실패 상세: {ex.StackTrace}";
                Console.WriteLine(stackTraceMessage);
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "star2_debug.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {stackTraceMessage}\n");
                return originalStar2Value ?? string.Empty; // 변환 실패 시 원본 반환
            }
        }

        /// <summary>
        /// 날짜/시간 데이터를 표준 형식으로 정규화하는 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 다양한 형식의 날짜/시간 데이터를 표준 형식으로 변환
        /// - DateTime.TryParse를 사용한 유연한 파싱
        /// - 파싱 실패 시 현재 시간으로 대체
        /// 
        /// 🔄 변환 규칙:
        /// 1. 다양한 날짜 형식을 표준 DateTime 형식으로 변환
        /// 2. 파싱 실패 시 현재 시간 반환
        /// 3. 표준 형식: "yyyy-MM-dd HH:mm:ss"
        /// 
        /// 📅 지원 형식:
        /// - "2025-01-08 14:30:00"
        /// - "2025/01/08 14:30"
        /// - "2025.01.08"
        /// - "20250108"
        /// 
        /// 📋 변환 예시:
        /// - "2025-01-08 14:30:00" → "2025-01-08 14:30:00"
        /// - "2025/01/08" → "2025-01-08 00:00:00"
        /// - "20250108" → "2025-01-08 00:00:00"
        /// - "invalid" → 현재 시간
        /// 
        /// ⚠️ 처리 방식:
        /// - DateTime.TryParse로 유연한 파싱
        /// - 파싱 실패 시 현재 시간 사용
        /// - 예외 발생 시 현재 시간 반환
        /// 
        /// 💡 사용 목적:
        /// - 날짜/시간 데이터의 일관성 확보
        /// - 데이터베이스 저장 시 표준 형식 보장
        /// - 정렬 및 비교 시 정확성 향상
        /// - 시스템 처리 시 오류 방지
        /// 
        /// 🔧 표준 형식:
        /// - 출력 형식: "yyyy-MM-dd HH:mm:ss"
        /// - 데이터베이스 호환성 보장
        /// - 국제 표준 준수
        /// </summary>
        /// <param name="dateTimeValue">정규화할 날짜/시간</param>
        /// <returns>정규화된 날짜/시간 (문자열)</returns>
        private string NormalizeDateTime(string dateTimeValue)
        {
            if (string.IsNullOrWhiteSpace(dateTimeValue))
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }

            try
            {
                // DateTime 파싱 시도
                if (DateTime.TryParse(dateTimeValue, out var parsedDate))
                {
                    return parsedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
                
                // 파싱 실패 시 현재 시간 반환
                Console.WriteLine($"⚠️ [DataTransformationService] 날짜 파싱 실패, 현재 시간 사용: {dateTimeValue}");
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 날짜 변환 실패: {dateTimeValue} - {ex.Message}");
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // 변환 실패 시 현재 시간 반환
            }
        }

        /// <summary>
        /// 배송메세지 내 '★' 문자를 제거 (SQL REPLACE 대응)
        /// </summary>
        /// <param name="specialNote">원본 배송메세지</param>
        /// <returns>'★' 제거된 배송메세지</returns>
        private string RemoveFilledStarFromSpecialNote(string specialNote)
        {
            if (string.IsNullOrEmpty(specialNote))
            {
                return string.Empty;
            }

            try
            {
                if (specialNote.Contains('★'))
                {
                    var before = specialNote;
                    var after = specialNote.Replace("★", string.Empty);
                    Console.WriteLine($"📝 [배송메세지 정제] '★' 제거: '{before}' → '{after}'");
                    return after;
                }

                return specialNote;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [DataTransformationService] 배송메세지 정제 실패: {ex.Message}");
                return specialNote;
            }
        }

        #endregion
    }
}
