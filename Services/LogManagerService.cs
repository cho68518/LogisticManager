using System;
using System.IO;
using System.Text;

namespace LogisticManager.Services
{
    /// <summary>
    /// 통합 로그 관리 서비스
    /// 
    /// 📋 주요 기능:
    /// - 중앙화된 로그 파일 관리
    /// - 로그 레벨별 분류
    /// - 로그 로테이션 (크기/날짜 기반)
    /// - 로그 폴더 자동 생성
    /// 
    /// 🎯 사용 목적:
    /// - 모든 로그를 logs/current/app.log에 통합 저장
    /// - 로그 파일 크기 및 보관 기간 관리
    /// - 일관된 로그 형식 제공
    /// </summary>
    public static class LogManagerService
    {
        #region 상수 (Constants)

        /// <summary>로그 폴더 경로</summary>
        private static readonly string LogFolderPath = Path.Combine(GetProjectRootDirectory(), "logs", "current");

        /// <summary>메인 로그 파일명</summary>
        private static readonly string MainLogFileName = "app.log";

        /// <summary>로그 파일 최대 크기 (10MB)</summary>
        private static readonly long MaxLogFileSize = 10 * 1024 * 1024;

        /// <summary>로그 보관 기간 (30일)</summary>
        private static readonly int LogRetentionDays = 30;

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// 정보 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// 경고 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// 오류 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public static void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// 디버그 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public static void LogDebug(string message)
        {
            WriteLog("DEBUG", message);
        }

        /// <summary>
        /// 일반 로그 기록 (기존 File.AppendAllText 호환성)
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public static void LogMessage(string message)
        {
            WriteLog("INFO", message);
        }

        #endregion

        #region 비공개 메서드 (Private Methods)

        /// <summary>
        /// 로그 파일에 메시지 기록
        /// </summary>
        /// <param name="level">로그 레벨</param>
        /// <param name="message">로그 메시지</param>
        private static void WriteLog(string level, string message)
        {
            try
            {
                // 로그 폴더 생성 확인
                EnsureLogDirectoryExists();

                // 로그 파일 경로
                var logFilePath = Path.Combine(LogFolderPath, MainLogFileName);

                // 로그 파일 크기 확인 및 로테이션
                CheckAndRotateLogFile(logFilePath);

                // 로그 메시지 형식
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

                // 로그 파일에 기록
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // 로그 기록 실패 시 콘솔에 출력 (폴백)
                Console.WriteLine($"❌ 로그 기록 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 로그 디렉토리 존재 확인 및 생성
        /// </summary>
        private static void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(LogFolderPath))
            {
                Directory.CreateDirectory(LogFolderPath);
            }
        }

        /// <summary>
        /// 로그 파일 크기 확인 및 로테이션
        /// </summary>
        /// <param name="logFilePath">로그 파일 경로</param>
        private static void CheckAndRotateLogFile(string logFilePath)
        {
            if (File.Exists(logFilePath))
            {
                var fileInfo = new FileInfo(logFilePath);
                if (fileInfo.Length > MaxLogFileSize)
                {
                    RotateLogFile(logFilePath);
                }
            }
        }

        /// <summary>
        /// 로그 파일 로테이션
        /// </summary>
        /// <param name="logFilePath">현재 로그 파일 경로</param>
        private static void RotateLogFile(string logFilePath)
        {
            try
            {
                var archiveFolder = Path.Combine(GetProjectRootDirectory(), "logs", "archive");
                if (!Directory.Exists(archiveFolder))
                {
                    Directory.CreateDirectory(archiveFolder);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var archiveFileName = $"app_{timestamp}.log";
                var archivePath = Path.Combine(archiveFolder, archiveFileName);

                // 현재 로그 파일을 아카이브로 이동
                File.Move(logFilePath, archivePath);

                // 아카이브 폴더 정리 (오래된 로그 삭제)
                CleanupOldLogs(archiveFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 로그 로테이션 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 오래된 로그 파일 정리
        /// </summary>
        /// <param name="archiveFolder">아카이브 폴더 경로</param>
        private static void CleanupOldLogs(string archiveFolder)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-LogRetentionDays);
                var logFiles = Directory.GetFiles(archiveFolder, "app_*.log");

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 로그 정리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 프로젝트 루트 디렉토리 경로 반환
        /// </summary>
        /// <returns>프로젝트 루트 디렉토리 경로</returns>
        private static string GetProjectRootDirectory()
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                while (!string.IsNullOrEmpty(currentDir))
                {
                    var configPath = Path.Combine(currentDir, "config");
                    if (Directory.Exists(configPath))
                    {
                        return currentDir;
                    }
                    var parentDir = Directory.GetParent(currentDir);
                    if (parentDir == null) break;
                    currentDir = parentDir.FullName;
                }
                return Directory.GetCurrentDirectory();
            }
            catch
            {
                return Directory.GetCurrentDirectory();
            }
        }

        #endregion
    }
}
