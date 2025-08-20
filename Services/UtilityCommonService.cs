using System;
using System.Text;

namespace LogisticManager.Services
{
    /// <summary>
    /// 공통 유틸리티 기능을 제공하는 서비스 클래스
    /// 
    /// 📋 주요 기능:
    /// - 문자열 처리 및 검증
    /// - 예외 처리 및 오류 메시지 생성
    /// - 공통 유틸리티 메서드들
    /// 
    /// 💡 사용법:
    /// var utilityService = new UtilityCommonService();
    /// var result = utilityService.TruncateAndPadRight("텍스트", 10);
    /// </summary>
    public class UtilityCommonService
    {
        /// <summary>
        /// 문자열을 지정된 길이로 자르고 오른쪽에 공백을 채우는 메서드
        /// </summary>
        /// <param name="input">입력 문자열</param>
        /// <param name="maxLength">최대 길이</param>
        /// <returns>처리된 문자열</returns>
        public string TruncateAndPadRight(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return new string(' ', maxLength);

            if (input.Length <= maxLength)
                return input.PadRight(maxLength);

            return input.Substring(0, maxLength);
        }

        /// <summary>
        /// 문자가 한글인지 확인하는 메서드
        /// </summary>
        /// <param name="c">확인할 문자</param>
        /// <returns>한글 여부</returns>
        public bool IsKoreanChar(char c)
        {
            return c >= 0xAC00 && c <= 0xD7A3;
        }

        /// <summary>
        /// 메시지를 안전하게 정리하는 공통 메서드
        /// 
        /// 🎯 주요 기능:
        /// - URL 제거 (http/https 링크)
        /// - 과도한 공백 정리
        /// - 특수 문자 및 제어 문자 제거
        /// - 안전한 로그 메시지 생성
        /// 
        /// 🔧 처리 과정:
        /// 1. null/빈 문자열 검증
        /// 2. URL 패턴 제거 (정규식)
        /// 3. 연속 공백 정리
        /// 4. 특수문자 필터링
        /// 
        /// 💡 사용 목적:
        /// - 로그 메시지 정제
        /// - 사용자 입력 정제
        /// - 안전한 문자열 처리
        /// 
        /// ⚠️ 처리 방식:
        /// - Regex를 사용한 URL 제거
        /// - 문자별 필터링으로 안전성 보장
        /// - 오류 발생 시 원본 메시지 반환
        /// </summary>
        /// <param name="message">정제할 원본 메시지</param>
        /// <returns>정제된 안전한 메시지</returns>
        public static string SanitizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            try
            {
                // 1단계: URL 제거 (http/https 링크)
                var sanitized = System.Text.RegularExpressions.Regex.Replace(message, @"https?://\S+", string.Empty);
                
                // 2단계: 과도한 공백 정리
                sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", " ").Trim();
                
                // 3단계: 특수 문자 및 제어 문자 제거 (안전한 문자만 허용)
                var result = new StringBuilder();
                foreach (char c in sanitized)
                {
                    if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                    {
                        result.Append(c);
                    }
                }

                return result.ToString();
            }
            catch (Exception)
            {
                // 오류 발생 시 원본 메시지 반환 (안전성 보장)
                return message;
            }
        }

        /// <summary>
        /// 예외를 분류하는 메서드 (InvoiceProcessor 호환성 개선)
        /// </summary>
        /// <param name="ex">분류할 예외</param>
        /// <returns>예외 분류</returns>
        public string ClassifyException(Exception ex)
        {
            return ex switch
            {
                FileNotFoundException => "파일 접근 오류",
                UnauthorizedAccessException => "권한 부족 오류",
                OutOfMemoryException => "메모리 부족 오류",
                TimeoutException => "시간 초과 오류",
                ArgumentException => "입력 데이터 오류",
                InvalidOperationException => "시스템 상태 오류",
                _ => "일반 시스템 오류"
            };
        }

        /// <summary>
        /// 예외의 심각도를 판단하는 메서드 (InvoiceProcessor 호환성 개선)
        /// </summary>
        /// <param name="ex">판단할 예외</param>
        /// <returns>심각도 수준</returns>
        public string DetermineErrorSeverity(Exception ex)
        {
            return ex switch
            {
                OutOfMemoryException => "심각 (Critical)",
                UnauthorizedAccessException => "높음 (High)",
                FileNotFoundException => "중간 (Medium)",
                ArgumentException => "낮음 (Low)",
                TimeoutException => "높음 (High)",
                InvalidOperationException => "중간 (Medium)",
                _ => "중간 (Medium)"
            };
        }

        /// <summary>
        /// 예외에 대한 복구 조치를 제안하는 메서드 (InvoiceProcessor 호환성 개선)
        /// </summary>
        /// <param name="ex">복구 조치를 제안할 예외</param>
        /// <returns>복구 조치 제안</returns>
        public string GetRecoveryAction(Exception ex)
        {
            return ex switch
            {
                FileNotFoundException => "파일 경로 확인 및 파일 존재 여부 검증",
                UnauthorizedAccessException => "실행 권한 확인 및 관리자 권한으로 재실행",
                OutOfMemoryException => "시스템 메모리 확인 및 불필요한 프로세스 종료",
                ArgumentException => "입력 데이터 형식 및 내용 검증",
                TimeoutException => "네트워크 상태 확인 및 재시도",
                InvalidOperationException => "시스템 상태 점검 및 재시도",
                _ => "시스템 상태 점검 및 재시도"
            };
        }

        /// <summary>
        /// 사용자 친화적인 오류 메시지를 생성하는 메서드 (InvoiceProcessor 호환성 개선)
        /// </summary>
        /// <param name="ex">오류 메시지를 생성할 예외</param>
        /// <param name="category">오류 카테고리</param>
        /// <returns>사용자 친화적인 오류 메시지</returns>
        public string GenerateUserFriendlyErrorMessage(Exception ex, string category)
        {
            var classification = ClassifyException(ex);
            var severity = DetermineErrorSeverity(ex);
            var recovery = GetRecoveryAction(ex);

            // InvoiceProcessor에서 사용하는 카테고리별 사용자 친화적 메시지
            var userFriendlyMessage = category switch
            {
                "파일 접근 오류" => "Excel 파일을 찾을 수 없거나 접근할 수 없습니다",
                "권한 부족 오류" => "파일이나 데이터베이스에 접근할 권한이 부족합니다",
                "메모리 부족 오류" => "처리할 데이터가 너무 많아 메모리가 부족합니다",
                "입력 데이터 오류" => "Excel 파일의 데이터 형식이 올바르지 않습니다",
                "시간 초과 오류" => "데이터 처리 시간이 초과되었습니다",
                "시스템 상태 오류" => "시스템 상태가 올바르지 않습니다",
                _ => "시스템 처리 중 예상치 못한 오류가 발생했습니다"
            };

            return $"📋 오류 카테고리: {category}\n" +
                   $"🔍 오류 유형: {classification}\n" +
                   $"⚠️ 심각도: {severity}\n" +
                   $"💡 복구 방법: {recovery}\n" +
                   $"📝 상세 내용: {userFriendlyMessage}\n" +
                   $"🔧 기술적 오류: {ex.Message}";
        }

        /// <summary>
        /// 예외 체인에서 근본 원인을 추출하는 메서드
        /// </summary>
        /// <param name="ex">분석할 예외</param>
        /// <returns>근본 원인 예외</returns>
        public Exception GetRootCause(Exception ex)
        {
            if (ex == null)
                return new InvalidOperationException("예외 객체가 null입니다.");

            var current = ex;
            while (current.InnerException != null)
                current = current.InnerException;
            return current;
        }

        /// <summary>
        /// 긴급 상황에서 시스템 정리 작업을 수행하는 공통 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 메모리 정리 (가비지 컬렉션)
        /// - 임시 파일 정리
        /// - 리소스 해제
        /// - 연결 종료
        /// 
        /// 🔧 처리 과정:
        /// 1. 메모리 압박 해소 (GC.Collect)
        /// 2. 임시 파일 정리 (선택적)
        /// 3. 시스템 리소스 정리
        /// 
        /// 💡 사용 목적:
        /// - 예외 발생 시 시스템 안정성 확보
        /// - 메모리 부족 상황 대응
        /// - 리소스 누수 방지
        /// 
        /// ⚠️ 주의사항:
        /// - 성능에 영향을 줄 수 있음
        /// - 긴급 상황에서만 사용 권장
        /// </summary>
        public void PerformEmergencyCleanup()
        {
            try
            {
                // 1단계: 메모리 정리 (가비지 컬렉션)
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // 2단계: 임시 파일 정리 (선택적)
                // var tempPath = Path.GetTempPath();
                // CleanupTempFiles(tempPath);
                
                // 3단계: 시스템 리소스 정리
                // 추가적인 정리 작업은 필요에 따라 구현
            }
            catch (Exception ex)
            {
                // 정리 작업 실패 시에도 시스템은 계속 동작해야 함
                // 로깅만 수행하고 예외를 전파하지 않음
                LogManagerService.LogWarning($"⚠️ 긴급 정리 작업 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 임시 파일 정리 메서드 (선택적 구현)
        /// </summary>
        /// <param name="tempPath">임시 파일 경로</param>
        private void CleanupTempFiles(string tempPath)
        {
            try
            {
                var tempFiles = Directory.GetFiles(tempPath, "*.tmp");
                foreach (var file in tempFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < DateTime.Now.AddHours(-1)) // 1시간 이상 된 파일만
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception)
                    {
                        // 개별 파일 삭제 실패는 무시하고 계속 진행
                    }
                }
            }
            catch (Exception)
            {
                // 임시 파일 정리 실패는 무시
            }
        }
    }
}
