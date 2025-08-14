using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticManager.Services
{
    /// <summary>
    /// 프로시저 실행 로그 관리 서비스
    /// 
    /// 🎯 주요 목적:
    /// - SQL 프로시저 실행 결과를 상세하게 로깅
    /// - 각 단계별 처리 건수와 실행 시간 기록
    /// - 프로시저 성능 모니터링 및 문제 추적
    /// 
    /// 📋 핵심 기능:
    /// - 프로시저 실행 단계별 로그 기록
    /// - 처리된 행 수와 실행 시간 측정
    /// - 에러 발생 시 상세한 오류 정보 기록
    /// - 로그 파일에 구조화된 형태로 저장
    /// </summary>
    public class ProcedureLogService
    {
        private readonly LogManagementService _logService;
        private readonly string _logFilePath;

        /// <summary>
        /// 프로시저 실행 로그 정보
        /// </summary>
        public class ProcedureExecutionLog
        {
            public int StepID { get; set; }
            public string OperationDescription { get; set; } = string.Empty;
            public int AffectedRows { get; set; }
            public TimeSpan ExecutionTime { get; set; }
            public DateTime ExecutionDate { get; set; }
        }

        /// <summary>
        /// 프로시저 실행 요약 정보
        /// </summary>
        public class ProcedureExecutionSummary
        {
            public string ProcedureName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public TimeSpan TotalExecutionTime { get; set; }
            public int TotalSteps { get; set; }
            public int TotalAffectedRows { get; set; }
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public List<ProcedureExecutionLog> StepLogs { get; set; } = new List<ProcedureExecutionLog>();
        }

        /// <summary>
        /// 프로시저 로그 서비스 생성자
        /// </summary>
        /// <param name="logService">로그 관리 서비스</param>
        public ProcedureLogService(LogManagementService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _logFilePath = logService.LogFilePath;
        }

        /// <summary>
        /// 프로시저 실행 로그를 파일에 저장
        /// </summary>
        /// <param name="procedureName">프로시저 이름</param>
        /// <param name="logs">실행 로그 목록</param>
        /// <param name="executionTime">총 실행 시간</param>
        /// <param name="isSuccess">성공 여부</param>
        /// <param name="errorMessage">에러 메시지 (실패 시)</param>
        public async Task SaveProcedureLogsAsync(string procedureName, List<ProcedureExecutionLog> logs, 
            TimeSpan executionTime, bool isSuccess = true, string errorMessage = "")
        {
            try
            {
                var summary = new ProcedureExecutionSummary
                {
                    ProcedureName = procedureName,
                    StartTime = DateTime.Now.Subtract(executionTime),
                    EndTime = DateTime.Now,
                    TotalExecutionTime = executionTime,
                    TotalSteps = logs?.Count ?? 0,
                    TotalAffectedRows = logs?.Sum(l => l.AffectedRows) ?? 0,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    StepLogs = logs ?? new List<ProcedureExecutionLog>()
                };

                await SaveProcedureLogsToFileAsync(summary);
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"[ProcedureLogService] 프로시저 로그 저장 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 프로시저 실행 로그를 파일에 저장 (동기 방식)
        /// </summary>
        /// <param name="procedureName">프로시저 이름</param>
        /// <param name="logs">실행 로그 목록</param>
        /// <param name="executionTime">총 실행 시간</param>
        /// <param name="isSuccess">성공 여부</param>
        /// <param name="errorMessage">에러 메시지 (실패 시)</param>
        public void SaveProcedureLogs(string procedureName, List<ProcedureExecutionLog> logs, 
            TimeSpan executionTime, bool isSuccess = true, string errorMessage = "")
        {
            try
            {
                var summary = new ProcedureExecutionSummary
                {
                    ProcedureName = procedureName,
                    StartTime = DateTime.Now.Subtract(executionTime),
                    EndTime = DateTime.Now,
                    TotalExecutionTime = executionTime,
                    TotalSteps = logs?.Count ?? 0,
                    TotalAffectedRows = logs?.Sum(l => l.AffectedRows) ?? 0,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    StepLogs = logs ?? new List<ProcedureExecutionLog>()
                };

                SaveProcedureLogsToFile(summary);
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"[ProcedureLogService] 프로시저 로그 저장 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 프로시저 실행 로그를 파일에 저장 (비동기)
        /// </summary>
        /// <param name="summary">실행 요약 정보</param>
        private async Task SaveProcedureLogsToFileAsync(ProcedureExecutionSummary summary)
        {
            var logBuilder = new StringBuilder();
            
            // 프로시저 실행 시작 구분선
            logBuilder.AppendLine(new string('=', 100));
            logBuilder.AppendLine($"🚀 프로시저 실행 시작: {summary.ProcedureName}");
            logBuilder.AppendLine($"⏰ 시작 시간: {summary.StartTime:yyyy-MM-dd HH:mm:ss.fff}");
            logBuilder.AppendLine($"⏱️  총 실행 시간: {summary.TotalExecutionTime.TotalSeconds:F3}초");
            logBuilder.AppendLine($"📊 총 단계 수: {summary.TotalSteps}");
            logBuilder.AppendLine($"📈 총 처리 행 수: {summary.TotalAffectedRows:N0}");
            logBuilder.AppendLine($"✅ 실행 결과: {(summary.IsSuccess ? "성공" : "실패")}");
            
            if (!summary.IsSuccess && !string.IsNullOrEmpty(summary.ErrorMessage))
            {
                logBuilder.AppendLine($"❌ 오류 메시지: {summary.ErrorMessage}");
            }
            
            logBuilder.AppendLine(new string('-', 100));

            // 각 단계별 상세 로그
            if (summary.StepLogs?.Any() == true)
            {
                logBuilder.AppendLine("📋 단계별 실행 상세:");
                logBuilder.AppendLine($"{"단계",-4} {"처리내용",-50} {"처리행수",-10} {"실행시간",-12}");
                logBuilder.AppendLine(new string('-', 80));

                foreach (var step in summary.StepLogs.OrderBy(s => s.StepID))
                {
                    var stepTime = step.ExecutionTime.TotalMilliseconds > 0 
                        ? $"{step.ExecutionTime.TotalMilliseconds:F1}ms" 
                        : "N/A";
                    
                    logBuilder.AppendLine($"{step.StepID,-4} {TruncateString(step.OperationDescription, 48),-50} {step.AffectedRows,-10:N0} {stepTime,-12}");
                }
            }

            // 실행 완료 구분선
            logBuilder.AppendLine(new string('-', 100));
            logBuilder.AppendLine($"🏁 프로시저 실행 완료: {summary.ProcedureName}");
            logBuilder.AppendLine($"⏰ 완료 시간: {summary.EndTime:yyyy-MM-dd HH:mm:ss.fff}");
            logBuilder.AppendLine(new string('=', 100));
            logBuilder.AppendLine();

            // 로그 파일에 저장
            var logMessage = logBuilder.ToString();
            await Task.Run(() => _logService.LogMessage(logMessage));
        }

        /// <summary>
        /// 프로시저 실행 로그를 파일에 저장 (동기)
        /// </summary>
        /// <param name="summary">실행 요약 정보</param>
        private void SaveProcedureLogsToFile(ProcedureExecutionSummary summary)
        {
            var logBuilder = new StringBuilder();
            
            // 프로시저 실행 시작 구분선
            logBuilder.AppendLine(new string('=', 100));
            logBuilder.AppendLine($"🚀 프로시저 실행 시작: {summary.ProcedureName}");
            logBuilder.AppendLine($"⏰ 시작 시간: {summary.StartTime:yyyy-MM-dd HH:mm:ss.fff}");
            logBuilder.AppendLine($"⏱️  총 실행 시간: {summary.TotalExecutionTime.TotalSeconds:F3}초");
            logBuilder.AppendLine($"📊 총 단계 수: {summary.TotalSteps}");
            logBuilder.AppendLine($"📈 총 처리 행 수: {summary.TotalAffectedRows:N0}");
            logBuilder.AppendLine($"✅ 실행 결과: {(summary.IsSuccess ? "성공" : "실패")}");
            
            if (!summary.IsSuccess && !string.IsNullOrEmpty(summary.ErrorMessage))
            {
                logBuilder.AppendLine($"❌ 오류 메시지: {summary.ErrorMessage}");
            }
            
            logBuilder.AppendLine(new string('-', 100));

            // 각 단계별 상세 로그
            if (summary.StepLogs?.Any() == true)
            {
                logBuilder.AppendLine("📋 단계별 실행 상세:");
                logBuilder.AppendLine($"{"단계",-4} {"처리내용",-50} {"처리행수",-10} {"실행시간",-12}");
                logBuilder.AppendLine(new string('-', 80));

                foreach (var step in summary.StepLogs.OrderBy(s => s.StepID))
                {
                    var stepTime = step.ExecutionTime.TotalMilliseconds > 0 
                        ? $"{step.ExecutionTime.TotalMilliseconds:F1}ms" 
                        : "N/A";
                    
                    logBuilder.AppendLine($"{step.StepID,-4} {TruncateString(step.OperationDescription, 48),-50} {step.AffectedRows,-10:N0} {stepTime,-12}");
                }
            }

            // 실행 완료 구분선
            logBuilder.AppendLine(new string('-', 100));
            logBuilder.AppendLine($"🏁 프로시저 실행 완료: {summary.ProcedureName}");
            logBuilder.AppendLine($"⏰ 완료 시간: {summary.EndTime:yyyy-MM-dd HH:mm:ss.fff}");
            logBuilder.AppendLine(new string('=', 100));
            logBuilder.AppendLine();

            // 로그 파일에 저장
            var logMessage = logBuilder.ToString();
            _logService.LogMessage(logMessage);
        }

        /// <summary>
        /// 프로시저 실행 결과를 로그 객체로 변환
        /// </summary>
        /// <param name="dataReader">데이터 리더</param>
        /// <returns>프로시저 실행 로그 목록</returns>
        public List<ProcedureExecutionLog> ParseProcedureLogs(DbDataReader dataReader)
        {
            var logs = new List<ProcedureExecutionLog>();
            
            try
            {
                while (dataReader.Read())
                {
                    var log = new ProcedureExecutionLog
                    {
                        StepID = dataReader.GetInt32("StepID"),
                        OperationDescription = dataReader.GetString("OperationDescription"),
                        AffectedRows = dataReader.GetInt32("AffectedRows"),
                        ExecutionDate = DateTime.Now
                    };
                    
                    logs.Add(log);
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"[ProcedureLogService] 프로시저 로그 파싱 중 오류 발생: {ex.Message}");
            }
            
            return logs;
        }

        /// <summary>
        /// 프로시저 실행 결과를 로그 객체로 변환 (DataTable 사용)
        /// </summary>
        /// <param name="dataTable">데이터 테이블</param>
        /// <returns>프로시저 실행 로그 목록</returns>
        public List<ProcedureExecutionLog> ParseProcedureLogs(DataTable dataTable)
        {
            var logs = new List<ProcedureExecutionLog>();
            
            try
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    var log = new ProcedureExecutionLog
                    {
                        StepID = Convert.ToInt32(row["StepID"]),
                        OperationDescription = row["OperationDescription"].ToString() ?? "",
                        AffectedRows = Convert.ToInt32(row["AffectedRows"]),
                        ExecutionDate = DateTime.Now
                    };
                    
                    logs.Add(log);
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"[ProcedureLogService] 프로시저 로그 파싱 중 오류 발생: {ex.Message}");
            }
            
            return logs;
        }

        /// <summary>
        /// 문자열을 지정된 길이로 자르기
        /// </summary>
        /// <param name="input">입력 문자열</param>
        /// <param name="maxLength">최대 길이</param>
        /// <returns>자른 문자열</returns>
        private string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return "";
            if (input.Length <= maxLength) return input;
            return input.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// 프로시저 실행 성능 통계 출력
        /// </summary>
        /// <param name="summary">실행 요약 정보</param>
        public void PrintPerformanceStatistics(ProcedureExecutionSummary summary)
        {
            Console.WriteLine($"\n📊 프로시저 '{summary.ProcedureName}' 실행 성능 통계:");
            Console.WriteLine($"   총 실행 시간: {summary.TotalExecutionTime.TotalSeconds:F3}초");
            Console.WriteLine($"   총 단계 수: {summary.TotalSteps}");
            Console.WriteLine($"   총 처리 행 수: {summary.TotalAffectedRows:N0}");
            Console.WriteLine($"   평균 처리 행 수: {(summary.TotalSteps > 0 ? summary.TotalAffectedRows / summary.TotalSteps : 0):N0}");
            Console.WriteLine($"   실행 결과: {(summary.IsSuccess ? "✅ 성공" : "❌ 실패")}");
            
            if (summary.StepLogs?.Any() == true)
            {
                var slowestStep = summary.StepLogs.OrderByDescending(s => s.ExecutionTime).First();
                var fastestStep = summary.StepLogs.OrderBy(s => s.ExecutionTime).First();
                
                Console.WriteLine($"   가장 느린 단계: {slowestStep.StepID} - {slowestStep.OperationDescription} ({slowestStep.ExecutionTime.TotalMilliseconds:F1}ms)");
                Console.WriteLine($"   가장 빠른 단계: {fastestStep.StepID} - {fastestStep.OperationDescription} ({fastestStep.ExecutionTime.TotalMilliseconds:F1}ms)");
            }
        }
    }
}
