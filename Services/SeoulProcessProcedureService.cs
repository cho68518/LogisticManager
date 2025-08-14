using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogisticManager.Services
{
    /// <summary>
    /// 서울 프로세스 프로시저 실행 서비스
    /// 
    /// 🎯 주요 목적:
    /// - sp_ProcessStarInvoice 프로시저 실행
    /// - 프로시저 실행 결과를 상세하게 로깅
    /// - 실행 성능 모니터링 및 문제 추적
    /// 
    /// 📋 핵심 기능:
    /// - 프로시저 실행 시간 측정
    /// - 각 단계별 처리 건수 수집
    /// - 실행 결과를 app.log 파일에 저장
    /// - 성능 통계 및 분석 정보 제공
    /// </summary>
    public class SeoulProcessProcedureService
    {
        private readonly DatabaseService _databaseService;
        private readonly ProcedureLogService _procedureLogService;
        private readonly LogManagementService _logService;

        /// <summary>
        /// 서울 프로세스 프로시저 서비스 생성자
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스</param>
        /// <param name="procedureLogService">프로시저 로그 서비스</param>
        /// <param name="logService">로그 관리 서비스</param>
        public SeoulProcessProcedureService(
            DatabaseService databaseService, 
            ProcedureLogService procedureLogService,
            LogManagementService logService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _procedureLogService = procedureLogService ?? throw new ArgumentNullException(nameof(procedureLogService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        /// <summary>
        /// 서울 프로세스 프로시저 실행 (비동기)
        /// </summary>
        /// <returns>프로시저 실행 결과</returns>
        public async Task<ProcedureExecutionResult> ExecuteSeoulProcessProcedureAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var startTime = DateTime.Now;
            
            try
            {
                _logService.LogMessage("[SeoulProcessProcedureService] 서울 프로세스 프로시저 실행 시작");
                
                // 프로시저 실행
                var logs = await ExecuteProcedureWithLoggingAsync();
                
                stopwatch.Stop();
                var executionTime = stopwatch.Elapsed;
                
                // 로그 저장
                await _procedureLogService.SaveProcedureLogsAsync(
                    "sp_ProcessStarInvoice", 
                    logs, 
                    executionTime, 
                    true);
                
                // 성능 통계 출력
                var summary = new ProcedureLogService.ProcedureExecutionSummary
                {
                    ProcedureName = "sp_ProcessStarInvoice",
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    TotalExecutionTime = executionTime,
                    TotalSteps = logs?.Count ?? 0,
                    TotalAffectedRows = logs?.Sum(l => l.AffectedRows) ?? 0,
                    IsSuccess = true,
                    StepLogs = logs ?? new List<ProcedureLogService.ProcedureExecutionLog>()
                };
                
                _procedureLogService.PrintPerformanceStatistics(summary);
                
                _logService.LogMessage($"[SeoulProcessProcedureService] 서울 프로세스 프로시저 실행 완료 - 총 {executionTime.TotalSeconds:F3}초 소요");
                
                return new ProcedureExecutionResult
                {
                    IsSuccess = true,
                    ExecutionTime = executionTime,
                    TotalSteps = logs?.Count ?? 0,
                    TotalAffectedRows = logs?.Sum(l => l.AffectedRows) ?? 0,
                    Logs = logs ?? new List<ProcedureLogService.ProcedureExecutionLog>()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var executionTime = stopwatch.Elapsed;
                
                _logService.LogMessage($"[SeoulProcessProcedureService] 서울 프로세스 프로시저 실행 중 오류 발생: {ex.Message}");
                
                // 에러 로그 저장
                await _procedureLogService.SaveProcedureLogsAsync(
                    "sp_ProcessStarInvoice", 
                    new List<ProcedureLogService.ProcedureExecutionLog>(), 
                    executionTime, 
                    false, 
                    ex.Message);
                
                return new ProcedureExecutionResult
                {
                    IsSuccess = false,
                    ExecutionTime = executionTime,
                    ErrorMessage = ex.Message,
                    Logs = new List<ProcedureLogService.ProcedureExecutionLog>()
                };
            }
        }

        /// <summary>
        /// 서울 프로세스 프로시저 실행 (동기)
        /// </summary>
        /// <returns>프로시저 실행 결과</returns>
        public ProcedureExecutionResult ExecuteSeoulProcessProcedure()
        {
            var stopwatch = Stopwatch.StartNew();
            var startTime = DateTime.Now;
            
            try
            {
                _logService.LogMessage("[SeoulProcessProcedureService] 서울 프로세스 프로시저 실행 시작");
                
                // 프로시저 실행
                var logs = ExecuteProcedureWithLogging();
                
                stopwatch.Stop();
                var executionTime = stopwatch.Elapsed;
                
                // 로그 저장
                _procedureLogService.SaveProcedureLogs(
                    "sp_ProcessStarInvoice", 
                    logs, 
                    executionTime, 
                    true);
                
                // 성능 통계 출력
                var summary = new ProcedureLogService.ProcedureExecutionSummary
                {
                    ProcedureName = "sp_ProcessStarInvoice",
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    TotalExecutionTime = executionTime,
                    TotalSteps = logs?.Count ?? 0,
                    TotalAffectedRows = logs?.Sum(l => l.AffectedRows) ?? 0,
                    IsSuccess = true,
                    StepLogs = logs ?? new List<ProcedureLogService.ProcedureExecutionLog>()
                };
                
                _procedureLogService.PrintPerformanceStatistics(summary);
                
                _logService.LogMessage($"[SeoulProcessProcedureService] 서울 프로세스 프로시저 실행 완료 - 총 {executionTime.TotalSeconds:F3}초 소요");
                
                return new ProcedureExecutionResult
                {
                    IsSuccess = true,
                    ExecutionTime = executionTime,
                    TotalSteps = logs?.Count ?? 0,
                    TotalAffectedRows = logs?.Sum(l => l.AffectedRows) ?? 0,
                    Logs = logs ?? new List<ProcedureLogService.ProcedureExecutionLog>()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var executionTime = stopwatch.Elapsed;
                
                _logService.LogMessage($"[SeoulProcessProcedureService] 서울 프로세스 프로시저 실행 중 오류 발생: {ex.Message}");
                
                // 에러 로그 저장
                _procedureLogService.SaveProcedureLogs(
                    "sp_ProcessStarInvoice", 
                    new List<ProcedureLogService.ProcedureExecutionLog>(), 
                    executionTime, 
                    false, 
                    ex.Message);
                
                return new ProcedureExecutionResult
                {
                    IsSuccess = false,
                    ExecutionTime = executionTime,
                    ErrorMessage = ex.Message,
                    Logs = new List<ProcedureLogService.ProcedureExecutionLog>()
                };
            }
        }

        /// <summary>
        /// 프로시저 실행 및 로깅 (비동기)
        /// </summary>
        /// <returns>프로시저 실행 로그</returns>
        private async Task<List<ProcedureLogService.ProcedureExecutionLog>> ExecuteProcedureWithLoggingAsync()
        {
            var logs = new List<ProcedureLogService.ProcedureExecutionLog>();
            
            try
            {
                using (var connection = await _databaseService.GetConnectionAsync())
                {
                    await connection.OpenAsync();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "sp_ProcessStarInvoice";
                        command.CommandTimeout = 300; // 5분 타임아웃
                        
                        _logService.LogMessage("[SeoulProcessProcedureService] 프로시저 실행 중...");
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // 첫 번째 결과셋은 로그 데이터
                            logs = _procedureLogService.ParseProcedureLogs(reader);
                            
                            _logService.LogMessage($"[SeoulProcessProcedureService] 프로시저 실행 완료 - {logs.Count}개 단계 처리됨");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"[SeoulProcessProcedureService] 프로시저 실행 중 데이터베이스 오류: {ex.Message}");
                throw;
            }
            
            return logs;
        }

        /// <summary>
        /// 프로시저 실행 및 로깅 (동기)
        /// </summary>
        /// <returns>프로시저 실행 로그</returns>
        private List<ProcedureLogService.ProcedureExecutionLog> ExecuteProcedureWithLogging()
        {
            var logs = new List<ProcedureLogService.ProcedureExecutionLog>();
            
            try
            {
                using (var connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "sp_ProcessStarInvoice";
                        command.CommandTimeout = 300; // 5분 타임아웃
                        
                        _logService.LogMessage("[SeoulProcessProcedureService] 프로시저 실행 중...");
                        
                        using (var reader = command.ExecuteReader())
                        {
                            // 첫 번째 결과셋은 로그 데이터
                            logs = _procedureLogService.ParseProcedureLogs(reader);
                            
                            _logService.LogMessage($"[SeoulProcessProcedureService] 프로시저 실행 완료 - {logs.Count}개 단계 처리됨");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"[SeoulProcessProcedureService] 프로시저 실행 중 데이터베이스 오류: {ex.Message}");
                throw;
            }
            
            return logs;
        }

        /// <summary>
        /// 프로시저 실행 결과
        /// </summary>
        public class ProcedureExecutionResult
        {
            public bool IsSuccess { get; set; }
            public TimeSpan ExecutionTime { get; set; }
            public int TotalSteps { get; set; }
            public int TotalAffectedRows { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public List<ProcedureLogService.ProcedureExecutionLog> Logs { get; set; } = new List<ProcedureLogService.ProcedureExecutionLog>();
        }
    }
}
