using System.Drawing.Drawing2D;
using LogisticManager.Models;
using LogisticManager.Services;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 송장 처리 진행상황을 표시하는 사용자 정의 컨트롤
    /// 
    /// 주요 기능:
    /// - 원형 진행률 차트 (Donut Chart)
    /// - 단계별 상태 표시
    /// - 실시간 진행상황 업데이트
    /// - 반응형 레이아웃
    /// </summary>
    public partial class ProgressDisplayControl : UserControl, IProgressStepReporter
    {
        #region 필드 (Private Fields)
        
        /// <summary>
        /// 진행상황 단계 목록
        /// </summary>
        private List<CommonCode> _progressSteps = new();
        
        /// <summary>
        /// 현재 진행 중인 단계 인덱스
        /// </summary>
        private int _currentStepIndex = -1;
        
        /// <summary>
        /// 완료된 단계 수
        /// </summary>
        private int _completedSteps = 0;
        
        /// <summary>
        /// 전체 단계 수
        /// </summary>
        private int _totalSteps = 0;
        
        /// <summary>
        /// 원형 차트 그리기용 펜
        /// </summary>
        private readonly Pen _progressPen;
        
        /// <summary>
        /// 배경 원 그리기용 펜
        /// </summary>
        private readonly Pen _backgroundPen;
        
        /// <summary>
        /// 애니메이션 타이머
        /// </summary>
        private readonly System.Windows.Forms.Timer _animationTimer;
        
        /// <summary>
        /// 애니메이션 각도
        /// </summary>
        private float _animationAngle = 0f;
        
        /// <summary>
        /// 통합 시간 관리자 (중앙 집중식 시간 관리)
        /// </summary>
        private readonly ProcessingTimeManager _timeManager;
        
        #endregion

        #region 속성 (Properties)
        
        /// <summary>
        /// 진행상황 단계 목록
        /// </summary>
        public List<CommonCode> ProgressSteps
        {
            get => _progressSteps;
            set
            {
                _progressSteps = value ?? new List<CommonCode>();
                _totalSteps = _progressSteps.Count;
                _completedSteps = 0;
                _currentStepIndex = -1;
                Invalidate(); // 컨트롤 다시 그리기
            }
        }
        
        /// <summary>
        /// 현재 진행 중인 단계 인덱스
        /// </summary>
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (_currentStepIndex != value)
                {
                    _currentStepIndex = value;
                    UpdateProgress();
                    Invalidate();
                }
            }
        }
        
        /// <summary>
        /// 완료된 단계 수
        /// </summary>
        public int CompletedSteps
        {
            get => _completedSteps;
            set
            {
                if (_completedSteps != value)
                {
                    _completedSteps = Math.Max(0, Math.Min(value, _totalSteps));
                    Invalidate();
                }
            }
        }
        
        #endregion

        #region 생성자 (Constructor)
        
        /// <summary>
        /// ProgressDisplayControl 생성자
        /// </summary>
        public ProgressDisplayControl()
        {
            InitializeComponent();
            
            // 통합 시간 관리자 인스턴스 가져오기
            _timeManager = ProcessingTimeManager.Instance;
            
            // 시간 관리자 이벤트 구독
            _timeManager.TimeUpdated += OnTimeUpdated;
            _timeManager.ProcessingCompleted += OnProcessingCompleted;
            _timeManager.StepUpdated += OnStepUpdated;
            
            // 펜 초기화
            _progressPen = new Pen(Color.FromArgb(46, 204, 113), 10f) // 초록색
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            
            _backgroundPen = new Pen(Color.FromArgb(236, 240, 241), 10f) // 연한 회색
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            
            // 애니메이션 타이머 초기화
            _animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 50 // 50ms마다 업데이트
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            
            // 컨트롤 설정
            this.DoubleBuffered = true; // 깜빡임 방지
            this.BackColor = Color.Transparent;
            
            // 크기 조정 이벤트
            this.Resize += ProgressDisplayControl_Resize;
        }
        
        #endregion

        #region 초기화 (Initialization)
        
        /// <summary>
        /// 컨트롤 초기화
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
        
        #endregion

        #region 시간 관리자 이벤트 핸들러 (Time Manager Event Handlers)
        
        /// <summary>
        /// 시간 업데이트 이벤트 핸들러 (실시간 동기화)
        /// </summary>
        private void OnTimeUpdated(object? sender, ProcessingTimeEventArgs e)
        {
            // UI 스레드에서 실행되도록 보장
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnTimeUpdated(sender, e)));
                return;
            }
            
            // 화면 갱신 (시간 표시 업데이트)
            Invalidate();
        }
        
        /// <summary>
        /// 처리 완료 이벤트 핸들러
        /// </summary>
        private void OnProcessingCompleted(object? sender, ProcessingTimeEventArgs e)
        {
            // UI 스레드에서 실행되도록 보장
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnProcessingCompleted(sender, e)));
                return;
            }
            
            // 애니메이션 중지
            _animationTimer.Stop();
            
            // 화면 갱신
            Invalidate();
        }
        
        /// <summary>
        /// 단계 업데이트 이벤트 핸들러
        /// </summary>
        private void OnStepUpdated(object? sender, ProcessingStepEventArgs e)
        {
            // UI 스레드에서 실행되도록 보장
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnStepUpdated(sender, e)));
                return;
            }
            
            // 단계 정보 업데이트
            _completedSteps = e.CurrentStep;
            _currentStepIndex = e.CurrentStep < e.TotalSteps ? e.CurrentStep : -1;
            
            // 화면 갱신
            Invalidate();
        }
        
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        
        /// <summary>
        /// 애니메이션 타이머 이벤트
        /// </summary>
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            _animationAngle += 10f; // 10도씩 회전
            if (_animationAngle >= 360f)
                _animationAngle = 0f;
            
            // 처리 시간 실시간 업데이트를 위해 화면 갱신
            Invalidate(); // 컨트롤 다시 그리기
        }
        
        /// <summary>
        /// 컨트롤 크기 변경 이벤트
        /// </summary>
        private void ProgressDisplayControl_Resize(object? sender, EventArgs e)
        {
            Invalidate(); // 크기 변경 시 다시 그리기
        }
        
        #endregion

        #region 진행상황 업데이트 (Progress Update)
        
        /// <summary>
        /// 진행상황 업데이트
        /// </summary>
        private void UpdateProgress()
        {
            if (_currentStepIndex >= 0 && _currentStepIndex < _totalSteps)
            {
                // 현재 단계를 진행 중으로 설정
                if (!_animationTimer.Enabled)
                {
                    _animationTimer.Start();
                }
                
                // 처리 시간 실시간 업데이트를 위해 화면 갱신
                Invalidate();
            }
            else
            {
                // 애니메이션 중지
                _animationTimer.Stop();
            }
        }
        
        /// <summary>
        /// 특정 단계를 완료로 표시
        /// </summary>
        /// <param name="stepIndex">완료할 단계 인덱스</param>
        public void CompleteStep(int stepIndex)
        {
            if (stepIndex >= 0 && stepIndex < _totalSteps)
            {
                // 통합 시간 관리자에 단계 업데이트 알림
                _timeManager.UpdateStep(stepIndex + 1, _progressSteps.Count > stepIndex ? _progressSteps[stepIndex].Description : null);
                
                // 로컬 상태 업데이트 (이벤트 핸들러에서도 업데이트되지만 즉시 반영을 위해)
                _completedSteps = Math.Max(_completedSteps, stepIndex + 1);
                
                Invalidate();
            }
        }
        
        /// <summary>
        /// 모든 단계 초기화
        /// </summary>
        public void ResetProgress()
        {
            _completedSteps = 0;
            _currentStepIndex = -1;
            _animationTimer.Stop();
            
            // 통합 시간 관리자 초기화
            _timeManager.Reset();
            
            Invalidate();
        }
        
        #endregion

        #region IProgressStepReporter 구현
        
        /// <summary>
        /// 특정 단계를 진행 중으로 설정
        /// </summary>
        /// <param name="stepIndex">진행할 단계 인덱스 (0부터 시작)</param>
        public void ReportStepProgress(int stepIndex)
        {
            if (stepIndex >= 0 && stepIndex < _totalSteps)
            {
                // 첫 번째 단계 시작 시 처리 시작 (통합 시간 관리자에서 관리)
                if (!_timeManager.IsProcessing)
                {
                    _timeManager.StartProcessing();
                }
                
                CurrentStepIndex = stepIndex;
            }
        }
        
        /// <summary>
        /// 특정 단계를 완료로 표시
        /// </summary>
        /// <param name="stepIndex">완료할 단계 인덱스 (0부터 시작)</param>
        public void ReportStepCompleted(int stepIndex)
        {
            if (stepIndex >= 0 && stepIndex < _totalSteps)
            {
                CompleteStep(stepIndex);
            }
        }
        
        /// <summary>
        /// 모든 단계 초기화
        /// </summary>
        public void ResetAllSteps()
        {
            ResetProgress();
        }
        
        /// <summary>
        /// 처리 완료 상태로 설정 (메시지 창 표시 시점에 호출)
        /// </summary>
        public void SetProcessingCompleted()
        {
            // 통합 시간 관리자에서 처리 완료 설정
            if (_timeManager.IsProcessing)
            {
                _timeManager.CompleteProcessing();
            }
        }
        
        /// <summary>
        /// 처리 시간을 초 단위로 포맷팅 (통합 시간 관리자 사용)
        /// </summary>
        /// <returns>포맷팅된 처리 시간 문자열</returns>
        private string GetFormattedProcessingTime()
        {
            // 통합 시간 관리자에서 경과 시간 가져오기
            var elapsedTime = _timeManager.GetElapsedTime();
            
            // 초 단위로 표시 (소수점 1자리)
            return $"{elapsedTime.TotalSeconds:F1}초";
        }
        
        #endregion

        #region 그리기 (Drawing)
        
        /// <summary>
        /// 컨트롤 그리기
        /// </summary>
        /// <param name="e">그리기 이벤트 인수</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (_totalSteps == 0) return;
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // 부드러운 그리기
            
            // 처리 시간 표시 (오른쪽 상단)
            DrawProcessingTime(g);
            
            // 원형 차트 그리기
            DrawProgressChart(g);
            
            // 단계별 상태 표시
            DrawStepStatus(g);
        }
        
        /// <summary>
        /// 처리 시간 표시 (오른쪽 상단)
        /// </summary>
        /// <param name="g">그래픽스 객체</param>
        private void DrawProcessingTime(Graphics g)
        {
            // 오른쪽 상단 위치 계산
            int rightMargin = 20;
            int topMargin = 20;
            int x = this.Width - rightMargin;
            int y = topMargin;
            
            // 시계 아이콘 추가 (가장 안전한 유니코드 문자 사용)
            string clockIcon = "⏰";
            
            // 처리 시간 텍스트 생성
            string timeText = $"총 처리 시간: {GetFormattedProcessingTime()}";
            
            // 폰트 크기를 더 작게 조정 (화면 크기에 따라)
            //float fontSize = Math.Max(6f, Math.Min(9f, this.Width / 120f));
            float fontSize = 9f;
            using (Font font = new Font("맑은 고딕", fontSize, FontStyle.Regular))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Far; // 오른쪽 정렬
                sf.LineAlignment = StringAlignment.Near; // 상단 정렬
                
                // 텍스트 크기 측정
                SizeF textSize = g.MeasureString(timeText, font);
                
                // 시계 아이콘 크기 측정
                SizeF iconSize = g.MeasureString(clockIcon, font);
                
                // 시계 아이콘 그리기 (텍스트 왼쪽에 배치)
                g.DrawString(clockIcon, font, Brushes.DarkSlateGray, 
                    new RectangleF(x - textSize.Width - iconSize.Width - 5, y, iconSize.Width, iconSize.Height), sf);
                
                // 텍스트 그리기 (아이콘 오른쪽에 배치)
                g.DrawString(timeText, font, Brushes.DarkSlateGray, 
                    new RectangleF(x - textSize.Width, y, textSize.Width, textSize.Height), sf);
            }
        }
        
        /// <summary>
        /// 진행률 차트 그리기
        /// </summary>
        /// <param name="g">그래픽스 객체</param>
        private void DrawProgressChart(Graphics g)
        {
            int centerX = this.Width / 2;
            
            // 화면 크기에 따른 동적 반지름 계산 (세로 크기 고려)
            int maxRadius = Math.Min(this.Width, this.Height) / 4;
            int minRadius = 30; // 최소 반지름
            int radius = Math.Max(minRadius, maxRadius);
            
            // 원형 차트 위치를 화면 크기에 따라 동적으로 조정 (상단 여백 확보)
            int centerY;
            if (this.Height < 300)
            {
                // 작은 화면에서는 상단 1/3 지점 + 여백
                centerY = (this.Height / 3) + 40;
            }
            else
            {
                // 큰 화면에서는 상단 1/5 지점 + 여백 (더 많은 상단 공간 확보)
                centerY = (this.Height / 5) + 60;
            }
            
            // 펜 두께를 더 두껍게 조정 (원형 차트의 시각적 임팩트 향상)
            float penWidth = Math.Max(12f, Math.Min(22f, radius / 4f));
            _progressPen.Width = penWidth;
            _backgroundPen.Width = penWidth;
            
            // 배경 원 그리기
            g.DrawEllipse(_backgroundPen, centerX - radius, centerY - radius, radius * 2, radius * 2);
            
            // 진행률 원 그리기
            if (_totalSteps > 0)
            {
                float progressPercentage = (float)_completedSteps / _totalSteps;
                float sweepAngle = progressPercentage * 360f;
                
                if (sweepAngle > 0)
                {
                    g.DrawArc(_progressPen, centerX - radius, centerY - radius, radius * 2, radius * 2, -90f, sweepAngle);
                }
            }
            
            // 중앙 텍스트 그리기 (폰트 크기 동적 조정)
            string progressText = $"{_completedSteps}/{_totalSteps}";
            if (_totalSteps > 0)
            {
                progressText += $"\n{(_completedSteps * 100 / _totalSteps):D}%";
            }
            
            float fontSize = Math.Max(8f, Math.Min(16f, radius / 3f));
            using (Font font = new Font("맑은 고딕", fontSize, FontStyle.Bold))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                
                SizeF textSize = g.MeasureString(progressText, font);
                g.DrawString(progressText, font, Brushes.DarkSlateGray, 
                    new RectangleF(centerX - textSize.Width / 2, centerY - textSize.Height / 2, 
                    textSize.Width, textSize.Height), sf);
            }
        }
        
        /// <summary>
        /// 단계별 상태 표시
        /// </summary>
        /// <param name="g">그래픽스 객체</param>
        private void DrawStepStatus(Graphics g)
        {
            if (_progressSteps.Count == 0) return;
            
            // 동적 시작 위치 계산 (원형 차트 아래 여백 고려)
            int chartCenterY;
            if (this.Height < 300)
            {
                chartCenterY = (this.Height / 3) + 40;
            }
            else
            {
                chartCenterY = (this.Height / 5) + 60;
            }
            
            int chartRadius = Math.Max(30, Math.Min(this.Width, this.Height) / 4);
            int startY = Math.Max(chartCenterY + chartRadius + 30, this.Height / 2); // 최소한 중간 이하에서 시작
            
            // 화면 높이에 따른 동적 단계 높이 조정
            int availableHeight = this.Height - startY - 20; // 하단 여백 20px
            int stepHeight = Math.Max(20, Math.Min(30, availableHeight / Math.Max(1, (_progressSteps.Count + 3) / 4))); // 4열 기준
            
            // 화면 크기에 따른 동적 조정
            int minStepWidth = 200; // 최소 단계 너비
            int maxStepsPerRow = Math.Max(1, (this.Width - 40) / minStepWidth); // 여백 고려하여 계산
            
            // 폰트 크기 동적 조정
            float iconFontSize = Math.Max(8f, Math.Min(12f, this.Width / 80f));
            float codeFontSize = Math.Max(7f, Math.Min(9f, this.Width / 90f));
            float descFontSize = Math.Max(6f, Math.Min(8f, this.Width / 100f));
            
            for (int i = 0; i < _progressSteps.Count; i++)
            {
                int row = i / maxStepsPerRow;
                int col = i % maxStepsPerRow;
                
                int stepWidth = (this.Width - 40) / maxStepsPerRow;
                int x = 20 + col * stepWidth;
                int y = startY + row * stepHeight;
                
                // 단계 상태에 따른 색상 및 아이콘 결정
                Color stepColor;
                string stepIcon;
                
                if (i < _completedSteps)
                {
                    stepColor = Color.FromArgb(46, 204, 113); // 완료: 초록색
                    stepIcon = "●";
                }
                else if (i == _currentStepIndex)
                {
                    stepColor = Color.FromArgb(52, 152, 219); // 진행중: 파란색
                    stepIcon = "🔄";
                }
                else
                {
                    stepColor = Color.FromArgb(189, 195, 199); // 대기: 회색
                    stepIcon = "⭕";
                }
                
                // 단계 아이콘 그리기
                using (Font iconFont = new Font("맑은 고딕", iconFontSize))
                {
                    g.DrawString(stepIcon, iconFont, new SolidBrush(stepColor), x, y);
                }
                
                // 단계 코드 그리기 (예: [4-1]) - 완료된 단계는 Bold 스타일 적용
                string stepCode = _progressSteps[i].CodeName;
                FontStyle codeFontStyle = (i < _completedSteps) ? FontStyle.Bold : FontStyle.Regular;
                using (Font codeFont = new Font("맑은 고딕", codeFontSize, codeFontStyle))
                {
                    g.DrawString(stepCode, codeFont, new SolidBrush(stepColor), x + 25, y);
                }
                
                // 단계 설명 그리기 (화면 크기에 따른 동적 축약) - 완료된 단계는 Bold 스타일 적용
                string stepDescription = _progressSteps[i].Description ?? "";
                int maxDescLength = Math.Max(10, stepWidth / 8); // 화면 너비에 따른 최대 길이
                if (stepDescription.Length > maxDescLength)
                {
                    stepDescription = stepDescription.Substring(0, maxDescLength) + "...";
                }
                
                FontStyle descFontStyle = (i < _completedSteps) ? FontStyle.Bold : FontStyle.Regular;
                using (Font descFont = new Font("맑은 고딕", descFontSize, descFontStyle))
                {
                    g.DrawString(stepDescription, descFont, new SolidBrush(stepColor), x + 90, y);
                }
            }
        }
        
        #endregion

        #region 리소스 해제 (Dispose)
        
        /// <summary>
        /// 리소스 해제
        /// </summary>
        /// <param name="disposing">관리되는 리소스 해제 여부</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 시간 관리자 이벤트 구독 해제
                _timeManager.TimeUpdated -= OnTimeUpdated;
                _timeManager.ProcessingCompleted -= OnProcessingCompleted;
                _timeManager.StepUpdated -= OnStepUpdated;
                
                _progressPen?.Dispose();
                _backgroundPen?.Dispose();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
        
        #endregion
    }
}
