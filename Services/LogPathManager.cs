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
                // 프로젝트 루트 디렉토리를 찾는 더 안전한 방법
                var projectRoot = FindProjectRoot();
                
                _projectRoot = projectRoot;
                _logsDirectory = Path.Combine(_projectRoot, "logs");

                // 로그 디렉토리가 없으면 생성
                if (!Directory.Exists(_logsDirectory))
                {
                    Directory.CreateDirectory(_logsDirectory);
                }

                // logs/current 디렉토리도 생성
                var currentLogsDir = Path.Combine(_logsDirectory, "current");
                if (!Directory.Exists(currentLogsDir))
                {
                    Directory.CreateDirectory(currentLogsDir);
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
        /// 프로젝트 루트 디렉토리를 찾는 안전한 방법
        /// </summary>
        private static string FindProjectRoot()
        {
            // 방법 1: 현재 작업 디렉토리에서 .csproj 파일 찾기
            var currentDir = Environment.CurrentDirectory;
            var projectRoot = FindProjectRootByCsproj(currentDir);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                return projectRoot;
            }

            // 방법 2: AppDomain.CurrentDomain.BaseDirectory에서 상위로 이동하며 찾기
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            projectRoot = FindProjectRootByCsproj(baseDir);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                return projectRoot;
            }

            // 방법 3: 실행 파일 위치에서 상위로 이동하며 찾기
            var exeDir = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(exeDir))
            {
                var exeDirPath = Path.GetDirectoryName(exeDir);
                if (!string.IsNullOrEmpty(exeDirPath))
                {
                    projectRoot = FindProjectRootByCsproj(exeDirPath);
                    if (!string.IsNullOrEmpty(projectRoot))
                    {
                        return projectRoot;
                    }
                }
            }

            // 모든 방법이 실패하면 현재 작업 디렉토리 사용
            Console.WriteLine("⚠️ [LogPathManager] 프로젝트 루트를 찾을 수 없어 현재 작업 디렉토리 사용");
            return Environment.CurrentDirectory;
        }

        /// <summary>
        /// 지정된 디렉토리에서 .csproj 파일을 찾아 프로젝트 루트 찾기
        /// </summary>
        private static string FindProjectRootByCsproj(string startDirectory)
        {
            try
            {
                var currentDir = startDirectory;
                var maxDepth = 10; // 최대 10단계 상위로 검색

                for (int i = 0; i < maxDepth; i++)
                {
                    if (string.IsNullOrEmpty(currentDir) || !Directory.Exists(currentDir))
                    {
                        break;
                    }

                    // .csproj 파일이 있는지 확인
                    var csprojFiles = Directory.GetFiles(currentDir, "*.csproj");
                    if (csprojFiles.Length > 0)
                    {
                        Console.WriteLine($"✅ [LogPathManager] .csproj 파일 발견: {csprojFiles[0]}");
                        return currentDir;
                    }

                    // 상위 디렉토리로 이동
                    var parent = Directory.GetParent(currentDir);
                    if (parent == null)
                    {
                        break;
                    }
                    currentDir = parent.FullName;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [LogPathManager] .csproj 파일 검색 오류: {ex.Message}");
                return string.Empty;
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
        public static string AppLogPath => Path.Combine(_logsDirectory, "current", "app.log");

        /// <summary>
        /// kakaowork_debug.log 파일의 전체 경로
        /// </summary>
        public static string KakaoWorkDebugLogPath => Path.Combine(_logsDirectory, "current", "kakaowork_debug.log");



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
                ("kakaowork_debug.log", KakaoWorkDebugLogPath)
            };

            foreach (var (fileName, correctPath) in logFiles)
            {
                var exists = File.Exists(correctPath);
                var size = exists ? new FileInfo(correctPath).Length : 0;
                var sizeMB = Math.Round(size / (1024.0 * 1024.0), 2);
                
                Console.WriteLine($"   {fileName}: {(exists ? "✅" : "❌")} - {correctPath} ({(exists ? $"{sizeMB}MB" : "파일 없음")})");
            }
        }

        /// <summary>
        /// 프로젝트 루트 디렉토리를 찾는 공통 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 프로젝트 루트 디렉토리를 안전하게 찾기
        /// - config 폴더 존재 여부로 프로젝트 루트 판단
        /// - 다양한 시작점에서 프로젝트 루트 검색
        /// 
        /// 🔧 검색 방법:
        /// 1. 현재 작업 디렉토리에서 시작
        /// 2. AppDomain.CurrentDomain.BaseDirectory에서 시작
        /// 3. config 폴더 존재 여부로 프로젝트 루트 판단
        /// 
        /// ⚠️ 처리 방식:
        /// - config 폴더가 있는 디렉토리를 프로젝트 루트로 인식
        /// - 상위 디렉토리로 이동하며 검색 (최대 10단계)
        /// - 검색 실패 시 현재 실행 디렉토리 반환
        /// 
        /// 💡 사용 목적:
        /// - 로그 파일 경로 설정
        /// - 설정 파일 경로 설정
        /// - 프로젝트 관련 파일 경로 설정
        /// 
        /// 🔄 반환 값:
        /// - 성공 시: 프로젝트 루트 디렉토리 경로
        /// - 실패 시: 현재 실행 디렉토리 경로
        /// </summary>
        /// <returns>프로젝트 루트 디렉토리 경로</returns>
        public static string GetProjectRootDirectory()
        {
            try
            {
                // 방법 1: 현재 작업 디렉토리에서 시작
                var currentDir = Environment.CurrentDirectory;
                var projectRoot = FindProjectRootByConfig(currentDir);
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    return projectRoot;
                }

                // 방법 2: AppDomain.CurrentDomain.BaseDirectory에서 시작
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                projectRoot = FindProjectRootByConfig(baseDir);
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    return projectRoot;
                }

                // 방법 3: 실행 파일 위치에서 시작
                var exeDir = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exeDir))
                {
                    var exeDirPath = Path.GetDirectoryName(exeDir);
                    if (!string.IsNullOrEmpty(exeDirPath))
                    {
                        projectRoot = FindProjectRootByConfig(exeDirPath);
                        if (!string.IsNullOrEmpty(projectRoot))
                        {
                            return projectRoot;
                        }
                    }
                }

                // 모든 방법이 실패하면 현재 실행 디렉토리 반환
                Console.WriteLine("⚠️ [LogPathManager] 프로젝트 루트를 찾을 수 없어 현재 실행 디렉토리 사용");
                return AppDomain.CurrentDomain.BaseDirectory;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [LogPathManager] 프로젝트 루트 검색 오류: {ex.Message}");
                // 오류 발생 시 현재 실행 디렉토리 반환
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /// <summary>
        /// config 폴더 존재 여부로 프로젝트 루트를 찾는 헬퍼 메서드
        /// </summary>
        /// <param name="startDirectory">검색 시작 디렉토리</param>
        /// <returns>프로젝트 루트 디렉토리 경로 (찾지 못한 경우 빈 문자열)</returns>
        private static string FindProjectRootByConfig(string startDirectory)
        {
            try
            {
                var currentDir = startDirectory;
                var maxDepth = 10; // 최대 10단계 상위로 검색

                for (int i = 0; i < maxDepth; i++)
                {
                    if (string.IsNullOrEmpty(currentDir) || !Directory.Exists(currentDir))
                    {
                        break;
                    }

                    // config 폴더가 있는지 확인
                    var configPath = Path.Combine(currentDir, "config");
                    if (Directory.Exists(configPath))
                    {
                        Console.WriteLine($"✅ [LogPathManager] config 폴더 발견: {configPath}");
                        return currentDir;
                    }

                    // 상위 디렉토리로 이동
                    var parent = Directory.GetParent(currentDir);
                    if (parent == null)
                    {
                        break;
                    }
                    currentDir = parent.FullName;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [LogPathManager] config 폴더 검색 오류: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
