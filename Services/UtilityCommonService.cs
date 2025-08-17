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
        /// 메시지를 안전하게 정리하는 메서드
        /// </summary>
        /// <param name="message">원본 메시지</param>
        /// <returns>정리된 메시지</returns>
        public static string SanitizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            // 특수 문자 및 제어 문자 제거
            var sanitized = new StringBuilder();
            foreach (char c in message)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                {
                    sanitized.Append(c);
                }
            }

            return sanitized.ToString();
        }

        /// <summary>
        /// 예외를 분류하는 메서드
        /// </summary>
        /// <param name="ex">분류할 예외</param>
        /// <returns>예외 분류</returns>
        public string ClassifyException(Exception ex)
        {
            return ex switch
            {
                ArgumentException => "매개변수 오류",
                InvalidOperationException => "잘못된 작업",
                TimeoutException => "시간 초과",
                UnauthorizedAccessException => "권한 없음",
                _ => "기타 오류"
            };
        }

        /// <summary>
        /// 예외의 심각도를 판단하는 메서드
        /// </summary>
        /// <param name="ex">판단할 예외</param>
        /// <returns>심각도 수준</returns>
        public string DetermineErrorSeverity(Exception ex)
        {
            return ex switch
            {
                ArgumentException => "낮음",
                InvalidOperationException => "보통",
                TimeoutException => "높음",
                UnauthorizedAccessException => "높음",
                _ => "보통"
            };
        }

        /// <summary>
        /// 예외에 대한 복구 조치를 제안하는 메서드
        /// </summary>
        /// <param name="ex">복구 조치를 제안할 예외</param>
        /// <returns>복구 조치 제안</returns>
        public string GetRecoveryAction(Exception ex)
        {
            return ex switch
            {
                ArgumentException => "매개변수를 확인하고 다시 시도하세요",
                InvalidOperationException => "작업 상태를 확인하고 다시 시도하세요",
                TimeoutException => "네트워크 상태를 확인하고 다시 시도하세요",
                UnauthorizedAccessException => "권한을 확인하고 다시 시도하세요",
                _ => "시스템을 다시 시작하고 다시 시도하세요"
            };
        }

        /// <summary>
        /// 사용자 친화적인 오류 메시지를 생성하는 메서드
        /// </summary>
        /// <param name="ex">오류 메시지를 생성할 예외</param>
        /// <param name="category">오류 카테고리</param>
        /// <returns>사용자 친화적인 오류 메시지</returns>
        public string GenerateUserFriendlyErrorMessage(Exception ex, string category)
        {
            var classification = ClassifyException(ex);
            var severity = DetermineErrorSeverity(ex);
            var recovery = GetRecoveryAction(ex);

            return $"📋 오류 카테고리: {category}\n" +
                   $"🔍 오류 유형: {classification}\n" +
                   $"⚠️ 심각도: {severity}\n" +
                   $"💡 복구 방법: {recovery}\n" +
                   $"📝 상세 내용: {ex.Message}";
        }
    }
}
