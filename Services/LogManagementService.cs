using System;
using System.IO;
using System.Text;

namespace LogisticManager.Services
{
    /// <summary>
    /// 로그 파일 관리 서비스
    /// 
    /// 🎯 주요 목적:
    /// - 로그 파일의 크기를 모니터링하고 관리
    /// - 파일 크기가 임계값을 초과하면 자동으로 클리어
    /// - 로그 파일의 무한 증가 방지
    /// 
    /// 📋 핵심 기능:
    /// - 로그 파일 크기 실시간 모니터링
    /// - 200MB 임계값 기반 자동 클리어
    /// - 로그 파일 백업 및 복구 기능
    /// - 안전한 파일 처리 및 예외 처리
    /// 
    /// ⚡ 처리 시점:
    /// - 로그 작성 시마다 크기 체크
    /// - 임계값 초과 시 즉시 클리어
    /// 
    /// 💡 사용 목적:
    /// - 디스크 공간 절약
    /// - 로그 파일 성능 최적화
    /// - 시스템 안정성 확보
    /// </summary>
    public class LogManagementService
    {
        private readonly string _logFilePath;
        private readonly long _maxFileSizeBytes;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 로그 파일 경로 (읽기 전용)
        /// </summary>
        public string LogFilePath => _logFilePath;

        /// <summary>
        /// 로그 관리 서비스 생성자
        /// </summary>
        /// <param name="logFilePath">로그 파일 경로</param>
        /// <param name="maxFileSizeMB">최대 파일 크기 (MB)</param>
        public LogManagementService(string? logFilePath = null, int maxFileSizeMB = 200)
        {
            _logFilePath = logFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            _maxFileSizeBytes = maxFileSizeMB * 1024L * 1024L; // MB를 바이트로 변환
        }

        /// <summary>
        /// 로그 파일 크기 체크 및 필요시 클리어
        /// 
        /// 🎯 주요 기능:
        /// - 현재 로그 파일 크기를 확인
        /// - 임계값 초과 시 파일을 안전하게 클리어
        /// - 클리어 후 새로운 로그 헤더 작성
        /// 
        /// ⚠️ 처리 방식:
        /// - 스레드 안전을 위한 락 사용
        /// - 파일 존재 여부 확인 후 처리
        /// - 예외 발생 시 안전하게 처리
        /// 
        /// 💡 사용 목적:
        /// - 로그 파일 크기 자동 관리
        /// - 시스템 리소스 보호
        /// - 로그 파일 성능 최적화
        /// </summary>
        public void CheckAndClearLogFileIfNeeded()
        {
            try
            {
                lock (_lockObject)
                {
                    // 파일이 존재하지 않으면 처리하지 않음
                    if (!File.Exists(_logFilePath))
                    {
                        return;
                    }

                    // 파일 크기 확인
                    var fileInfo = new FileInfo(_logFilePath);
                    if (fileInfo.Length > _maxFileSizeBytes)
                    {
                        ClearLogFile();
                    }
                }
            }
            catch (Exception ex)
            {
                // 로그 관리 중 오류가 발생해도 애플리케이션에 영향을 주지 않도록 처리
                Console.WriteLine($"[LogManagementService] 로그 파일 크기 체크 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 로그 파일 클리어 및 새로운 헤더 작성
        /// 
        /// 🎯 주요 기능:
        /// - 기존 로그 파일을 완전히 클리어
        /// - 새로운 로그 시작 헤더 작성
        /// - 클리어 작업 로그 기록
        /// 
        /// ⚠️ 처리 방식:
        /// - 파일을 UTF-8 인코딩으로 새로 생성
        /// - 클리어 시간과 이유를 기록
        /// - 안전한 파일 처리 보장
        /// 
        /// 💡 사용 목적:
        /// - 로그 파일 크기 제한 유지
        /// - 새로운 로그 세션 시작
        /// - 로그 관리 이력 추적
        /// </summary>
        private void ClearLogFile()
        {
            try
            {
                // 클리어 전 백업 파일 생성 (선택사항)
                var backupPath = _logFilePath + $".backup.{DateTime.Now:yyyyMMdd_HHmmss}";
                if (File.Exists(_logFilePath))
                {
                    File.Copy(_logFilePath, backupPath);
                }

                // 파일을 UTF-8로 새로 생성
                using (var writer = new StreamWriter(_logFilePath, false, Encoding.UTF8))
                {
                    writer.WriteLine(new string('=', 80));
                    writer.WriteLine($"로그 파일 클리어됨 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"클리어 사유: 파일 크기 초과 (임계값: {_maxFileSizeBytes / (1024 * 1024)}MB)");
                    writer.WriteLine($"백업 파일: {Path.GetFileName(backupPath)}");
                    writer.WriteLine(new string('=', 80));
                    writer.WriteLine();
                }

                Console.WriteLine($"[LogManagementService] 로그 파일이 클리어되었습니다. 크기: {new FileInfo(_logFilePath).Length / (1024 * 1024)}MB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogManagementService] 로그 파일 클리어 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 로그 파일 크기 정보 조회
        /// 
        /// 🎯 주요 기능:
        /// - 로그 파일의 현재 크기를 바이트와 MB 단위로 반환
        /// - 파일 존재 여부 확인
        /// - 크기 정보를 사용자 친화적으로 표시
        /// 
        /// 💡 사용 목적:
        /// - 로그 파일 상태 모니터링
        /// - 디버깅 및 관리 목적
        /// - 시스템 상태 확인
        /// </summary>
        /// <returns>로그 파일 크기 정보</returns>
        public (bool exists, long sizeBytes, double sizeMB) GetLogFileSizeInfo()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    return (false, 0, 0);
                }

                var fileInfo = new FileInfo(_logFilePath);
                var sizeMB = Math.Round((double)fileInfo.Length / (1024 * 1024), 2);
                
                return (true, fileInfo.Length, sizeMB);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogManagementService] 로그 파일 크기 조회 중 오류 발생: {ex.Message}");
                return (false, 0, 0);
            }
        }

        /// <summary>
        /// 로그 파일 관리 상태 출력
        /// 
        /// 🎯 주요 기능:
        /// - 현재 로그 파일 상태를 콘솔에 출력
        /// - 크기 정보와 임계값 비교 표시
        /// - 관리 권장사항 제공
        /// 
        /// 💡 사용 목적:
        /// - 시스템 관리자에게 상태 정보 제공
        /// - 로그 관리 효율성 향상
        /// - 문제 예방 및 조기 발견
        /// </summary>
        public void PrintLogFileStatus()
        {
            var (exists, sizeBytes, sizeMB) = GetLogFileSizeInfo();
            var maxSizeMB = _maxFileSizeBytes / (1024 * 1024);

            Console.WriteLine("📊 로그 파일 상태:");
            Console.WriteLine($"   파일 경로: {_logFilePath}");
            Console.WriteLine($"   파일 존재: {(exists ? "✅" : "❌")}");
            
            if (exists)
            {
                Console.WriteLine($"   현재 크기: {sizeMB}MB / {maxSizeMB}MB");
                Console.WriteLine($"   사용률: {Math.Round((sizeMB / maxSizeMB) * 100, 1)}%");
                
                if (sizeMB > maxSizeMB * 0.8)
                {
                    Console.WriteLine("   ⚠️  경고: 로그 파일 크기가 임계값의 80%를 초과했습니다.");
                }
            }
        }
    }
}
