namespace LogisticManager.Models
{
    /// <summary>
    /// 카카오워크 알림 종류를 정의하는 열거형
    /// 각 알림 종류는 App.config의 채팅방 ID와 매핑됩니다.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// 판매입력 자료 알림
        /// </summary>
        SalesData,
        
        /// <summary>
        /// 통합송장 알림
        /// </summary>
        Integrated,
        
        /// <summary>
        /// 서울냉동 알림
        /// </summary>
        SeoulFrozen,
        
        /// <summary>
        /// 경기냉동 알림
        /// </summary>
        GyeonggiFrozen,
        
        /// <summary>
        /// 서울공산 알림
        /// </summary>
        SeoulGongsan,
        
        /// <summary>
        /// 경기공산 알림
        /// </summary>
        GyeonggiGongsan,
        
        /// <summary>
        /// 부산청과 알림
        /// </summary>
        BusanCheonggwa,
        
        /// <summary>
        /// 부산청과 소분 프린트 자료 알림
        /// </summary>
        BusanCheonggwaPrint,
        
        /// <summary>
        /// 감천냉동 알림
        /// </summary>
        GamcheonFrozen,
        
        /// <summary>
        /// 모니터링체크용(봇방) 알림
        /// </summary>
        Check
    }
} 