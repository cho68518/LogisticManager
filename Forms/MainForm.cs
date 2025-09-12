using LogisticManager.Services;
using LogisticManager.Models;
using LogisticManager.Processors;
using LogisticManager.Repositories;
using LogisticManager.Forms;
using System.Drawing.Drawing2D;
using System.Configuration;
using System.Reflection; // 버전 정보를 얻기 위해 필요
using System.IO; // 파일 및 경로 처리용

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
        /// 공통 코드 리포지토리 - 사용자 인증 및 기타 데이터베이스 작업 담당
        /// </summary>
        private readonly ICommonCodeRepository _commonCodeRepository;

        /// <summary>
        /// 인증 서비스 - 사용자 로그인 및 인증 상태 관리
        /// </summary>
        private AuthenticationService _authenticationService = null!;
        
        /// <summary>
        /// 사용자가 선택한 Excel 파일의 전체 경로
        /// </summary>
        private string? _selectedFilePath;

        #endregion

        #region 속성 (Properties)
        
        /// <summary>
        /// 선택된 차수 정보를 반환하는 속성
        /// </summary>
        public string SelectedBatch => cmbBatch?.SelectedItem?.ToString() ?? "1차";
        
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
        private CheckBox chkKakaoSend = null!; // [한글 주석] 카카오워크 전송 여부 체크박스
        private ComboBox cmbBatch = null!; // [한글 주석] 차수 선택 콤보박스
        
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
        /// 파일 목록 표시 판넬 - 업로드된 파일들의 목록을 표시
        /// </summary>
        private Panel fileListPanel = null!;

        /// <summary>
        /// 파일 목록 제목 라벨
        /// </summary>
        private Label lblFileListTitle = null!;

        /// <summary>
        /// 파일 목록 컨테이너 컨트롤 (새로운 카드 기반 UI)
        /// </summary>
        private FileListContainerControl fileListContainer = null!;

        /// <summary>
        /// 파일 다운로드 버튼
        /// </summary>
        private Button btnDownloadFiles = null!;

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

        /// <summary>
        /// 상태표시줄 및 날짜/시간 라벨
        /// </summary>
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel toolStripStatusLabelDateTime = null!;

        /// <summary>
        /// 현재 로그인된 사용자명 표시 라벨
        /// </summary>
        private Label lblCurrentUser = null!;

        /// <summary>
        /// 툴팁 컨트롤 (사용자명 전체 표시용)
        /// </summary>
        private ToolTip toolTip = null!;

        /// <summary>
        /// 비밀번호 변경 버튼 (열쇠 아이콘)
        /// </summary>
        private Button btnChangePassword = null!;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// MainForm 생성자
        /// 
        /// 초기화 순서:
        /// 1. 폼 기본 설정 (InitializeComponent)
        /// 2. 서비스 객체들 초기화 (FileService, DatabaseService, ApiService)
        /// 3. 로그인 체크 (App.config 설정에 따라)
        /// 4. UI 컨트롤들 생성 및 배치 (InitializeUI)
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
            // 서비스 객체들 초기화
            _fileService = new FileService();
            _databaseService = DatabaseService.Instance; // Singleton 인스턴스 사용
            _apiService = new ApiService();
            _commonCodeRepository = new CommonCodeRepository(_databaseService);
            _authenticationService = new AuthenticationService(_databaseService);
            
            // 로그인 체크 및 처리
            if (!CheckLoginRequired())
            {
                // 로그인이 필요하지 않거나 로그인에 실패한 경우
                LogMessage("⚠️ 로그인이 완료되지 않았습니다. 프로그램을 종료합니다.");
                
                // 폼을 안전하게 닫고 애플리케이션 종료
                this.BeginInvoke(new Action(() =>
                {
                    this.Close();
                    Application.Exit();
                }));
                return;
            }
            
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
            // App.config에서 Login 설정 읽기
            string loginSetting = System.Configuration.ConfigurationManager.AppSettings["Login"] ?? "N";
            bool showUserInfo = loginSetting.ToUpper() == "Y";
            
            // 로그에 설정값 출력
            LogMessage($"🔍 MainForm: Login 설정값 = '{loginSetting}', showUserInfo = {showUserInfo}");
            // 폼 기본 설정 (상단 좌측 창 제목에 차수 정보만 표시, 버전 숨김)
            this.Text = GetBatchTitle("송장 처리 시스템");
            this.Size = new Size(1100, 900); // 폼 크기를 1100으로 조정
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable; // 크기 조절 가능하도록 변경
            this.MaximizeBox = true; // 최대화 버튼 활성화
            this.MinimizeBox = true; // 최소화 버튼 활성화
            this.MinimumSize = new Size(1000, 700); // 최소 크기도 더 크게 조정
            this.BackColor = Color.FromArgb(240, 244, 248); // 연한 회색 배경
            
            // 폼 아이콘 설정 (invoice.ico 사용)
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "invoice.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                // 아이콘 로드 실패 시 로그 기록 (사용자에게는 표시하지 않음)
                LogMessage($"⚠️ 아이콘 로드 실패: {ex.Message}");
            }

            // 타이틀 라벨 생성 및 설정 (배치구분규칙에 따른 동적 타이틀 표시)
            lblTitle = new Label
            {
                Text = GetBatchTitle("📦 송장 처리 시스템"),
                Location = new Point(20, 20),
                Size = new Size(860, 40),
                Font = new Font("맑은 고딕", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 0),
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
            btnSettings = CreateModernButton("⚙️ 설정/확인", new Point(690, 80), new Size(90, 40), Color.FromArgb(52, 152, 219));
            btnSettings.Click += BtnSettings_Click;

            // [한글 주석] 카카오워크 전송 그룹박스 생성
            var grpKakao = new GroupBox
            {
                Text = "알림 설정",
                Location = new Point(0, 0), // 임시 위치, MainForm_Resize에서 실제 위치 설정
                Size = new Size(150, 50),
                Font = new Font("맑은 고딕", 8F, FontStyle.Bold),
                ForeColor = Color.Blue, // 파란색 텍스트로 변경
                BackColor = Color.Transparent // 배경색 제거
            };

            // [한글 주석] 카카오워크 전송 체크박스 생성 (그룹박스 내부에 배치)
            var kakaoCheckValue = System.Configuration.ConfigurationManager.AppSettings["KakaoCheck"] ?? "N";
            bool isKakaoChecked = kakaoCheckValue.Equals("Y", StringComparison.OrdinalIgnoreCase);
            chkKakaoSend = new CheckBox
            {
                Text = "카카오워크 전송",
                AutoSize = true,
                Location = new Point(10, 20), // 그룹박스 내부 위치
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold), // 굵은 글꼴 적용
                Checked = isKakaoChecked
            };
            // 그룹박스 텍스트는 파란색이지만, 체크박스 텍스트는 기본 텍스트 색상으로 유지
            chkKakaoSend.ForeColor = SystemColors.ControlText;
            
            // 그룹박스에 체크박스 추가
            grpKakao.Controls.Add(chkKakaoSend);

            // [한글 주석] 차수 선택 그룹박스 생성
            var grpBatch = new GroupBox
            {
                Text = "차수 선택",
                Location = new Point(0, 0), // 임시 위치, MainForm_Resize에서 실제 위치 설정
                Size = new Size(120, 50),
                Font = new Font("맑은 고딕", 8F, FontStyle.Bold),
                ForeColor = Color.Blue, // 파란색 텍스트로 변경
                BackColor = Color.FromArgb(240, 248, 255) // 연한 파란색 배경
            };

            // [한글 주석] 차수 선택 콤보박스 생성 (그룹박스 내부에 배치)
            cmbBatch = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(10, 20), // 그룹박스 내부 위치
                Size = new Size(100, 25),
                Font = new Font("맑은 고딕", 9F),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            
            // 그룹박스에 콤보박스 추가
            grpBatch.Controls.Add(cmbBatch);
            
            // 차수 옵션 추가 (1차~5차, 특별 차수는 별도 표시)
            for (int i = 1; i <= 5; i++)
            {
                cmbBatch.Items.Add($"{i}차");
            }
            cmbBatch.Items.Add("막차");
            cmbBatch.Items.Add("추가");
            
            // 현재 차수로 설정 (BatchTimeService의 배치 타입을 콤보박스 형식에 맞게 변환)
            var currentBatchType = BatchTimeService.Instance.GetCurrentBatchType();
            string currentBatch = "1차"; // 기본값
            
            // BatchTimeService의 배치 타입을 콤보박스 형식으로 변환
            switch (currentBatchType)
            {
                case "1차": currentBatch = "1차"; break;
                case "2차": currentBatch = "2차"; break;
                case "3차": currentBatch = "3차"; break;
                case "4차": currentBatch = "4차"; break;
                case "5차": currentBatch = "5차"; break;
                case "막차": currentBatch = "막차"; break;
                case "추가": currentBatch = "추가"; break;
                case "기타": currentBatch = "추가"; break;
                case "대기": 
                    // 배치 시간이 아닌 경우 가장 가까운 다음 배치로 설정
                    currentBatch = GetNextBatch();
                    break;
                default: currentBatch = "1차"; break;
            }
            
            // 프로그램 시작 시 현재 시간에 맞는 차수를 콤보박스에 한 번만 설정
            try
            {
                var currentTime = DateTime.Now.ToString("HH:mm:ss");
                LogMessage($"🕒 프로그램 시작 시 차수 설정 시작: {currentTime} → {currentBatchType} → {currentBatch}");
                
                // 콤보박스에 항목이 있는지 확인
                if (cmbBatch.Items.Count > 0)
                {
                    // SelectedItem으로 설정 시도
                    cmbBatch.SelectedItem = currentBatch;
                    
                    // SelectedItem이 제대로 설정되었는지 확인
                    if (cmbBatch.SelectedItem?.ToString() != currentBatch)
                    {
                        // SelectedItem으로 설정되지 않았다면 SelectedIndex로 시도
                        int targetIndex = cmbBatch.Items.IndexOf(currentBatch);
                        if (targetIndex >= 0)
                        {
                            cmbBatch.SelectedIndex = targetIndex;
                            LogMessage($"✅ SelectedIndex로 차수 설정 성공: {currentBatch} (인덱스: {targetIndex})");
                        }
                        else
                        {
                            LogMessage($"⚠️ 콤보박스에 '{currentBatch}' 항목이 없음, 기본값(1차) 사용");
                            cmbBatch.SelectedIndex = 0; // 첫 번째 항목(1차) 선택
                        }
                    }
                    else
                    {
                        LogMessage($"✅ SelectedItem으로 차수 설정 성공: {currentBatch}");
                    }
                }
                else
                {
                    LogMessage($"⚠️ 콤보박스에 항목이 없음, 기본값(1차) 사용");
                    cmbBatch.SelectedIndex = 0; // 첫 번째 항목(1차) 선택
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 차수 설정 실패, 기본값(1차) 사용: {ex.Message}");
                cmbBatch.SelectedIndex = 0; // 첫 번째 항목(1차) 선택
            }
            
            // [한글 주석] 콤보박스 선택 변경 이벤트 추가
            cmbBatch.SelectedIndexChanged += (s, e) =>
            {
                try
                {
                    var selectedBatch = cmbBatch.SelectedItem?.ToString() ?? "1차";
                    LogMessage($"🔄 사용자가 차수를 변경했습니다: {selectedBatch}");
                }
                catch (Exception ex)
                {
                    LogMessage($"⚠️ 차수 변경 이벤트 처리 중 오류: {ex.Message}");
                }
            };
            
            // [한글 주석] 체크 변경 시 App.config 값 업데이트
            chkKakaoSend.CheckedChanged += (s, e) =>
            {
                try
                {
                    // 체크되면 'Y', 아니면 'N'으로 설정
                    string newValue = chkKakaoSend.Checked ? "Y" : "N";
                    var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["KakaoCheck"] == null)
                    {
                        config.AppSettings.Settings.Add("KakaoCheck", newValue);
                    }
                    else
                    {
                        config.AppSettings.Settings["KakaoCheck"].Value = newValue;
                    }
                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                }
                catch (Exception ex)
                {
                    // [한글 주석] 설정 저장 실패 시 사용자에게 안내하고 체크 상태를 되돌림
                    MessageBox.Show($"설정 저장 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    chkKakaoSend.CheckedChanged -= (s, e) => { };
                    chkKakaoSend.Checked = !chkKakaoSend.Checked;
                    chkKakaoSend.CheckedChanged += (s, e) => { };
                }
            };

            // Dropbox 테스트 버튼 생성 및 설정 (우상단 고정)
            btnDropboxTest = CreateModernButton("☁️ Dropbox 테스트", new Point(550, 80), new Size(130, 40), Color.FromArgb(155, 89, 182));
            btnDropboxTest.Click += BtnDropboxTest_Click;
            // [한글 주석] 요구사항: 버튼은 삭제하지 말고 화면에서 숨김 처리
            btnDropboxTest.Visible = false;

            // KakaoWork 테스트 버튼 생성 및 설정 (우상단 고정)
            btnKakaoWorkTest = CreateModernButton("💬 KakaoWork 테스트", new Point(410, 80), new Size(130, 40), Color.FromArgb(46, 204, 113));
            btnKakaoWorkTest.Click += BtnKakaoWorkTest_Click;
            // [한글 주석] 요구사항: 버튼은 삭제하지 말고 화면에서 숨김 처리
            btnKakaoWorkTest.Visible = false;

            // 툴팁 컨트롤 초기화
            toolTip = new ToolTip
            {
                AutoPopDelay = 5000, // 5초 후 자동으로 사라짐
                InitialDelay = 1000, // 1초 후 표시
                ReshowDelay = 500,   // 다시 표시까지 0.5초
                ShowAlways = true    // 항상 표시
            };

            // 현재 로그인된 사용자명 표시 라벨 생성 및 설정 (Login 설정에 따라 표시/숨김)
            lblCurrentUser = new Label
            {
                Text = "사용자: 로딩 중...",
                Location = new Point(790, 50),
                Size = new Size(280, 25), // 사용자명이 완전히 표시되도록 너비를 280px로 확대
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false, // 고정 크기 사용
                Visible = showUserInfo, // Login 설정에 따라 표시 여부 결정
                BorderStyle = BorderStyle.None, // 테두리 제거하여 깔끔하게 표시
                TabIndex = 100 // 높은 탭 인덱스로 설정하여 다른 컨트롤에 가려지지 않도록 함
            };

            // 비밀번호 변경 버튼 (열쇠 아이콘) 생성 및 설정 (Login 설정에 따라 표시/숨김)
            btnChangePassword = new Button
            {
                Text = "\uE192", // Segoe MDL2 Assets: Key icon
                Size = new Size(22, 22),
                Location = new Point(0, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.Black,
                TabStop = false,
                Cursor = Cursors.Hand,
                Visible = showUserInfo // Login 설정에 따라 표시 여부 결정
            };
            btnChangePassword.FlatAppearance.BorderSize = 0;
            btnChangePassword.Font = new Font("Segoe MDL2 Assets", 9F, FontStyle.Regular);
            btnChangePassword.TextAlign = ContentAlignment.MiddleCenter;
            btnChangePassword.Margin = new Padding(0);
            btnChangePassword.Padding = new Padding(0);
            toolTip.SetToolTip(btnChangePassword, "비밀번호 변경");
            btnChangePassword.Click += BtnChangePassword_Click;
            btnChangePassword.BringToFront();

            // 종료 버튼 생성 및 설정 (우상단 고정)
            btnExit = CreateModernButton("❌ 종료", new Point(790, 80), new Size(80, 40), Color.FromArgb(231, 76, 60));
            btnExit.Click += BtnExit_Click;

            // 송장 처리 시작 버튼 생성 및 설정 (파일선택 버튼 오른쪽에 배치)
            btnStartProcess = CreateModernButton("🚀 송장 처리 시작", new Point(150, 80), new Size(150, 45), Color.FromArgb(46, 204, 113));
            btnStartProcess.Enabled = false;  // 파일이 선택되기 전까지 비활성화
            btnStartProcess.Click += BtnStartProcess_Click;

            // 비활성 상태에서도 손가락 커서 표시를 위해 폼 레벨에서 마우스 이동 감지
            this.MouseMove += MainForm_MouseMoveForStartProcessCursor;

            // 판매입력 데이터 처리 버튼 생성 및 설정 (독립 실행용) - 현재 숨김 처리
            btnSalesDataProcess = CreateModernButton("📊 판매입력 데이터 처리", new Point(180, 160), new Size(150, 45), Color.FromArgb(155, 89, 182));
            btnSalesDataProcess.Click += BtnSalesDataProcess_Click;
            btnSalesDataProcess.Visible = false; // 버튼 숨김 처리

            // 디버그용 버튼 (임시)
            btnDebugSalesData = CreateModernButton("🐛 디버그: 판매입력", new Point(340, 160), new Size(120, 45), Color.FromArgb(231, 76, 60));
            btnDebugSalesData.Click += BtnDebugSalesData_Click;
            
            // 배치구분 테스트 버튼 추가
            var btnBatchTest = CreateModernButton("⏰ 배치구분 테스트", new Point(470, 160), new Size(120, 45), Color.FromArgb(52, 152, 219));
            btnBatchTest.Click += BtnBatchTest_Click;
            
            // 배치 타이틀 수동 업데이트 버튼 추가
            var btnUpdateTitle = CreateModernButton("🔄 타이틀 업데이트", new Point(600, 160), new Size(120, 45), Color.FromArgb(155, 89, 182));
            btnUpdateTitle.Click += (sender, e) => UpdateBatchTitle();

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

            // 로그 표시 텍스트박스 생성 및 설정 (50% 비율로 조정)
            txtLog = new RichTextBox
            {
                Location = new Point(20, 660), // 진행상황 컨트롤 아래로 이동 (위치 조정됨)
                Size = new Size(580, 200), // 폭을 절반으로 조정 (50% 비율)
                ReadOnly = true,  // 사용자 입력 방지
                Font = new Font("맑은 고딕", 9F),
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.FromArgb(46, 204, 113),  // 밝은 녹색
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // 파일 목록 제목 라벨 생성 및 설정 (모던한 카드 스타일)
            lblFileListTitle = new Label
            {
                Text = "■ 업로드된 파일 목록 (파일명을 더블클릭하면 파일이 열립니다)",
                Location = new Point(620, 660), // 로그창 오른쪽에 배치
                Size = new Size(560, 35),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 73, 94), // 진한 회색 텍스트
                BackColor = Color.FromArgb(240, 248, 255), // 연한 파란색 배경
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.None,
                FlatStyle = FlatStyle.Flat
            };

            // 파일 목록 컨테이너 컨트롤 생성 및 설정 (새로운 카드 기반 UI)
            fileListContainer = new FileListContainerControl
            {
                Location = new Point(620, 695), // 제목 라벨 아래에 배치
                Size = new Size(560, 165) // 로그창과 동일한 높이
            };
            
            // 파일 열기 이벤트 등록
            fileListContainer.FileOpened += FileListContainer_FileOpened;

            // 파일 목록 판넬 생성 및 설정 (모던한 스타일)
            fileListPanel = new Panel
            {
                Location = new Point(620, 660), // 로그창 오른쪽에 배치
                Size = new Size(560, 200), // 로그창과 동일한 크기
                //BackColor = Color.FromArgb(248, 250, 252), // 연한 회색 배경
                BorderStyle = BorderStyle.None
            };
            
            // 파일 다운로드 버튼 생성 및 설정 (파일 목록 패널 하단에 배치)
            btnDownloadFiles = CreateModernButton("📥 파일 다운로드", new Point(10, 160), new Size(120, 35), Color.FromArgb(46, 204, 113));
            btnDownloadFiles.Click += BtnDownloadFiles_Click;
            btnDownloadFiles.Visible = true; // 명시적으로 보이도록 설정
            
            // 파일목록 패널에 모든 컨트롤 추가
            fileListPanel.Controls.Add(lblFileListTitle);
            fileListPanel.Controls.Add(fileListContainer);
            fileListPanel.Controls.Add(btnDownloadFiles);

            // 파일 목록 제목에 세련된 그림자 효과 적용
            lblFileListTitle.Paint += (sender, e) =>
            {
                var label = sender as Label;
                if (label != null)
                {
                    // 배경 그리기
                    using (var brush = new SolidBrush(label.BackColor))
                    {
                        e.Graphics.FillRectangle(brush, label.ClientRectangle);
                    }
                    
                    // 하단 그림자 효과 그리기
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                    {
                        e.Graphics.FillRectangle(shadowBrush, 0, label.Height - 2, label.Width, 2);
                    }
                    
                    // 하단 테두리 그리기 (세련된 구분선)
                    using (var pen = new Pen(Color.FromArgb(189, 195, 199), 1))
                    {
                        e.Graphics.DrawLine(pen, 0, label.Height - 1, label.Width, label.Height - 1);
                    }
                    
                    // 텍스트 그림자 효과
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    using (var format = new StringFormat())
                    {
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        e.Graphics.DrawString(label.Text, label.Font, shadowBrush, 
                            new Rectangle(1, 1, label.Width, label.Height), format);
                    }
                    
                    // 텍스트 그리기
                    using (var textBrush = new SolidBrush(label.ForeColor))
                    using (var format = new StringFormat())
                    {
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        e.Graphics.DrawString(label.Text, label.Font, textBrush, label.ClientRectangle, format);
                    }
                }
            };
            
            // 파일 목록 리스트박스에 모던한 스타일 적용
            // CheckedListBox는 기본 렌더링을 사용 (소프트 체크박스, 클릭 즉시 체크)

            // 상태표시줄(StatusStrip) 및 날짜/시간 라벨 생성
            statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom
            };
            toolStripStatusLabelDateTime = new ToolStripStatusLabel
            {
                // 초기 텍스트는 빈 값으로 설정 후 아래에서 실제 날짜/시간으로 설정
                Text = string.Empty,
                Spring = true, // 남는 공간을 채워 가운데 정렬 효과
                TextAlign = ContentAlignment.MiddleCenter // 텍스트 가운데 정렬
            };
            statusStrip.Items.Add(toolStripStatusLabelDateTime);

            // 모든 컨트롤을 폼에 추가
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                btnSelectFile,
                grpBatch, // 차수 선택 그룹박스 추가
                grpKakao, // 카카오워크 알림 그룹박스 추가
                lblFilePath,
                btnSettings,
                btnDropboxTest,
                btnKakaoWorkTest,
                lblCurrentUser,
                btnChangePassword,
                btnExit,
                btnStartProcess,
                btnSalesDataProcess,
                progressBar,
                lblStatus,
                lblDbStatus,
                progressDisplayControl,
                txtLog,
                fileListPanel,
                statusStrip
            });

            // Z-Order 최종 보정: 사용자명 라벨과 열쇠 아이콘을 최상위로 올림
            lblCurrentUser.BringToFront();
            btnChangePassword.BringToFront();
            
            // 다운로드 버튼 상태 확인 및 로그 출력
            //LogMessage($"🔍 다운로드 버튼 생성 완료: 위치({btnDownloadFiles.Location.X}, {btnDownloadFiles.Location.Y}), 크기({btnDownloadFiles.Size.Width}x{btnDownloadFiles.Size.Height}), 보임여부: {btnDownloadFiles.Visible}");

            // 폼 리사이즈 이벤트 핸들러 추가
            this.Resize += MainForm_Resize;

            // 초기 크기 조정 적용
            MainForm_Resize(this, EventArgs.Empty);
            
            // 다운로드 버튼을 확실히 표시
            if (btnDownloadFiles != null)
            {
                btnDownloadFiles.Visible = true;
                btnDownloadFiles.BringToFront();
                //LogMessage($"🔍 다운로드 버튼 최종 확인: 위치({btnDownloadFiles.Location.X}, {btnDownloadFiles.Location.Y}), 보임여부: {btnDownloadFiles.Visible}");
            }

            // 초기 로그 메시지 출력
            LogMessage("🎉 송장 처리 시스템이 시작되었습니다.");
            LogMessage("📁 파일을 선택하고 '송장 처리 시작' 버튼을 클릭하세요.");
            // 현재 선택된 콤보박스 차수를 로그로 남김 (한글 주석)
            
            // 현재 날짜/시간을 상태표시줄 라벨에 표시
            UpdateDateTimeDisplay();
            
            // 날짜/시간 자동 업데이트 타이머 설정 (1초마다)
            var dateTimeTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000, // 1초 = 1,000ms
                Enabled = true
            };
            dateTimeTimer.Tick += (sender, e) => 
            {
                UpdateDateTimeDisplay();
                // 차수 자동 업데이트 제거 - 프로그램 시작 시 한 번만 설정
            };
            
            // UI 초기화 완료 후 배치 타이틀 설정 및 타이머 시작
            UpdateBatchTitle();
            
            // 배치 타이틀 자동 업데이트 타이머 설정 (1분마다)
            var batchTitleTimer = new System.Windows.Forms.Timer
            {
                Interval = 60000, // 1분 = 60,000ms
                Enabled = true
            };
            batchTitleTimer.Tick += (sender, e) => UpdateBatchTitle();
            
            // 초기 크기 조정 적용 (버튼 위치 설정을 위해)
            MainForm_Resize(this, EventArgs.Empty);
        }

        /// <summary>
        /// 마우스가 송장 처리 시작 버튼 위에 있을 때 손가락 모양 커서를 강제로 표시
        /// </summary>
        private void MainForm_MouseMoveForStartProcessCursor(object? sender, MouseEventArgs e)
        {
            try
            {
                if (btnStartProcess != null)
                {
                    var clientPoint = btnStartProcess.PointToClient(this.PointToScreen(e.Location));
                    var within = clientPoint.X >= 0 && clientPoint.Y >= 0 && clientPoint.X < btnStartProcess.Width && clientPoint.Y < btnStartProcess.Height;
                    if (within)
                    {
                        this.Cursor = Cursors.Hand; // 손가락 모양
                    }
                    else
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
            catch { /* 안전 무시 */ }
        }

        /// <summary>
        /// 비밀번호 변경 버튼 클릭 핸들러
        /// </summary>
        private void BtnChangePassword_Click(object? sender, EventArgs e)
        {
            try
            {
                // 로그인 상태 확인
                if (_authenticationService == null || !_authenticationService.IsLoggedIn)
                {
                    MessageBox.Show(this, "로그인 후 이용 가능합니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 비밀번호 변경 폼 표시
                using (var dlg = new LogisticManager.Forms.ChangePasswordForm(_authenticationService))
                {
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            // [한글 주석] 설정 버튼 왼쪽에 그룹박스들을 정렬
            // 카카오워크 그룹박스 위치 설정
            int kakaoGroupX = btnSettings.Location.X - 20 - 150; // 설정 버튼과 20px 간격 (그룹박스 너비 150px)
            int kakaoGroupY = padding + titleHeight + 15; // 버튼보다 약간 위에 배치
            if (this.Controls.OfType<GroupBox>().Any(g => g.Text == "알림 설정"))
            {
                var grpKakao = this.Controls.OfType<GroupBox>().First(g => g.Text == "알림 설정");
                grpKakao.Location = new Point(kakaoGroupX, kakaoGroupY);
            }
            
            // 차수 선택 그룹박스 위치 설정
            int batchGroupX = kakaoGroupX - 20 - 120; // 카카오워크 그룹박스와 20px 간격 (그룹박스 너비 120px)
            int batchGroupY = padding + titleHeight + 15; // 카카오워크 그룹박스와 같은 높이
            if (this.Controls.OfType<GroupBox>().Any(g => g.Text == "차수 선택"))
            {
                var grpBatch = this.Controls.OfType<GroupBox>().First(g => g.Text == "차수 선택");
                grpBatch.Location = new Point(batchGroupX, batchGroupY);
            }
            btnDropboxTest.Location = new Point(btnSettings.Location.X - btnDropboxTest.Width - buttonSpacing, padding + titleHeight + 20);
            btnKakaoWorkTest.Location = new Point(btnDropboxTest.Location.X - btnKakaoWorkTest.Width - buttonSpacing, padding + titleHeight + 20);

            // 송장 처리 시작 버튼 위치 조정 (파일선택 버튼 오른쪽 옆에 위치)
            btnStartProcess.Location = new Point(btnSelectFile.Location.X + btnSelectFile.Width + 10, btnSelectFile.Location.Y);

            // 사용자명 표시 라벨 및 비밀번호 변경 버튼 위치 조정 (우상단 고정)
            // 버튼은 가장 오른쪽, 라벨은 버튼 왼쪽에 배치
            int keyButtonWidth = btnChangePassword.Width;
            int keyButtonHeight = btnChangePassword.Height;
            int keyButtonX = this.ClientSize.Width - rightMargin - keyButtonWidth;
            int keyButtonY = padding + titleHeight - 30 + (25 - keyButtonHeight) / 2; // 라벨 높이(25) 기준 수직 중앙 정렬
            btnChangePassword.Location = new Point(keyButtonX, keyButtonY);

            // lblCurrentUser.Width가 0이거나 음수가 되는 것을 방지
            int userLabelWidth = Math.Max(280, lblCurrentUser.Width);
            int userLabelX = Math.Max(padding, keyButtonX - 6 - userLabelWidth); // 버튼 왼쪽 6px 간격
            lblCurrentUser.Location = new Point(userLabelX, padding + titleHeight - 30);
            
            // 디버그: 사용자명 라벨 위치 확인
            // LogMessage($"🔍 사용자명 라벨 위치: X={userLabelX}, Y={padding + titleHeight - 30}, Width={userLabelWidth}, Visible={lblCurrentUser.Visible}");

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

            // 로그 텍스트박스와 파일 목록 판넬을 50:50으로 분할하여 조정
            int logTop = progressDisplayControl.Location.Y + progressDisplayControl.Height + 20;
            
            // 전체 폼 너비에서 패딩을 제외한 공간을 50:50으로 분할
            int totalWidth = this.ClientSize.Width - (padding * 2);
            int halfWidth = totalWidth / 2;
            int spacing = 10; // 두 컨트롤 사이의 간격
            
            // 로그창 (왼쪽 50%)
            txtLog.Size = new Size(halfWidth - spacing/2, logHeight);
            txtLog.Location = new Point(padding, logTop);
            
            // 파일 목록 판넬 (오른쪽 50%)
            fileListPanel.Size = new Size(halfWidth - spacing/2, logHeight);
            fileListPanel.Location = new Point(padding + halfWidth + spacing/2, logTop);
            
            // 파일 목록 제목 라벨 조정 (패널 내부 좌표)
            lblFileListTitle.Size = new Size(fileListPanel.Width, 25);
            lblFileListTitle.Location = new Point(0, 0); // 패널의 (0,0)에 위치
            
            // 파일 목록 컨테이너 컨트롤 조정 (패널 내부 좌표, 버튼 공간 제외)
            int listHeight = fileListPanel.Height - lblFileListTitle.Height - btnDownloadFiles.Height - 20; // 제목, 버튼, 여백 제외
            if (listHeight < 0) listHeight = 0;
            fileListContainer.Size = new Size(fileListPanel.Width, listHeight);
            fileListContainer.Location = new Point(0, lblFileListTitle.Height); // 제목 아래에 위치
            
            // 파일 다운로드 버튼 조정 (패널 하단에 배치)
            int downloadButtonTop = fileListPanel.Height - btnDownloadFiles.Height - 10; // 패널 하단에서 10px 위
            int downloadButtonLeft = (fileListPanel.Width - btnDownloadFiles.Width) / 2; // 패널 가운데 정렬
            
            btnDownloadFiles.Location = new Point(downloadButtonLeft, downloadButtonTop);
            btnDownloadFiles.Visible = true; // 항상 보이도록 설정
            
            // 디버그 정보 출력
            //LogMessage($"🔍 파일목록 패널 레이아웃: 패널({fileListPanel.Width}x{fileListPanel.Height}), 제목({lblFileListTitle.Location.X}, {lblFileListTitle.Location.Y}), 리스트({fileListContainer.Location.X}, {fileListContainer.Location.Y}, {fileListContainer.Size.Height}), 버튼({downloadButtonLeft}, {downloadButtonTop})");
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
                //LogMessage("☁️ Dropbox 테스트 화면을 엽니다...");
                
                // Dropbox 테스트 폼을 모달로 열기
                var dropboxTestForm = new DropboxTestForm();
                dropboxTestForm.ShowDialog(this);
                
                //LogMessage("✅ Dropbox 테스트 화면이 닫혔습니다.");
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
                //LogMessage("💬 KakaoWork 테스트 화면을 엽니다...");
                
                // KakaoWork 테스트 폼을 모달로 열기
                var kakaoWorkTestForm = new KakaoWorkTestForm();
                kakaoWorkTestForm.ShowDialog(this);
                
                //LogMessage("✅ KakaoWork 테스트 화면이 닫혔습니다.");
            }
            catch (Exception ex)
            {
                //LogMessage($"❌ KakaoWork 테스트 화면 열기 중 오류 발생: {ex.Message}");
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
                    
                    //LogMessage($"📁 새 파일이 선택되었습니다: {fileName}");
                    //LogMessage($"📊 파일 크기: {new FileInfo(_selectedFilePath).Length / 1024} KB");
                    //LogMessage($"⏰ 선택 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                //LogMessage($"❌ 파일 선택 중 오류 발생: {ex.Message}");
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

            // 차수 검증 로직 추가
            try
            {
                // 현재 선택된 차수 가져오기
                string selectedBatch = SelectedBatch;
                
                // 차수 검증 실행
                bool isBatchValid = BatchTimeService.Instance.IsBatchTimeValid(selectedBatch);
                
                // 차수가 올바르지 않은 경우 확인 다이얼로그 표시
                if (!isBatchValid)
                {
                    string mismatchMessage = BatchTimeService.Instance.GetBatchMismatchMessage(selectedBatch);
                    
                    // 사용자에게 확인 다이얼로그 표시
                    DialogResult result = MessageBox.Show(
                        mismatchMessage,
                        "차수 불일치 확인",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2 // 기본 선택을 "아니오"로 설정
                    );
                    
                    // 사용자가 "아니오"를 선택한 경우 처리 중단
                    if (result == DialogResult.No)
                    {
                        LogManagerService.LogInfo($"[MainForm] 사용자가 차수 불일치로 인해 송장처리를 취소했습니다. 선택된 차수: {selectedBatch}");
                        return;
                    }
                    
                    // 사용자가 "예"를 선택한 경우 계속 진행
                    LogManagerService.LogInfo($"[MainForm] 사용자가 차수 불일치에도 불구하고 송장처리를 계속 진행합니다. 선택된 차수: {selectedBatch}");
                }
                else
                {
                    LogManagerService.LogInfo($"[MainForm] 차수 검증 통과: 선택된 차수 {selectedBatch}가 현재 시간대와 일치합니다.");
                }
            }
            catch (Exception ex)
            {
                // 차수 검증 중 오류 발생 시 로그 기록하고 계속 진행
                LogManagerService.LogError($"[MainForm] 차수 검증 중 오류 발생: {ex.Message}");
                MessageBox.Show($"차수 검증 중 오류가 발생했습니다.\n\n오류: {ex.Message}\n\n계속 진행하시겠습니까?", 
                    "차수 검증 오류", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);
            }

            try
            {
                // 송장처리 시작 시 파일목록 클리어
                fileListContainer.ClearAllCards();
                
                // 통합 시간 관리자 시작 (송장처리 시작 버튼 클릭 시점)
                ProcessingTimeManager.Instance.StartProcessing();
                
                // [한글 주석] 현재 선택된 차수를 앱 로그에 기록
                LogManagerService.LogInfo($"[MainForm] 송장처리 시작 - 선택 차수: {SelectedBatch}");

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
                    logCallback, progressCallback, progressDisplayControl, (fileName, fileSize, uploadTime, dropboxPath) => AddFileToList(fileName, fileSize, uploadTime, dropboxPath));

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
                //   testLevel         : 처리 단계(1단계, 기본값)  ([4-1]~[4-24])
                //   SelectedBatch    : 사용자가 선택한 차수 (콤보박스에서 선택된 값)
                var testLevel = ConfigurationManager.AppSettings["TestLevel"] ?? "1"; // app.config에서 테스트 레벨 가져오기
                var result = await processor.ProcessAsync(_selectedFilePath, logCallback, progressCallback, int.Parse(testLevel), SelectedBatch);

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
        /// 애플리케이션 버전 문자열을 반환
        /// - ClickOnce 배포 시: Application.ProductVersion 사용 (ClickOnce 버전 노출)
        /// - 그 외: 어셈블리 버전
        /// </summary>
        /// <returns>"vMajor.Minor.Build.Revision" 형식의 버전 문자열</returns>
        private string GetAppVersionString()
        {
            try
            {
                // WinForms의 Application.ProductVersion 사용
                // - ClickOnce 배포 시 Publish Version이 노출됨
                // - 일반 실행 시 파일 버전/어셈블리 정보에 기반
                var productVersion = Application.ProductVersion; // 예: 1.2.3.4 또는 1.2.3+buildmeta
                
                // 디버깅: 실제 ProductVersion 값 로그 출력
                //LogMessage($"🔍 Debug: Application.ProductVersion = '{productVersion}'");
                
                if (!string.IsNullOrWhiteSpace(productVersion))
                {
                    // SemVer의 빌드메타/프리릴리즈(+/ - 이후) 제거
                    var semverCore = productVersion.Split('+', '-')[0];
                    //LogMessage($"🔍 Debug: SemVer Core = '{semverCore}'");
                    
                    if (Version.TryParse(semverCore, out var ver))
                    {
                        // Revision이 0이 아닌 경우 포함하여 표시
                        var shortText = ver.Revision > 0 
                            ? $"v{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}"
                            : $"v{ver.Major}.{ver.Minor}.{ver.Build}";
                        //LogMessage($"🔍 Debug: Version Object - Major: {ver.Major}, Minor: {ver.Minor}, Build: {ver.Build}, Revision: {ver.Revision}");
                        //LogMessage($"🔍 Debug: Parsed Version = {shortText}");
                        return shortText;
                    }
                    // 파싱 실패 시 문자열을 점 기준으로 3부분까지만 노출 (메타 제거본 우선)
                    var parts = semverCore.Split('.');
                    var shortParts = parts.Take(Math.Min(3, parts.Length));
                    var fallbackText = $"v{string.Join('.', shortParts)}";
                    //LogMessage($"🔍 Debug: Fallback Version = {fallbackText}");
                    return fallbackText;
                }
                
                // Application.ProductVersion이 없는 경우 어셈블리 버전 사용
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (assemblyVersion != null)
                {
                    var assemblyText = $"v{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
                    //LogMessage($"🔍 Debug: Assembly Version = {assemblyText}");
                    return assemblyText;
                }
            }
            catch (Exception ex)
            {
                // ClickOnce API 호출 실패 시 어셈블리 버전으로 폴백
                LogMessage($"Debug: Exception in ProductVersion: {ex.Message}");
            }

            // 폴백: 어셈블리 버전 사용
            var asmVer = Assembly.GetExecutingAssembly().GetName().Version;
            if (asmVer != null)
            {
                var asmText = $"v{asmVer.Major}.{asmVer.Minor}.{asmVer.Build}"; // 짧게 표시
                //LogMessage($"Debug: Assembly Version = {asmText}");
                return asmText;
            }

            // 추가 안전장치: 버전을 얻지 못한 경우 기본값
            //LogMessage("Debug: Using default version v0.0.0.0");
            return "v0.0.0.0";
        }

        /// <summary>
        /// 로그 메시지를 텍스트박스와 파일에 출력하는 메서드
        /// 
        /// 기능:
        /// - 현재 시간과 함께 메시지 구성
        /// - UI 스레드에서 안전하게 실행
        /// - 자동 스크롤 및 UI 업데이트
        /// - "[처리 중단]" 메시지는 굵은 폰트와 빨간색으로 표시
        /// - app.log 파일에도 로그 출력
        /// </summary>
        /// <param name="message">출력할 로그 메시지</param>
        private void LogMessage(string message)
        {
            try
            {
                // 현재 시간과 함께 메시지 구성
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";

                // 파일에 로그 출력 (app.log)
                try
                {
                    var logPath = Path.Combine(Application.StartupPath, "app.log");
                    File.AppendAllText(logPath, logMessage + Environment.NewLine);
                }
                catch (Exception fileEx)
                {
                    // 파일 로깅 실패 시 콘솔에 출력
                    Console.WriteLine($"파일 로깅 실패: {fileEx.Message}");
                }

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
                LogMessage($"📊 목표 Level: {ProcessingTimeManager.Instance.TargetTestLevel}");
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
                    //LogManagerService.LogInfo("📡 MainForm: DatabaseService Singleton 인스턴스 사용");
                    
                    // 기존 _databaseService 정보 로그 출력
                    var oldDbInfo = _databaseService.GetConnectionInfo();
                    LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - Server: {oldDbInfo.Server}");
                    LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - Database: {oldDbInfo.Database}");
                    //LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - User: {oldDbInfo.User}");
                    //LogManagerService.LogInfo($"🔍 MainForm: 기존 DB 정보 - Port: {oldDbInfo.Port}");
                    
                    // Singleton 인스턴스에서 연결 테스트 실행
                    var testResult = _databaseService.TestConnectionWithDetailsAsync().GetAwaiter().GetResult();
                    
                    //LogManagerService.LogInfo($"📊 MainForm: 연결 테스트 결과 = {testResult.IsConnected}");
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
                        //LogManagerService.LogInfo($"🔍 MainForm: 최신 DB 정보 - User: {latestDbInfo.User}");
                        //LogManagerService.LogInfo($"🔍 MainForm: 최신 DB 정보 - Port: {latestDbInfo.Port}");
                        
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

        #region 배치구분 테스트 (Batch Time Test)

        /// <summary>
        /// 배치구분 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void BtnBatchTest_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("⏰ 배치구분 테스트를 시작합니다...");
                
                var batchService = BatchTimeService.Instance;
                
                // 현재 시간 테스트
                var currentTime = DateTime.Now;
                var currentBatchType = batchService.GetCurrentBatchType();
                var currentTitle = batchService.GetBatchTitle("📦 송장 처리 시스템");
                
                LogMessage($"🕐 현재 시간: {currentTime:HH:mm:ss}");
                LogMessage($"🏷️ 현재 배치구분: {currentBatchType}");
                LogMessage($"📝 현재 타이틀: {currentTitle}");
                
                // 시간대별 테스트
                var testTimes = new[]
                {
                    new TimeSpan(0, 30, 0),   // 00:30
                    new TimeSpan(2, 15, 0),   // 02:15
                    new TimeSpan(8, 45, 0),   // 08:45
                    new TimeSpan(11, 30, 0),  // 11:30
                    new TimeSpan(14, 20, 0),  // 14:20
                    new TimeSpan(16, 45, 0),  // 16:45
                    new TimeSpan(20, 15, 0),  // 20:15
                    new TimeSpan(7, 30, 0),   // 07:30 (배치 시간 아님)
                    new TimeSpan(10, 30, 0),  // 10:30 (배치 시간 아님)
                };
                
                LogMessage("=== 시간대별 배치구분 테스트 ===");
                foreach (var time in testTimes)
                {
                    var batchType = batchService.GetBatchTypeAtTime(time);
                    var title = batchService.GetBatchTitle("📦 송장 처리 시스템");
                    LogMessage($"{time:hh\\:mm} → {batchType} → {title}");
                }
                
                LogMessage("=== 모든 배치구분 정보 ===");
                var allBatchTypes = batchService.GetAllBatchTypes();
                foreach (var batch in allBatchTypes)
                {
                    LogMessage($"{batch.Key}: {batch.Value}");
                }
                
                LogMessage("✅ 배치구분 테스트가 완료되었습니다.");
                
                // 타이틀 업데이트
                UpdateBatchTitle();
                LogMessage($"🔄 타이틀이 업데이트되었습니다: {lblTitle.Text}");
                
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 배치구분 테스트 중 오류가 발생했습니다: {ex.Message}");
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
        /// 로그인 필요 여부를 확인하고 처리하는 메서드
        /// </summary>
        /// <returns>로그인 성공 또는 로그인 불필요 시 true, 로그인 실패 시 false</returns>
        private bool CheckLoginRequired()
        {
            try
            {
                // App.config에서 Login 설정 확인
                var loginRequired = ConfigurationManager.AppSettings["Login"];
                
                if (string.IsNullOrEmpty(loginRequired) || loginRequired.ToUpper() != "Y")
                {
                    // 로그인이 필요하지 않은 경우
                    LogMessage("ℹ️ 로그인 기능이 비활성화되어 있습니다.");
                    return true;
                }
                
                LogMessage("🔐 로그인 기능이 활성화되어 있습니다. 로그인 폼을 표시합니다.");
                
                // AuthenticationService 생성 및 MainForm의 인스턴스 업데이트
                var authService = new AuthenticationService(_databaseService);
                _authenticationService = authService; // MainForm의 인스턴스 업데이트
                
                // 로그인 폼 표시 (모달로 표시하여 메인 폼이 활성화되지 않도록 함)
                using (var loginForm = new LoginForm(authService, () => UpdateCurrentUserDisplay()))
                {
                    // 로그인 폼을 모달로 표시하고 결과 확인
                    var result = loginForm.ShowDialog(this);
                    
                    if (result == DialogResult.OK)
                    {
                        // 로그인 성공 (메시지 표시하지 않음)
                        // LogMessage($"✅ 로그인 성공: {authService.CurrentUser?.Username}");
                        
                        // 로그인 성공 후 사용자명 표시 업데이트
                        this.BeginInvoke(new Action(() => UpdateCurrentUserDisplay()));
                        
                        return true;
                    }
                    else
                    {
                        // 로그인 취소 또는 실패
                        LogMessage("⚠️ 로그인이 완료되지 않았습니다.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 로그인 체크 중 오류 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 배치구분규칙에 따른 타이틀을 반환하는 메서드
        /// </summary>
        /// <param name="baseTitle">기본 타이틀</param>
        /// <returns>배치구분이 포함된 타이틀</returns>
        private string GetBatchTitle(string baseTitle)
        {
            try
            {
                //LogMessage($"🔍 GetBatchTitle 호출 시작: baseTitle={baseTitle}");
                
                var batchTitle = BatchTimeService.Instance.GetBatchTitle(baseTitle);
                //LogMessage($"🔍 배치 타이틀 생성: {baseTitle} → {batchTitle}");
                
                return batchTitle;
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 배치 타이틀 생성 중 오류: {ex.Message}");
                LogMessage($"⚠️ 스택 트레이스: {ex.StackTrace}");
                return baseTitle; // 오류 시 기본 타이틀 반환
            }
        }
        
        /// <summary>
        /// 현재 시간이 배치 시간이 아닌 경우 가장 가까운 다음 배치를 반환하는 메서드
        /// </summary>
        /// <returns>다음 배치 문자열</returns>
        private string GetNextBatch()
        {
            try
            {
                DateTime now = DateTime.Now;
                TimeSpan currentTime = now.TimeOfDay;
                
                LogMessage($"GetNextBatch 호출: 현재 시간 {now:HH:mm:ss}");
                
                // 현재 시간 이후의 다음 배치 시간대를 찾기 (KakaoWorkService 규칙 적용)
                if (currentTime >= new TimeSpan(0, 0, 0) && currentTime < new TimeSpan(1, 0, 0))
                {
                    LogMessage($"00:00~01:00 시간대 → 다음 배치: 1차");
                    return "1차"; // 00:00~01:00 → 1차
                }
                else if (currentTime >= new TimeSpan(23, 0, 0) && currentTime < new TimeSpan(24, 0, 0))
                {
                    LogMessage($"23:00~24:00 시간대 → 다음 배치: 1차");
                    return "1차"; // 23:00~24:00 → 다음날 1차
                }
                else if (currentTime >= new TimeSpan(1, 0, 0) && currentTime <= new TimeSpan(7, 0, 0))
                {
                    LogMessage($"1차 배치 시간대 → 다음 배치: 2차");
                    return "2차"; // 1차 배치 중 → 2차
                }
                else if (currentTime > new TimeSpan(7, 0, 0) && currentTime <= new TimeSpan(10, 0, 0))
                {
                    LogMessage($"2차 배치 시간대 → 다음 배치: 3차");
                    return "3차"; // 2차 배치 중 → 3차
                }
                else if (currentTime > new TimeSpan(10, 0, 0) && currentTime <= new TimeSpan(11, 0, 0))
                {
                    LogMessage($"3차 배치 시간대 → 다음 배치: 4차");
                    return "4차"; // 3차 배치 중 → 4차
                }
                else if (currentTime > new TimeSpan(11, 0, 0) && currentTime <= new TimeSpan(13, 0, 0))
                {
                    LogMessage($"4차 배치 시간대 → 다음 배치: 5차");
                    return "5차"; // 4차 배치 중 → 5차
                }
                else if (currentTime > new TimeSpan(13, 0, 0) && currentTime <= new TimeSpan(15, 0, 0))
                {
                    LogMessage($"5차 배치 시간대 → 다음 배치: 막차");
                    return "막차"; // 5차 배치 중 → 막차
                }
                else if (currentTime > new TimeSpan(15, 0, 0) && currentTime <= new TimeSpan(18, 0, 0))
                {
                    LogMessage($"막차 배치 시간대 → 다음 배치: 추가");
                    return "추가"; // 막차 배치 중 → 추가
                }
                else if (currentTime > new TimeSpan(18, 0, 0) && currentTime <= new TimeSpan(23, 0, 0))
                {
                    LogMessage($"추가 배치 시간대 → 다음 배치: 1차");
                    return "1차"; // 추가 배치 중 → 다음날 1차
                }
                else
                {
                    // 배치 시간이 아닌 경우 가장 가까운 다음 배치로 설정
                    LogMessage($"알 수 없는 시간대 → 기본값: 1차");
                    return "1차"; // 기본값
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 다음 배치 계산 실패: {ex.Message}");
                return "1차"; // 오류 시 기본값
            }
        }
        
        /// <summary>
        /// 타이틀을 현재 배치구분에 맞게 업데이트하는 메서드
        /// </summary>
        private void UpdateBatchTitle()
        {
            try
            {
                //LogMessage("🔄 UpdateBatchTitle 메서드 시작");
                
                if (lblTitle != null)
                {
                    //LogMessage("✅ lblTitle이 null이 아님, 타이틀 업데이트 시작");
                    
                    var newTitle = GetBatchTitle("📦 송장 처리 시스템");
                    //LogMessage($"📝 새 타이틀 생성: {newTitle}");
                    lblTitle.Text = newTitle;
                    
                    // 폼 타이틀도 함께 업데이트 (차수 정보만 포함, 버전 숨김)
                    var formTitle = GetBatchTitle("송장 처리 시스템");
                    //LogMessage($"🖼️ 폼 타이틀 업데이트: {formTitle}");
                    this.Text = formTitle;
                    
                    // 디버그 로그 추가
                    //LogMessage($"🔄 타이틀 업데이트 완료: {newTitle}");
                }
                else
                {
                    LogMessage("⚠️ lblTitle이 null입니다. UI 초기화가 완료되지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 타이틀 업데이트 중 오류: {ex.Message}");
                LogMessage($"⚠️ 스택 트레이스: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 상태바에 현재 날짜/시간을 표시하는 메서드
        /// </summary>
        private void UpdateDateTimeDisplay()
        {
            try
            {
                if (toolStripStatusLabelDateTime != null)
                {
                    var now = DateTime.Now;
                    var dayOfWeek = GetKoreanDayOfWeek(now.DayOfWeek);
                    var timePeriod = GetKoreanTimePeriod(now.Hour);
                    
                    var dateTimeText = $"{now:yyyy-MM-dd} ({dayOfWeek}) {timePeriod} {now:h:mm}";
                    toolStripStatusLabelDateTime.Text = dateTimeText;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ 날짜/시간 표시 업데이트 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 요일을 한국어로 변환하는 메서드
        /// </summary>
        /// <param name="dayOfWeek">요일</param>
        /// <returns>한국어 요일</returns>
        private string GetKoreanDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "월요일",
                DayOfWeek.Tuesday => "화요일",
                DayOfWeek.Wednesday => "수요일",
                DayOfWeek.Thursday => "목요일",
                DayOfWeek.Friday => "금요일",
                DayOfWeek.Saturday => "토요일",
                DayOfWeek.Sunday => "일요일",
                _ => "알 수 없음"
            };
        }
        
        /// <summary>
        /// 시간을 한국어 시간대별로 변환하는 메서드
        /// </summary>
        /// <param name="hour">시간 (0-23)</param>
        /// <returns>한국어 시간대</returns>
        private string GetKoreanTimePeriod(int hour)
        {
            if (hour >= 0 && hour < 12)
            {
                return "오전";
            }
            else if (hour >= 12 && hour < 18)
            {
                return "오후";
            }
            else
            {
                return "오후"; // 18시 이후도 오후로 표시
            }
        }
        
        /// <summary>
        /// 체크된 파일 다운로드 버튼 클릭 이벤트 핸들러
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnDownloadFiles_Click(object? sender, EventArgs e)
        {
            try
            {
                // 체크된 파일이 있는지 확인
                var checkedFiles = fileListContainer.CheckedFiles.ToList();

                if (checkedFiles.Count == 0)
                {
                    MessageBox.Show("다운로드할 파일을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 다운로드 폴더 선택
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "다운로드할 폴더를 선택하세요";
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        var downloadFolder = folderDialog.SelectedPath;
                        LogMessage($"📥 다운로드 시작: {checkedFiles.Count}개 파일 → {downloadFolder}");

                        // 실제 다운로드 처리
                        var dropboxService = DropboxService.Instance;
                        var downloadCount = 0;
                        var errorCount = 0;

                        foreach (var file in checkedFiles)
                        {
                            try
                            {
                                string fileName = file.FileName;
                                string? dropboxPath = file.DropboxPath;

                                var localFilePath = Path.Combine(downloadFolder, fileName);
                                
                                LogMessage($"📥 다운로드 중: {fileName}");
                                
                                if (!string.IsNullOrEmpty(dropboxPath))
                                {
                                    // 실제 Dropbox에서 파일 다운로드
                                    LogMessage($"🔗 Dropbox 경로: {dropboxPath}");
                                    var downloadResult = await dropboxService.DownloadFileAsync(dropboxPath, localFilePath);
                                    
                                    if (downloadResult)
                                    {
                                        downloadCount++;
                                        LogMessage($"✅ 다운로드 완료: {fileName}");
                                    }
                                    else
                                    {
                                        errorCount++;
                                        LogMessage($"❌ 다운로드 실패: {fileName} (Dropbox 다운로드 실패)");
                                    }
                                }
                                else
                                {
                                    // Dropbox 경로가 없는 경우 임시 파일 생성 (테스트용)
                                    LogMessage($"⚠️ Dropbox 경로가 없어 임시 파일을 생성합니다: {fileName}");
                                    var tempContent = $"이 파일은 {fileName}의 다운로드 예시입니다.\n실제 구현에서는 Dropbox에서 파일을 다운로드합니다.";
                                    await File.WriteAllTextAsync(localFilePath, tempContent);
                                    downloadCount++;
                                    LogMessage($"✅ 임시 파일 생성 완료: {fileName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                LogMessage($"❌ 다운로드 실패: {ex.Message}");
                            }
                            
                            await Task.Delay(100); // UI 응답성을 위한 짧은 지연
                        }

                        var resultMessage = $"다운로드가 완료되었습니다.\n\n다운로드 폴더: {downloadFolder}\n";
                        if (downloadCount > 0)
                        {
                            resultMessage += $"성공: {downloadCount}개 파일\n";
                        }
                        if (errorCount > 0)
                        {
                            resultMessage += $"실패: {errorCount}개 파일\n";
                        }
                        
                        LogMessage($"✅ 다운로드 완료: 성공 {downloadCount}개, 실패 {errorCount}개");
                        MessageBox.Show(resultMessage, "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 파일 다운로드 중 오류 발생: {ex.Message}");
                MessageBox.Show($"파일 다운로드 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 파일 목록에서 파일이 열렸을 때 호출되는 이벤트 핸들러
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">파일 열기 이벤트 인수</param>
        private void FileListContainer_FileOpened(object? sender, FileListContainerControl.FileOpenedEventArgs e)
        {
            try
            {
                LogMessage($"📂 파일이 열렸습니다: {e.FileInfo.FileName}");
                //LogMessage($"📍 임시 파일 경로: {e.LocalFilePath}");
                
                // 파일 열기 성공 로그 기록
                //LogManagerService.LogInfo($"파일 열기 성공: {e.FileInfo.FileName} -> {e.LocalFilePath}");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 파일 열기 이벤트 처리 중 오류: {ex.Message}");
                LogManagerService.LogError($"파일 열기 이벤트 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일 목록에 파일을 추가하는 메서드
        /// </summary>
        /// <param name="fileName">파일명</param>
        /// <param name="fileSize">파일 크기 (바이트)</param>
        /// <param name="uploadTime">업로드 시간</param>
        /// <param name="dropboxPath">Dropbox 경로 (선택사항)</param>
        public void AddFileToList(string fileName, long fileSize, DateTime uploadTime, string? dropboxPath = null)
        {
            try
            {
                //LogMessage($"🔍 AddFileToList 호출됨: {fileName}, 크기: {fileSize}, 시간: {uploadTime}");
                
                if (fileListContainer != null && !string.IsNullOrEmpty(fileName))
                {
                    //LogMessage($"✅ fileListContainer 존재함, AddFileCard 호출");
                    // 새 파일을 목록에 추가 (중복 체크는 컨트롤 내부에서 처리)
                    fileListContainer.AddFileCard(fileName, fileSize, uploadTime, dropboxPath);
                    //LogMessage($"✅ AddFileCard 호출 완료");
                }
                else
                {
                    LogMessage($"⚠️ fileListContainer가 null이거나 fileName이 비어있음: container={fileListContainer != null}, fileName={fileName}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 파일 목록 추가 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 파일 크기를 읽기 쉬운 형태로 포맷팅하는 메서드
        /// </summary>
        /// <param name="bytes">바이트 단위 크기</param>
        /// <returns>포맷팅된 파일 크기 문자열</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
        

        
        /// <summary>
        /// 폼 종료 시 이벤트 핸들러
        /// 
        /// 기능:
        /// - 리소스 정리
        /// - 임시 파일 정리
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
                
                // 임시 파일 정리
                CleanupTempFiles();
                
                LogMessage("👋 프로그램을 종료합니다.");
                
                // 리소스 정리는 GC가 자동으로 처리하므로 별도 작업 불필요
            }
            catch (Exception ex)
            {
                Console.WriteLine($"폼 종료 중 오류: {ex.Message}");
            }
            
            base.OnFormClosing(e);
        }
        
        /// <summary>
        /// 임시 파일들을 정리하는 메서드
        /// </summary>
        private void CleanupTempFiles()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "LogisticManager");
                if (Directory.Exists(tempDir))
                {
                    var files = Directory.GetFiles(tempDir);
                    var deletedCount = 0;
                    
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            LogManagerService.LogWarning($"임시 파일 삭제 실패: {file} - {ex.Message}");
                        }
                    }
                    
                    if (deletedCount > 0)
                    {
                        LogManagerService.LogInfo($"임시 파일 정리 완료: {deletedCount}개 파일 삭제");
                    }
                    
                    // 빈 디렉토리 삭제 시도
                    try
                    {
                        Directory.Delete(tempDir);
                    }
                    catch (Exception ex)
                    {
                        LogManagerService.LogWarning($"임시 디렉토리 삭제 실패: {tempDir} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"임시 파일 정리 중 오류: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 현재 로그인된 사용자명을 표시 라벨에 업데이트하는 메서드
        /// </summary>
        public void UpdateCurrentUserDisplay()
        {
            try
            {
                // App.config에서 Login 설정 읽기
                string loginSetting = System.Configuration.ConfigurationManager.AppSettings["Login"] ?? "N";
                bool showUserInfo = loginSetting.ToUpper() == "Y";
                
                if (lblCurrentUser != null && showUserInfo)
                {
                    // 디버그: 사용자명 라벨 상태 확인
                    LogMessage($"🔍 사용자명 라벨 상태: Visible={lblCurrentUser.Visible}, Location=({lblCurrentUser.Location.X}, {lblCurrentUser.Location.Y}), Size=({lblCurrentUser.Size.Width}x{lblCurrentUser.Size.Height})");
                    
                    // _authenticationService에서 현재 사용자 정보 가져오기
                    if (_authenticationService?.CurrentUser != null)
                    {
                        // Users 테이블의 name 컬럼 값을 우선 사용하고, 없으면 username 사용
                        var displayName = !string.IsNullOrEmpty(_authenticationService.CurrentUser.Name) 
                            ? _authenticationService.CurrentUser.Name 
                            : _authenticationService.CurrentUser.Username;
                        
                        LogMessage($"🔍 사용자 정보 로드됨: Name='{_authenticationService.CurrentUser.Name}', Username='{_authenticationService.CurrentUser.Username}', DisplayName='{displayName}'");
                        
                        // 사용자명이 너무 길 경우 축약하여 표시 (최대 15자)
                        var truncatedName = displayName.Length > 15 ? displayName.Substring(0, 12) + "..." : displayName;
                        lblCurrentUser.Text = $"사용자: {truncatedName}";
                        
                        // 전체 사용자명을 툴팁으로 표시
                        toolTip.SetToolTip(lblCurrentUser, $"사용자: {displayName}");
                        lblCurrentUser.ForeColor = Color.FromArgb(46, 204, 113); // 성공 색상 (녹색)
                        
                        // 사용자명 라벨을 확실히 보이도록 설정
                        lblCurrentUser.Visible = true;
                        lblCurrentUser.BringToFront();
                        
                        LogMessage($"✅ 사용자명 표시 완료: '{lblCurrentUser.Text}'");
                    }
                    else
                    {
                        LogMessage($"⚠️ _authenticationService 또는 CurrentUser가 null입니다. _authenticationService={(_authenticationService != null ? "존재" : "null")}");
                        lblCurrentUser.Text = "사용자: 미로그인";
                        lblCurrentUser.ForeColor = Color.FromArgb(231, 76, 60); // 오류 색상 (빨간색)
                        lblCurrentUser.Visible = true;
                    }
                }
                else
                {
                    LogMessage($"⚠️ lblCurrentUser가 null입니다.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 사용자명 표시 업데이트 중 오류: {ex.Message}");
                LogMessage($"❌ 스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Dropbox 경로 정보를 포함한 리스트 아이템 클래스
        /// </summary>
        private class ListItemWithTag
        {
            public string DisplayText { get; set; }
            public string DropboxPath { get; set; }

            public ListItemWithTag(string displayText, string dropboxPath)
            {
                DisplayText = displayText;
                DropboxPath = dropboxPath;
            }

            public override string ToString()
            {
                return DisplayText;
            }
        }
    }
} 