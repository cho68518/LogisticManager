using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LogisticManager.Constants;

namespace LogisticManager.Services
{
    /// <summary>
    /// 설정 파일 검증을 담당하는 서비스 클래스
    /// 
    /// 주요 기능:
    /// - 데이터베이스 설정값 유효성 검사
    /// - 포트 번호 유효성 검사
    /// - 서버 주소 형식 검사
    /// - 사용자 친화적인 검증 결과 메시지 제공
    /// </summary>
    public static class SettingsValidationService
    {
        #region 데이터베이스 설정 검증

        /// <summary>
        /// 데이터베이스 설정 전체를 검증하는 메서드
        /// </summary>
        /// <param name="server">서버 주소</param>
        /// <param name="database">데이터베이스 이름</param>
        /// <param name="user">사용자명</param>
        /// <param name="password">비밀번호</param>
        /// <param name="port">포트 번호</param>
        /// <returns>검증 결과 (성공 여부, 메시지 목록)</returns>
        public static (bool isValid, List<string> messages) ValidateDatabaseSettings(
            string server, string database, string user, string password, string port)
        {
            var messages = new List<string>();
            var isValid = true;

            // 1단계: 필수값 존재 여부 검증 (가장 중요)
            if (string.IsNullOrWhiteSpace(server))
            {
                isValid = false;
                messages.Add("❌ DB_SERVER는 필수 설정값입니다.");
            }
            
            if (string.IsNullOrWhiteSpace(database))
            {
                isValid = false;
                messages.Add("❌ DB_NAME은 필수 설정값입니다.");
            }
            
            if (string.IsNullOrWhiteSpace(user))
            {
                isValid = false;
                messages.Add("❌ DB_USER는 필수 설정값입니다.");
            }
            
            if (string.IsNullOrEmpty(password))
            {
                isValid = false;
                messages.Add("❌ DB_PASSWORD는 필수 설정값입니다.");
            }
            
            if (string.IsNullOrWhiteSpace(port))
            {
                isValid = false;
                messages.Add("❌ DB_PORT는 필수 설정값입니다.");
            }

            // 2단계: 필수값이 모두 있는 경우에만 상세 검증 수행
            if (isValid)
            {
                // 서버 주소 검증
                var (serverValid, serverMessage) = ValidateServerAddress(server);
                if (!serverValid)
                {
                    isValid = false;
                    messages.Add(serverMessage);
                }

                // 데이터베이스 이름 검증
                var (databaseValid, databaseMessage) = ValidateDatabaseName(database);
                if (!databaseValid)
                {
                    isValid = false;
                    messages.Add(databaseMessage);
                }

                // 사용자명 검증
                var (userValid, userMessage) = ValidateUserName(user);
                if (!userValid)
                {
                    isValid = false;
                    messages.Add(userMessage);
                }

                // 비밀번호 검증
                var (passwordValid, passwordMessage) = ValidatePassword(password);
                if (!passwordValid)
                {
                    isValid = false;
                    messages.Add(passwordMessage);
                }

                // 포트 번호 검증
                var (portValid, portMessage) = ValidatePortNumber(port);
                if (!portValid)
                {
                    isValid = false;
                    messages.Add(portMessage);
                }
            }

            return (isValid, messages);
        }

        /// <summary>
        /// 서버 주소를 검증하는 메서드
        /// </summary>
        /// <param name="server">서버 주소</param>
        /// <returns>검증 결과 (성공 여부, 메시지)</returns>
        public static (bool isValid, string message) ValidateServerAddress(string server)
        {
            if (string.IsNullOrWhiteSpace(server))
            {
                return (false, "❌ 서버 주소가 비어있습니다.");
            }

            if (server.Length < DatabaseConstants.MIN_SERVER_LENGTH)
            {
                return (false, $"❌ 서버 주소가 너무 짧습니다. (최소 {DatabaseConstants.MIN_SERVER_LENGTH}자 필요)");
            }

            if (server.Length > DatabaseConstants.MAX_SERVER_LENGTH)
            {
                return (false, $"❌ 서버 주소가 너무 깁니다. (최대 {DatabaseConstants.MAX_SERVER_LENGTH}자)");
            }

            // 서버 주소 형식 검사 (도메인 또는 IP 주소)
            if (!IsValidServerAddress(server))
            {
                return (false, "❌ 서버 주소 형식이 올바르지 않습니다. (예: example.com 또는 192.168.1.1)");
            }

            return (true, "✅ 서버 주소가 올바릅니다.");
        }

        /// <summary>
        /// 데이터베이스 이름을 검증하는 메서드
        /// </summary>
        /// <param name="database">데이터베이스 이름</param>
        /// <returns>검증 결과 (성공 여부, 메시지)</returns>
        public static (bool isValid, string message) ValidateDatabaseName(string database)
        {
            if (string.IsNullOrWhiteSpace(database))
            {
                return (false, "❌ 데이터베이스 이름이 비어있습니다.");
            }

            if (database.Length < DatabaseConstants.MIN_DATABASE_LENGTH)
            {
                return (false, $"❌ 데이터베이스 이름이 너무 짧습니다. (최소 {DatabaseConstants.MIN_DATABASE_LENGTH}자 필요)");
            }

            if (database.Length > DatabaseConstants.MAX_DATABASE_LENGTH)
            {
                return (false, $"❌ 데이터베이스 이름이 너무 깁니다. (최대 {DatabaseConstants.MAX_DATABASE_LENGTH}자)");
            }

            // MySQL 데이터베이스 이름 규칙 검사
            if (!IsValidDatabaseName(database))
            {
                return (false, "❌ 데이터베이스 이름에 허용되지 않는 문자가 포함되어 있습니다. (영문자, 숫자, 언더스코어만 허용)");
            }

            return (true, "✅ 데이터베이스 이름이 올바릅니다.");
        }

        /// <summary>
        /// 사용자명을 검증하는 메서드
        /// </summary>
        /// <param name="user">사용자명</param>
        /// <returns>검증 결과 (성공 여부, 메시지)</returns>
        public static (bool isValid, string message) ValidateUserName(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                return (false, "❌ 사용자명이 비어있습니다.");
            }

            if (user.Length < DatabaseConstants.MIN_USER_LENGTH)
            {
                return (false, $"❌ 사용자명이 너무 짧습니다. (최소 {DatabaseConstants.MIN_USER_LENGTH}자 필요)");
            }

            if (user.Length > DatabaseConstants.MAX_USER_LENGTH)
            {
                return (false, $"❌ 사용자명이 너무 깁니다. (최대 {DatabaseConstants.MAX_USER_LENGTH}자)");
            }

            // MySQL 사용자명 규칙 검사
            if (!IsValidUserName(user))
            {
                return (false, "❌ 사용자명에 허용되지 않는 문자가 포함되어 있습니다. (영문자, 숫자, 언더스코어만 허용)");
            }

            return (true, "✅ 사용자명이 올바릅니다.");
        }

        /// <summary>
        /// 비밀번호를 검증하는 메서드
        /// </summary>
        /// <param name="password">비밀번호</param>
        /// <returns>검증 결과 (성공 여부, 메시지)</returns>
        public static (bool isValid, string message) ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return (false, "❌ 비밀번호가 비어있습니다.");
            }

            if (password.Length < DatabaseConstants.MIN_PASSWORD_LENGTH)
            {
                return (false, $"❌ 비밀번호가 너무 짧습니다. (최소 {DatabaseConstants.MIN_PASSWORD_LENGTH}자 필요)");
            }

            if (password.Length > DatabaseConstants.MAX_PASSWORD_LENGTH)
            {
                return (false, $"❌ 비밀번호가 너무 깁니다. (최대 {DatabaseConstants.MAX_PASSWORD_LENGTH}자)");
            }

            return (true, "✅ 비밀번호가 올바릅니다.");
        }

        /// <summary>
        /// 포트 번호를 검증하는 메서드
        /// </summary>
        /// <param name="port">포트 번호</param>
        /// <returns>검증 결과 (성공 여부, 메시지)</returns>
        public static (bool isValid, string message) ValidatePortNumber(string port)
        {
            if (string.IsNullOrWhiteSpace(port))
            {
                return (false, "❌ 포트 번호가 비어있습니다.");
            }

            if (!int.TryParse(port, out int portNumber))
            {
                return (false, "❌ 포트 번호가 숫자가 아닙니다.");
            }

            if (portNumber < DatabaseConstants.MIN_PORT || portNumber > DatabaseConstants.MAX_PORT)
            {
                return (false, $"❌ 포트 번호가 범위를 벗어났습니다. ({DatabaseConstants.MIN_PORT}-{DatabaseConstants.MAX_PORT})");
            }

            // 일반적으로 사용되는 포트 번호 검사
            if (portNumber == 22 || portNumber == 23 || portNumber == 25 || portNumber == 53 || 
                portNumber == 80 || portNumber == 110 || portNumber == 143 || portNumber == 443)
            {
                return (false, $"⚠️ 포트 {portNumber}는 일반적으로 다른 서비스에서 사용됩니다. 확인이 필요합니다.");
            }

            return (true, "✅ 포트 번호가 올바릅니다.");
        }

        #endregion

        #region 형식 검증 헬퍼 메서드

        /// <summary>
        /// 서버 주소 형식이 유효한지 검사하는 메서드
        /// </summary>
        /// <param name="server">서버 주소</param>
        /// <returns>유효하면 true</returns>
        private static bool IsValidServerAddress(string server)
        {
            // 도메인 형식 검사 (예: example.com, sub.example.com)
            var domainPattern = @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$";
            
            // IP 주소 형식 검사 (예: 192.168.1.1, 10.0.0.1)
            var ipPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            
            // localhost 검사
            if (server.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Regex.IsMatch(server, domainPattern) || Regex.IsMatch(server, ipPattern);
        }

        /// <summary>
        /// 데이터베이스 이름이 유효한지 검사하는 메서드
        /// </summary>
        /// <param name="database">데이터베이스 이름</param>
        /// <returns>유효하면 true</returns>
        private static bool IsValidDatabaseName(string database)
        {
            // MySQL 데이터베이스 이름 규칙: 영문자, 숫자, 언더스코어만 허용
            var pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
            return Regex.IsMatch(database, pattern);
        }

        /// <summary>
        /// 사용자명이 유효한지 검사하는 메서드
        /// </summary>
        /// <param name="user">사용자명</param>
        /// <returns>유효하면 true</returns>
        private static bool IsValidUserName(string user)
        {
            // MySQL 사용자명 규칙: 영문자, 숫자, 언더스코어만 허용
            var pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
            return Regex.IsMatch(user, pattern);
        }

        #endregion

        #region 설정 파일 경로 검증

        /// <summary>
        /// 설정 파일 경로가 유효한지 검사하는 메서드
        /// </summary>
        /// <param name="filePath">설정 파일 경로</param>
        /// <returns>검증 결과 (성공 여부, 메시지)</returns>
        public static (bool isValid, string message) ValidateSettingsFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return (false, "❌ 설정 파일 경로가 비어있습니다.");
            }

            if (!System.IO.Path.IsPathRooted(filePath))
            {
                return (false, "❌ 설정 파일 경로가 절대 경로가 아닙니다.");
            }

            if (!System.IO.File.Exists(filePath))
            {
                return (false, $"❌ 설정 파일이 존재하지 않습니다: {filePath}");
            }

            try
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return (false, "❌ 설정 파일이 비어있습니다.");
                }

                if (fileInfo.Length > 1024 * 1024) // 1MB 제한
                {
                    return (false, "❌ 설정 파일이 너무 큽니다. (1MB 이하여야 함)");
                }
            }
            catch (Exception ex)
            {
                return (false, $"❌ 설정 파일 접근 중 오류가 발생했습니다: {ex.Message}");
            }

            return (true, "✅ 설정 파일 경로가 올바릅니다.");
        }

        #endregion

        #region 종합 검증 결과 메시지 생성

        /// <summary>
        /// 검증 결과를 종합하여 사용자 친화적인 메시지를 생성하는 메서드
        /// </summary>
        /// <param name="isValid">전체 검증 성공 여부</param>
        /// <param name="messages">개별 검증 메시지 목록</param>
        /// <returns>종합 검증 결과 메시지</returns>
        public static string GenerateValidationSummary(bool isValid, List<string> messages)
        {
            if (isValid)
            {
                return "✅ 모든 설정값이 올바릅니다!\n\n데이터베이스 연결을 시도할 수 있습니다.";
            }

            var summary = "❌ 설정값에 문제가 있습니다:\n\n";
            foreach (var message in messages)
            {
                summary += $"• {message}\n";
            }

            summary += "\n위 문제들을 해결한 후 다시 시도해주세요.";
            return summary;
        }

        #endregion
    }
}
