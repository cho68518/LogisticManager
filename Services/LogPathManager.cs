using System;
using System.IO;

namespace LogisticManager.Services
{
    /// <summary>
    /// 프로젝트 전체에서 사용할 통합된 로그 경로 관리 클래스
    /// 
    /// 🎯 주요 목적:
    /// - 모든 로그 파일의 경로를 프로젝트 루트로 통일
    /// - bin/Debug, obj 폴더에 로그 파일 생성 방지
    /// - 로그 파일 중복 생성 문제 해결
    /// 
    /// 📋 핵심 기능:
    /// - 프로젝트 루트 디렉토리 자동 감지
    /// - 로그 파일 경로 표준화
    /// - 디버그/릴리즈 환경 구분
    /// </summary>
    public static class LogPathManager
    {
        private static readonly string _projectRoot;
        private static readonly string _logsDirectory;

        /// <summary>
        /// 정적 생성자 - 프로젝트 루트 디렉토리 자동 감지
        /// </summary>
        static LogPathManager()
        {
            try
            {
                // 현재 실행 파일의 위치에서 프로젝트 루트 찾기
                var currentDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // bin/Debug/net8.0-windows/win-x64/ 에서 프로젝트 루트로 이동
                var projectRoot = currentDir;
                for (int i = 0; i < 4; i++)
                {
                    var parent = Directory.GetParent(projectRoot);
                    if (parent != null)
                    {
                        projectRoot = parent.FullName;
                    }
                    else
                    {
                        break;
                    }
                }

                _projectRoot = projectRoot;
                _logsDirectory = Path.Combine(_projectRoot, "logs");

                // 로그 디렉토리가 없으면 생성
                if (!Directory.Exists(_logsDirectory))
                {
                    Directory.CreateDirectory(_logsDirectory);
                }

                Console.WriteLine($"📁 [LogPathManager] 프로젝트 루트 감지: {_projectRoot}");
                Console.WriteLine($"📁 [LogPathManager] 로그 디렉토리: {_logsDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [LogPathManager] 초기화 오류: {ex.Message}");
                // 오류 발생 시 현재 작업 디렉토리 사용
                _projectRoot = Environment.CurrentDirectory;
                _logsDirectory = Path.Combine(_projectRoot, "logs");
            }
        }

        /// <summary>
        /// 프로젝트 루트 디렉토리 경로
        /// </summary>
        public static string ProjectRoot => _projectRoot;

        /// <summary>
        /// 로그 디렉토리 경로
        /// </summary>
        public static string LogsDirectory => _logsDirectory;

        /// <summary>
        /// app.log 파일의 전체 경로
        /// </summary>
        public static string AppLogPath => Path.Combine(_projectRoot, "app.log");

        /// <summary>
        /// kakaowork_debug.log 파일의 전체 경로
        /// </summary>
        public static string KakaoWorkDebugLogPath => Path.Combine(_projectRoot, "kakaowork_debug.log");

        /// <summary>
        /// star2_debug.log 파일의 전체 경로
        /// </summary>
        public static string Star2DebugLogPath => Path.Combine(_projectRoot, "star2_debug.log");

        /// <summary>
        /// 로그 파일 경로 정보 출력
        /// </summary>
        public static void PrintLogPathInfo()
        {
            Console.WriteLine("📁 [LogPathManager] 로그 경로 정보:");
            Console.WriteLine($"   현재 작업 디렉토리: {Environment.CurrentDirectory}");
            Console.WriteLine($"   애플리케이션 기본 디렉토리: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"   프로젝트 루트 디렉토리: {_projectRoot}");
            Console.WriteLine($"   로그 디렉토리: {_logsDirectory}");
            Console.WriteLine($"   app.log 경로: {AppLogPath}");
            Console.WriteLine($"   kakaowork_debug.log 경로: {KakaoWorkDebugLogPath}");
        }

        /// <summary>
        /// 로그 파일이 올바른 위치에 있는지 확인
        /// </summary>
        public static void ValidateLogFileLocations()
        {
            Console.WriteLine("🔍 [LogPathManager] 로그 파일 위치 검증:");
            
            var logFiles = new[]
            {
                ("app.log", AppLogPath),
                ("kakaowork_debug.log", KakaoWorkDebugLogPath),
                ("star2_debug.log", Star2DebugLogPath)
            };

            foreach (var (fileName, correctPath) in logFiles)
            {
                var exists = File.Exists(correctPath);
                var size = exists ? new FileInfo(correctPath).Length : 0;
                var sizeMB = Math.Round(size / (1024.0 * 1024.0), 2);
                
                Console.WriteLine($"   {fileName}: {(exists ? "✅" : "❌")} - {correctPath} ({(exists ? $"{sizeMB}MB" : "파일 없음")})");
            }
        }
    }
}
