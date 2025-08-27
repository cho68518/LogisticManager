using System;
using System.Collections.Generic; // Added missing import for Dictionary

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
        /// 2차 배치: 08:00~10:00
        /// </summary>
        public const string BATCH_2ND = "2차";
        
        /// <summary>
        /// 3차 배치: 11:00~12:00
        /// </summary>
        public const string BATCH_3RD = "3차";
        
        /// <summary>
        /// 4차 배치: 12:00~13:00
        /// </summary>
        public const string BATCH_4TH = "4차";
        
        /// <summary>
        /// 5차 배치: 14:00~15:00
        /// </summary>
        public const string BATCH_5TH = "5차";
        
        /// <summary>
        /// 막차 배치: 16:00~18:00
        /// </summary>
        public const string BATCH_LAST = "막차";
        
        /// <summary>
        /// 추가 배치: 19:00~23:00
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

        #region 공개 메서드
        
        /// <summary>
        /// 현재 시간에 해당하는 배치구분을 반환합니다.
        /// </summary>
        /// <returns>현재 배치구분 문자열</returns>
        public string GetCurrentBatchType()
        {
            DateTime now = DateTime.Now;
            TimeSpan currentTime = now.TimeOfDay;
            
            // 배치구분규칙에 따른 시간대별 분류
            if (currentTime >= new TimeSpan(1, 0, 0) && currentTime < new TimeSpan(7, 0, 0))
            {
                return BATCH_1ST;
            }
            else if (currentTime >= new TimeSpan(8, 0, 0) && currentTime < new TimeSpan(10, 0, 0))
            {
                return BATCH_2ND;
            }
            else if (currentTime >= new TimeSpan(11, 0, 0) && currentTime < new TimeSpan(12, 0, 0))
            {
                return BATCH_3RD;
            }
            else if (currentTime >= new TimeSpan(12, 0, 0) && currentTime < new TimeSpan(13, 0, 0))
            {
                return BATCH_4TH;
            }
            else if (currentTime >= new TimeSpan(14, 0, 0) && currentTime < new TimeSpan(15, 0, 0))
            {
                return BATCH_5TH;
            }
            else if (currentTime >= new TimeSpan(16, 0, 0) && currentTime < new TimeSpan(18, 0, 0))
            {
                return BATCH_LAST;
            }
            else if (currentTime >= new TimeSpan(19, 0, 0) && currentTime < new TimeSpan(23, 0, 0))
            {
                return BATCH_EXTRA;
            }
            else if (currentTime >= new TimeSpan(0, 0, 0) && currentTime < new TimeSpan(1, 0, 0))
            {
                return BATCH_ETC;
            }
            else
            {
                // 07:00~08:00, 10:00~11:00, 13:00~14:00, 15:00~16:00, 18:00~19:00, 23:00~00:00
                // 배치 시간이 아닌 경우 "대기" 상태로 표시
                return "대기";
            }
        }
        
        /// <summary>
        /// 현재 배치구분을 포함한 타이틀을 반환합니다.
        /// </summary>
        /// <param name="baseTitle">기본 타이틀 (예: "송장 처리 시스템")</param>
        /// <returns>배치구분이 포함된 타이틀 (예: "송장 처리 시스템 (2차)")</returns>
        public string GetBatchTitle(string baseTitle)
        {
            string batchType = GetCurrentBatchType();
            
            // 배치 시간이 아닌 경우 기본 타이틀만 반환
            if (batchType == "대기")
            {
                return baseTitle;
            }
            
            return $"{baseTitle} ({batchType})";
        }
        
        /// <summary>
        /// 특정 시간에 해당하는 배치구분을 반환합니다.
        /// </summary>
        /// <param name="time">확인할 시간</param>
        /// <returns>해당 시간의 배치구분 문자열</returns>
        public string GetBatchTypeAtTime(TimeSpan time)
        {
            if (time >= new TimeSpan(1, 0, 0) && time < new TimeSpan(7, 0, 0))
            {
                return BATCH_1ST;
            }
            else if (time >= new TimeSpan(8, 0, 0) && time < new TimeSpan(10, 0, 0))
            {
                return BATCH_2ND;
            }
            else if (time >= new TimeSpan(11, 0, 0) && time < new TimeSpan(12, 0, 0))
            {
                return BATCH_3RD;
            }
            else if (time >= new TimeSpan(12, 0, 0) && time < new TimeSpan(13, 0, 0))
            {
                return BATCH_4TH;
            }
            else if (time >= new TimeSpan(14, 0, 0) && time < new TimeSpan(15, 0, 0))
            {
                return BATCH_5TH;
            }
            else if (time >= new TimeSpan(16, 0, 0) && time < new TimeSpan(18, 0, 0))
            {
                return BATCH_LAST;
            }
            else if (time >= new TimeSpan(19, 0, 0) && time < new TimeSpan(23, 0, 0))
            {
                return BATCH_EXTRA;
            }
            else if (time >= new TimeSpan(0, 0, 0) && time < new TimeSpan(1, 0, 0))
            {
                return BATCH_ETC;
            }
            else
            {
                return "대기";
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
                { BATCH_2ND, "08:00~10:00" },
                { BATCH_3RD, "11:00~12:00" },
                { BATCH_4TH, "12:00~13:00" },
                { BATCH_5TH, "14:00~15:00" },
                { BATCH_LAST, "16:00~18:00" },
                { BATCH_EXTRA, "19:00~23:00" },
                { BATCH_ETC, "00:00" }
            };
        }
        
        #endregion
    }
}
