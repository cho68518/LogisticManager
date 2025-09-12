using System;
using System.Collections.Generic; // Added missing import for Dictionary
using System.IO; // 로깅을 위해 추가

namespace LogisticManager.Services
{
    /// <summary>
    /// 배치구분규칙에 따른 배치 시간을 관리하는 서비스
    /// </summary>
    public class BatchTimeService
    {
        #region 배치구분규칙 상수
        
        /// <summary>
        /// 1차 배치: 01:00~07:00
        /// </summary>
        public const string BATCH_1ST = "1차";
        
        /// <summary>
        /// 2차 배치: 07:00~10:00
        /// </summary>
        public const string BATCH_2ND = "2차";
        
        /// <summary>
        /// 3차 배치: 10:00~11:00
        /// </summary>
        public const string BATCH_3RD = "3차";
        
        /// <summary>
        /// 4차 배치: 11:00~13:00
        /// </summary>
        public const string BATCH_4TH = "4차";
        
        /// <summary>
        /// 5차 배치: 13:00~15:00
        /// </summary>
        public const string BATCH_5TH = "5차";
        
        /// <summary>
        /// 막차 배치: 15:00~18:00
        /// </summary>
        public const string BATCH_LAST = "막차";
        
        /// <summary>
        /// 추가 배치: 18:00~23:00
        /// </summary>
        public const string BATCH_EXTRA = "추가";
        
        /// <summary>
        /// 기타 배치: 00:00
        /// </summary>
        public const string BATCH_ETC = "기타";
        
        #endregion

        #region 싱글톤 인스턴스
        
        private static readonly Lazy<BatchTimeService> _instance = new Lazy<BatchTimeService>(() => new BatchTimeService());
        
        /// <summary>
        /// BatchTimeService의 싱글톤 인스턴스
        /// </summary>
        public static BatchTimeService Instance => _instance.Value;
        
        #endregion

        #region 생성자
        
        /// <summary>
        /// BatchTimeService 생성자 (싱글톤 패턴)
        /// </summary>
        private BatchTimeService()
        {
        }
        
        #endregion

        #region 로깅 메서드
        
        /// <summary>
        /// 로그 메시지를 파일에 기록하는 메서드
        /// </summary>
        /// <param name="message">로그 메시지</param>
        private void LogMessage(string message)
        {
            try
            {
                string logPath = Path.Combine("logs", "current", "app.log");
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [BATCH] {message}";
                
                // 디렉토리가 없으면 생성
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // 로깅 실패 시 무시 (무한 루프 방지)
            }
        }
        
        #endregion

        #region 공개 메서드
        
        /// <summary>
        /// 현재 시간에 해당하는 배치구분을 반환합니다.
        /// </summary>
        /// <returns>현재 배치구분 문자열</returns>
        public string GetCurrentBatchType()
        {
            DateTime now = DateTime.Now;
            TimeSpan currentTime = now.TimeOfDay;
            
            // 디버그 로그 추가 (매번 로그)
            LogMessage($"현재 시간: {now:HH:mm:ss}, TimeSpan: {currentTime}");
            
            // 배치구분규칙에 따른 시간대별 분류 (KakaoWorkService와 동일한 규칙)
            if (currentTime >= new TimeSpan(1, 0, 0) && currentTime <= new TimeSpan(7, 0, 0))
            {
                LogMessage($"1차 배치 시간대: {currentTime} → {BATCH_1ST}");
                return BATCH_1ST;
            }
            else if (currentTime > new TimeSpan(7, 0, 0) && currentTime <= new TimeSpan(10, 0, 0))
            {
                LogMessage($"2차 배치 시간대: {currentTime} → {BATCH_2ND}");
                return BATCH_2ND;
            }
            else if (currentTime > new TimeSpan(10, 0, 0) && currentTime <= new TimeSpan(12, 0, 0))
            {
                LogMessage($"3차 배치 시간대: {currentTime} → {BATCH_3RD}");
                return BATCH_3RD;
            }
            else if (currentTime > new TimeSpan(12, 0, 0) && currentTime <= new TimeSpan(14, 0, 0))
            {
                LogMessage($"4차 배치 시간대: {currentTime} → {BATCH_4TH}");
                return BATCH_4TH;
            }
            else if (currentTime > new TimeSpan(14, 0, 0) && currentTime <= new TimeSpan(15, 30, 0))
            {
                LogMessage($"5차 배치 시간대: {currentTime} → {BATCH_5TH}");
                return BATCH_5TH;
            }
            else if (currentTime >= new TimeSpan(15, 30, 0) && currentTime <= new TimeSpan(18, 0, 0))
            {
                LogMessage($"막차 배치 시간대: {currentTime} → {BATCH_LAST}");
                return BATCH_LAST;
            }
            else if (currentTime > new TimeSpan(18, 0, 0) && currentTime <= new TimeSpan(23, 0, 0))
            {
                LogMessage($"추가 배치 시간대: {currentTime} → {BATCH_EXTRA}");
                return BATCH_EXTRA;
            }
            //else if (currentTime >= new TimeSpan(0, 0, 0) && currentTime < new TimeSpan(1, 0, 0))
            //{
            //    LogMessage($"기타 배치 시간대: {currentTime} → {BATCH_ETC}");
            //    return BATCH_ETC;
            //}
            else
            {
                // 배치 시간이 아닌 경우 "대기" 상태로 표시
                LogMessage($"대기 시간대: {currentTime} → 대기");
                return "대기";
            }
        }
        
        /// <summary>
        /// 현재 배치구분을 포함한 타이틀을 포함한 타이틀을 반환합니다.
        /// </summary>
        /// <param name="baseTitle">기본 타이틀 (예: "송장 처리 시스템")</param>
        /// <returns>배치구분이 포함된 타이틀 (예: "송장 처리 시스템 (2차)")</returns>
        public string GetBatchTitle(string baseTitle)
        {
            string batchType = GetCurrentBatchType();
            
            // 디버그 로그 추가
            //LogMessage($"GetBatchTitle 호출: baseTitle={baseTitle}, batchType={batchType}");
            
            // 배치 시간이 아닌 경우 기본 타이틀만 반환
            if (batchType == "대기")
            {
                //LogMessage($"배치 시간이 아님: {batchType}");
                return baseTitle;
            }
            
            var result = $"{baseTitle} ({batchType})";
            //LogMessage($"최종 타이틀: {result}");
            return result;
        }
        
        /// <summary>
        /// 특정 시간에 해당하는 배치구분을 반환합니다.
        /// </summary>
        /// <param name="time">확인할 시간</param>
        /// <returns>해당 시간의 배치구분 문자열</returns>
        public string GetBatchTypeAtTime(TimeSpan time)
        {
            // KakaoWorkService와 동일한 차수 규칙 적용
            if (time >= new TimeSpan(1, 0, 0) && time <= new TimeSpan(7, 0, 0))
            {
                return BATCH_1ST;
            }
            else if (time > new TimeSpan(7, 0, 0) && time <= new TimeSpan(10, 0, 0))
            {
                return BATCH_2ND;
            }
            else if (time > new TimeSpan(10, 0, 0) && time <= new TimeSpan(11, 0, 0))
            {
                return BATCH_3RD;
            }
            else if (time > new TimeSpan(11, 0, 0) && time <= new TimeSpan(13, 0, 0))
            {
                return BATCH_4TH;
            }
            else if (time > new TimeSpan(13, 0, 0) && time <= new TimeSpan(15, 0, 0))
            {
                return BATCH_5TH;
            }
            else if (time > new TimeSpan(15, 0, 0) && time <= new TimeSpan(18, 0, 0))
            {
                return BATCH_LAST;
            }
            else if (time > new TimeSpan(18, 0, 0) && time <= new TimeSpan(23, 0, 0))
            {
                return BATCH_EXTRA;
            }
            else
            {
                return BATCH_ETC;
            }

        }
        
        /// <summary>
        /// 모든 배치구분 정보를 반환합니다.
        /// </summary>
        /// <returns>배치구분 정보 딕셔너리</returns>
        public Dictionary<string, string> GetAllBatchTypes()
        {
            return new Dictionary<string, string>
            {
                { BATCH_1ST, "01:00~07:00" },
                { BATCH_2ND, "07:00~10:00" },
                { BATCH_3RD, "10:00~11:00" },
                { BATCH_4TH, "11:00~13:00" },
                { BATCH_5TH, "13:00~15:00" },
                { BATCH_LAST, "15:00~18:00" },
                { BATCH_EXTRA, "18:00~23:00" },
                { BATCH_ETC, "00:00" }
            };
        }
        
        /// <summary>
        /// 선택된 차수가 현재 시간대에 맞는지 검증합니다.
        /// </summary>
        /// <param name="selectedBatch">사용자가 선택한 차수</param>
        /// <returns>차수가 올바른지 여부</returns>
        public bool IsBatchTimeValid(string selectedBatch)
        {
            try
            {
                // 현재 시간대에 맞는 차수 가져오기
                string currentBatch = GetCurrentBatchType();
                
                // 로그 기록
                LogMessage($"차수 검증: 선택된 차수={selectedBatch}, 현재 시간대 차수={currentBatch}");
                
                // 배치 시간이 아닌 경우 (대기 상태)는 항상 유효하지 않음
                if (currentBatch == "대기")
                {
                    LogMessage($"배치 시간이 아님: {currentBatch}");
                    return false;
                }
                
                // 선택된 차수와 현재 시간대 차수가 일치하는지 확인
                bool isValid = selectedBatch == currentBatch;
                
                LogMessage($"차수 검증 결과: {isValid}");
                return isValid;
            }
            catch (Exception ex)
            {
                LogMessage($"차수 검증 중 오류 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 차수 불일치 시 사용자에게 보여줄 메시지를 생성합니다.
        /// </summary>
        /// <param name="selectedBatch">사용자가 선택한 차수</param>
        /// <returns>차수 불일치 메시지</returns>
        public string GetBatchMismatchMessage(string selectedBatch)
        {
            try
            {
                string currentBatch = GetCurrentBatchType();
                string currentTime = DateTime.Now.ToString("HH:mm:ss");
                
                // 배치 시간이 아닌 경우
                if (currentBatch == "대기")
                {
                    return $"현재 시간({currentTime})은 배치 처리 시간이 아닙니다.\n\n" +
                           $"선택된 차수: {selectedBatch}\n" +
                           $"현재 상태: 배치 시간 아님\n\n" +
                           $"배치 처리 시간:\n" +
                           $"• 1차: 01:00~07:00\n" +
                           $"• 2차: 07:00~10:00\n" +
                           $"• 3차: 10:00~11:00\n" +
                           $"• 4차: 11:00~13:00\n" +
                           $"• 5차: 13:00~15:00\n" +
                           $"• 막차: 15:00~18:00\n" +
                           $"• 추가: 18:00~23:00\n\n" +
                           $"그래도 계속 진행하시겠습니까?";
                }
                
                // 차수가 다른 경우
                return $"선택된 차수가 현재 시간대와 맞지 않습니다.\n\n" +
                       $"선택된 차수: {selectedBatch}\n" +
                       $"현재 시간대 차수: {currentBatch}\n" +
                       $"현재 시간: {currentTime}\n\n" +
                       $"그래도 계속 진행하시겠습니까?";
            }
            catch (Exception ex)
            {
                LogMessage($"차수 불일치 메시지 생성 중 오류: {ex.Message}");
                return $"차수 검증 중 오류가 발생했습니다.\n\n그래도 계속 진행하시겠습니까?";
            }
        }
        
        #endregion
    }
}
