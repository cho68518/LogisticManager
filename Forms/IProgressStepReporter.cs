namespace LogisticManager.Forms
{
    /// <summary>
    /// 진행상황 단계별 업데이트를 위한 인터페이스
    /// </summary>
    public interface IProgressStepReporter
    {
        /// <summary>
        /// 특정 단계를 진행 중으로 설정
        /// </summary>
        /// <param name="stepIndex">진행할 단계 인덱스 (0부터 시작)</param>
        void ReportStepProgress(int stepIndex);
        
        /// <summary>
        /// 특정 단계를 완료로 표시
        /// </summary>
        /// <param name="stepIndex">완료할 단계 인덱스 (0부터 시작)</param>
        void ReportStepCompleted(int stepIndex);
        
        /// <summary>
        /// 모든 단계 초기화
        /// </summary>
        void ResetAllSteps();
    }
}
