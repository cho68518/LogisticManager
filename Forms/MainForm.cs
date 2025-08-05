using LogisticManager.Services;
using LogisticManager.Models;
using LogisticManager.Processors;
using System.Drawing.Drawing2D;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 송장 처리 애플리케이션의 메인 폼
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
        private readonly DatabaseService _databaseService;
        
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
        private TextBox txtLog = null!;
        
        /// <summary>
        /// 진행률 표시바 - 송장 처리 작업의 진행 상황 표시
        /// </summary>
        private ProgressBar progressBar = null!;

        /// <summary>
        /// 타이틀 라벨 - 애플리케이션 제목
        /// </summary>
        private Label lblTitle = null!;

        /// <summary>
        /// 상태 라벨 - 현재 상태 표시
        /// </summary>
        private Label lblStatus = null!;

        /// <summary>
        /// 종료 버튼 - 애플리케이션 완전 종료
        /// </summary>
        private Button btnExit = null!;

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
            _databaseService = new DatabaseService();
            _apiService = new ApiService();
            
            InitializeUI();
            
            // 데이터베이스 연결 테스트 및 완료 메시지 표시
            TestDatabaseConnection();
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
            this.Text = "송장 처리 자동화 시스템";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable; // 크기 조절 가능하도록 변경
            this.MaximizeBox = true; // 최대화 버튼 활성화
            this.MinimizeBox = true; // 최소화 버튼 활성화
            this.MinimumSize = new Size(800, 600); // 최소 크기 설정
            this.BackColor = Color.FromArgb(240, 244, 248); // 연한 회색 배경

            // 타이틀 라벨 생성 및 설정
            lblTitle = new Label
            {
                Text = "📦 송장 처리 자동화 시스템",
                Location = new Point(20, 20),
                Size = new Size(860, 40),
                Font = new Font("맑은 고딕", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 파일 선택 버튼 생성 및 설정 (둥근 모서리, 그라데이션)
            btnSelectFile = CreateModernButton("📁 파일 선택", new Point(20, 80), new Size(120, 40));
            btnSelectFile.Click += BtnSelectFile_Click;

            // 선택된 파일 경로 라벨 생성 및 설정
            lblFilePath = new Label
            {
                Text = "선택된 파일: 없음",
                Location = new Point(160, 90),
                Size = new Size(500, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(127, 140, 141),
                BackColor = Color.Transparent
            };

            // 설정 버튼 생성 및 설정
            btnSettings = CreateModernButton("⚙️ 설정", new Point(680, 80), new Size(80, 40), Color.FromArgb(52, 152, 219));
            btnSettings.Click += BtnSettings_Click;

            // 종료 버튼 생성 및 설정
            btnExit = CreateModernButton("❌ 종료", new Point(780, 80), new Size(80, 40), Color.FromArgb(231, 76, 60));
            btnExit.Click += BtnExit_Click;

            // 송장 처리 시작 버튼 생성 및 설정
            btnStartProcess = CreateModernButton("🚀 송장 처리 시작", new Point(20, 140), new Size(150, 45), Color.FromArgb(46, 204, 113));
            btnStartProcess.Enabled = false;  // 파일이 선택되기 전까지 비활성화
            btnStartProcess.Click += BtnStartProcess_Click;

            // 진행률 표시바 생성 및 설정
            progressBar = new ProgressBar
            {
                Location = new Point(190, 145),
                Size = new Size(500, 35),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            // 진행률 표시바 스타일 설정
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.ForeColor = Color.FromArgb(46, 204, 113);

            // 상태 라벨 생성 및 설정
            lblStatus = new Label
            {
                Text = "대기 중...",
                Location = new Point(190, 185),
                Size = new Size(500, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(127, 140, 141),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 로그 표시 텍스트박스 생성 및 설정
            txtLog = new TextBox
            {
                Location = new Point(20, 220),
                Size = new Size(840, 420),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,  // 사용자 입력 방지
                Font = new Font("맑은 고딕", 9F),
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.FromArgb(46, 204, 113),  // 밝은 녹색
                WordWrap = true,
                BorderStyle = BorderStyle.None
            };

            // 모든 컨트롤을 폼에 추가
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                btnSelectFile,
                lblFilePath,
                btnSettings,
                btnExit,
                btnStartProcess,
                progressBar,
                lblStatus,
                txtLog
            });

            // 폼 리사이즈 이벤트 핸들러 추가
            this.Resize += MainForm_Resize;

            // 초기 로그 메시지 출력
            LogMessage("🎉 송장 처리 자동화 시스템이 시작되었습니다.");
            LogMessage("📁 파일을 선택하고 '송장 처리 시작' 버튼을 클릭하세요.");
        }

        /// <summary>
        /// 모던한 스타일의 버튼을 생성하는 메서드
        /// 
        /// 특징:
        /// - 둥근 모서리
        /// - 그라데이션 배경
        /// - 호버 효과
        /// - 아이콘 포함
        /// </summary>
        /// <param name="text">버튼 텍스트</param>
        /// <param name="location">위치</param>
        /// <param name="size">크기</param>
        /// <param name="backgroundColor">배경색 (기본값: 파란색)</param>
        /// <returns>생성된 버튼</returns>
        private Button CreateModernButton(string text, Point location, Size size, Color? backgroundColor = null)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = backgroundColor ?? Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };

            // 둥근 모서리 설정
            button.Region = new Region(CreateRoundedRectangle(button.ClientRectangle, 10));

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
            const int buttonHeight = 40;
            const int titleHeight = 40;

            // 타이틀 라벨 조정
            lblTitle.Size = new Size(this.ClientSize.Width - (padding * 2), titleHeight);
            lblTitle.Location = new Point(padding, padding);

            // 파일 선택 버튼 위치 조정 (좌상단 고정)
            btnSelectFile.Location = new Point(padding, padding + titleHeight + 20);

            // 설정 버튼 위치 조정 (우상단 고정)
            btnSettings.Location = new Point(this.ClientSize.Width - btnSettings.Width - padding, padding + titleHeight + 20);

            // 파일 경로 라벨 조정 (파일 선택 버튼과 설정 버튼 사이)
            int fileLabelWidth = btnSettings.Location.X - btnSelectFile.Location.X - btnSelectFile.Width - 20;
            lblFilePath.Size = new Size(fileLabelWidth, 20);
            lblFilePath.Location = new Point(btnSelectFile.Location.X + btnSelectFile.Width + 20, btnSelectFile.Location.Y + 10);

            // 송장 처리 시작 버튼 위치 조정
            btnStartProcess.Location = new Point(padding, btnSelectFile.Location.Y + buttonHeight + 20);

            // 진행률 표시바 조정
            int progressBarWidth = this.ClientSize.Width - btnStartProcess.Width - (padding * 3);
            progressBar.Size = new Size(progressBarWidth, 35);
            progressBar.Location = new Point(btnStartProcess.Location.X + btnStartProcess.Width + 20, btnStartProcess.Location.Y + 5);

            // 상태 라벨 조정
            lblStatus.Size = new Size(progressBarWidth, 20);
            lblStatus.Location = new Point(progressBar.Location.X, progressBar.Location.Y + progressBar.Height + 5);

            // 로그 텍스트박스 조정 (하단 전체 영역)
            int logTop = lblStatus.Location.Y + lblStatus.Height + 20;
            int logHeight = this.ClientSize.Height - logTop - padding;
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
                if (result == DialogResult.OK || result == DialogResult.Cancel)
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
                            LogMessage("💡 애플리케이션을 재시작하면 새로운 설정이 적용됩니다.");
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
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 설정 창 열기 중 오류 발생: {ex.Message}");
                MessageBox.Show($"설정 창을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    _selectedFilePath = openFileDialog.FileName;
                    lblFilePath.Text = $"선택된 파일: {Path.GetFileName(_selectedFilePath)}";
                    btnStartProcess.Enabled = true;
                    
                    LogMessage($"📁 파일이 선택되었습니다: {Path.GetFileName(_selectedFilePath)}");
                    LogMessage($"📊 파일 크기: {new FileInfo(_selectedFilePath).Length / 1024} KB");
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
                // UI 상태 변경
                btnStartProcess.Enabled = false;
                btnSelectFile.Enabled = false;
                btnSettings.Enabled = false;
                progressBar.Value = 0;
                lblStatus.Text = "처리 중...";
                lblStatus.ForeColor = Color.FromArgb(52, 152, 219);

                LogMessage("🚀 송장 처리 작업을 시작합니다...");

                // InvoiceProcessor 생성 및 처리 실행
                var processor = new InvoiceProcessor(_fileService, _databaseService, _apiService);
                
                // 진행률 콜백 설정
                var progressCallback = new Progress<int>(value => 
                { 
                    progressBar.Value = value; 
                    Application.DoEvents(); 
                });
                
                // 로그 콜백 설정
                var logCallback = new Progress<string>(message => 
                { 
                    LogMessage(message); 
                    Application.DoEvents(); 
                });

                // 송장 처리 실행
                await processor.ProcessAsync(_selectedFilePath, logCallback, progressCallback);

                // 작업 완료 처리
                LogMessage("✅ 송장 처리가 성공적으로 완료되었습니다!");
                lblStatus.Text = "완료";
                lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                
                MessageBox.Show("송장 처리가 성공적으로 완료되었습니다!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // 오류 처리
                LogMessage($"❌ 송장 처리 중 오류가 발생했습니다: {ex.Message}");
                lblStatus.Text = "오류 발생";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                
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

        #region 유틸리티 메서드 (Utility Methods)

        /// <summary>
        /// 로그 메시지를 텍스트박스에 출력하는 메서드
        /// 
        /// 기능:
        /// - 현재 시간과 함께 메시지 출력
        /// - 텍스트박스 자동 스크롤
        /// - 긴 메시지 처리
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

                // 텍스트박스에 메시지 추가
                txtLog.AppendText(logMessage + Environment.NewLine);
                
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
                
                // DB 연결 정보 가져오기
                var dbInfo = _databaseService.GetConnectionInfo();
                LogMessage($"📊 DB 서버: {dbInfo.Server}");
                LogMessage($"📊 DB 이름: {dbInfo.Database}");
                LogMessage($"📊 DB 사용자: {dbInfo.User}");
                LogMessage($"📊 DB 포트: {dbInfo.Port}");
                
                // 동기적으로 연결 테스트 실행 (UI 스레드에서 직접 실행)
                try
                {
                    Console.WriteLine("📡 MainForm: 새로운 DatabaseService 생성");
                    
                    // 새로운 DatabaseService 인스턴스 생성 (최신 JSON 설정 적용)
                    var freshDatabaseService = new DatabaseService();
                    
                    Console.WriteLine("📡 MainForm: DatabaseService 연결 테스트 호출");
                    
                    // 연결 테스트 실행
                    var testResult = freshDatabaseService.TestConnectionWithDetailsAsync().GetAwaiter().GetResult();
                    
                    Console.WriteLine($"📊 MainForm: 연결 테스트 결과 = {testResult.IsConnected}");
                    Console.WriteLine($"📊 MainForm: 오류 메시지 = {testResult.ErrorMessage}");
                    
                    if (testResult.IsConnected)
                    {
                        LogMessage("✅ 데이터베이스 접속이 완료되었습니다!");
                        LogMessage("📊 송장 처리 시스템이 준비되었습니다.");
                        lblStatus.Text = "데이터베이스 연결됨";
                        lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                        Console.WriteLine("✅ MainForm: 연결 성공 처리 완료");
                    }
                    else
                    {
                        LogMessage("⚠️ 데이터베이스 연결에 실패했습니다.");
                        LogMessage($"🔍 오류 상세: {testResult.ErrorMessage}");
                        LogMessage("💡 설정 화면에서 데이터베이스 정보를 확인해주세요.");
                        lblStatus.Text = "데이터베이스 연결 실패";
                        lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                        Console.WriteLine("❌ MainForm: 연결 실패 처리 완료");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ MainForm: 연결 테스트 중 예외 발생: {ex.Message}");
                    Console.WriteLine($"🔍 MainForm: 예외 상세: {ex}");
                    Console.WriteLine($"🔍 MainForm: 예외 스택 트레이스: {ex.StackTrace}");
                    
                    LogMessage($"❌ 데이터베이스 연결 중 오류가 발생했습니다: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        LogMessage($"🔍 상세 오류: {ex.InnerException.Message}");
                        Console.WriteLine($"🔍 MainForm: 내부 예외: {ex.InnerException.Message}");
                    }
                    LogMessage("💡 설정 화면에서 데이터베이스 정보를 확인해주세요.");
                    lblStatus.Text = "데이터베이스 연결 오류";
                    lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                }
            }
            catch (Exception ex)
            {
                // 최상위 예외 처리
                Console.WriteLine($"❌ MainForm: 최상위 예외 발생: {ex.Message}");
                Console.WriteLine($"🔍 MainForm: 최상위 예외 상세: {ex}");
                LogMessage($"❌ 데이터베이스 연결 테스트 중 오류 발생: {ex.Message}");
                lblStatus.Text = "연결 오류";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
            }
        }

        #endregion

        #region 폼 이벤트 (Form Events)

        /// <summary>
        /// 종료 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - 종료 확인 메시지 표시
        /// - 사용자 확인 시 애플리케이션 완전 종료
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
                    "정말로 애플리케이션을 종료하시겠습니까?",
                    "종료 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    LogMessage("👋 애플리케이션을 종료합니다.");
                    
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
                    
                    // 애플리케이션 완전 종료
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
                LogMessage("👋 애플리케이션을 종료합니다.");
                
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