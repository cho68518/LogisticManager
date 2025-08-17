using System;
using System.IO;
using System.Text;
using System.Collections.Generic; // Added missing import for List

namespace LogisticManager.Services
{
    /// <summary>
    /// 공통 로깅 기능을 제공하는 서비스 클래스
    /// 
    /// 📋 주요 기능:
    /// - 로그 파일 쓰기 및 플러시
    /// - 다중 라인 로그 처리
    /// - 로그 파일 상태 진단
    /// 
    /// 💡 사용법:
    /// var loggingService = new LoggingCommonService();
    /// loggingService.WriteLogWithFlush("app.log", "로그 메시지");
    /// </summary>
    public class LoggingCommonService
    {
        private const string LOG_PATH = "app.log";
        private const string LOG_TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 로그를 파일에 쓰고 즉시 플러시하는 메서드
        /// </summary>
        /// <param name="logPath">로그 파일 경로</param>
        /// <param name="message">로그 메시지</param>
        public void WriteLogWithFlush(string logPath, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString(LOG_TIMESTAMP_FORMAT);
                var logEntry = $"[{timestamp}] {message}";

                // 로그 디렉토리 확인 및 생성
                var directoryPath = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 로그 파일에 쓰기 및 플러시
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
                
                Console.WriteLine($"📝 로그 기록 완료: {logPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 로그 기록 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 다중 라인 로그를 처리하여 파일에 쓰는 메서드
        /// </summary>
        /// <param name="logPath">로그 파일 경로</param>
        /// <param name="prefix">로그 접두사</param>
        /// <param name="message">로그 메시지</param>
        /// <param name="maxLineLength">최대 라인 길이 (기본값: 80)</param>
        public void WriteLogWithFlushMultiLine(string logPath, string prefix, string message, int maxLineLength = 80)
        {
            try
            {
                var timestamp = DateTime.Now.ToString(LOG_TIMESTAMP_FORMAT);
                var lines = SplitMessageIntoLines(message, maxLineLength);

                foreach (var line in lines)
                {
                    var logEntry = $"[{timestamp}] [{prefix}] {line}";
                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
                }

                Console.WriteLine($"📝 다중 라인 로그 기록 완료: {logPath} ({lines.Length}줄)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 다중 라인 로그 기록 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지를 지정된 길이로 라인을 나누는 메서드
        /// </summary>
        /// <param name="message">원본 메시지</param>
        /// <param name="maxLineLength">최대 라인 길이</param>
        /// <returns>분할된 라인 배열</returns>
        private string[] SplitMessageIntoLines(string message, int maxLineLength)
        {
            if (string.IsNullOrEmpty(message))
                return new string[0];

            if (message.Length <= maxLineLength)
                return new[] { message };

            var lines = new List<string>();
            var currentLine = new StringBuilder();

            foreach (var word in message.Split(' '))
            {
                if (currentLine.Length + word.Length + 1 <= maxLineLength)
                {
                    if (currentLine.Length > 0)
                        currentLine.Append(' ');
                    currentLine.Append(word);
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            return lines.ToArray();
        }

        /// <summary>
        /// 로그 파일 상태를 진단하는 메서드
        /// </summary>
        /// <param name="logPath">진단할 로그 파일 경로</param>
        /// <returns>진단 결과 메시지</returns>
        public string DiagnoseLogFileStatus(string logPath)
        {
            try
            {
                if (!File.Exists(logPath))
                {
                    return $"❌ 로그 파일이 존재하지 않습니다: {logPath}";
                }

                var fileInfo = new FileInfo(logPath);
                var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                var lastModified = fileInfo.LastWriteTime;

                var status = new StringBuilder();
                status.AppendLine($"📋 로그 파일 진단 결과: {logPath}");
                status.AppendLine($"📁 파일 크기: {sizeInMB:F2} MB");
                status.AppendLine($"🕒 마지막 수정: {lastModified:yyyy-MM-dd HH:mm:ss}");
                status.AppendLine($"✅ 파일 상태: 정상");

                // 파일 크기 경고
                if (sizeInMB > 100)
                {
                    status.AppendLine($"⚠️ 경고: 로그 파일이 100MB를 초과합니다. 로그 정리를 고려하세요.");
                }

                // 파일 수정 시간 경고
                var timeSinceLastModified = DateTime.Now - lastModified;
                if (timeSinceLastModified.TotalDays > 7)
                {
                    status.AppendLine($"⚠️ 경고: 로그 파일이 7일 이상 수정되지 않았습니다.");
                }

                return status.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ 로그 파일 진단 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 로그 파일이 쓰기 가능한지 확인하는 메서드
        /// </summary>
        /// <param name="logPath">확인할 로그 파일 경로</param>
        /// <returns>쓰기 가능 여부</returns>
        public bool CanWriteToFile(string logPath)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(logPath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    directoryPath = Environment.CurrentDirectory;
                }

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 테스트 파일 생성 시도
                var testPath = Path.Combine(directoryPath, "test_write.tmp");
                File.WriteAllText(testPath, "test");
                File.Delete(testPath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 파일 쓰기 권한 확인 실패: {ex.Message}");
                return false;
            }
        }
    }
}
