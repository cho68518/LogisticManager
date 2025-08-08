using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace LogisticManager
{
    /// <summary>
    /// 로그 파일을 확인하고 관리하는 도구 클래스
    /// </summary>
    public static class LogViewer
    {
        /// <summary>
        /// 로그 파일 경로
        /// </summary>
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

        /// <summary>
        /// 최근 로그를 확인하는 메서드
        /// </summary>
        /// <param name="lineCount">확인할 라인 수 (기본값: 50)</param>
        /// <returns>최근 로그 내용</returns>
        public static string GetRecentLogs(int lineCount = 50)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return "❌ 로그 파일이 존재하지 않습니다.";
                }

                var lines = File.ReadAllLines(LogFilePath, Encoding.UTF8);
                var recentLines = lines.Skip(Math.Max(0, lines.Length - lineCount)).ToArray();
                
                return string.Join(Environment.NewLine, recentLines);
            }
            catch (Exception ex)
            {
                return $"❌ 로그 파일 읽기 중 오류 발생: {ex.Message}";
            }
        }

        /// <summary>
        /// 특정 키워드가 포함된 로그를 검색하는 메서드
        /// </summary>
        /// <param name="keyword">검색할 키워드</param>
        /// <param name="maxResults">최대 결과 수 (기본값: 100)</param>
        /// <returns>검색 결과</returns>
        public static string SearchLogs(string keyword, int maxResults = 100)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return "❌ 로그 파일이 존재하지 않습니다.";
                }

                var lines = File.ReadAllLines(LogFilePath, Encoding.UTF8);
                var matchingLines = lines
                    .Where(line => line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .Take(maxResults)
                    .ToArray();

                if (matchingLines.Length == 0)
                {
                    return $"🔍 키워드 '{keyword}'에 대한 검색 결과가 없습니다.";
                }

                return $"🔍 키워드 '{keyword}' 검색 결과 ({matchingLines.Length}개):\n\n" + 
                       string.Join(Environment.NewLine, matchingLines);
            }
            catch (Exception ex)
            {
                return $"❌ 로그 검색 중 오류 발생: {ex.Message}";
            }
        }

        /// <summary>
        /// 로그 파일 정보를 확인하는 메서드
        /// </summary>
        /// <returns>로그 파일 정보</returns>
        public static string GetLogFileInfo()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return "❌ 로그 파일이 존재하지 않습니다.";
                }

                var fileInfo = new FileInfo(LogFilePath);
                var sizeMB = Math.Round((double)fileInfo.Length / (1024 * 1024), 2);
                var lineCount = File.ReadAllLines(LogFilePath, Encoding.UTF8).Length;

                return $"📊 로그 파일 정보:\n" +
                       $"   📁 파일 경로: {LogFilePath}\n" +
                       $"   📏 파일 크기: {sizeMB}MB\n" +
                       $"   📄 총 라인 수: {lineCount:N0}줄\n" +
                       $"   🕒 마지막 수정: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                return $"❌ 로그 파일 정보 확인 중 오류 발생: {ex.Message}";
            }
        }

        /// <summary>
        /// 로그 파일을 클리어하는 메서드
        /// </summary>
        /// <returns>클리어 결과</returns>
        public static string ClearLogFile()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return "❌ 로그 파일이 존재하지 않습니다.";
                }

                // 백업 파일 생성
                var backupPath = LogFilePath + $".backup.{DateTime.Now:yyyyMMdd_HHmmss}";
                File.Copy(LogFilePath, backupPath);

                // 로그 파일 클리어
                File.WriteAllText(LogFilePath, "", Encoding.UTF8);

                return $"✅ 로그 파일이 클리어되었습니다.\n" +
                       $"   📁 백업 파일: {Path.GetFileName(backupPath)}";
            }
            catch (Exception ex)
            {
                return $"❌ 로그 파일 클리어 중 오류 발생: {ex.Message}";
            }
        }

        /// <summary>
        /// 오류 로그만 필터링하여 확인하는 메서드
        /// </summary>
        /// <param name="maxResults">최대 결과 수 (기본값: 50)</param>
        /// <returns>오류 로그 목록</returns>
        public static string GetErrorLogs(int maxResults = 50)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return "❌ 로그 파일이 존재하지 않습니다.";
                }

                var lines = File.ReadAllLines(LogFilePath, Encoding.UTF8);
                var errorLines = lines
                    .Where(line => line.Contains("오류", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("실패", StringComparison.OrdinalIgnoreCase) ||
                                  line.Contains("❌", StringComparison.OrdinalIgnoreCase))
                    .Take(maxResults)
                    .ToArray();

                if (errorLines.Length == 0)
                {
                    return "✅ 오류 로그가 없습니다.";
                }

                return $"⚠️ 오류 로그 목록 ({errorLines.Length}개):\n\n" + 
                       string.Join(Environment.NewLine, errorLines);
            }
            catch (Exception ex)
            {
                return $"❌ 오류 로그 확인 중 오류 발생: {ex.Message}";
            }
        }
    }
}
