using System;
using System.Configuration;

namespace LogisticManager.Services
{
    /// <summary>
    /// 처리 시간을 중앙 집중식으로 관리하는 서비스
    /// 
    /// 📋 주요 기능:
    /// - 송장 처리 시작/완료 시간 통합 관리
    /// - 모든 UI 요소에 일관된 시간 정보 제공
    /// - TestLevel 기반 완료 시점 자동 판단
    /// - 실시간 시간 업데이트 지원
    /// 
    /// 🎯 시간 측정 기준:
    /// - 시작 시점: 송장처리 시작 버튼 클릭 시
    /// - 완료 시점: App.config의 TestLevel에 도달 시
    /// 
    /// 💡 사용법:
    /// ProcessingTimeManager.Instance.StartProcessing();
    /// ProcessingTimeManager.Instance.CompleteProcessing();
    /// var elapsed = ProcessingTimeManager.Instance.GetElapsedTime();
    /// </summary>
    public sealed class ProcessingTimeManager
    {
        #region 싱글톤 패턴

        private static readonly Lazy<ProcessingTimeManager> _instance = new(() => new ProcessingTimeManager());
        
        /// <summary>
        /// ProcessingTimeManager의 싱글톤 인스턴스
        /// </summary>
        public static ProcessingTimeManager Instance => _instance.Value;

        private ProcessingTimeManager() { }

        #endregion

        #region 필드 및 속성

        /// <summary>
        /// 처리 시작 시간
        /// </summary>
        private DateTime? _startTime;

        /// <summary>
        /// 처리 완료 시간
        /// </summary>
        private DateTime? _endTime;

        /// <summary>
        /// 현재 처리 중인지 여부
        /// </summary>
        private bool _isProcessing;

        /// <summary>
        /// 목표 TestLevel (App.config에서 읽어옴)
        /// </summary>
        private int _targetTestLevel;

        /// <summary>
        /// 현재 처리 단계
        /// </summary>
        private int _currentStep;

        /// <summary>
        /// 처리 시작 시간 (읽기 전용)
        /// </summary>
        public DateTime? StartTime => _startTime;

        /// <summary>
        /// 처리 완료 시간 (읽기 전용)
        /// </summary>
        public DateTime? EndTime => _endTime;

        /// <summary>
        /// 현재 처리 중인지 여부 (읽기 전용)
        /// </summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// 목표 TestLevel (읽기 전용)
        /// </summary>
        public int TargetTestLevel => _targetTestLevel;

        /// <summary>
        /// 현재 처리 단계 (읽기 전용)
        /// </summary>
        public int CurrentStep => _currentStep;

        #endregion

        #region 시간 관리 이벤트

        /// <summary>
        /// 처리 시작 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<ProcessingTimeEventArgs>? ProcessingStarted;

        /// <summary>
        /// 처리 완료 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<ProcessingTimeEventArgs>? ProcessingCompleted;

        /// <summary>
        /// 시간 업데이트 시 발생하는 이벤트 (실시간 동기화용)
        /// </summary>
        public event EventHandler<ProcessingTimeEventArgs>? TimeUpdated;

        /// <summary>
        /// 단계 업데이트 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<ProcessingStepEventArgs>? StepUpdated;

        #endregion

        #region 시간 관리 메서드

        /// <summary>
        /// 송장 처리를 시작하고 시간 측정을 시작합니다.
        /// 
        /// 📋 수행 작업:
        /// - App.config에서 TestLevel 읽기
        /// - 시작 시간 기록
        /// - 처리 상태를 진행 중으로 설정
        /// - ProcessingStarted 이벤트 발생
        /// 
        /// 🎯 호출 시점: 송장처리 시작 버튼 클릭 시
        /// </summary>
        public void StartProcessing()
        {
            try
            {
                // App.config에서 TestLevel 읽기
                var testLevelStr = ConfigurationManager.AppSettings["TestLevel"] ?? "22";
                if (!int.TryParse(testLevelStr, out _targetTestLevel))
                {
                    _targetTestLevel = 22; // 기본값
                }

                // 시간 측정 시작
                _startTime = DateTime.Now;
                _endTime = null;
                _isProcessing = true;
                _currentStep = 0;

                // 로그 기록
                LogManagerService.LogInfo($"🕒 ProcessingTimeManager: 처리 시간 측정 시작");
                LogManagerService.LogInfo($"   시작 시간: {_startTime:yyyy-MM-dd HH:mm:ss.fff}");
                LogManagerService.LogInfo($"   : {_targetTestLevel}");

                // 이벤트 발생
                ProcessingStarted?.Invoke(this, new ProcessingTimeEventArgs(_startTime.Value, null, TimeSpan.Zero));
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ ProcessingTimeManager.StartProcessing 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 송장 처리를 완료하고 시간 측정을 종료합니다.
        /// 
        /// 📋 수행 작업:
        /// - 완료 시간 기록
        /// - 처리 상태를 완료로 설정
        /// - 총 처리 시간 계산
        /// - ProcessingCompleted 이벤트 발생
        /// 
        /// 🎯 호출 시점: TestLevel에 도달하거나 처리 완료 시
        /// </summary>
        public void CompleteProcessing()
        {
            try
            {
                if (!_isProcessing || !_startTime.HasValue)
                {
                    LogManagerService.LogWarning("⚠️ ProcessingTimeManager: 처리가 시작되지 않았거나 이미 완료되었습니다.");
                    return;
                }

                // 시간 측정 완료
                _endTime = DateTime.Now;
                _isProcessing = false;

                var totalTime = _endTime.Value - _startTime.Value;

                // 로그 기록
                LogManagerService.LogInfo($"🕒 ProcessingTimeManager: 처리 시간 측정 완료");
                LogManagerService.LogInfo($"   완료 시간: {_endTime:yyyy-MM-dd HH:mm:ss.fff}");
                LogManagerService.LogInfo($"   총 처리 시간: {totalTime:hh\\:mm\\:ss\\.fff}");
                LogManagerService.LogInfo($"   완료된 단계: {_currentStep}/{_targetTestLevel}");

                // 이벤트 발생
                ProcessingCompleted?.Invoke(this, new ProcessingTimeEventArgs(_startTime.Value, _endTime.Value, totalTime));
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ ProcessingTimeManager.CompleteProcessing 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 현재 단계를 업데이트하고 TestLevel 도달 여부를 확인합니다.
        /// 
        /// 📋 수행 작업:
        /// - 현재 단계 업데이트
        /// - TestLevel 도달 시 자동 완료 처리
        /// - StepUpdated 이벤트 발생
        /// 
        /// 🎯 호출 시점: 각 처리 단계 완료 시
        /// </summary>
        /// <param name="step">완료된 단계 번호</param>
        /// <param name="stepName">단계명 (선택사항)</param>
        public void UpdateStep(int step, string? stepName = null)
        {
            try
            {
                if (!_isProcessing)
                {
                    return;
                }

                _currentStep = step;

                // 로그 기록
                var stepInfo = string.IsNullOrEmpty(stepName) ? $"단계 {step}" : $"단계 {step}: {stepName}";
                LogManagerService.LogInfo($"📊 ProcessingTimeManager: {stepInfo} 완료 ({step}/{_targetTestLevel})");

                // StepUpdated 이벤트 발생
                StepUpdated?.Invoke(this, new ProcessingStepEventArgs(step, _targetTestLevel, stepName));

                // TestLevel에 도달했는지 확인
                if (step >= _targetTestLevel)
                {
                    LogManagerService.LogInfo($"🎯 ProcessingTimeManager: TestLevel({_targetTestLevel})에 도달하여 처리를 완료합니다.");
                    CompleteProcessing();
                }

                // 실시간 시간 업데이트 이벤트 발생
                var currentElapsed = GetElapsedTime();
                TimeUpdated?.Invoke(this, new ProcessingTimeEventArgs(_startTime!.Value, _endTime, currentElapsed));
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ ProcessingTimeManager.UpdateStep 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 현재까지의 경과 시간을 반환합니다.
        /// 
        /// 📋 계산 방식:
        /// - 처리 완료된 경우: 완료 시간 - 시작 시간
        /// - 처리 중인 경우: 현재 시간 - 시작 시간
        /// - 시작되지 않은 경우: TimeSpan.Zero
        /// 
        /// 🎯 사용 목적: 실시간 시간 표시
        /// </summary>
        /// <returns>경과 시간</returns>
        public TimeSpan GetElapsedTime()
        {
            try
            {
                if (!_startTime.HasValue)
                {
                    return TimeSpan.Zero;
                }

                if (_endTime.HasValue)
                {
                    // 처리 완료된 경우: 고정된 시간 반환
                    return _endTime.Value - _startTime.Value;
                }
                else if (_isProcessing)
                {
                    // 처리 중인 경우: 실시간 시간 반환
                    return DateTime.Now - _startTime.Value;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ ProcessingTimeManager.GetElapsedTime 실패: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 경과 시간을 포맷된 문자열로 반환합니다.
        /// 
        /// 📋 포맷: "3.8초" (초 단위, 소수점 1자리)
        /// 🎯 사용 목적: UI 표시용 문자열 (원형진행률 판넬과 동일한 형식)
        /// </summary>
        /// <returns>포맷된 경과 시간 문자열</returns>
        public string GetFormattedElapsedTime()
        {
            try
            {
                var elapsed = GetElapsedTime();
                var totalSeconds = elapsed.TotalSeconds;
                return $"{totalSeconds:F1}초";
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ ProcessingTimeManager.GetFormattedElapsedTime 실패: {ex.Message}");
                return "0.0초";
            }
        }

        /// <summary>
        /// 처리를 강제로 중단합니다.
        /// 
        /// 📋 수행 작업:
        /// - 처리 상태를 중단으로 설정
        /// - 현재 시간을 완료 시간으로 기록
        /// - ProcessingCompleted 이벤트 발생
        /// 
        /// 🎯 호출 시점: 사용자가 처리를 중단하거나 오류 발생 시
        /// </summary>
        /// <param name="reason">중단 사유 (선택사항)</param>
        public void AbortProcessing(string? reason = null)
        {
            try
            {
                if (!_isProcessing)
                {
                    return;
                }

                _endTime = DateTime.Now;
                _isProcessing = false;

                var totalTime = _endTime.Value - _startTime!.Value;
                var reasonText = string.IsNullOrEmpty(reason) ? "사용자 요청" : reason;

                // 로그 기록
                LogManagerService.LogWarning($"⚠️ ProcessingTimeManager: 처리가 중단되었습니다.");
                LogManagerService.LogWarning($"   중단 사유: {reasonText}");
                LogManagerService.LogWarning($"   중단 시간: {_endTime:yyyy-MM-dd HH:mm:ss.fff}");
                LogManagerService.LogWarning($"   처리 시간: {totalTime.TotalSeconds:F1}초");
                LogManagerService.LogWarning($"   진행 단계: {_currentStep}/{_targetTestLevel}");

                // 이벤트 발생
                ProcessingCompleted?.Invoke(this, new ProcessingTimeEventArgs(_startTime.Value, _endTime.Value, totalTime));
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ ProcessingTimeManager.AbortProcessing 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 시간 관리자를 초기화합니다.
        /// 
        /// 📋 수행 작업:
        /// - 모든 시간 정보 초기화
        /// - 처리 상태 초기화
        /// - 단계 정보 초기화
        /// 
        /// 🎯 호출 시점: 새로운 처리를 시작하기 전
        /// </summary>
        public void Reset()
        {
            try
            {
                _startTime = null;
                _endTime = null;
                _isProcessing = false;
                _currentStep = 0;
                _targetTestLevel = 22; // 기본값

                LogManagerService.LogInfo("🔄 ProcessingTimeManager: 시간 관리자가 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"❌ ProcessingTimeManager.Reset 실패: {ex.Message}");
                throw;
            }
        }

        #endregion
    }

    #region 이벤트 인수 클래스

    /// <summary>
    /// 처리 시간 관련 이벤트 인수
    /// </summary>
    public class ProcessingTimeEventArgs : EventArgs
    {
        /// <summary>
        /// 시작 시간
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// 완료 시간 (처리 중인 경우 null)
        /// </summary>
        public DateTime? EndTime { get; }

        /// <summary>
        /// 경과 시간
        /// </summary>
        public TimeSpan ElapsedTime { get; }

        /// <summary>
        /// 처리가 완료되었는지 여부
        /// </summary>
        public bool IsCompleted => EndTime.HasValue;

        /// <summary>
        /// ProcessingTimeEventArgs 생성자
        /// </summary>
        /// <param name="startTime">시작 시간</param>
        /// <param name="endTime">완료 시간</param>
        /// <param name="elapsedTime">경과 시간</param>
        public ProcessingTimeEventArgs(DateTime startTime, DateTime? endTime, TimeSpan elapsedTime)
        {
            StartTime = startTime;
            EndTime = endTime;
            ElapsedTime = elapsedTime;
        }
    }

    /// <summary>
    /// 처리 단계 관련 이벤트 인수
    /// </summary>
    public class ProcessingStepEventArgs : EventArgs
    {
        /// <summary>
        /// 현재 단계
        /// </summary>
        public int CurrentStep { get; }

        /// <summary>
        /// 총 단계 수 (TestLevel)
        /// </summary>
        public int TotalSteps { get; }

        /// <summary>
        /// 단계명 (선택사항)
        /// </summary>
        public string? StepName { get; }

        /// <summary>
        /// 진행률 (0.0 ~ 1.0)
        /// </summary>
        public double Progress => TotalSteps > 0 ? (double)CurrentStep / TotalSteps : 0.0;

        /// <summary>
        /// ProcessingStepEventArgs 생성자
        /// </summary>
        /// <param name="currentStep">현재 단계</param>
        /// <param name="totalSteps">총 단계 수</param>
        /// <param name="stepName">단계명</param>
        public ProcessingStepEventArgs(int currentStep, int totalSteps, string? stepName = null)
        {
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            StepName = stepName;
        }
    }

    #endregion
}
