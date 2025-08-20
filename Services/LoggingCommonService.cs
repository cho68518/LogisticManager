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
        private const string LOG_PATH = "logs/current/app.log";
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
        /// 로그 파일 상태를 진단하는 공통 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 로그 파일 경로 및 존재 여부 확인
        /// - 파일 크기, 수정시간, 권한 정보 진단
        /// - 시스템 환경 정보 수집
        /// - 경고 메시지 및 권장사항 제공
        /// 
        /// 🔧 처리 과정:
        /// 1. 파일 존재 여부 확인
        /// 2. 파일 상세 정보 수집 (크기, 수정시간, 권한)
        /// 3. 시스템 환경 정보 수집
        /// 4. 경고 및 권장사항 생성
        /// 
        /// 💡 사용 목적:
        /// - 로그 파일 문제 진단
        /// - 시스템 환경 분석
        /// - 로그 관리 최적화
        /// 
        /// ⚠️ 처리 방식:
        /// - 상세한 진단 정보 제공
        /// - 사용자 친화적인 메시지 생성
        /// - 오류 발생 시 안전한 fallback
        /// </summary>
        /// <param name="logPath">진단할 로그 파일 경로</param>
        /// <returns>진단 결과 메시지</returns>
        public string DiagnoseLogFileStatus(string logPath)
        {
            try
            {
                var status = new StringBuilder();
                status.AppendLine($"=== 📋 로그 파일 상태 진단 ===");
                status.AppendLine($"📍 대상 경로: {logPath}");
                status.AppendLine($"🔗 절대 경로: {Path.GetFullPath(logPath)}");
                
                // 디렉토리 및 파일 존재 여부 확인
                var directoryPath = Path.GetDirectoryName(logPath);
                status.AppendLine($"📁 디렉토리 존재: {(Directory.Exists(directoryPath) ? "✅" : "❌")}");
                status.AppendLine($"📄 파일 존재: {(File.Exists(logPath) ? "✅" : "❌")}");
                
                if (File.Exists(logPath))
                {
                    var fileInfo = new FileInfo(logPath);
                    var sizeInBytes = fileInfo.Length;
                    var sizeInMB = sizeInBytes / (1024.0 * 1024.0);
                    var lastModified = fileInfo.LastWriteTime;
                    
                    status.AppendLine($"📊 파일 크기: {sizeInBytes:N0} bytes ({sizeInMB:F2} MB)");
                    status.AppendLine($"🕒 마지막 수정: {lastModified:yyyy-MM-dd HH:mm:ss}");
                    status.AppendLine($"🔒 읽기 전용: {(fileInfo.IsReadOnly ? "✅" : "❌")}");
                    status.AppendLine($"✏️ 쓰기 권한: {(CanWriteToFile(logPath) ? "✅" : "❌")}");
                    status.AppendLine($"✅ 파일 상태: 정상");
                    
                    // 파일 크기 경고
                    if (sizeInMB > 100)
                    {
                        status.AppendLine($"⚠️ 경고: 로그 파일이 100MB를 초과합니다. 로그 정리를 고려하세요.");
                    }
                    else if (sizeInMB > 50)
                    {
                        status.AppendLine($"⚠️ 주의: 로그 파일이 50MB를 초과합니다. 모니터링이 필요합니다.");
                    }
                    
                    // 파일 수정 시간 경고
                    var timeSinceLastModified = DateTime.Now - lastModified;
                    if (timeSinceLastModified.TotalDays > 7)
                    {
                        status.AppendLine($"⚠️ 경고: 로그 파일이 7일 이상 수정되지 않았습니다.");
                    }
                    else if (timeSinceLastModified.TotalDays > 3)
                    {
                        status.AppendLine($"ℹ️ 정보: 로그 파일이 3일 이상 수정되지 않았습니다.");
                    }
                }
                else
                {
                    status.AppendLine($"❌ 파일이 존재하지 않습니다.");
                    status.AppendLine($"💡 해결 방법: 파일 경로를 확인하거나 로그 디렉토리를 생성하세요.");
                }
                
                // 시스템 환경 정보
                status.AppendLine($"🖥️ 현재 작업 디렉토리: {Directory.GetCurrentDirectory()}");
                status.AppendLine($"🔧 AppDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                
                // 권장사항
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    status.AppendLine($"💡 권장사항: 디렉토리 '{directoryPath}'를 생성하세요.");
                }
                
                status.AppendLine($"=== 🎯 진단 완료 ===");
                
                return status.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ 로그 파일 상태 진단 실패:\n   오류 내용: {ex.Message}\n   스택 트레이스: {ex.StackTrace}";
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
