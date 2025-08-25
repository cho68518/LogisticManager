using LogisticManager.Services;
using LogisticManager.Models;
using LogisticManager.Processors;
using LogisticManager.Repositories;
using System.Drawing.Drawing2D;
using System.Configuration;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 송장 처리 프로그램의 메인 폼
    /// 
    /// 주요 기능:
    /// - Excel 파일 선택 및 업로드
    /// - 송장 처리 작업 실행
    /// - 실시간 진행률 표시
    /// - 로그 메시지 출력
    /// - 설정 관리
    /// 
    /// 사용 방법:
    /// 1. "파일 선택" 버튼으로 Excel 파일 선택
    /// 2. "설정" 버튼으로 데이터베이스/API 설정
    /// 3. "송장 처리 시작" 버튼으로 자동화 작업 실행
    /// </summary>
    public partial class MainForm : Form
    {
        #region 필드 (Private Fields)
        
        /// <summary>
        /// 파일 처리 서비스 - Excel 파일 읽기/쓰기 담당
        /// </summary>
        private readonly FileService _fileService;
        
        /// <summary>
        /// 데이터베이스 서비스 - MySQL 연결 및 쿼리 실행 담당
        /// </summary>
        private DatabaseService _databaseService;
        
        /// <summary>
        /// API 서비스 - Dropbox 업로드, Kakao Work 알림 담당
        /// </summary>
        private readonly ApiService _apiService;
        
        /// <summary>
        /// 사용자가 선택한 Excel 파일의 전체 경로
        /// </summary>
        private string? _selectedFilePath;

        #endregion

        #region UI 컨트롤 (UI Controls)
        
        /// <summary>
        /// 파일 선택 버튼 - Excel 파일을 선택하는 대화상자 열기
        /// </summary>
        private Button btnSelectFile = null!;
        
        /// <summary>
        /// 송장 처리 시작 버튼 - 선택된 파일로 자동화 작업 실행
        /// </summary>
        private Button btnStartProcess = null!;
        
        /// <summary>
        /// 설정 버튼 - 데이터베이스/API 설정 창 열기
        /// </summary>
        private Button btnSettings = null!;
        
        /// <summary>
        /// 선택된 파일 경로 표시 라벨
        /// </summary>
        private Label lblFilePath = null!;
        
        /// <summary>
        /// 로그 메시지 출력 텍스트박스 (검은 배경, 녹색 글씨)
        /// </summary>
        private RichTextBox txtLog = null!;
        
        /// <summary>
        /// 진행률 표시바 - 송장 처리 작업의 진행 상황 표시
        /// </summary>
        private ProgressBar progressBar = null!;

        /// <summary>
        /// 진행상황 표시 컨트롤 - 원형 차트와 단계별 상태 표시
        /// </summary>
        private ProgressDisplayControl progressDisplayControl = null!;

        /// <summary>
        /// 데이터베이스 연결 상태 표시 라벨
        /// </summary>
        private Label lblDbStatus = null!;

        /// <summary>
        /// 타이틀 라벨 - 프로그램 제목
        /// </summary>
        private Label lblTitle = null!;

        /// <summary>
        /// 상태 라벨 - 현재 상태 표시
        /// </summary>
        private Label lblStatus = null!;

        /// <summary>
        /// 종료 버튼 - 프로그램 완전 종료
        /// </summary>
        private Button btnExit = null!;

        /// <summary>
        /// Dropbox 테스트 버튼 - Dropbox 연결 테스트 및 파일 업로드
        /// </summary>
        private Button btnDropboxTest = null!;

        /// <summary>
        /// 판매입력 데이터 처리 버튼 - ProcessSalesInputData 메서드를 독립적으로 실행
        /// </summary>
        private Button btnSalesDataProcess = null!;

        /// <summary>
        /// 디버그용 판매입력 데이터 처리 버튼 - 문제 진단용
        /// </summary>
        private Button btnDebugSalesData = null!;

        /// <summary>
        /// KakaoWork 테스트 버튼
        /// </summary>
        private Button btnKakaoWorkTest = null!;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// MainForm 생성자
        /// 
        /// 초기화 순서:
        /// 1. 폼 기본 설정 (InitializeComponent)
        /// 2. 서비스 객체들 초기화 (FileService, DatabaseService, ApiService)
        /// 3. UI 컨트롤들 생성 및 배치 (InitializeUI)
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
            // 서비스 객체들 초기화
            _fileService = new FileService();
            _databaseService = DatabaseService.Instance; // Singleton 인스턴스 사용
            _apiService = new ApiService();
            
            InitializeUI();
            
            // 통합 시간 관리자 이벤트 구독
            ProcessingTimeManager.Instance.ProcessingStarted += OnProcessingStarted;
            ProcessingTimeManager.Instance.ProcessingCompleted += OnProcessingCompleted;
            ProcessingTimeManager.Instance.StepUpdated += OnStepUpdated;
            ProcessingTimeManager.Instance.TimeUpdated += OnTimeUpdated;
            
            // 데이터베이스 연결 테스트 및 완료 메시지 표시
            TestDatabaseConnection();
            
            // Dropbox 연결 테스트
            TestDropboxConnection();
            
            // KakaoWork 연결 테스트
            TestKakaoWorkConnection();
            
            // 진행상황 단계 데이터 로딩
            _ = LoadProgressStepsAsync();

        }

        #endregion

        #region UI 초기화 (UI Initialization)

        /// <summary>
        /// 기본 폼 설정을 초기화하는 메서드
        /// </summary>
        private void InitializeComponent()
        {
            // 기본 폼 설정
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        /// <summary>
        /// UI 컨트롤들을 초기화하고 배치하는 메서드
        /// 
        /// 배치된 컨트롤들:
        /// - 타이틀 라벨 (상단 중앙)
        /// - 파일 선택 버튼 (좌상단)
        /// - 선택된 파일 경로 라벨 (파일 선택 버튼 옆)
        /// - 설정 버튼 (우상단)
        /// - 송장 처리 시작 버튼 (중앙 상단)
        /// - 진행률 표시바 (처리 시작 버튼 옆)
        /// - 상태 라벨 (진행률 표시바 아래)
        /// - 로그 표시 텍스트박스 (하단 전체)
        /// </summary>
        private void InitializeUI()
        {
            // 폼 기본 설정
            this.Text = "송장 처리 시스템";
            this.Size = new Size(1100, 900); // 폼 크기를 1100으로 조정
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable; // 크기 조절 가능하도록 변경
            this.MaximizeBox = true; // 최대화 버튼 활성화
            this.MinimizeBox = true; // 최소화 버튼 활성화
            this.MinimumSize = new Size(1000, 700); // 최소 크기도 더 크게 조정
            this.BackColor = Color.FromArgb(240, 244, 248); // 연한 회색 배경

            // 타이틀 라벨 생성 및 설정
            lblTitle = new Label
            {
                Text = "📦 송장 처리 시스템",
                Location = new Point(20, 20),
                Size = new Size(860, 40),
                Font = new Font("맑은 고딕", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 파일 선택 버튼 생성 및 설정 (둥근 모서리, 그라데이션)
            btnSelectFile = CreateModernButton("📁 파일 선택", new Point(20, 80), new Size(150, 45));
            btnSelectFile.Click += BtnSelectFile_Click;

            // 파일 경로 라벨 생성 및 설정 (파일 선택 버튼 밑에 위치)
            lblFilePath = new Label
            {
                Text = "선택된 파일: 없음",
                Location = new Point(20, 130), // 파일 선택 버튼 밑으로 이동
                Size = new Size(400, 20), // 크기를 늘려서 긴 파일명도 표시 가능하도록
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(127, 140, 141),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 설정 버튼 생성 및 설정 (우상단 고정)
            btnSettings = CreateModernButton("⚙️ 설정", new Point(690, 80), new Size(80, 40), Color.FromArgb(52, 152, 219));
            btnSettings.Click += BtnSettings_Click;

            // Dropbox 테스트 버튼 생성 및 설정 (우상단 고정)
            btnDropboxTest = CreateModernButton("☁️ Dropbox 테스트", new Point(550, 80), new Size(130, 40), Color.FromArgb(155, 89, 182));
            btnDropboxTest.Click += BtnDropboxTest_Click;

            // KakaoWork 테스트 버튼 생성 및 설정 (우상단 고정)
            btnKakaoWorkTest = CreateModernButton("💬 KakaoWork 테스트", new Point(410, 80), new Size(130, 40), Color.FromArgb(46, 204, 113));
            btnKakaoWorkTest.Click += BtnKakaoWorkTest_Click;

            // 종료 버튼 생성 및 설정 (우상단 고정)
            btnExit = CreateModernButton("❌ 종료", new Point(790, 80), new Size(80, 40), Color.FromArgb(231, 76, 60));
            btnExit.Click += BtnExit_Click;

            // 송장 처리 시작 버튼 생성 및 설정 (파일선택 버튼 오른쪽에 배치)
            btnStartProcess = CreateModernButton("🚀 송장 처리 시작", new Point(150, 80), new Size(150, 45), Color.FromArgb(46, 204, 113));
            btnStartProcess.Enabled = false;  // 파일이 선택되기 전까지 비활성화
            btnStartProcess.Click += BtnStartProcess_Click;

            // 판매입력 데이터 처리 버튼 생성 및 설정 (독립 실행용) - 현재 숨김 처리
            btnSalesDataProcess = CreateModernButton("📊 판매입력 데이터 처리", new Point(180, 160), new Size(150, 45), Color.FromArgb(155, 89, 182));
            btnSalesDataProcess.Click += BtnSalesDataProcess_Click;
            btnSalesDataProcess.Visible = false; // 버튼 숨김 처리

            // 디버그용 버튼 (임시)
            btnDebugSalesData = CreateModernButton("🐛 디버그: 판매입력", new Point(340, 160), new Size(120, 45), Color.FromArgb(231, 76, 60));
            btnDebugSalesData.Click += BtnDebugSalesData_Click;

            // 진행률 표시바 생성 및 설정 (현재 숨김 처리됨 - 원형 진행률 차트로 대체)
            progressBar = new ProgressBar
            {
                Location = new Point(190, 165),
                Size = new Size(500, 35),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false // 진행률바 숨김 처리 - 원형 진행률 차트 사용
            };

            // 진행률 표시바 스타일 설정
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.ForeColor = Color.FromArgb(46, 204, 113);

            // 상태 라벨 생성 및 설정
            lblStatus = new Label
            {
                Text = "대기 중...",
                Location = new Point(190, 205),
                Size = new Size(500, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(127, 140, 141),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 데이터베이스 연결 상태 표시 라벨 생성 및 설정
            lblDbStatus = new Label
            {
                Text = "데이터베이스 연결 확인 중...",
                Location = new Point(800, 240), // 초기 위치를 오른쪽으로 설정
                Font = new Font("맑은 고딕", 8F), // 폰트 크기를 8로 줄이고 Bold 제거
                ForeColor = Color.FromArgb(52, 73, 94),
                BackColor = Color.Transparent, // 배경색을 투명하게
                TextAlign = ContentAlignment.MiddleRight, // 오른쪽 정렬로 변경
                BorderStyle = BorderStyle.None, // 테두리 제거
                AutoSize = true, // 자동 크기 조정 활성화하여 텍스트 완전 표시
                MaximumSize = new Size(400, 25) // 최대 크기 제한 (폭: 400px, 높이: 25px)
            };

            // 진행상황 표시 컨트롤 생성 및 설정 (60% 비율)
            progressDisplayControl = new ProgressDisplayControl
            {
                Location = new Point(20, 265), // 데이터베이스 상태 라벨 아래로 이동 (위치 조정)
                Size = new Size(1160, 360), // 높이 조정 (상태 라벨 공간 고려)
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 로그 표시 텍스트박스 생성 및 설정 (40% 비율)
            txtLog = new RichTextBox
            {
                Location = new Point(20, 660), // 진행상황 컨트롤 아래로 이동 (위치 조정됨)
                Size = new Size(1160, 200), // 높이 조정 (40% 비율)
                ReadOnly = true,  // 사용자 입력 방지
                Font = new Font("맑은 고딕", 9F),
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.FromArgb(46, 204, 113),  // 밝은 녹색
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // 모든 컨트롤을 폼에 추가
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                btnSelectFile,
                lblFilePath,
                btnSettings,
                btnDropboxTest,
                btnKakaoWorkTest,
                btnExit,
                btnStartProcess,
                btnSalesDataProcess,
                progressBar,
                lblStatus,
                lblDbStatus,
                progressDisplayControl,
                txtLog
            });

            // 폼 리사이즈 이벤트 핸들러 추가
            this.Resize += MainForm_Resize;

            // 초기 크기 조정 적용
            MainForm_Resize(this, EventArgs.Empty);

            // 초기 로그 메시지 출력
            LogMessage("🎉 송장 처리 시스템이 시작되었습니다.");
            LogMessage("📁 파일을 선택하고 '송장 처리 시작' 버튼을 클릭하세요.");
        }

        /// <summary>
        /// 모던한 스타일의 버튼을 생성하는 메서드
        /// </summary>
        /// <param name="text">버튼 텍스트</param>
        /// <param name="location">위치</param>
        /// <param name="size">크기</param>
        /// <param name="backgroundColor">배경색</param>
        /// <returns>생성된 버튼</returns>
        private Button CreateModernButton(string text, Point location, Size size, Color? backgroundColor = null)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                Font = new Font("맑은 고딕", 8F, FontStyle.Bold), // 폰트 크기를 8로 더 작게 조정
                FlatStyle = FlatStyle.Flat,
                BackColor = backgroundColor ?? Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }, // 테두리 제거
                TextAlign = ContentAlignment.MiddleCenter, // 텍스트 중앙 정렬
                UseMnemonic = false // 앰퍼샌드(&) 문자를 특수 문자로 처리하지 않음
            };

            // 둥근 모서리 제거 - 일반 사각형 버튼 사용
            // button.Region = new Region(CreateRoundedRectangle(button.ClientRectangle, 10));

            // 호버 효과
            button.MouseEnter += (sender, e) =>
            {
                button.BackColor = Color.FromArgb(
                    Math.Min(255, button.BackColor.R + 20),
                    Math.Min(255, button.BackColor.G + 20),
                    Math.Min(255, button.BackColor.B + 20)
                );
            };

            button.MouseLeave += (sender, e) =>
            {
                button.BackColor = backgroundColor ?? Color.FromArgb(52, 152, 219);
            };

            return button;
        }

        /// <summary>
        /// 둥근 모서리 사각형 경로를 생성하는 메서드
        /// </summary>
        /// <param name="rect">사각형 영역</param>
        /// <param name="radius">모서리 반지름</param>
        /// <returns>둥근 모서리 경로</returns>
        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Width - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Width - diameter, rect.Height - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// 폼 크기 변경 시 UI 컨트롤들을 적절히 조정하는 이벤트 핸들러
        /// 
        /// 조정되는 요소들:
        /// - 타이틀 라벨: 폼 너비에 맞춰 조정
        /// - 파일 경로 라벨: 남은 공간에 맞춰 조정
        /// - 설정 버튼: 우측 상단 고정
        /// - 진행률 표시바: 남은 공간에 맞춰 조정
        /// - 로그 텍스트박스: 폼 크기에 맞춰 조정
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                return;

            // 폼 패딩 설정
            const int padding = 20;
            const int titleHeight = 40;

            // 타이틀 라벨 조정
            lblTitle.Size = new Size(this.ClientSize.Width - (padding * 2), titleHeight);
            lblTitle.Location = new Point(padding, padding);

            // 파일 선택 버튼 위치 고정 (좌상단)
            btnSelectFile.Location = new Point(padding, padding + titleHeight + 20);

            // 파일 경로 라벨 조정 (파일 선택 버튼 밑에 위치)
            lblFilePath.Location = new Point(btnSelectFile.Location.X, btnSelectFile.Location.Y + btnSelectFile.Height + 10);
            lblFilePath.Size = new Size(400, 20); // 고정 크기로 설정

            // 우상단 버튼들 위치 동적 조정 (창 크기에 따라 항상 오른쪽에 정렬)
            int buttonSpacing = 10; // 버튼 간 간격
            int rightMargin = padding; // 오른쪽 여백
            
            // 오른쪽부터 역순으로 배치 (Exit → Settings → Dropbox → KakaoWork)
            // 각 버튼의 실제 Width 속성을 사용하여 정확한 위치 계산
            btnExit.Location = new Point(this.ClientSize.Width - rightMargin - btnExit.Width, padding + titleHeight + 20);
            btnSettings.Location = new Point(btnExit.Location.X - btnSettings.Width - buttonSpacing, padding + titleHeight + 20);
            btnDropboxTest.Location = new Point(btnSettings.Location.X - btnDropboxTest.Width - buttonSpacing, padding + titleHeight + 20);
            btnKakaoWorkTest.Location = new Point(btnDropboxTest.Location.X - btnKakaoWorkTest.Width - buttonSpacing, padding + titleHeight + 20);

            // 송장 처리 시작 버튼 위치 조정 (파일선택 버튼 오른쪽 옆에 위치)
            btnStartProcess.Location = new Point(btnSelectFile.Location.X + btnSelectFile.Width + 10, btnSelectFile.Location.Y);

            // 진행률 표시바 조정 (현재 숨김 처리됨)
            int progressBarWidth = this.ClientSize.Width - btnStartProcess.Width - (padding * 3);
            progressBar.Size = new Size(progressBarWidth, 35);
            progressBar.Location = new Point(btnStartProcess.Location.X + btnStartProcess.Width + 20, btnStartProcess.Location.Y + 5);

            // 상태 라벨 조정 (송장처리시작 버튼 오른쪽 옆에 위치)
            lblStatus.Size = new Size(200, 20); // 고정 크기로 설정
            lblStatus.Location = new Point(btnStartProcess.Location.X + btnStartProcess.Width + 10, btnStartProcess.Location.Y + 12); // 버튼 중앙에 맞춤

            // 진행상황 표시 컨트롤 조정 (동적 높이) - 먼저 위치 계산
            int progressTop = btnStartProcess.Location.Y + btnStartProcess.Height + 40; // 버튼 아래 40px 여백
            int remainingHeight = this.ClientSize.Height - progressTop - (padding * 2);
            
            // 진행상황 컨트롤과 로그의 비율 설정 (진행상황: 60%, 로그: 40%)
            int progressHeight = Math.Max(200, Math.Min(500, (int)(remainingHeight * 0.6))); // 최소 200px, 최대 500px
            int logHeight = remainingHeight - progressHeight - 20; // 여백 20px 고려
            
            // 로그 높이가 너무 작아지지 않도록 보장
            if (logHeight < 150)
            {
                progressHeight = remainingHeight - 170; // 로그를 최소 150px로 보장
                logHeight = 150;
            }
            
            progressDisplayControl.Size = new Size(this.ClientSize.Width - (padding * 2), progressHeight);
            progressDisplayControl.Location = new Point(padding, progressTop);

            // 데이터베이스 상태 라벨 조정 (진행상황 컨트롤 위의 오른쪽 끝에 위치)
            int dbStatusTop = progressDisplayControl.Location.Y - 25; // 진행상황 컨트롤 위 25px
            int dbStatusLeft = progressDisplayControl.Location.X + progressDisplayControl.Width - 300; // 오른쪽 끝에서 300px 왼쪽
            lblDbStatus.Location = new Point(Math.Max(padding, dbStatusLeft), dbStatusTop);
            // AutoSize = true이므로 Size는 자동으로 조정됨

            // 로그 텍스트박스 조정 (진행상황 컨트롤 아래)
            int logTop = progressDisplayControl.Location.Y + progressDisplayControl.Height + 20;
            txtLog.Size = new Size(this.ClientSize.Width - (padding * 2), logHeight);
            txtLog.Location = new Point(padding, logTop);
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// 설정 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - SettingsForm을 모달로 열기
        /// - 설정 변경 후 로그 메시지 출력
        /// - 예외 처리 및 오류 메시지 표시
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnSettings_Click(object? sender, EventArgs e)
        {
            try
            {
                // 설정 폼을 모달로 열기
                var settingsForm = new SettingsForm();
                var result = settingsForm.ShowDialog(this);
                
                // 설정이 변경되었는지 확인하고 DatabaseService 재초기화
                if (result == DialogResult.OK)
                {
                    // 설정이 실제로 변경되었는지 확인
                    if (settingsForm.SettingsChanged)
                    {
                        try
                        {
                            // DatabaseService 재초기화 (새로운 설정 적용)
                            var newDatabaseService = new DatabaseService();
                            
                            // 연결 테스트 (비동기로 실행)
                            var testResult = await newDatabaseService.TestConnectionAsync();
                            
                            if (testResult)
                            {
                                // 성공 시 로그 메시지만 출력 (readonly 필드이므로 재할당 불가)
                                LogMessage("✅ 데이터베이스 설정이 업데이트되었습니다.");
                                LogMessage("🔗 새로운 설정으로 데이터베이스 연결이 성공했습니다.");
                                LogMessage("💡 프로그램을 재시작하면 새로운 설정이 적용됩니다.");
                            }
                            else
                            {
                                LogMessage("⚠️ 데이터베이스 설정이 업데이트되었지만 연결 테스트에 실패했습니다.");
                            }
                        }
                        catch (Exception dbEx)
                        {
                            LogMessage($"⚠️ 데이터베이스 서비스 재초기화 중 오류: {dbEx.Message}");
                            LogMessage("💡 설정은 저장되었지만 데이터베이스 연결에 문제가 있을 수 있습니다.");
                        }
                    }
                    else
                    {
                        LogMessage("ℹ️ 설정이 변경되지 않았습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 설정 창 열기 중 오류 발생: {ex.Message}");
                MessageBox.Show($"설정 창을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Dropbox 테스트 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - DropboxTestForm을 모달로 열기
        /// - Dropbox 연결 테스트 및 파일 업로드 기능 제공
        /// - 예외 처리 및 오류 메시지 표시
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void BtnDropboxTest_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("☁️ Dropbox 테스트 화면을 엽니다...");
                
                // Dropbox 테스트 폼을 모달로 열기
                var dropboxTestForm = new DropboxTestForm();
                dropboxTestForm.ShowDialog(this);
                
                LogMessage("✅ Dropbox 테스트 화면이 닫혔습니다.");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Dropbox 테스트 화면 열기 중 오류 발생: {ex.Message}");
                MessageBox.Show($"Dropbox 테스트 화면을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// KakaoWork 테스트 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - KakaoWorkTestForm을 모달로 열기
        /// - KakaoWork 연결 테스트 및 파일 업로드 기능 제공
        /// - 예외 처리 및 오류 메시지 표시
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void BtnKakaoWorkTest_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("💬 KakaoWork 테스트 화면을 엽니다...");
                
                // KakaoWork 테스트 폼을 모달로 열기
                var kakaoWorkTestForm = new KakaoWorkTestForm();
                kakaoWorkTestForm.ShowDialog(this);
                
                LogMessage("✅ KakaoWork 테스트 화면이 닫혔습니다.");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ KakaoWork 테스트 화면 열기 중 오류 발생: {ex.Message}");
                MessageBox.Show($"KakaoWork 테스트 화면을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 파일 선택 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - Excel 파일 선택 대화상자 열기
        /// - 선택된 파일 경로 저장 및 표시
        /// - 송장 처리 시작 버튼 활성화
        /// - 로그 메시지 출력
        /// - 예외 처리 및 오류 메시지 표시
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void BtnSelectFile_Click(object? sender, EventArgs e)
        {
            try
            {
                // 파일 선택 대화상자 설정
                using var openFileDialog = new OpenFileDialog
                {
                    Title = "Excel 파일 선택",
                    Filter = "Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls|모든 파일 (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };

                // 파일 선택 대화상자 실행
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 새 파일이 선택되었으므로 로그 초기화
                    //ClearLog();
                    
                    // UI 상태 초기화
                    progressBar.Value = 0;
                    lblStatus.Text = "파일 선택됨";
                    lblStatus.ForeColor = Color.FromArgb(52, 152, 219);
                    
                    _selectedFilePath = openFileDialog.FileName;
                    var fileName = Path.GetFileName(_selectedFilePath);
                    lblFilePath.Text = $"📄 선택된 파일: {fileName}";
                    btnStartProcess.Enabled = true;
                    
                    LogMessage($"📁 새 파일이 선택되었습니다: {fileName}");
                    LogMessage($"📊 파일 크기: {new FileInfo(_selectedFilePath).Length / 1024} KB");
                    LogMessage($"⏰ 선택 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 파일 선택 중 오류 발생: {ex.Message}");
                MessageBox.Show($"파일을 선택할 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 송장 처리 시작 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - UI 상태 변경 (버튼 비활성화, 진행률 초기화)
        /// - InvoiceProcessor를 사용한 송장 처리 작업 실행
        /// - 실시간 진행률 및 로그 메시지 표시
        /// - 작업 완료 후 결과 메시지 표시
        /// - 예외 처리 및 오류 메시지 표시
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnStartProcess_Click(object? sender, EventArgs e)
        {
            // 파일이 선택되지 않은 경우 처리
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("먼저 Excel 파일을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 통합 시간 관리자 시작 (송장처리 시작 버튼 클릭 시점)
                ProcessingTimeManager.Instance.StartProcessing();
                
                // UI 상태 변경
                btnStartProcess.Enabled = false;
                btnSelectFile.Enabled = false;
                btnSettings.Enabled = false;
                // progressBar.Value = 0; // 진행률바 숨김 처리됨 - 원형 진행률 차트 사용
                lblStatus.Text = "처리 중...";
                lblStatus.ForeColor = Color.FromArgb(52, 152, 219);

                //LogMessage("🚀 송장 처리 작업을 시작합니다...");

                // 진행률 콜백 설정 (현재 진행률바 숨김 처리됨 - 원형 진행률 차트로 대체)
                var progressCallback = new Progress<int>(value => 
                { 
                    // progressBar.Value = value; // 진행률바 숨김 처리됨
                    Application.DoEvents(); 
                });
                
                // 로그 콜백 설정
                var logCallback = new Progress<string>(message => 
                { 
                    LogMessage(message); 
                    Application.DoEvents(); 
                });

                // InvoiceProcessor 생성 및 처리 실행
                var processor = new InvoiceProcessor(_fileService, _databaseService, _apiService, 
                    logCallback, progressCallback, progressDisplayControl);

                // 진행상황 단계별 업데이트 콜백 설정
                var stepProgressCallback = new Progress<int>(stepIndex => 
                { 
                    progressDisplayControl?.ReportStepProgress(stepIndex);
                    Application.DoEvents(); 
                });
                
                // 송장 처리 실행
                // ProcessAsync 메서드 호출
                // 매개변수:
                //   _selectedFilePath : 사용자가 선택한 엑셀 파일 경로
                //   logCallback       : 로그 메시지 Progress 콜백 (UI 및 로그 기록용)
                //   progressCallback  : 진행률 Progress 콜백 (UI 진행률 표시용)
                //   1                 : 처리 단계(1단계, 기본값)  ([4-1]~[4-22])
                var testLevel = ConfigurationManager.AppSettings["TestLevel"] ?? "1"; // app.config에서 테스트 레벨 가져오기
                var result = await processor.ProcessAsync(_selectedFilePath, logCallback, progressCallback, int.Parse(testLevel));

                // 처리 결과에 따른 메시지 표시 (약간의 지연을 두어 로그 순서 보장)
                await Task.Delay(100); // UI 업데이트를 위한 짧은 지연
                
                if (result)
                {
                    // 성공적인 처리 완료
                    LogMessage("✅ 송장 처리가 성공적으로 완료되었습니다!");
                    lblStatus.Text = "완료";
                    lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                    
                    // 진행상황 컨트롤에 처리 완료 상태 설정 (통합 시간 관리자에서 자동 처리됨)
                    progressDisplayControl?.SetProcessingCompleted();
                    
                    MessageBox.Show("송장 처리가 성공적으로 완료되었습니다!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // 처리 중단 (데이터가 없는 경우 등)
                    LogMessage("⚠️ 송장 처리가 중단되었습니다. 처리 가능한 데이터가 없거나 파일 형식에 문제가 있을 수 있습니다.");
                    lblStatus.Text = "처리 중단";
                    lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                    
                    // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                    progressDisplayControl?.SetProcessingCompleted();
                    
                    MessageBox.Show("송장 처리가 중단되었습니다.\n\n확인사항:\n• 파일에 처리 가능한 주문 데이터가 있는지 확인\n• 파일 형식이 올바른지 확인\n• 헤더 행이 존재하는지 확인", "처리 중단", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // 오류 처리
                LogMessage($"❌ 송장 처리 중 오류가 발생했습니다: {ex.Message}");
                lblStatus.Text = "오류 발생";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                
                // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                progressDisplayControl?.SetProcessingCompleted();
                
                MessageBox.Show($"송장 처리 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // UI 상태 복원
                btnStartProcess.Enabled = true;
                btnSelectFile.Enabled = true;
                btnSettings.Enabled = true;
            }
        }

        #endregion

        #region 판매입력 데이터 처리 (Sales Data Processing)

        /// <summary>
        /// 판매입력 데이터 처리 버튼 클릭 이벤트 핸들러
        /// ProcessSalesInputData 메서드를 독립적으로 실행
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnSalesDataProcess_Click(object? sender, EventArgs e)
        {
            try
            {
                // UI 상태 변경
                btnSalesDataProcess.Enabled = false;
                btnSalesDataProcess.Text = "처리 중...";
                lblStatus.Text = "판매입력 데이터 처리 중...";
                lblStatus.ForeColor = Color.FromArgb(243, 156, 18);

                LogMessage("📊 판매입력 데이터 처리 시작...");

                // InvoiceProcessor 인스턴스 생성
                var processor = new InvoiceProcessor(_fileService, _databaseService, _apiService);

                // ProcessSalesInputData 메서드 직접 호출
                var result = await processor.ProcessSalesInputData();

                if (result)
                {
                    LogMessage("✅ 판매입력 데이터 처리가 성공적으로 완료되었습니다!");
                    lblStatus.Text = "판매입력 데이터 처리 완료";
                    lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                    
                    // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                    progressDisplayControl?.SetProcessingCompleted();
                    
                    MessageBox.Show("판매입력 데이터 처리가 성공적으로 완료되었습니다!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("❌ 판매입력 데이터 처리에 실패했습니다.");
                    lblStatus.Text = "판매입력 데이터 처리 실패";
                    lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                    
                    // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                    progressDisplayControl?.SetProcessingCompleted();
                    
                    MessageBox.Show("판매입력 데이터 처리에 실패했습니다.\n\n로그 파일(app.log)을 확인하여 상세 오류 내용을 파악하세요.", "실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 판매입력 데이터 처리 중 오류가 발생했습니다: {ex.Message}");
                lblStatus.Text = "오류 발생";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                
                // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                progressDisplayControl?.SetProcessingCompleted();
                
                MessageBox.Show($"판매입력 데이터 처리 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // UI 상태 복원
                btnSalesDataProcess.Enabled = true;
                btnSalesDataProcess.Text = "📊 판매입력 데이터 처리";
            }
        }

        #endregion

        #region 디버그 메서드 (Debug Methods)

        /// <summary>
        /// 디버그용 판매입력 데이터 처리 버튼 클릭 이벤트 핸들러
        /// ProcessSalesInputData 메서드만 독립적으로 실행하여 문제 진단
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnDebugSalesData_Click(object? sender, EventArgs e)
        {
            try
            {
                // UI 상태 변경
                btnDebugSalesData.Enabled = false;
                btnDebugSalesData.Text = "디버그 중...";
                lblStatus.Text = "디버그: 판매입력 데이터 처리 중...";
                lblStatus.ForeColor = Color.FromArgb(243, 156, 18);

                LogMessage("🐛 디버그: 판매입력 데이터 처리 시작...");

                // InvoiceProcessor 인스턴스 생성
                var processor = new InvoiceProcessor(_fileService, _databaseService, _apiService);

                // ProcessSalesInputData 메서드 직접 호출
                LogMessage("🐛 ProcessSalesInputData 메서드 호출 시작...");
                var result = await processor.ProcessSalesInputData();
                LogMessage($"🐛 ProcessSalesInputData 메서드 호출 완료 - 결과: {result}");

                if (result)
                {
                    LogMessage("✅ 디버그: 판매입력 데이터 처리가 성공적으로 완료되었습니다!");
                    lblStatus.Text = "디버그: 판매입력 데이터 처리 완료";
                    lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                    
                    // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                    progressDisplayControl?.SetProcessingCompleted();
                    
                    MessageBox.Show("디버그: 판매입력 데이터 처리가 성공적으로 완료되었습니다!", "디버그 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("❌ 디버그: 판매입력 데이터 처리에 실패했습니다.");
                    lblStatus.Text = "디버그: 판매입력 데이터 처리 실패";
                    lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                    
                    // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                    progressDisplayControl?.SetProcessingCompleted();
                    
                    MessageBox.Show("디버그: 판매입력 데이터 처리에 실패했습니다.\n\n로그 파일(app.log)을 확인하여 상세 오류 내용을 파악하세요.", "디버그 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 디버그: 판매입력 데이터 처리 중 오류가 발생했습니다: {ex.Message}");
                LogMessage($"❌ 디버그: 상세 오류: {ex.StackTrace}");
                lblStatus.Text = "디버그: 오류 발생";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                
                // 진행상황 컨트롤에 처리 완료 상태 설정 (처리시간 고정)
                progressDisplayControl?.SetProcessingCompleted();
                
                MessageBox.Show($"디버그: 판매입력 데이터 처리 중 오류가 발생했습니다:\n{ex.Message}\n\n상세 오류:\n{ex.StackTrace}", "디버그 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // UI 상태 복원
                btnDebugSalesData.Enabled = true;
                btnDebugSalesData.Text = "🐛 디버그: 판매입력";
            }
        }

        #endregion

        #region 유틸리티 메서드 (Utility Methods)

        /// <summary>
        /// 로그 메시지를 텍스트박스에 출력하는 메서드
        /// 
        /// 기능:
        /// - 현재 시간과 함께 메시지 구성
        /// - UI 스레드에서 안전하게 실행
        /// - 자동 스크롤 및 UI 업데이트
        /// - "[처리 중단]" 메시지는 굵은 폰트와 빨간색으로 표시
        /// </summary>
        /// <param name="message">출력할 로그 메시지</param>
        private void LogMessage(string message)
        {
            try
            {
                // 현재 시간과 함께 메시지 구성
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";

                // UI 스레드에서 안전하게 실행
                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() => LogMessage(message)));
                    return;
                }

                // "[처리 중단]" 메시지인지 확인
                if (message.Contains("[처리 중단]"))
                {
                    // 굵은 폰트와 빨간색으로 표시
                    txtLog.SelectionStart = txtLog.TextLength;
                    txtLog.SelectionLength = 0;
                    
                    // 굵은 폰트 설정
                    txtLog.SelectionFont = new Font("맑은 고딕", 9F, FontStyle.Bold);
                    // 빨간색 설정
                    txtLog.SelectionColor = Color.Red;
                    
                    // 메시지 추가
                    txtLog.AppendText(logMessage + Environment.NewLine);
                    
                    // 기본 폰트와 색상으로 복원
                    txtLog.SelectionStart = txtLog.TextLength;
                    txtLog.SelectionLength = 0;
                    txtLog.SelectionFont = new Font("맑은 고딕", 9F);
                    txtLog.SelectionColor = Color.FromArgb(46, 204, 113);
                }
                else
                {
                    // 일반 메시지는 기본 스타일로 표시
                    txtLog.AppendText(logMessage + Environment.NewLine);
                }
                
                // 자동 스크롤
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
                
                // UI 업데이트
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                // 로그 출력 중 오류가 발생한 경우 콘솔에 출력
                Console.WriteLine($"로그 출력 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 로그 내용을 초기화하는 메서드
        /// 
        /// 기능:
        /// - RichTextBox의 모든 내용을 지움
        /// - UI 스레드에서 안전하게 실행
        /// - 초기화 완료 후 자동 스크롤
        /// </summary>
        private void ClearLog()
        {
            try
            {
                // UI 스레드에서 안전하게 실행
                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() => ClearLog()));
                    return;
                }

                // 로그 내용 초기화
                txtLog.Clear();
                
                // 자동 스크롤
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
                
                // UI 업데이트
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                // 로그 초기화 중 오류가 발생한 경우 콘솔에 출력
                Console.WriteLine($"로그 초기화 중 오류: {ex.Message}");
            }
        }

        #endregion

        #region 통합 시간 관리자 이벤트 핸들러 (Processing Time Manager Event Handlers)
        
        /// <summary>
        /// 처리 시작 이벤트 핸들러
        /// </summary>
        private void OnProcessingStarted(object? sender, ProcessingTimeEventArgs e)
        {
            try
            {
                // UI 스레드에서 안전하게 실행
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => OnProcessingStarted(sender, e)));
                    return;
                }
                
                LogMessage("🕒 송장 처리 시간 측정이 시작되었습니다.");
                LogMessage($"📊 목표 TestLevel: {ProcessingTimeManager.Instance.TargetTestLevel}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnProcessingStarted 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 처리 완료 이벤트 핸들러
        /// </summary>
        private void OnProcessingCompleted(object? sender, ProcessingTimeEventArgs e)
        {
            try
            {
                // UI 스레드에서 안전하게 실행
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => OnProcessingCompleted(sender, e)));
                    return;
                }
                
                var totalTime = ProcessingTimeManager.Instance.GetFormattedElapsedTime();
                LogMessage("═══════════════════════════════════════");
                LogMessage($"🏁 송장 처리 완료! 총 처리 시간: {totalTime}");
                LogMessage($"📊 완료된 단계: {ProcessingTimeManager.Instance.CurrentStep}/{ProcessingTimeManager.Instance.TargetTestLevel}");
                LogMessage("═══════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnProcessingCompleted 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 단계 업데이트 이벤트 핸들러
        /// </summary>
        private void OnStepUpdated(object? sender, ProcessingStepEventArgs e)
        {
            try
            {
                // UI 스레드에서 안전하게 실행
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => OnStepUpdated(sender, e)));
                    return;
                }
                
                // 주요 단계마다 현재 처리시간 로그 출력 (5단계마다)
                if (e.CurrentStep > 0 && e.CurrentStep % 5 == 0)
                {
                    var currentTime = ProcessingTimeManager.Instance.GetFormattedElapsedTime();
                    var stepName = string.IsNullOrEmpty(e.StepName) ? $"단계 {e.CurrentStep}" : e.StepName;
                    LogMessage($"📊 {stepName} 완료 | 경과 시간: {currentTime} | 진행률: {e.Progress:P0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnStepUpdated 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 시간 업데이트 이벤트 핸들러 (실시간 동기화)
        /// </summary>
        private void OnTimeUpdated(object? sender, ProcessingTimeEventArgs e)
        {
            try
            {
                // UI 스레드에서 안전하게 실행
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => OnTimeUpdated(sender, e)));
                    return;
                }
                
                // 10초마다 한 번씩 로그에 현재 처리시간 출력 (너무 자주 출력하지 않도록)
                var timeManager = ProcessingTimeManager.Instance;
                if (timeManager.IsProcessing)
                {
                    var elapsedSeconds = (int)timeManager.GetElapsedTime().TotalSeconds;
                    if (elapsedSeconds > 0 && elapsedSeconds % 10 == 0) // 10초마다
                    {
                        var elapsedTime = timeManager.GetFormattedElapsedTime();
                        var currentStep = timeManager.CurrentStep;
                        var targetStep = timeManager.TargetTestLevel;
                        LogMessage($"⏱️ 현재 처리 시간: {elapsedTime} | 진행: {currentStep}/{targetStep}단계");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnTimeUpdated 오류: {ex.Message}");
            }
        }
        

        
        #endregion

        #region 데이터베이스 연결 테스트 (Database Connection Test)

        /// <summary>
        /// 데이터베이스 연결을 테스트하고 결과를 표시하는 메서드
        /// 
        /// 기능:
        /// - DatabaseService를 사용한 연결 테스트
        /// - 성공 시 연결 완료 메시지 표시
        /// - 실패 시 상세 오류 메시지 표시
        /// - 동기적으로 실행하여 UI 블로킹 방지
        /// - 오류 발생 시에도 UI가 정상 작동하도록 보장
        /// </summary>
        private void TestDatabaseConnection()
        {
            try
            {
                LogMessage("🔗 데이터베이스 연결을 확인하고 있습니다...");
                Console.WriteLine("🔄 MainForm: 데이터베이스 연결 테스트 시작");
                
                // 동기적으로 연결 테스트 실행 (UI 스레드에서 직접 실행)
                try
                {
                    LogManagerService.LogInfo("📡 MainForm: DatabaseService Singleton 인스턴스 사용");
                    
                    // 기존 _databaseService 정보 로그 출력
                    var oldDbInfo = _databaseService.GetConnectionInfo();
                    LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - Server: {oldDbInfo.Server}");
                    LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - Database: {oldDbInfo.Database}");
                    LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - User: {oldDbInfo.User}");
                    LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - Port: {oldDbInfo.Port}");
                    
                    // Singleton 인스턴스에서 연결 테스트 실행
                    var testResult = _databaseService.TestConnectionWithDetailsAsync().GetAwaiter().GetResult();
                    
                    LogManagerService.LogInfo($"📊 MainForm: 연결 테스트 결과 = {testResult.IsConnected}");
                    LogManagerService.LogInfo($"📊 MainForm: 메시지 = {testResult.ErrorMessage}");
                    
                    if (testResult.IsConnected)
                    {
                        LogMessage("✅ 데이터베이스 접속이 완료되었습니다!");
                        LogMessage("📊 송장 처리 시스템이 준비되었습니다.");
                        
                        // 최신 DB 연결 정보로 상태 메시지 생성
                        var latestDbInfo = _databaseService.GetConnectionInfo();
                        var dbInfoText = $"✅ 데이터베이스 연결됨 ({latestDbInfo.Server})";
                        
                        // 디버그 로그 추가
                        LogManagerService.LogInfo($"🔍 MainForm: 최신 DB 정보 - Server: {latestDbInfo.Server}");
                        LogManagerService.LogInfo($"🔍 MainForm: 최신 DB 정보 - Database: {latestDbInfo.Database}");
                        LogManagerService.LogInfo($"🔍 MainForm: 최신 DB 정보 - User: {latestDbInfo.User}");
                        LogManagerService.LogInfo($"🔍 MainForm: 최신 DB 정보 - Port: {latestDbInfo.Port}");
                        
                        lblDbStatus.Text = dbInfoText;
                        lblDbStatus.ForeColor = Color.FromArgb(46, 204, 113);
                        lblDbStatus.BackColor = Color.Transparent; // 배경색을 투명하게
                        LogManagerService.LogInfo("✅ MainForm: 연결 성공 처리 완료");
                    }
                    else
                    {
                        LogMessage("⚠️ 데이터베이스 연결에 실패했습니다.");
                        LogMessage($"🔍 오류 상세: {testResult.ErrorMessage}");
                        LogMessage("💡 설정 화면에서 데이터베이스 정보를 확인해주세요.");
                        lblDbStatus.Text = "❌ 데이터베이스 연결 실패";
                        lblDbStatus.ForeColor = Color.FromArgb(231, 76, 60);
                        lblDbStatus.BackColor = Color.Transparent; // 배경색을 투명하게
                        LogManagerService.LogInfo("❌ MainForm: 연결 실패 처리 완료");
                    }
                }
                catch (Exception ex)
                {
                    LogManagerService.LogError($"❌ MainForm: 연결 테스트 중 예외 발생: {ex.Message}");
                    LogManagerService.LogError($"🔍 MainForm: 예외 상세: {ex}");
                    LogManagerService.LogError($"🔍 MainForm: 예외 스택 트레이스: {ex.StackTrace}");
                    
                    LogMessage($"❌ 데이터베이스 연결 중 오류가 발생했습니다: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        LogMessage($"🔍 상세 오류: {ex.InnerException.Message}");
                        LogManagerService.LogError($"🔍 MainForm: 내부 예외: {ex.InnerException.Message}");
                    }
                    LogMessage("💡 설정 화면에서 데이터베이스 정보를 확인해주세요.");
                    lblDbStatus.Text = "❌ 데이터베이스 연결 오류";
                    lblDbStatus.ForeColor = Color.FromArgb(231, 76, 60);
                    lblDbStatus.BackColor = Color.Transparent; // 배경색을 투명하게
                }
            }
            catch (Exception ex)
            {
                // 최상위 예외 처리
                LogManagerService.LogError($"❌ MainForm: 최상위 예외 발생: {ex.Message}");
                LogManagerService.LogError($"🔍 MainForm: 최상위 예외 상세: {ex}");
                LogMessage($"❌ 데이터베이스 연결 테스트 중 오류 발생: {ex.Message}");
                lblDbStatus.Text = "❌ 연결 오류";
                lblDbStatus.ForeColor = Color.FromArgb(231, 76, 60);
                lblDbStatus.BackColor = Color.Transparent; // 배경색을 투명하게
            }
        }

        /// <summary>
        /// Dropbox 연결 상태를 테스트하고 결과를 로그에 표시
        /// 
        /// 기능:
        /// - DropboxService Singleton 인스턴스 사용
        /// - 비동기 연결 테스트 실행
        /// - 연결 성공/실패 결과를 로그에 표시
        /// - UI 상태 업데이트
        /// </summary>
        private async void TestDropboxConnection()
        {
            try
            {
                LogMessage("☁️ Dropbox 연결을 확인하고 있습니다...");
                Console.WriteLine("🔄 MainForm: Dropbox 연결 테스트 시작");
                
                // DropboxService Singleton 인스턴스 사용
                var dropboxService = DropboxService.Instance;
                
                // 비동기 연결 테스트 실행
                var isConnected = await dropboxService.TestConnectionAsync();
                
                Console.WriteLine($"📊 MainForm: Dropbox 연결 테스트 결과 = {isConnected}");
                
                if (isConnected)
                {
                    LogMessage("✅ Dropbox 연결이 완료되었습니다!");
                    LogMessage("☁️ 파일 업로드 기능을 사용할 수 있습니다.");
                    Console.WriteLine("✅ MainForm: Dropbox 연결 성공 처리 완료");
                }
                else
                {
                    LogMessage("⚠️ Dropbox 연결에 실패했습니다.");
                    LogMessage("💡 설정 화면에서 Dropbox 인증 정보를 확인해주세요.");
                    LogMessage("💡 App.config에서 Dropbox.AppKey, Dropbox.AppSecret, Dropbox.RefreshToken을 확인해주세요.");
                    Console.WriteLine("❌ MainForm: Dropbox 연결 실패 처리 완료");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainForm: Dropbox 연결 테스트 중 예외 발생: {ex.Message}");
                Console.WriteLine($"🔍 MainForm: Dropbox 예외 상세: {ex}");
                
                LogMessage($"❌ Dropbox 연결 중 오류가 발생했습니다: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogMessage($"🔍 상세 오류: {ex.InnerException.Message}");
                    Console.WriteLine($"🔍 MainForm: Dropbox 내부 예외: {ex.InnerException.Message}");
                }
                LogMessage("💡 설정 화면에서 Dropbox 인증 정보를 확인해주세요.");
            }
        }

        /// <summary>
        /// KakaoWork 연결 상태를 테스트하는 메서드
        /// </summary>
        private async void TestKakaoWorkConnection()
        {
            try
            {
                LogMessage("💬 KakaoWork 연결을 확인하고 있습니다...");
                Console.WriteLine("🔄 MainForm: KakaoWork 연결 테스트 시작");
                
                // KakaoWorkService 인스턴스 생성 시도
                var kakaoWorkService = KakaoWorkService.Instance;
                Console.WriteLine("✅ KakaoWorkService 인스턴스 생성 성공");
                
                // 연결 테스트
                var isConnected = await kakaoWorkService.TestConnectionAsync();
                
                Console.WriteLine($"📊 MainForm: KakaoWork 연결 테스트 결과 = {isConnected}");
                
                if (isConnected)
                {
                    LogMessage("✅ KakaoWork 연결이 정상입니다.");
                    Console.WriteLine("✅ MainForm: KakaoWork 연결 성공");
                }
                else
                {
                    LogMessage("❌ KakaoWork 연결에 실패했습니다.");
                    Console.WriteLine("❌ MainForm: KakaoWork 연결 실패");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainForm: KakaoWork 연결 테스트 중 예외 발생: {ex.Message}");
                Console.WriteLine($"🔍 MainForm: KakaoWork 예외 상세: {ex}");
                
                LogMessage($"❌ KakaoWork 연결 중 오류가 발생했습니다: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogMessage($"🔍 상세 오류: {ex.InnerException.Message}");
                    Console.WriteLine($"🔍 MainForm: KakaoWork 내부 예외: {ex.InnerException.Message}");
                }
                LogMessage("💡 App.config에서 KakaoWork 인증 정보를 확인해주세요.");
            }
        }

        #endregion

        #region 폼 이벤트 (Form Events)

        /// <summary>
        /// 종료 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - 종료 확인 메시지 표시
        /// - 사용자 확인 시 프로그램 완전 종료
        /// - 취소 시 폼 유지
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void BtnExit_Click(object? sender, EventArgs e)
        {
            try
            {
                // 종료 확인 메시지 표시
                var result = MessageBox.Show(
                    "프로그램을 종료하시겠습니까?",
                    "종료 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    LogMessage("👋 프로그램을 종료합니다.");
                    
                    // 강제 종료를 위한 타이머 설정
                    var exitTimer = new System.Windows.Forms.Timer();
                    exitTimer.Interval = 1000; // 1초 후 강제 종료
                    exitTimer.Tick += (s, args) =>
                    {
                        exitTimer.Stop();
                        Environment.Exit(0); // 강제 종료
                    };
                    exitTimer.Start();
                    
                    // 모든 리소스 정리
                    Dispose();
                    
                    // 프로그램 완전 종료
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시에도 강제 종료
                try
                {
                    LogMessage($"종료 중 오류 발생: {ex.Message}");
                    Environment.Exit(0);
                }
                catch
                {
                    Environment.Exit(0);
                }
            }
        }



        /// <summary>
        /// UI 상태를 최초 상태로 초기화하는 메서드
        /// 
        /// 기능:
        /// - 진행률바 초기화
        /// - 상태 라벨 초기화
        /// - 버튼 상태 초기화
        /// - 파일 경로 라벨 초기화
        /// - 진행상황 표시 컨트롤 초기화
        /// </summary>
        private void ResetUIState()
        {
            try
            {
                // UI 스레드에서 안전하게 실행
                if (this.InvokeRequired)
                {
                    this.Invoke(() => ResetUIState());
                    return;
                }

                // 진행률바 초기화 (현재 숨김 처리됨 - 원형 진행률 차트 사용)
                // progressBar.Value = 0;
                
                // 상태 라벨 초기화
                lblStatus.Text = "대기 중...";
                lblStatus.ForeColor = Color.FromArgb(127, 140, 141);
                
                // 파일 경로 라벨 초기화
                lblFilePath.Text = "선택된 파일: 없음";
                
                // 진행상황 표시 컨트롤 초기화
                progressDisplayControl?.ResetProgress();
                
                // 버튼 상태 초기화 (초기화 버튼은 백그라운드 작업 완료 후 설정)
                btnStartProcess.Enabled = false;
                btnSelectFile.Enabled = true;
                btnSettings.Enabled = true;
                btnDropboxTest.Enabled = true;
                btnKakaoWorkTest.Enabled = true;
                btnExit.Enabled = true;
                // btnReset.Enabled = true; // 이 부분은 백그라운드 작업 완료 후 설정
                
                // UI 업데이트
                Application.DoEvents();
                
                // 추가 UI 업데이트를 위한 짧은 대기
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UI 상태 초기화 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 공통코드에서 진행상황 단계 데이터를 로딩하는 메서드
        /// </summary>
        private async Task LoadProgressStepsAsync()
        {
            try
            {
                // CommonCodeRepository를 통해 'PG_PROC' 그룹의 데이터 로딩
                var commonCodeRepository = new CommonCodeRepository(_databaseService);
                var progressSteps = await commonCodeRepository.GetCommonCodesByGroupAsync("PG_PROC");
                
                // UI 스레드에서 안전하게 실행
                if (this.InvokeRequired)
                {
                    this.Invoke(() => UpdateProgressDisplay(progressSteps));
                }
                else
                {
                    UpdateProgressDisplay(progressSteps);
                }
                
                LogMessage($"📊 진행상황 단계 {progressSteps.Count}개를 로딩했습니다.");
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 진행상황 단계 로딩 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 진행상황 표시 컨트롤을 업데이트하는 메서드
        /// </summary>
        /// <param name="progressSteps">진행상황 단계 목록</param>
        private void UpdateProgressDisplay(List<CommonCode> progressSteps)
        {
            try
            {
                if (progressDisplayControl != null)
                {
                    // SortOrder 기준으로 정렬
                    var sortedSteps = progressSteps
                        .Where(step => step.IsUsed) // 사용 중인 단계만
                        .OrderBy(step => step.SortOrder)
                        .ToList();
                    
                    progressDisplayControl.ProgressSteps = sortedSteps;
                    LogMessage($"🔄 진행상황 표시가 업데이트되었습니다. (총 {sortedSteps.Count}단계)");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 진행상황 표시 업데이트 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 폼 종료 시 이벤트 핸들러
        /// 
        /// 기능:
        /// - 리소스 정리
        /// - 종료 확인 메시지
        /// </summary>
        /// <param name="e">폼 종료 이벤트 인수</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 통합 시간 관리자 이벤트 구독 해제
                ProcessingTimeManager.Instance.ProcessingStarted -= OnProcessingStarted;
                ProcessingTimeManager.Instance.ProcessingCompleted -= OnProcessingCompleted;
                ProcessingTimeManager.Instance.StepUpdated -= OnStepUpdated;
                ProcessingTimeManager.Instance.TimeUpdated -= OnTimeUpdated;
                
                LogMessage("👋 프로그램을 종료합니다.");
                
                // 리소스 정리는 GC가 자동으로 처리하므로 별도 작업 불필요
            }
            catch (Exception ex)
            {
                Console.WriteLine($"폼 종료 중 오류: {ex.Message}");
            }
            
            base.OnFormClosing(e);
        }

        #endregion
    }
} 