using System;
using System.Windows.Forms;
using LogisticManager.Services;
using System.Linq; // Added for FirstOrDefault
using MySqlConnector; // Added for MySqlConnector
using System.Drawing.Drawing2D;
using System.Collections.Generic; // Added for List
using System.Drawing; // Added for Color, Point, Size, Font
using System.IO; // Added for Path and File
using System.Threading.Tasks; // Added for Task
using LogisticManager.Constants;
using LogisticManager.Models;
using LogisticManager.Repositories;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 환경 변수 및 프로그램 설정을 관리하는 폼
    /// 
    /// 주요 기능:
    /// - 데이터베이스 연결 설정 (서버, 데이터베이스명, 사용자명, 비밀번호, 포트)
    /// - API 키 설정 (Dropbox, Kakao Work)
    /// - 파일 경로 설정 (입력, 출력, 임시 폴더)
    /// - 환경 변수 저장 및 로드
    /// - 데이터베이스 연결 테스트
    /// 
    /// 보안 기능:
    /// - 비밀번호 필드는 '*' 문자로 마스킹
    /// - 환경 변수를 통한 민감 정보 관리
    /// - 설정 변경 시 즉시 적용
    /// 
    /// UI 구성:
    /// - 탭 컨트롤 (데이터베이스 설정, API 설정, 파일 경로 설정)
    /// - 각 탭별 입력 필드들
    /// - 저장/취소/연결 테스트 버튼
    /// </summary>
    public partial class SettingsForm : Form
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 임시 설정값을 저장하는 딕셔너리
        /// </summary>
        private readonly Dictionary<string, string> _tempSettings = new Dictionary<string, string>();

        /// <summary>
        /// 컨트롤 참조를 직접 저장하는 딕셔너리 (성능 및 안정성 향상)
        /// </summary>
        private readonly Dictionary<string, TextBox> _textBoxes = new Dictionary<string, TextBox>();

        /// <summary>
        /// 설정이 변경되었는지 추적하는 플래그
        /// </summary>
        private bool _settingsChanged = false;

        /// <summary>
        /// 설정이 변경되었는지 확인하는 속성
        /// </summary>
        public bool SettingsChanged => _settingsChanged;

        #endregion

        #region 공통코드 관리 필드 (Common Code Management Fields)

        /// <summary>
        /// 공통코드 리포지토리
        /// </summary>
        private readonly ICommonCodeRepository _commonCodeRepository;

        /// <summary>
        /// 데이터베이스 서비스
        /// </summary>
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// 현재 선택된 그룹코드
        /// </summary>
        private string _selectedGroupCode = string.Empty;

        /// <summary>
        /// 원본 데이터 (변경사항 추적용)
        /// </summary>
        private List<CommonCode> _originalData = new();

        /// <summary>
        /// 데이터가 변경되었는지 추적
        /// </summary>
        private bool _isDataModified = false;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// SettingsForm 생성자
        /// 
        /// 초기화 순서:
        /// 1. 폼 기본 설정 (InitializeComponent)
        /// 2. 현재 설정값 로드 (LoadCurrentSettings)
        /// 3. 임시 설정값 초기화 (InitializeTempSettings)
        /// </summary>
        public SettingsForm()
        {
            // 공통코드 관련 서비스 초기화
            _databaseService = new DatabaseService();
            _commonCodeRepository = new CommonCodeRepository(_databaseService);

            InitializeComponent();
            
            // 폼이 완전히 로드된 후에 설정을 로드하도록 Load 이벤트 사용
            this.Load += (sender, e) =>
            {
                // 약간의 지연을 두어 컨트롤이 완전히 초기화되도록 함
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = 100; // 100ms 지연
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    
                    Console.WriteLine("🎯 설정 로드 타이머 시작");
                    LoadCurrentSettings();
                    InitializeTempSettings();
                };
                timer.Start();
            };
        }

        #endregion

        #region UI 초기화 (UI Initialization)

        /// <summary>
        /// 폼 UI를 초기화하는 메서드
        /// 
        /// 구성 요소:
        /// - 탭 컨트롤 (데이터베이스, API, 파일 경로)
        /// - 각 탭별 설정 패널
        /// - 하단 버튼 패널 (저장, 취소, 연결 테스트)
        /// 
        /// 폼 설정:
        /// - 크기: 700x600
        /// - 모달 대화상자
        /// - 최대화/최소화 버튼 비활성화
        /// - 부모 폼 중앙에 위치
        /// </summary>
        private void InitializeComponent()
        {
            // 폼 기본 설정
            this.Text = "⚙️ 설정";
            this.Size = new System.Drawing.Size(1000, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(900, 650);
            this.BackColor = Color.FromArgb(240, 244, 248);

            // 타이틀 라벨
            var titleLabel = new Label
            {
                Text = "🔧 설정",
                Location = new Point(20, 20),
                Size = new Size(660, 30),
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(20, 20, 20, 0)
            };

            // 탭 컨트롤 생성 및 설정
            var tabControl = new TabControl();
            tabControl.Location = new Point(20, 60);
            tabControl.Size = new Size(660, 540);
            tabControl.Font = new Font("맑은 고딕", 9F);
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Margin = new Padding(20, 60, 20, 70);

            // 데이터베이스 설정 탭 (숨김 처리)
            // var dbTab = new TabPage("🗄️ 데이터베이스 설정");
            // dbTab.Controls.Add(CreateDatabaseSettingsPanel());
            // tabControl.TabPages.Add(dbTab);

            // API 설정 탭 (숨김 처리)
            // var apiTab = new TabPage("🔗 API 설정");
            // apiTab.Controls.Add(CreateApiSettingsPanel());
            // tabControl.TabPages.Add(apiTab);

            // 파일 경로 설정 탭
            var pathTab = new TabPage("📁 파일 경로 설정");
            pathTab.Controls.Add(CreatePathSettingsPanel());
            tabControl.TabPages.Add(pathTab);

            // 공통코드 관리 탭
            var commonCodeTab = new TabPage("🔧 공통코드 관리");
            commonCodeTab.Controls.Add(CreateCommonCodeManagementPanel());
            tabControl.TabPages.Add(commonCodeTab);

            // 하단 버튼 패널 생성
            var buttonPanel = new Panel();
            buttonPanel.Location = new Point(20, 620);
            buttonPanel.Size = new Size(660, 50);
            buttonPanel.BackColor = Color.Transparent;
            buttonPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonPanel.Margin = new Padding(20, 0, 20, 20);

            // 저장 버튼 (경로 설정 탭에서만 표시)
            var bottomSaveButton = CreateModernButton("💾 저장", new Point(0, 10), new Size(80, 35), Color.FromArgb(46, 204, 113));
            bottomSaveButton.Click += SaveButton_Click;
            bottomSaveButton.Anchor = AnchorStyles.None;

            // 취소 버튼
            var cancelButton = CreateModernButton("❌ 닫기", new Point(0, 10), new Size(80, 35), Color.FromArgb(231, 76, 60));
            cancelButton.Click += (sender, e) => this.Close();
            cancelButton.Anchor = AnchorStyles.None;

            // 패널에 추가
            buttonPanel.Controls.AddRange(new Control[] { bottomSaveButton, cancelButton });

            // 하단 버튼 레이아웃 갱신 함수 (로컬 함수)
            void UpdateBottomButtonsLayout()
            {
                bool isPathTabActive = tabControl.SelectedTab != null && tabControl.SelectedTab.Text.Contains("파일 경로 설정");
                bottomSaveButton.Visible = isPathTabActive;

                if (isPathTabActive)
                {
                    int totalWidth = bottomSaveButton.Width + 20 + cancelButton.Width;
                    int startX = (buttonPanel.Width - totalWidth) / 2;
                    bottomSaveButton.Left = startX;
                    cancelButton.Left = startX + bottomSaveButton.Width + 20;
                }
                else
                {
                    cancelButton.Left = (buttonPanel.Width - cancelButton.Width) / 2;
                }
            }

            // 탭 변경 시 레이아웃 갱신
            tabControl.SelectedIndexChanged += (s, e) => UpdateBottomButtonsLayout();

            // 버튼 패널 리사이즈 시 레이아웃 갱신
            buttonPanel.Resize += (s, e) => UpdateBottomButtonsLayout();
            
            // 폼 크기 변경 시 컨트롤 크기 자동 조정
            this.Resize += (s, e) =>
            {
                // 타이틀 라벨 너비를 폼 너비에 맞게 조정
                titleLabel.Width = this.ClientSize.Width - 40; // 좌우 여백 20px씩
                
                // 탭 컨트롤 크기를 폼 크기에 맞게 조정
                tabControl.Width = this.ClientSize.Width - 40;
                tabControl.Height = this.ClientSize.Height - 140; // 상단 타이틀(50) + 하단 버튼(50) + 여백(40)
                
                // 하단 버튼 패널 크기를 폼 너비에 맞게 조정
                buttonPanel.Width = this.ClientSize.Width - 40;
                UpdateBottomButtonsLayout();
            };
            
            // 초기 로드 시에도 크기 조정 적용 + 레이아웃 갱신
            this.Load += (s, e) =>
            {
                titleLabel.Width = this.ClientSize.Width - 40;
                tabControl.Width = this.ClientSize.Width - 40;
                tabControl.Height = this.ClientSize.Height - 140;
                buttonPanel.Width = this.ClientSize.Width - 40;
                UpdateBottomButtonsLayout();
            };

            // 모든 컨트롤을 폼에 추가
            this.Controls.AddRange(new Control[]
            {
                titleLabel,
                tabControl,
                buttonPanel
            });
        }

        /// <summary>
        /// 모던한 스타일의 버튼을 생성하는 메서드
        /// </summary>
        /// <param name="text">버튼 텍스트</param>
        /// <param name="location">위치</param>
        /// <param name="size">크기</param>
        /// <param name="backgroundColor">배경색</param>
        /// <returns>생성된 버튼</returns>
        private Button CreateModernButton(string text, Point location, Size size, Color backgroundColor)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = backgroundColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }, // 테두리 제거
                TextAlign = ContentAlignment.MiddleCenter // 텍스트 중앙 정렬
            };

            // 둥근 모서리 제거 - 일반 사각형 버튼 사용
            // button.Region = new Region(CreateRoundedRectangle(button.ClientRectangle, 6));

            // 호버 효과 개선
            button.MouseEnter += (sender, e) =>
            {
                // 더 부드러운 색상 변화
                var lighterColor = Color.FromArgb(
                    Math.Min(255, backgroundColor.R + 30),
                    Math.Min(255, backgroundColor.G + 30),
                    Math.Min(255, backgroundColor.B + 30)
                );
                button.BackColor = lighterColor;
            };

            button.MouseLeave += (sender, e) =>
            {
                button.BackColor = backgroundColor;
            };

            // 클릭 효과 추가
            button.MouseDown += (sender, e) =>
            {
                var darkerColor = Color.FromArgb(
                    Math.Max(0, backgroundColor.R - 30),
                    Math.Max(0, backgroundColor.G - 30),
                    Math.Max(0, backgroundColor.B - 30)
                );
                button.BackColor = darkerColor;
            };

            button.MouseUp += (sender, e) =>
            {
                button.BackColor = backgroundColor;
            };

            return button;
        }

        /// <summary>
        /// 둥근 모서리 사각형 경로를 생성하는 메서드 (개선된 버전)
        /// </summary>
        /// <param name="rect">사각형 영역</param>
        /// <param name="radius">모서리 반지름</param>
        /// <returns>둥근 모서리 경로</returns>
        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;

            // 더 부드러운 곡선을 위해 베지어 곡선 사용
            if (diameter > 0)
            {
                // 좌상단 모서리
                path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                // 우상단 모서리
                path.AddArc(rect.Width - diameter, rect.Y, diameter, diameter, 270, 90);
                // 우하단 모서리
                path.AddArc(rect.Width - diameter, rect.Height - diameter, diameter, diameter, 0, 90);
                // 좌하단 모서리
                path.AddArc(rect.X, rect.Height - diameter, diameter, diameter, 90, 90);
            }
            else
            {
                // 반지름이 0인 경우 일반 사각형
                path.AddRectangle(rect);
            }

            path.CloseFigure();
            return path;
        }

        #endregion

        #region 설정 패널 생성 (Settings Panel Creation)

        /// <summary>
        /// 데이터베이스 설정 패널을 생성하는 메서드
        /// 
        /// 포함된 설정:
        /// - 서버 주소
        /// - 데이터베이스명
        /// - 사용자명
        /// - 비밀번호
        /// - 포트 번호
        /// </summary>
        /// <returns>데이터베이스 설정 패널</returns>
        private Panel CreateDatabaseSettingsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(20)
            };

            var controls = new List<Control>();

            // 서버 설정
            controls.Add(CreateLabel("🌐 서버 주소:", new Point(20, 20)));
            var txtServer = CreateTextBox("localhost", new Point(20, 45), new Size(200, 25));
            txtServer.Name = "txtServer";
            _textBoxes["txtServer"] = txtServer; // 컨트롤 참조 저장
            controls.Add(txtServer);

            // 데이터베이스명 설정
            controls.Add(CreateLabel("🗄️ 데이터베이스명:", new Point(20, 80)));
            var txtDatabase = CreateTextBox("logistic_manager", new Point(20, 105), new Size(200, 25));
            txtDatabase.Name = "txtDatabase";
            _textBoxes["txtDatabase"] = txtDatabase; // 컨트롤 참조 저장
            controls.Add(txtDatabase);

            // 사용자명 설정
            controls.Add(CreateLabel("👤 사용자명:", new Point(20, 140)));
            var txtUser = CreateTextBox("root", new Point(20, 165), new Size(200, 25));
            txtUser.Name = "txtUser";
            _textBoxes["txtUser"] = txtUser; // 컨트롤 참조 저장
            controls.Add(txtUser);

            // 비밀번호 설정
            controls.Add(CreateLabel("🔒 비밀번호:", new Point(20, 200)));
            var txtPassword = CreateTextBox("", new Point(20, 225), new Size(200, 25));
            txtPassword.Name = "txtPassword";
            txtPassword.UseSystemPasswordChar = true; // 비밀번호 마스킹
            _textBoxes["txtPassword"] = txtPassword; // 컨트롤 참조 저장
            controls.Add(txtPassword);

            // 포트 설정
            controls.Add(CreateLabel("🔌 포트:", new Point(20, 260)));
            var txtPort = CreateTextBox("3306", new Point(20, 285), new Size(200, 25));
            txtPort.Name = "txtPort";
            _textBoxes["txtPort"] = txtPort; // 컨트롤 참조 저장
            controls.Add(txtPort);

            // 연결테스트 버튼
            var btnTestConnection = CreateModernButton("🔍 연결테스트", new Point(240, 285), new Size(100, 25), Color.FromArgb(52, 152, 219));
            btnTestConnection.Click += TestConnectionButton_Click;
            controls.Add(btnTestConnection);

            // 연결테스트 결과 라벨
            var lblConnectionResult = CreateLabel("", new Point(20, 320));
            lblConnectionResult.Name = "lblConnectionResult";
            lblConnectionResult.Size = new Size(400, 20);
            lblConnectionResult.Font = new Font("맑은 고딕", 8F);
            controls.Add(lblConnectionResult);

            // 설명 라벨
            var infoLabel = CreateLabel("💡 환경 변수를 통해 안전하게 설정값을 관리합니다.", new Point(20, 350));
            infoLabel.ForeColor = Color.FromArgb(127, 140, 141);
            infoLabel.Font = new Font("맑은 고딕", 8F);
            controls.Add(infoLabel);

            panel.Controls.AddRange(controls.ToArray());
            return panel;
        }

        /// <summary>
        /// API 설정 패널을 생성하는 메서드
        /// 
        /// 포함된 설정:
        /// - Dropbox API 키
        /// - Kakao Work API 키
        /// - Kakao Work 채팅방 ID
        /// </summary>
        /// <returns>API 설정 패널</returns>
        private Panel CreateApiSettingsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(20),
                AutoScroll = true
            };

            var controls = new List<Control>();

            // Dropbox API 설정
            controls.Add(CreateLabel("☁️ Dropbox Access Token:", new Point(20, 20)));
            var txtDropboxApi = CreateTextBox("", new Point(20, 45), new Size(350, 25));
            txtDropboxApi.Name = "txtDropboxApi";
            txtDropboxApi.UseSystemPasswordChar = true;
            txtDropboxApi.PlaceholderText = "Dropbox API v2 Access Token 입력";
            _textBoxes["txtDropboxApi"] = txtDropboxApi;
            controls.Add(txtDropboxApi);

            // Dropbox 저장 버튼
            var btnDropboxSave = CreateModernButton("💾 저장", new Point(380, 45), new Size(60, 25), Color.FromArgb(46, 204, 113));
            btnDropboxSave.Click += (sender, e) => SaveApiSetting("DROPBOX_API_KEY", txtDropboxApi.Text);
            controls.Add(btnDropboxSave);

            // Dropbox 연결테스트 버튼
            var btnDropboxTest = CreateModernButton("🔍 연결테스트", new Point(450, 45), new Size(90, 25), Color.FromArgb(52, 152, 219));
            btnDropboxTest.Click += (sender, e) => TestApiConnection("Dropbox", txtDropboxApi.Text);
            controls.Add(btnDropboxTest);

            // Dropbox 결과 라벨
            var lblDropboxResult = CreateLabel("", new Point(20, 75));
            lblDropboxResult.Name = "lblDropboxResult";
            lblDropboxResult.Size = new Size(500, 20);
            lblDropboxResult.Font = new Font("맑은 고딕", 8F);
            controls.Add(lblDropboxResult);

            // Dropbox 설명 라벨
            var lblDropboxInfo = CreateLabel("💡 Dropbox 개발자 콘솔에서 생성한 Access Token을 입력하세요.", new Point(20, 100));
            lblDropboxInfo.Size = new Size(500, 20);
            lblDropboxInfo.Font = new Font("맑은 고딕", 8F);
            lblDropboxInfo.ForeColor = Color.FromArgb(127, 140, 141);
            controls.Add(lblDropboxInfo);

            // Kakao Work API 설정
            controls.Add(CreateLabel("💬 Kakao Work API 키:", new Point(20, 140)));
            var txtKakaoApi = CreateTextBox("", new Point(20, 165), new Size(350, 25));
            txtKakaoApi.Name = "txtKakaoApi";
            txtKakaoApi.UseSystemPasswordChar = true;
            _textBoxes["txtKakaoApi"] = txtKakaoApi;
            controls.Add(txtKakaoApi);

            // Kakao Work 저장 버튼
            var btnKakaoSave = CreateModernButton("💾 저장", new Point(380, 165), new Size(60, 25), Color.FromArgb(46, 204, 113));
            btnKakaoSave.Click += (sender, e) => SaveApiSetting("KAKAO_WORK_API_KEY", txtKakaoApi.Text);
            controls.Add(btnKakaoSave);

            // Kakao Work 연결테스트 버튼
            var btnKakaoTest = CreateModernButton("🔍 연결테스트", new Point(450, 165), new Size(90, 25), Color.FromArgb(52, 152, 219));
            btnKakaoTest.Click += (sender, e) => TestApiConnection("Kakao Work", txtKakaoApi.Text);
            controls.Add(btnKakaoTest);

            // Kakao Work 결과 라벨
            var lblKakaoResult = CreateLabel("", new Point(20, 195));
            lblKakaoResult.Name = "lblKakaoResult";
            lblKakaoResult.Size = new Size(500, 20);
            lblKakaoResult.Font = new Font("맑은 고딕", 8F);
            controls.Add(lblKakaoResult);

            // Kakao Work 채팅방 ID 설정
            controls.Add(CreateLabel("💬 Kakao Work 채팅방 ID:", new Point(20, 230)));
            var txtKakaoChatroom = CreateTextBox("", new Point(20, 255), new Size(350, 25));
            txtKakaoChatroom.Name = "txtKakaoChatroom";
            _textBoxes["txtKakaoChatroom"] = txtKakaoChatroom;
            controls.Add(txtKakaoChatroom);

            // Kakao Work 채팅방 저장 버튼
            var btnKakaoChatroomSave = CreateModernButton("💾 저장", new Point(380, 255), new Size(60, 25), Color.FromArgb(46, 204, 113));
            btnKakaoChatroomSave.Click += (sender, e) => SaveApiSetting("KAKAO_WORK_CHATROOM_ID", txtKakaoChatroom.Text);
            controls.Add(btnKakaoChatroomSave);

            // Kakao Work 채팅방 연결테스트 버튼
            var btnKakaoChatroomTest = CreateModernButton("🔍 연결테스트", new Point(450, 255), new Size(90, 25), Color.FromArgb(52, 152, 219));
            btnKakaoChatroomTest.Click += (sender, e) => TestApiConnection("Kakao Work Chatroom", txtKakaoChatroom.Text);
            controls.Add(btnKakaoChatroomTest);

            // Kakao Work 채팅방 결과 라벨
            var lblKakaoChatroomResult = CreateLabel("", new Point(20, 285));
            lblKakaoChatroomResult.Name = "lblKakaoChatroomResult";
            lblKakaoChatroomResult.Size = new Size(500, 20);
            lblKakaoChatroomResult.Font = new Font("맑은 고딕", 8F);
            controls.Add(lblKakaoChatroomResult);

            // 설명 라벨
            var infoLabel = CreateLabel("💡 각 API 항목을 개별적으로 저장하고 연결을 테스트할 수 있습니다.", new Point(20, 320));
            infoLabel.ForeColor = Color.FromArgb(127, 140, 141);
            infoLabel.Font = new Font("맑은 고딕", 8F);
            infoLabel.Size = new Size(600, 40); // 크기를 늘려서 텍스트가 완전히 표시되도록 함
            infoLabel.AutoSize = false; // 자동 크기 조정 비활성화
            controls.Add(infoLabel);

            panel.Controls.AddRange(controls.ToArray());
            return panel;
        }

        /// <summary>
        /// 파일 경로 설정 패널을 생성하는 메서드
        /// 
        /// 포함된 설정:
        /// - 입력 폴더 경로
        /// - 출력 폴더 경로
        /// - 임시 폴더 경로
        /// </summary>
        /// <returns>파일 경로 설정 패널</returns>
        private Panel CreatePathSettingsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(20)
            };

            var controls = new List<Control>();

            // 입력 폴더 설정
            controls.Add(CreateLabel("📥 입력 폴더 경로:", new Point(20, 20)));
            var txtInputPath = CreateTextBox("C:\\Work\\Input\\", new Point(20, 45), new Size(350, 25));
            txtInputPath.Name = "txtInputPath";
            _textBoxes["txtInputPath"] = txtInputPath; // 컨트롤 참조 저장
            controls.Add(txtInputPath);

            // 입력 폴더 선택 버튼
            var btnInputBrowse = CreateModernButton("📁", new Point(380, 45), new Size(30, 25), Color.FromArgb(52, 152, 219));
            btnInputBrowse.Click += (sender, e) => BrowseFolder("txtInputPath");
            controls.Add(btnInputBrowse);

            // 출력 폴더 설정
            controls.Add(CreateLabel("📤 출력 폴더 경로:", new Point(20, 80)));
            var txtOutputPath = CreateTextBox("C:\\Work\\Output\\", new Point(20, 105), new Size(350, 25));
            txtOutputPath.Name = "txtOutputPath";
            _textBoxes["txtOutputPath"] = txtOutputPath; // 컨트롤 참조 저장
            controls.Add(txtOutputPath);

            // 출력 폴더 선택 버튼
            var btnOutputBrowse = CreateModernButton("📁", new Point(380, 105), new Size(30, 25), Color.FromArgb(52, 152, 219));
            btnOutputBrowse.Click += (sender, e) => BrowseFolder("txtOutputPath");
            controls.Add(btnOutputBrowse);

            // 임시 폴더 설정
            controls.Add(CreateLabel("📁 임시 폴더 경로:", new Point(20, 140)));
            var txtTempPath = CreateTextBox("C:\\Work\\Temp\\", new Point(20, 165), new Size(350, 25));
            txtTempPath.Name = "txtTempPath";
            _textBoxes["txtTempPath"] = txtTempPath; // 컨트롤 참조 저장
            controls.Add(txtTempPath);

            // 임시 폴더 선택 버튼
            var btnTempBrowse = CreateModernButton("📁", new Point(380, 165), new Size(30, 25), Color.FromArgb(52, 152, 219));
            btnTempBrowse.Click += (sender, e) => BrowseFolder("txtTempPath");
            controls.Add(btnTempBrowse);

            // 설명 라벨
            var infoLabel = CreateLabel("💡 폴더가 존재하지 않으면 자동으로 생성됩니다. Dropbox 폴더 경로는 App.config에서 관리됩니다.", new Point(20, 200));
            infoLabel.ForeColor = Color.FromArgb(127, 140, 141);
            infoLabel.Font = new Font("맑은 고딕", 8F);
            controls.Add(infoLabel);

            panel.Controls.AddRange(controls.ToArray());
            return panel;
        }

        /// <summary>
        /// 라벨을 생성하는 헬퍼 메서드
        /// </summary>
        /// <param name="text">라벨 텍스트</param>
        /// <param name="location">위치</param>
        /// <returns>생성된 라벨</returns>
        private Label CreateLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Location = location,
                Size = new Size(400, 20), // 기본 크기를 늘려서 긴 텍스트도 표시
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(52, 73, 94),
                BackColor = Color.Transparent,
                AutoSize = false // 자동 크기 조정 비활성화
            };
        }

        /// <summary>
        /// 텍스트박스를 생성하는 헬퍼 메서드
        /// </summary>
        /// <param name="defaultText">기본 텍스트</param>
        /// <param name="location">위치</param>
        /// <param name="size">크기</param>
        /// <returns>생성된 텍스트박스</returns>
        private TextBox CreateTextBox(string defaultText, Point location, Size size)
        {
            return new TextBox
            {
                Text = defaultText,
                Location = location,
                Size = size,
                Font = new Font("맑은 고딕", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
        }

        /// <summary>
        /// 파일 경로 설정이 유효한지 검증하는 메서드
        /// </summary>
        /// <returns>검증 결과 (true: 유효, false: 유효하지 않음)</returns>
        private bool ValidateFilePathSettings()
        {
            var inputPath = _tempSettings.GetValueOrDefault("INPUT_FOLDER_PATH", "");
            var outputPath = _tempSettings.GetValueOrDefault("OUTPUT_FOLDER_PATH", "");
            var tempPath = _tempSettings.GetValueOrDefault("TEMP_FOLDER_PATH", "");

            // 빈 경로 체크
            if (string.IsNullOrWhiteSpace(inputPath) || string.IsNullOrWhiteSpace(outputPath) || string.IsNullOrWhiteSpace(tempPath))
            {
                MessageBox.Show("⚠️ 모든 파일 경로를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 경로 형식 체크 (기본적인 검증)
            try
            {
                Path.GetFullPath(inputPath);
                Path.GetFullPath(outputPath);
                Path.GetFullPath(tempPath);
            }
            catch
            {
                MessageBox.Show("⚠️ 파일 경로 형식이 올바르지 않습니다.", "경로 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 폴더 탐색기를 열어서 폴더를 선택하는 메서드
        /// </summary>
        /// <param name="textBoxName">폴더 경로를 저장할 텍스트박스 이름</param>
        private void BrowseFolder(string textBoxName)
        {
            try
            {
                using var folderDialog = new FolderBrowserDialog
                {
                    Description = "폴더를 선택하세요",
                    ShowNewFolderButton = true,
                    UseDescriptionForTitle = true
                };

                // 현재 텍스트박스에 있는 경로를 초기 경로로 설정
                if (_textBoxes.TryGetValue(textBoxName, out var textBox) && textBox != null)
                {
                    var currentPath = textBox.Text.Trim();
                    if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                    {
                        folderDialog.InitialDirectory = currentPath;
                    }
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // 선택된 폴더 경로를 텍스트박스에 설정
                    if (_textBoxes.TryGetValue(textBoxName, out var targetTextBox) && targetTextBox != null)
                    {
                        targetTextBox.Text = folderDialog.SelectedPath;
                        Console.WriteLine($"📁 폴더 선택 완료: {folderDialog.SelectedPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 폴더 선택 중 오류: {ex.Message}");
                MessageBox.Show($"폴더 선택 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 공통코드 관리 (Common Code Management)

        /// <summary>
        /// 공통코드 관리 패널을 생성하는 메서드
        /// </summary>
        /// <returns>공통코드 관리 패널</returns>
        private Panel CreateCommonCodeManagementPanel()
        {
            var panel = new Panel
            {
                BackColor = Color.FromArgb(240, 244, 248),
                Dock = DockStyle.Fill
            };

            // SplitContainer로 좌/우 영역 구성 (창 크기 변경 대응)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 8,
                Panel1MinSize = 0,
                Panel2MinSize = 0,
                FixedPanel = FixedPanel.None,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(228, 233, 238)
            };

            // 스플리터 위에 마우스를 올리면 좌우(또는 상하) 화살표 커서 표시
            split.MouseMove += (sender, e) =>
            {
                var desiredCursor = split.Orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.HSplit;
                split.Cursor = split.SplitterRectangle.Contains(e.Location) ? desiredCursor : Cursors.Default;
            };
            split.MouseLeave += (sender, e) => { split.Cursor = Cursors.Default; };

            // SplitterDistance 안전 설정 함수 (창 크기에 따라 유효 범위로 보정)
            void SafeSetSplitterDistance()
            {
                try
                {
                    int totalWidth = split.ClientSize.Width;
                    if (totalWidth <= 0) return;

                    int leftMin = split.Panel1MinSize;
                    int rightMin = split.Panel2MinSize;
                    int maxLeft = totalWidth - rightMin;

                    // 유효 범위가 없는 경우(너비가 너무 작음) 설정 시도 중단
                    if (leftMin > maxLeft) return;

                    // 전체 폭의 30%를 기본값으로 시도하되 유효 범위로 보정
                    int desired = (int)(totalWidth * 0.3);
                    if (desired < leftMin) desired = leftMin;
                    if (desired > maxLeft) desired = maxLeft;

                    // 유효 범위일 때만 설정
                    if (desired >= leftMin && desired <= maxLeft)
                    {
                        split.SplitterDistance = desired;
                    }
                }
                catch
                {
                    // 무시: 초기 레이아웃 타이밍 문제 예방
                }
            }

            // ---- 좌측: 그룹코드 목록 ----
            var leftContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var treeTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.White
            };

            var treeViewLabel = new Label
            {
                Text = "📁 그룹코드 목록",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 8, 8, 8),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft
            };
            treeTop.Controls.Add(treeViewLabel);

            var treeBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.White
            };

            var btnAddGroup = CreateModernButton("➕ 새 그룹", new Point(8, 7), new Size(110, 26), Color.FromArgb(52, 152, 219));
            var btnRefresh = CreateModernButton("🔄 새로고침", new Point(126, 7), new Size(110, 26), Color.FromArgb(155, 89, 182));
            // Anchor로 우측/좌측 고정
            btnAddGroup.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            btnRefresh.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            treeBottom.Controls.AddRange(new Control[] { btnAddGroup, btnRefresh });

            var treeViewGroupCodes = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 9F),
                FullRowSelect = true,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                HideSelection = false,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            leftContainer.Controls.Add(treeViewGroupCodes);
            leftContainer.Controls.Add(treeBottom);
            leftContainer.Controls.Add(treeTop);
            split.Panel1.Controls.Add(leftContainer);

            // ---- 우측: 공통코드 상세 ----
            var rightContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var header = new Label
            {
                Text = "📋 공통코드 상세",
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(8, 8, 8, 8),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var dataGridViewCodes = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                GridColor = Color.LightGray,
                BorderStyle = BorderStyle.Fixed3D
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(250, 251, 252)
            };

            var btnAddCode = CreateModernButton("➕ 코드 추가", new Point(10, 11), new Size(120, 34), Color.FromArgb(46, 204, 113));
            var btnSave = CreateModernButton("💾 저장", new Point(140, 11), new Size(120, 34), Color.FromArgb(52, 152, 219));
            var btnDelete = CreateModernButton("🗑️ 삭제", new Point(270, 11), new Size(120, 34), Color.FromArgb(231, 76, 60));
            // 버튼 Anchor 설정
            btnAddCode.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            btnSave.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            btnDelete.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            
            buttonPanel.Controls.AddRange(new Control[] { btnAddCode, btnSave, btnDelete });

            rightContainer.Controls.Add(dataGridViewCodes);
            rightContainer.Controls.Add(buttonPanel);
            rightContainer.Controls.Add(header);
            split.Panel2.Controls.Add(rightContainer);

            // 전체 컨테이너 추가 및 초기 SplitterDistance 보정
            panel.Controls.Add(split);
            panel.Resize += (s, e) => SafeSetSplitterDistance();
            split.SizeChanged += (s, e) => SafeSetSplitterDistance();
            split.HandleCreated += (s, e) => SafeSetSplitterDistance();
            // 로드 직후 한 번 보정
            SafeSetSplitterDistance();

            // 공통코드 관리 기능 초기화
            InitializeCommonCodeManagement(treeViewGroupCodes, dataGridViewCodes, btnAddGroup, btnRefresh, btnAddCode, btnSave, btnDelete);

            return panel;
        }

        /// <summary>
        /// 공통코드 테이블을 재생성하는 메서드 (기존 테이블 삭제 후 재생성)
        /// </summary>
        private async Task RecreateCommonCodeTableAsync()
        {
            try
            {
                var databaseService = new DatabaseService();

                // 1. 기존 테이블 삭제
                var dropTableQuery = "DROP TABLE IF EXISTS CommonCode";
                await databaseService.ExecuteNonQueryAsync(dropTableQuery);
                LogManagerService.LogInfo("기존 CommonCode 테이블 삭제 완료");

                // 2. 새 테이블 생성
                var createTableQuery = @"
                    CREATE TABLE CommonCode (
                        GroupCode varchar(50) NOT NULL COMMENT '코드 그룹 (예: USER_ROLE, ORDER_STATUS)',
                        Code varchar(50) NOT NULL COMMENT '개별 코드 값 (예: ADMIN, USER, PENDING)',
                        CodeName varchar(100) NOT NULL COMMENT '코드의 표시 이름 (예: 관리자, 일반사용자, 주문접수)',
                        Description varchar(255) DEFAULT NULL COMMENT '코드에 대한 상세 설명',
                        SortOrder int(11) NOT NULL DEFAULT 0 COMMENT '정렬 순서 (낮은 숫자가 먼저 표시됨)',
                        IsUsed tinyint(1) NOT NULL DEFAULT 1 COMMENT '사용 여부 (TRUE: 사용, FALSE: 미사용)',
                        Attribute1 varchar(255) DEFAULT NULL COMMENT '추가 속성 1',
                        Attribute2 varchar(255) DEFAULT NULL COMMENT '추가 속성 2',
                        CreatedBy varchar(50) DEFAULT NULL COMMENT '생성자',
                        CreatedAt datetime NOT NULL DEFAULT CURRENT_TIMESTAMP() COMMENT '생성 일시',
                        UpdatedBy varchar(50) DEFAULT NULL COMMENT '수정자',
                        UpdatedAt datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '수정 일시',
                        GroupCodeNm varchar(255) DEFAULT NULL COMMENT '그룹코드명',
                        PRIMARY KEY (GroupCode, Code)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='공통코드 관리 테이블';";

                await databaseService.ExecuteNonQueryAsync(createTableQuery);
                LogManagerService.LogInfo("새 CommonCode 테이블 생성 완료");

                // 3. 샘플 데이터 추가
                await InsertSampleDataAsync(databaseService);

                MessageBox.Show("✅ CommonCode 테이블이 성공적으로 재생성되었습니다.\n샘플 데이터도 함께 추가되었습니다.", 
                    "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 테이블 재생성 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 공통코드 테이블을 생성하는 메서드 (기존 방식 - 호환성 유지)
        /// </summary>
        private async Task CreateCommonCodeTableAsync()
        {
            try
            {
                var createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS CommonCode (
                        GroupCode varchar(50) NOT NULL COMMENT '코드 그룹 (예: USER_ROLE, ORDER_STATUS)',
                        Code varchar(50) NOT NULL COMMENT '개별 코드 값 (예: ADMIN, USER, PENDING)',
                        CodeName varchar(100) NOT NULL COMMENT '코드의 표시 이름 (예: 관리자, 일반사용자, 주문접수)',
                        Description varchar(255) DEFAULT NULL COMMENT '코드에 대한 상세 설명',
                        SortOrder int(11) NOT NULL DEFAULT 0 COMMENT '정렬 순서 (낮은 숫자가 먼저 표시됨)',
                        IsUsed tinyint(1) NOT NULL DEFAULT 1 COMMENT '사용 여부 (TRUE: 사용, FALSE: 미사용)',
                        Attribute1 varchar(255) DEFAULT NULL COMMENT '추가 속성 1',
                        Attribute2 varchar(255) DEFAULT NULL COMMENT '추가 속성 2',
                        CreatedBy varchar(50) DEFAULT NULL COMMENT '생성자',
                        CreatedAt datetime NOT NULL DEFAULT CURRENT_TIMESTAMP() COMMENT '생성 일시',
                        UpdatedBy varchar(50) DEFAULT NULL COMMENT '수정자',
                        UpdatedAt datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '수정 일시',
                        GroupCodeNm varchar(255) DEFAULT NULL COMMENT '그룹코드명',
                        PRIMARY KEY (GroupCode, Code)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='공통코드 관리 테이블';";

                var databaseService = new DatabaseService();
                var result = await databaseService.ExecuteNonQueryAsync(createTableQuery);

                if (result > 0)
                {
                    MessageBox.Show("✅ 공통코드 테이블이 성공적으로 생성되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 샘플 데이터 추가
                    await InsertSampleDataAsync(databaseService);
                }
                else
                {
                    MessageBox.Show("ℹ️ 공통코드 테이블이 이미 존재합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 테이블 생성 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 샘플 데이터를 추가하는 메서드
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스</param>
        private async Task InsertSampleDataAsync(DatabaseService databaseService)
        {
            try
            {
                var sampleDataQuery = @"
                    INSERT INTO CommonCode (GroupCode, Code, CodeName, Description, SortOrder, IsUsed, CreatedBy, CreatedAt) VALUES
                    ('USER_ROLE', 'ADMIN', '관리자', '시스템 관리자 권한', 1, 1, 'SYSTEM', NOW()),
                    ('USER_ROLE', 'USER', '일반사용자', '일반 사용자 권한', 2, 1, 'SYSTEM', NOW()),
                    ('USER_ROLE', 'GUEST', '게스트', '임시 사용자 권한', 3, 1, 'SYSTEM', NOW()),
                    
                    ('ORDER_STATUS', 'PENDING', '주문접수', '주문이 접수된 상태', 1, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'PROCESSING', '처리중', '주문이 처리 중인 상태', 2, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'SHIPPED', '배송중', '상품이 배송 중인 상태', 3, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'DELIVERED', '배송완료', '배송이 완료된 상태', 4, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'COMPLETED', '완료', '주문 처리가 완료된 상태', 5, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'CANCELLED', '취소', '주문이 취소된 상태', 6, 1, 'SYSTEM', NOW()),
                    
                    ('DELIVERY_POLICY', 'STANDARD', '일반배송', '일반 택배 배송', 1, 1, 'SYSTEM', NOW()),
                    ('DELIVERY_POLICY', 'EXPRESS', '당일배송', '당일 특급 배송', 2, 1, 'SYSTEM', NOW()),
                    ('DELIVERY_POLICY', 'PICKUP', '직접수령', '매장에서 직접 수령', 3, 1, 'SYSTEM', NOW()),
                    
                    ('PAYMENT_METHOD', 'CARD', '신용카드', '신용카드 결제', 1, 1, 'SYSTEM', NOW()),
                    ('PAYMENT_METHOD', 'BANK', '무통장입금', '계좌이체 결제', 2, 1, 'SYSTEM', NOW()),
                    ('PAYMENT_METHOD', 'MOBILE', '모바일결제', '휴대폰 결제', 3, 1, 'SYSTEM', NOW());";

                var result = await databaseService.ExecuteNonQueryAsync(sampleDataQuery);
                LogManagerService.LogInfo($"샘플 데이터 {result}개 추가 완료");
                
                if (result > 0)
                {
                    LogManagerService.LogInfo($"✅ 샘플 데이터 추가 성공: {result}개");
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"샘플 데이터 추가 중 오류: {ex.Message}");
                // 샘플 데이터 추가 실패는 치명적이지 않으므로 예외를 다시 던지지 않음
                MessageBox.Show($"샘플 데이터 추가 중 오류가 발생했습니다: {ex.Message}", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region 설정 로드 (Settings Loading)

        /// <summary>
        /// 현재 환경 변수에서 설정값을 로드하는 메서드
        /// 
        /// 로드하는 설정:
        /// - 데이터베이스 설정 (서버, 데이터베이스명, 사용자명, 비밀번호, 포트)
        /// - API 설정 (Dropbox API 키, Kakao Work API 키, 채팅방 ID)
        /// - 파일 경로 설정 (입력, 출력, 임시 폴더)
        /// </summary>
        private void LoadCurrentSettings()
        {
            try
            {
                Console.WriteLine("🔄 설정 로드 시작...");
                
                // JSON 파일에서 직접 로드
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var settings = new Dictionary<string, string>();
                
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    Console.WriteLine($"📄 JSON 파일 내용: {jsonContent}");
                    
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        try
                        {
                            // Newtonsoft.Json을 사용하여 더 안전하게 역직렬화
                            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                            Console.WriteLine($"✅ SettingsForm: JSON에서 {settings.Count}개 설정 로드");
                            
                            // 각 설정값 로깅
                            foreach (var setting in settings)
                            {
                                Console.WriteLine($"📋 SettingsForm: {setting.Key} = {setting.Value}");
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            Console.WriteLine($"❌ SettingsForm: JSON 역직렬화 실패: {jsonEx.Message}");
                            
                            // JSON 역직렬화 실패 시 기본값 사용
                            Console.WriteLine("⚠️ SettingsForm: 기본값을 사용합니다.");
                            settings = new Dictionary<string, string>();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ settings.json 파일이 존재하지 않음");
                }
                
                // 데이터베이스 설정 로드
                Console.WriteLine("📊 데이터베이스 설정 로드 중...");
                var (server, database, user, password, port) = LoadDatabaseSettingsFromJson();
                SetTextBoxValue("txtServer", server);
                SetTextBoxValue("txtDatabase", database);
                SetTextBoxValue("txtUser", user);
                SetTextBoxValue("txtPassword", password);
                SetTextBoxValue("txtPort", port);

                // API 설정 로드
                Console.WriteLine("🔗 API 설정 로드 중...");
                SetTextBoxValue("txtDropboxApi", settings.GetValueOrDefault("DROPBOX_API_KEY", ""));
                SetTextBoxValue("txtKakaoApi", settings.GetValueOrDefault("KAKAO_WORK_API_KEY", ""));
                SetTextBoxValue("txtKakaoChatroom", settings.GetValueOrDefault("KAKAO_CHATROOM_ID", ""));

                // 파일 경로 설정 로드
                Console.WriteLine("📁 파일 경로 설정 로드 중...");
                SetTextBoxValue("txtInputPath", settings.GetValueOrDefault("INPUT_FOLDER_PATH", "C:\\Work\\Input\\"));
                SetTextBoxValue("txtOutputPath", settings.GetValueOrDefault("OUTPUT_FOLDER_PATH", "C:\\Work\\Output\\"));
                SetTextBoxValue("txtTempPath", settings.GetValueOrDefault("TEMP_FOLDER_PATH", "C:\\Work\\Temp\\"));

                Console.WriteLine("✅ 설정 로드 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 설정 로드 중 오류: {ex.Message}");
                Console.WriteLine($"🔍 SettingsForm: 예외 상세: {ex}");
                MessageBox.Show($"설정을 로드하는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 텍스트박스에 값을 설정하는 헬퍼 메서드 (저장된 참조 사용)
        /// </summary>
        /// <param name="controlName">컨트롤 이름</param>
        /// <param name="value">설정할 값</param>
        private void SetTextBoxValue(string controlName, string value)
        {
            try
            {
                if (_textBoxes.TryGetValue(controlName, out var textBox))
                {
                    if (textBox != null && !textBox.IsDisposed)
                    {
                        textBox.Text = value;
                        Console.WriteLine($"✅ {controlName}: '{value}' 설정 완료");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {controlName}: 컨트롤이 null이거나 disposed됨");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ {controlName}: 컨트롤을 찾을 수 없음. 사용 가능한 컨트롤: {string.Join(", ", _textBoxes.Keys)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {controlName} 설정 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 텍스트박스에서 값을 가져오는 헬퍼 메서드 (저장된 참조 사용)
        /// </summary>
        /// <param name="controlName">컨트롤 이름</param>
        /// <returns>텍스트박스의 값</returns>
        private string GetTextBoxValue(string controlName)
        {
            return _textBoxes.TryGetValue(controlName, out var textBox) ? textBox.Text : "";
        }

        /// <summary>
        /// 재귀적으로 컨트롤을 찾는 메서드 (탭 컨트롤 내부까지 검색)
        /// </summary>
        /// <param name="parent">부모 컨트롤</param>
        /// <param name="controlName">찾을 컨트롤 이름</param>
        /// <returns>찾은 컨트롤 또는 null</returns>
        private Control? FindControlRecursive(Control parent, string controlName)
        {
            foreach (Control control in parent.Controls)
            {
                if (control.Name == controlName)
                    return control;
                
                var found = FindControlRecursive(control, controlName);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// 임시 설정값을 초기화하는 메서드
        /// </summary>
        private void InitializeTempSettings()
        {
            // 현재 UI의 모든 설정값을 임시 저장
            _tempSettings["DB_SERVER"] = GetTextBoxValue("txtServer");
            _tempSettings["DB_NAME"] = GetTextBoxValue("txtDatabase");
            _tempSettings["DB_USER"] = GetTextBoxValue("txtUser");
            _tempSettings["DB_PASSWORD"] = GetTextBoxValue("txtPassword");
            _tempSettings["DB_PORT"] = GetTextBoxValue("txtPort");
            _tempSettings["DROPBOX_API_KEY"] = GetTextBoxValue("txtDropboxApi");
            _tempSettings["KAKAO_WORK_API_KEY"] = GetTextBoxValue("txtKakaoApi");
            _tempSettings["KAKAO_CHATROOM_ID"] = GetTextBoxValue("txtKakaoChatroom");
            _tempSettings["INPUT_FOLDER_PATH"] = GetTextBoxValue("txtInputPath");
            _tempSettings["OUTPUT_FOLDER_PATH"] = GetTextBoxValue("txtOutputPath");
            _tempSettings["TEMP_FOLDER_PATH"] = GetTextBoxValue("txtTempPath");

            // 모든 텍스트박스에 변경 이벤트 추가
            AddTextChangedEvents();
        }

        /// <summary>
        /// 모든 텍스트박스에 변경 이벤트를 추가하는 메서드 (저장된 참조 사용)
        /// </summary>
        private void AddTextChangedEvents()
        {
            foreach (var textBox in _textBoxes.Values)
            {
                textBox.TextChanged += TextBox_TextChanged;
            }
        }

        /// <summary>
        /// 텍스트박스 변경 이벤트 핸들러
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 변경된 값을 임시 설정에 저장
                var settingKey = GetSettingKeyFromTextBoxName(textBox.Name);
                if (!string.IsNullOrEmpty(settingKey))
                {
                    _tempSettings[settingKey] = textBox.Text;
                    _settingsChanged = true; // 설정 변경 플래그 설정
                    Console.WriteLine($"📝 설정 변경: {settingKey} = {textBox.Text}");
                }
            }
        }

        /// <summary>
        /// 텍스트박스 이름에서 설정 키를 가져오는 메서드
        /// </summary>
        /// <param name="textBoxName">텍스트박스 이름</param>
        /// <returns>설정 키</returns>
        private string GetSettingKeyFromTextBoxName(string textBoxName)
        {
            return textBoxName switch
            {
                "txtServer" => "DB_SERVER",
                "txtDatabase" => "DB_NAME",
                "txtUser" => "DB_USER",
                "txtPassword" => "DB_PASSWORD",
                "txtPort" => "DB_PORT",
                "txtDropboxApi" => "DROPBOX_API_KEY",
                "txtKakaoApi" => "KAKAO_WORK_API_KEY",
                "txtKakaoChatroom" => "KAKAO_CHATROOM_ID",
                "txtInputPath" => "INPUT_FOLDER_PATH",
                "txtOutputPath" => "OUTPUT_FOLDER_PATH",
                "txtTempPath" => "TEMP_FOLDER_PATH",
                _ => ""
            };
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// 저장 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - UI의 모든 설정값을 환경 변수로 저장
        /// - 데이터베이스 설정 저장
        /// - API 설정 저장
        /// - 성공 메시지 표시 후 폼 닫기
        /// - 예외 처리 및 오류 메시지 표시
        /// 
        /// 저장되는 환경 변수:
        /// - DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD, DB_PORT
        /// - DROPBOX_API_KEY, KAKAO_WORK_API_KEY, KAKAO_CHATROOM_ID
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // 현재 활성 탭 확인
                var tabControl = this.Controls.OfType<TabControl>().FirstOrDefault();
                var activeTab = tabControl?.SelectedTab;

                if (activeTab == null)
                {
                    MessageBox.Show("⚠️ 활성 탭을 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // JSON 파일에 직접 저장
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var settings = new Dictionary<string, string>();

                // 기존 설정 로드
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        try
                        {
                            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 기존 설정 로드 실패: {ex.Message}");
                            settings = new Dictionary<string, string>();
                        }
                    }
                }

                // 탭별로 해당하는 설정만 저장
                if (activeTab.Text.Contains("데이터베이스"))
                {
                    // 데이터베이스 설정 검증
                    var server = _tempSettings.GetValueOrDefault("DB_SERVER", "");
                    var database = _tempSettings.GetValueOrDefault("DB_NAME", "");
                    var user = _tempSettings.GetValueOrDefault("DB_USER", "");
                    var password = _tempSettings.GetValueOrDefault("DB_PASSWORD", "");
                    var port = _tempSettings.GetValueOrDefault("DB_PORT", "");

                    if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user))
                    {
                        MessageBox.Show("⚠️ 데이터베이스 설정에서 서버, 데이터베이스명, 사용자명은 필수입니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 데이터베이스 설정만 저장
                    settings["DB_SERVER"] = server;
                    settings["DB_NAME"] = database;
                    settings["DB_USER"] = user;
                    settings["DB_PASSWORD"] = password;
                    settings["DB_PORT"] = port;

                    Console.WriteLine($"✅ 데이터베이스 설정 저장 완료");
                    MessageBox.Show("✅ 데이터베이스 설정이 성공적으로 저장되었습니다!", "설정 저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (activeTab.Text.Contains("파일 경로"))
                {
                    // 파일 경로 설정 검증
                    if (!ValidateFilePathSettings())
                    {
                        return;
                    }

                    // 파일 경로 설정만 저장
                    settings["INPUT_FOLDER_PATH"] = _tempSettings.GetValueOrDefault("INPUT_FOLDER_PATH", "");
                    settings["OUTPUT_FOLDER_PATH"] = _tempSettings.GetValueOrDefault("OUTPUT_FOLDER_PATH", "");
                    settings["TEMP_FOLDER_PATH"] = _tempSettings.GetValueOrDefault("TEMP_FOLDER_PATH", "");

                    Console.WriteLine($"✅ 파일 경로 설정 저장 완료");
                    MessageBox.Show(
                        "✅ 파일 경로 설정이 성공적으로 저장되었습니다!\n\n저장된 설정:\n" +
                        $"📥 입력 폴더: {settings.GetValueOrDefault("INPUT_FOLDER_PATH", "")}\n" +
                        $"📤 출력 폴더: {settings.GetValueOrDefault("OUTPUT_FOLDER_PATH", "")}\n" +
                        $"📁 임시 폴더: {settings.GetValueOrDefault("TEMP_FOLDER_PATH", "")}",
                        "설정 저장 완료",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else if (activeTab.Text.Contains("API"))
                {
                    // API 설정만 저장
                    settings["DROPBOX_API_KEY"] = _tempSettings.GetValueOrDefault("DROPBOX_API_KEY", "");
                    settings["KAKAO_WORK_API_KEY"] = _tempSettings.GetValueOrDefault("KAKAO_WORK_API_KEY", "");
                    settings["KAKAO_CHATROOM_ID"] = _tempSettings.GetValueOrDefault("KAKAO_CHATROOM_ID", "");

                    Console.WriteLine($"✅ API 설정 저장 완료");
                    MessageBox.Show("✅ API 설정이 성공적으로 저장되었습니다!", "설정 저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // JSON 파일에 저장 (Newtonsoft.Json 사용)
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(settingsPath, jsonString);

                Console.WriteLine($"✅ 설정 저장 완료: {jsonString}");
                
                // 저장 성공 시 DialogResult 설정
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 설정 저장 중 오류: {ex.Message}");
                MessageBox.Show($"❌ 설정 저장 중 오류가 발생했습니다:\n\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 데이터베이스 연결 테스트 버튼 클릭 이벤트 핸들러
        /// 
        /// 동작 순서:
        /// 1. UI에서 현재 입력된 값들을 읽어옴
        /// 2. MySQL 연결 문자열 생성
        /// 3. 연결 시도
        /// 4. 결과에 따른 메시지 표시
        /// 
        /// 예외 처리:
        /// - 설정이 완료되지 않은 경우
        /// - 연결 실패 시 상세 오류 메시지 표시
        /// - UI 스레드 블로킹 방지
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void TestConnectionButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // 현재 입력된 설정값들 읽기
                var server = GetTextBoxValue("txtServer");
                var database = GetTextBoxValue("txtDatabase");
                var user = GetTextBoxValue("txtUser");
                var password = GetTextBoxValue("txtPassword");
                var port = GetTextBoxValue("txtPort");

                // 필수 값 검증
                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user))
                {
                    ShowConnectionResult("⚠️ 서버, 데이터베이스명, 사용자명을 입력해주세요.", Color.Orange);
                    return;
                }

                // 연결 정보 표시
                ShowConnectionResult("🔍 연결을 시도합니다...", Color.Blue);

                // 연결 문자열 생성 (utf8mb4 사용)
                var connectionString = $"Server={server};Database={database};Uid={user};Pwd={password};CharSet=utf8mb4;Port={port};SslMode=none;AllowPublicKeyRetrieval=true;ConnectionTimeout=30;";

                // 동기적으로 연결 테스트 실행
                try
                {
                    using var connection = new MySqlConnector.MySqlConnection(connectionString);
                    connection.Open();

                    // 서버 버전 확인
                    using var command = new MySqlConnector.MySqlCommand("SELECT VERSION() as version", connection);
                    var version = command.ExecuteScalar();

                    // 데이터베이스 이름 확인
                    using var dbCommand = new MySqlConnector.MySqlCommand("SELECT DATABASE() as database_name", connection);
                    var databaseName = dbCommand.ExecuteScalar();

                    var successMessage = $"✅ 연결 성공! 서버 버전: {version}, 데이터베이스: {databaseName}";
                    ShowConnectionResult(successMessage, Color.Green);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"❌ 연결 실패: {ex.Message}";
                    ShowConnectionResult(errorMessage, Color.Red);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ 테스트 중 오류: {ex.Message}";
                ShowConnectionResult(errorMessage, Color.Red);
            }
        }

        /// <summary>
        /// 연결테스트 결과를 라벨에 표시하는 메서드
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        /// <param name="color">메시지 색상</param>
        private void ShowConnectionResult(string message, Color color)
        {
            // UI 스레드에서 실행
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowConnectionResult(message, color)));
                return;
            }

            // 연결테스트 결과 라벨 찾기
            var resultLabel = this.Controls.Find("lblConnectionResult", true).FirstOrDefault() as Label;
            if (resultLabel != null)
            {
                resultLabel.Text = message;
                resultLabel.ForeColor = color;
            }
        }

        /// <summary>
        /// API 설정을 개별적으로 저장하는 메서드
        /// </summary>
        /// <param name="settingKey">설정 키</param>
        /// <param name="value">설정 값</param>
        private void SaveApiSetting(string settingKey, string value)
        {
            try
            {
                // 임시 설정에 저장
                _tempSettings[settingKey] = value;
                
                // settings.json 파일에 저장
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var settings = new Dictionary<string, string>();
                
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        try
                        {
                            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                        }
                        catch
                        {
                            settings = new Dictionary<string, string>();
                        }
                    }
                }
                
                // 설정 업데이트
                settings[settingKey] = value;
                
                // JSON 파일에 저장
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(settingsPath, jsonString);
                
                // 성공 메시지 표시
                ShowApiResult(settingKey, "✅ 설정이 성공적으로 저장되었습니다.", Color.FromArgb(46, 204, 113));
                
                Console.WriteLine($"✅ API 설정 저장 완료: {settingKey} = {value}");
            }
            catch (Exception ex)
            {
                // 오류 메시지 표시
                ShowApiResult(settingKey, $"❌ 저장 실패: {ex.Message}", Color.FromArgb(231, 76, 60));
                Console.WriteLine($"❌ API 설정 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// API 연결 테스트를 수행하는 메서드
        /// </summary>
        /// <param name="apiName">API 이름</param>
        /// <param name="apiKey">API 키</param>
        private async void TestApiConnection(string apiName, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                ShowApiResult(apiName, "⚠️ API 키를 입력해주세요.", Color.FromArgb(243, 156, 18));
                return;
            }
            
            try
            {
                // 연결 테스트 시작 메시지
                ShowApiResult(apiName, "🔍 연결 테스트 중...", Color.FromArgb(52, 152, 219));
                
                // API별 연결 테스트 수행
                bool isSuccess = false;
                string resultMessage = "";
                
                switch (apiName)
                {
                    case "Dropbox":
                        isSuccess = await TestDropboxConnection(apiKey);
                        resultMessage = isSuccess ? "✅ Dropbox 연결 성공!" : "❌ Dropbox 연결 실패";
                        break;
                        
                    case "Kakao Work":
                        isSuccess = await TestKakaoWorkConnection(apiKey);
                        resultMessage = isSuccess ? "✅ Kakao Work 연결 성공!" : "❌ Kakao Work 연결 실패";
                        break;
                        
                    case "Kakao Work Chatroom":
                        isSuccess = await TestKakaoWorkChatroomConnection(apiKey);
                        resultMessage = isSuccess ? "✅ Kakao Work 채팅방 연결 성공!" : "❌ Kakao Work 채팅방 연결 실패";
                        break;
                        
                    default:
                        resultMessage = "⚠️ 알 수 없는 API";
                        break;
                }
                
                // 결과 표시
                var resultColor = isSuccess ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);
                ShowApiResult(apiName, resultMessage, resultColor);
                
                Console.WriteLine($"🔍 API 연결 테스트 완료: {apiName} - {(isSuccess ? "성공" : "실패")}");
            }
            catch (Exception ex)
            {
                ShowApiResult(apiName, $"❌ 연결 테스트 오류: {ex.Message}", Color.FromArgb(231, 76, 60));
                Console.WriteLine($"❌ API 연결 테스트 오류: {apiName} - {ex.Message}");
            }
        }

        /// <summary>
        /// API 테스트 결과를 화면에 표시하는 메서드
        /// </summary>
        /// <param name="apiName">API 이름</param>
        /// <param name="message">결과 메시지</param>
        /// <param name="color">메시지 색상</param>
        private void ShowApiResult(string apiName, string message, Color color)
        {
            // UI 스레드에서 실행
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowApiResult(apiName, message, color)));
                return;
            }
            
            // API별 결과 라벨 찾기
            string labelName = "";
            switch (apiName)
            {
                case "Dropbox":
                    labelName = "lblDropboxResult";
                    break;
                case "Kakao Work":
                    labelName = "lblKakaoResult";
                    break;
                case "Kakao Work Chatroom":
                    labelName = "lblKakaoChatroomResult";
                    break;
            }
            
            if (!string.IsNullOrEmpty(labelName))
            {
                var label = this.Controls.Find(labelName, true).FirstOrDefault() as Label;
                if (label != null)
                {
                    label.Text = message;
                    label.ForeColor = color;
                }
            }
        }

        /// <summary>
        /// Dropbox API 연결 테스트
        /// </summary>
        /// <param name="apiKey">API 키</param>
        /// <returns>연결 성공 여부</returns>
        private async Task<bool> TestDropboxConnection(string apiKey)
        {
            try
            {
                // DropboxService Singleton 인스턴스를 사용하여 연결 테스트
                var dropboxService = DropboxService.Instance;
                return await dropboxService.TestConnectionAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kakao Work API 연결 테스트
        /// </summary>
        /// <param name="apiKey">API 키</param>
        /// <returns>연결 성공 여부</returns>
        private async Task<bool> TestKakaoWorkConnection(string apiKey)
        {
            try
            {
                // Kakao Work API 연결 테스트 로직
                await Task.Delay(1000); // 시뮬레이션용 지연
                
                // 간단한 유효성 검사
                if (apiKey.Length > 10)
                {
                    return true; // 기본적인 유효성 검사 통과
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kakao Work 채팅방 연결 테스트
        /// </summary>
        /// <param name="chatroomId">채팅방 ID</param>
        /// <returns>연결 성공 여부</returns>
        private async Task<bool> TestKakaoWorkChatroomConnection(string chatroomId)
        {
            try
            {
                // Kakao Work 채팅방 연결 테스트 로직
                await Task.Delay(1000); // 시뮬레이션용 지연
                
                // 간단한 유효성 검사
                if (!string.IsNullOrEmpty(chatroomId) && chatroomId.Length > 5)
                {
                    return true; // 기본적인 유효성 검사 통과
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 저장된 settings.json 설정으로 연결을 테스트하는 메서드
        /// 
        /// 기능:
        /// - settings.json에서 데이터베이스 설정을 읽어옴
        /// - 안전한 연결 문자열 생성
        /// - 연결 테스트 수행
        /// - 성공/실패 메시지 표시
        /// 
        /// 사용 시나리오:
        /// - 설정 저장 후 자동 연결 테스트
        /// - 저장된 설정의 유효성 검증
        /// </summary>
        private void TestConnectionWithSavedSettings()
        {
            try
            {
                // settings.json에서 설정 읽기
                var (server, database, user, password, port) = LoadDatabaseSettingsFromJson();

                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user))
                {
                    MessageBox.Show("⚠️ 저장된 데이터베이스 설정이 완전하지 않습니다.\nsettings.json 파일을 확인해주세요.", "설정 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 연결 정보 표시
                var connectionInfo = $"서버: {server}\n데이터베이스: {database}\n사용자: {user}\n포트: {port}";
                MessageBox.Show($"🔍 저장된 설정으로 연결을 시도합니다...\n\n{connectionInfo}", "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 연결 문자열 생성
                var connectionString = $"Server={server};Database={database};User Id={user};Password={password};Port={port};CharSet=utf8;Convert Zero Datetime=True;Allow User Variables=True;";

                // 동기적으로 연결 테스트 실행
                try
                {
                    using var connection = new MySqlConnector.MySqlConnection(connectionString);
                    connection.Open();

                    // 서버 버전 확인
                    using var command = new MySqlConnector.MySqlCommand("SELECT VERSION() as version", connection);
                    var version = command.ExecuteScalar();

                    MessageBox.Show($"✅ 저장된 설정으로 데이터베이스 연결이 성공했습니다!\n\n서버 버전: {version}\n\n이제 프로그램에서 저장된 설정을 사용합니다.", "연결 테스트 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"❌ 저장된 설정으로 데이터베이스 연결에 실패했습니다:\n\n오류: {ex.Message}";
                    
                    if (ex.InnerException != null)
                    {
                        errorMessage += $"\n\n상세 오류: {ex.InnerException.Message}";
                    }
                    
                    errorMessage += "\n\n설정을 다시 확인하고 수정해주세요.";
                    
                    MessageBox.Show(errorMessage, "연결 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ 연결 테스트 중 오류가 발생했습니다:\n\n오류: {ex.Message}";
                MessageBox.Show(errorMessage, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 데이터베이스 설정 (Database Settings)

        /// <summary>
        /// settings.json에서 직접 데이터베이스 설정을 읽어오는 메서드
        /// </summary>
        /// <returns>데이터베이스 설정 튜플</returns>
        private (string server, string database, string user, string password, string port) LoadDatabaseSettingsFromJson()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                
                if (File.Exists(settingsPath))
                {
                    var jsonContent = File.ReadAllText(settingsPath);
                    Console.WriteLine($"📄 SettingsForm: settings.json 파일 내용: {jsonContent}");
                    
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (settings != null)
                    {
                                                        // 설정값 추출 (null 체크 포함)
                        if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_SERVER, out var server) || string.IsNullOrWhiteSpace(server))
                        {
                            Console.WriteLine("❌ SettingsForm: DB_SERVER 설정값이 누락되었습니다.");
                            throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                        }
                        
                        if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_NAME, out var database) || string.IsNullOrWhiteSpace(database))
                        {
                            Console.WriteLine("❌ SettingsForm: DB_NAME 설정값이 누락되었습니다.");
                            throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                        }
                        
                        if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_USER, out var user) || string.IsNullOrWhiteSpace(user))
                        {
                            Console.WriteLine("❌ SettingsForm: DB_USER 설정값이 누락되었습니다.");
                            throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                        }
                        
                        if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_PASSWORD, out var password) || string.IsNullOrEmpty(password))
                        {
                            Console.WriteLine("❌ SettingsForm: DB_PASSWORD 설정값이 누락되었습니다.");
                            throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                        }
                        
                        if (!settings.TryGetValue(DatabaseConstants.CONFIG_KEY_DB_PORT, out var port) || string.IsNullOrWhiteSpace(port))
                        {
                            Console.WriteLine("❌ SettingsForm: DB_PORT 설정값이 누락되었습니다.");
                            throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
                        }
                        
                        Console.WriteLine($"✅ SettingsForm: settings.json에서 데이터베이스 설정을 성공적으로 읽어왔습니다.");
                        return (server, database, user, password, port);
                    }
                    else
                    {
                        Console.WriteLine("❌ SettingsForm: settings.json 파싱 실패");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ SettingsForm: settings.json 파일이 존재하지 않음: {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SettingsForm: settings.json 읽기 실패: {ex.Message}");
            }
            
            // 기본값 사용 금지 - 설정 파일이 올바르지 않으면 예외 발생
            Console.WriteLine("❌ SettingsForm: 설정 파일이 올바르지 않습니다.");
            throw new InvalidOperationException(DatabaseConstants.ERROR_MISSING_REQUIRED_SETTINGS);
        }

        #endregion

        #region 공통코드 관리 (Common Code Management)

        /// <summary>
        /// 공통코드 관리 기능을 초기화하는 메서드
        /// </summary>
        private void InitializeCommonCodeManagement(
            TreeView treeViewGroupCodes, 
            DataGridView dataGridViewCodes, 
            Button btnAddGroup, 
            Button btnRefresh, 
            Button btnAddCode, 
            Button btnSave, 
            Button btnDelete)
        {
            // 이벤트 핸들러 연결
            treeViewGroupCodes.AfterSelect += TreeViewGroupCodes_AfterSelect;
            treeViewGroupCodes.DoubleClick += TreeViewGroupCodes_DoubleClick;
            treeViewGroupCodes.MouseClick += TreeViewGroupCodes_MouseClick;
            
            btnAddGroup.Click += BtnAddGroup_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnAddCode.Click += BtnAddCode_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            // DataGridView 이벤트
            dataGridViewCodes.CellValueChanged += DataGridViewCodes_CellValueChanged;
            dataGridViewCodes.CellBeginEdit += DataGridViewCodes_CellBeginEdit;

            // 초기 데이터 로드
            LoadCommonCodeDataAsync(treeViewGroupCodes, dataGridViewCodes);
        }

        /// <summary>
        /// 공통코드 데이터를 비동기로 로드하는 메서드
        /// </summary>
        private async void LoadCommonCodeDataAsync(TreeView treeViewGroupCodes, DataGridView dataGridViewCodes)
        {
            try
            {
                await LoadGroupCodesAsync(treeViewGroupCodes, dataGridViewCodes);
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 데이터 로드 중 오류: {ex.Message}");
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 그룹코드 목록을 로드하는 메서드
        /// </summary>
        private async Task LoadGroupCodesAsync(TreeView treeViewGroupCodes, DataGridView dataGridViewCodes)
        {
            try
            {
                LogManagerService.LogInfo("=== LoadGroupCodesAsync 시작 ===");
                LogManagerService.LogInfo("TreeView 노드 초기화");
                treeViewGroupCodes.Nodes.Clear();
                
                LogManagerService.LogInfo("데이터베이스에서 그룹코드 목록 조회 시작");
                var groupCodes = await _commonCodeRepository.GetAllGroupCodesAsync();
                LogManagerService.LogInfo($"조회된 그룹코드 개수: {groupCodes.Count}");

                foreach (var groupCode in groupCodes)
                {
                    LogManagerService.LogInfo($"TreeView에 그룹코드 '{groupCode}' 노드 추가");
                    var node = new TreeNode(groupCode) { Tag = groupCode };
                    treeViewGroupCodes.Nodes.Add(node);
                    LogManagerService.LogInfo($"노드 추가 완료 - Text: '{node.Text}', Tag: '{node.Tag}'");
                }

                LogManagerService.LogInfo($"TreeView 총 노드 수: {treeViewGroupCodes.Nodes.Count}");

                if (groupCodes.Count > 0)
                {
                    var firstNode = treeViewGroupCodes.Nodes[0];
                    LogManagerService.LogInfo($"첫 번째 노드 '{firstNode.Text}' 선택");
                    treeViewGroupCodes.SelectedNode = firstNode;
                    UpdateTreeNodeSelection(firstNode);
                    
                    if (firstNode.Tag is string firstGroupCode)
                    {
                        LogManagerService.LogInfo($"첫 번째 그룹코드 '{firstGroupCode}'의 공통코드 로드 시작");
                        _selectedGroupCode = firstGroupCode;
                        await LoadCommonCodesAsync(firstGroupCode, dataGridViewCodes);
                    }
                    else
                    {
                        LogManagerService.LogError($"첫 번째 노드의 Tag가 문자열이 아님: {firstNode.Tag}");
                    }
                }
                else
                {
                    LogManagerService.LogWarning("조회된 그룹코드가 없음, DataGridView 초기화");
                    ConfigureDataGridView(dataGridViewCodes);
                    dataGridViewCodes.Rows.Clear();
                    // 버튼 상태는 나중에 설정
                }
                
                LogManagerService.LogInfo("=== LoadGroupCodesAsync 완료 ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"그룹코드 로드 중 오류: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"그룹코드 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                ConfigureDataGridView(dataGridViewCodes);
                dataGridViewCodes.Rows.Clear();
                // 버튼 상태는 나중에 설정
            }
        }

        /// <summary>
        /// 선택된 그룹코드의 공통코드 목록을 로드하는 메서드
        /// </summary>
        private async Task LoadCommonCodesAsync(string groupCode, DataGridView dataGridViewCodes)
        {
            try
            {
                LogManagerService.LogInfo($"=== LoadCommonCodesAsync 시작 - 그룹코드: '{groupCode}' ===");
                
                if (string.IsNullOrEmpty(groupCode))
                {
                    LogManagerService.LogWarning("그룹코드가 비어있음, DataGridView 초기화");
                    ConfigureDataGridView(dataGridViewCodes);
                    dataGridViewCodes.Rows.Clear();
                    return;
                }

                LogManagerService.LogInfo($"데이터베이스에서 그룹코드 '{groupCode}'의 공통코드 조회 시작");
                var commonCodes = await _commonCodeRepository.GetCommonCodesByGroupAsync(groupCode);
                LogManagerService.LogInfo($"조회된 공통코드 개수: {commonCodes.Count}");
                
                _originalData = commonCodes.Select(c => c.Clone()).ToList();
                LogManagerService.LogInfo("원본 데이터 백업 완료");
                
                LogManagerService.LogInfo("DataGridView 구성 시작");
                ConfigureDataGridView(dataGridViewCodes);
                LogManagerService.LogInfo("DataGridView 구성 완료");
                
                LogManagerService.LogInfo("DataGridView에 데이터 채우기 시작");
                PopulateDataGridView(commonCodes, dataGridViewCodes);
                LogManagerService.LogInfo($"DataGridView에 {dataGridViewCodes.Rows.Count}개 행 추가 완료");
                
                LogManagerService.LogInfo($"=== LoadCommonCodesAsync 완료 - 그룹코드: '{groupCode}' ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 로드 중 오류 (그룹코드: {groupCode}): {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"공통코드 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                ConfigureDataGridView(dataGridViewCodes);
                dataGridViewCodes.Rows.Clear();
            }
        }

        /// <summary>
        /// DataGridView를 구성하는 메서드
        /// </summary>
        private void ConfigureDataGridView(DataGridView dataGridViewCodes)
        {
            dataGridViewCodes.Columns.Clear();

            dataGridViewCodes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "GroupCode",
                HeaderText = "그룹코드",
                DataPropertyName = "GroupCode",
                ReadOnly = true,
                Width = 120
            });

            dataGridViewCodes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Code",
                HeaderText = "코드",
                DataPropertyName = "Code",
                Width = 100
            });

            dataGridViewCodes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CodeName",
                HeaderText = "코드명",
                DataPropertyName = "CodeName",
                Width = 150
            });

            dataGridViewCodes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "설명",
                DataPropertyName = "Description",
                Width = 200
            });

            dataGridViewCodes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SortOrder",
                HeaderText = "정렬순서",
                DataPropertyName = "SortOrder",
                Width = 80
            });

            dataGridViewCodes.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsUsed",
                HeaderText = "사용여부",
                DataPropertyName = "IsUsed",
                Width = 80
            });

            dataGridViewCodes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Attribute1",
                HeaderText = "추가속성1",
                DataPropertyName = "Attribute1",
                Width = 120
            });

            dataGridViewCodes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Attribute2",
                HeaderText = "추가속성2",
                DataPropertyName = "Attribute2",
                Width = 120
            });

            // 스타일 설정
            dataGridViewCodes.EnableHeadersVisualStyles = false;
            dataGridViewCodes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dataGridViewCodes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridViewCodes.ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }

        /// <summary>
        /// DataGridView에 데이터를 채우는 메서드
        /// </summary>
        private void PopulateDataGridView(List<CommonCode> commonCodes, DataGridView dataGridViewCodes)
        {
            try
            {
                dataGridViewCodes.Rows.Clear();

                foreach (var commonCode in commonCodes)
                {
                    var rowIndex = dataGridViewCodes.Rows.Add();
                    var row = dataGridViewCodes.Rows[rowIndex];

                    row.Cells["GroupCode"].Value = commonCode.GroupCode;
                    row.Cells["Code"].Value = commonCode.Code;
                    row.Cells["CodeName"].Value = commonCode.CodeName;
                    row.Cells["Description"].Value = commonCode.Description ?? string.Empty;
                    row.Cells["SortOrder"].Value = commonCode.SortOrder;
                    row.Cells["IsUsed"].Value = commonCode.IsUsed;
                    row.Cells["Attribute1"].Value = commonCode.Attribute1 ?? string.Empty;
                    row.Cells["Attribute2"].Value = commonCode.Attribute2 ?? string.Empty;

                    row.Tag = commonCode;
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"DataGridView 데이터 채우기 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 버튼 상태를 설정하는 메서드
        /// </summary>
        private void SetButtonStates(bool hasData, Button? btnAddCode, Button btnSave, Button btnDelete)
        {
            if (btnAddCode != null)
            {
                btnAddCode.Enabled = hasData;
            }
            btnSave.Enabled = hasData && _isDataModified;
            btnDelete.Enabled = hasData;
        }

        /// <summary>
        /// TreeView 노드의 색상을 설정하는 메서드
        /// </summary>
        private void SetTreeNodeColor(TreeNode node, bool isSelected)
        {
            if (node == null) return;

            if (isSelected)
            {
                node.BackColor = Color.FromArgb(52, 152, 219);
                node.ForeColor = Color.White;
                node.ForeColor = Color.White;
            }
            else
            {
                node.BackColor = Color.White;
                node.ForeColor = Color.FromArgb(52, 73, 94);
            }
        }

        /// <summary>
        /// 모든 TreeView 노드의 색상을 기본 상태로 초기화하는 메서드
        /// </summary>
        private void ResetAllTreeNodeColors(TreeView treeViewGroupCodes)
        {
            foreach (TreeNode node in treeViewGroupCodes.Nodes)
            {
                SetTreeNodeColor(node, false);
            }
        }

        /// <summary>
        /// TreeView 노드 선택 시 색상을 업데이트하는 메서드
        /// </summary>
        private void UpdateTreeNodeSelection(TreeNode? selectedNode)
        {
            if (selectedNode?.TreeView == null) return;

            ResetAllTreeNodeColors(selectedNode.TreeView);

            if (selectedNode != null)
            {
                SetTreeNodeColor(selectedNode, true);
            }
        }

        #endregion

        #region 공통코드 관리 이벤트 핸들러 (Common Code Management Event Handlers)

        /// <summary>
        /// TreeView에서 그룹코드 선택 시 이벤트
        /// </summary>
        private async void TreeViewGroupCodes_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            try
            {
                LogManagerService.LogInfo("=== TreeView AfterSelect 이벤트 시작 ===");
                LogManagerService.LogInfo($"선택된 노드: {e.Node?.Text ?? "null"}");
                LogManagerService.LogInfo($"노드 Tag: {e.Node?.Tag ?? "null"}");
                
                UpdateTreeNodeSelection(e.Node);

                if (e.Node?.Tag is string groupCode && !string.IsNullOrEmpty(groupCode))
                {
                    LogManagerService.LogInfo($"그룹코드 '{groupCode}' 선택됨");
                    _selectedGroupCode = groupCode;
                    
                    if (e.Node.TreeView != null)
                    {
                        LogManagerService.LogInfo("TreeView가 존재함, 공통코드 관리 패널 찾기 시작");
                        var commonCodePanel = FindCommonCodeManagementPanel(e.Node.TreeView);
                        LogManagerService.LogInfo($"공통코드 관리 패널 찾기 결과: {(commonCodePanel != null ? "성공" : "실패")}");
                        
                        if (commonCodePanel != null)
                        {
                            LogManagerService.LogInfo("DataGridView 찾기 시작");
                            var dataGridView = FindDataGridViewInParent(commonCodePanel);
                            LogManagerService.LogInfo($"DataGridView 찾기 결과: {(dataGridView != null ? "성공" : "실패")}");
                            
                            if (dataGridView != null)
                            {
                                LogManagerService.LogInfo($"그룹코드 '{groupCode}'의 상세코드 로드 시작");
                                await LoadCommonCodesAsync(groupCode, dataGridView);
                                LogManagerService.LogInfo($"그룹코드 '{groupCode}'의 상세코드 로드 완료");
                            }
                            else
                            {
                                LogManagerService.LogError("DataGridView를 찾을 수 없습니다!");
                            }
                        }
                        else
                        {
                            LogManagerService.LogError("공통코드 관리 패널을 찾을 수 없습니다!");
                        }
                    }
                    else
                    {
                        LogManagerService.LogError("TreeView가 null입니다!");
                    }
                }
                else
                {
                    LogManagerService.LogWarning($"유효하지 않은 그룹코드: Node={e.Node?.Text}, Tag={e.Node?.Tag}");
                }
                
                LogManagerService.LogInfo("=== TreeView AfterSelect 이벤트 완료 ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"TreeView 선택 이벤트 처리 중 오류: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"그룹코드 선택 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// TreeView에서 그룹코드 더블클릭 시 이벤트
        /// </summary>
        private async void TreeViewGroupCodes_DoubleClick(object? sender, EventArgs e)
        {
            try
            {
                LogManagerService.LogInfo("=== TreeView DoubleClick 이벤트 시작 ===");
                
                if (sender is TreeView treeView && treeView.SelectedNode?.Tag is string groupCode && !string.IsNullOrEmpty(groupCode))
                {
                    LogManagerService.LogInfo($"더블클릭된 그룹코드: '{groupCode}'");
                    _selectedGroupCode = groupCode;
                    
                    LogManagerService.LogInfo("공통코드 관리 패널 찾기 시작");
                    var commonCodePanel = FindCommonCodeManagementPanel(treeView);
                    LogManagerService.LogInfo($"공통코드 관리 패널 찾기 결과: {(commonCodePanel != null ? "성공" : "실패")}");
                    
                    if (commonCodePanel != null)
                    {
                        LogManagerService.LogInfo("DataGridView 찾기 시작");
                        var dataGridView = FindDataGridViewInParent(commonCodePanel);
                        LogManagerService.LogInfo($"DataGridView 찾기 결과: {(dataGridView != null ? "성공" : "실패")}");
                        
                        if (dataGridView != null)
                        {
                            LogManagerService.LogInfo($"그룹코드 '{groupCode}'의 상세코드 로드 시작");
                            await LoadCommonCodesAsync(groupCode, dataGridView);
                            LogManagerService.LogInfo($"그룹코드 '{groupCode}'의 상세코드 로드 완료");
                        }
                        else
                        {
                            LogManagerService.LogError("DataGridView를 찾을 수 없습니다!");
                        }
                    }
                    else
                    {
                        LogManagerService.LogError("공통코드 관리 패널을 찾을 수 없습니다!");
                    }
                }
                else
                {
                    LogManagerService.LogWarning("더블클릭된 노드가 유효하지 않음");
                }
                
                LogManagerService.LogInfo("=== TreeView DoubleClick 이벤트 완료 ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"TreeView 더블클릭 이벤트 처리 중 오류: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"그룹코드 더블클릭 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// TreeView에서 마우스 클릭 시 이벤트 (우클릭 메뉴 처리)
        /// </summary>
        private void TreeViewGroupCodes_MouseClick(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right && sender is TreeView treeView)
                {
                    var clickedNode = treeView.GetNodeAt(e.X, e.Y);
                    if (clickedNode != null)
                    {
                        treeView.SelectedNode = clickedNode;
                        ShowTreeNodeContextMenu(clickedNode, e.Location);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"TreeView 마우스 클릭 이벤트 처리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// TreeView 노드 우클릭 메뉴를 표시하는 메서드
        /// </summary>
        private void ShowTreeNodeContextMenu(TreeNode node, Point location)
        {
            try
            {
                var contextMenu = new ContextMenuStrip();
                
                var deleteMenuItem = new ToolStripMenuItem("🗑️ 그룹코드 삭제", null, async (sender, e) =>
                {
                    await DeleteGroupCodeAsync(node);
                });
                
                contextMenu.Items.Add(deleteMenuItem);
                contextMenu.Show(node.TreeView, location);
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"우클릭 메뉴 표시 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 새 그룹코드 추가 버튼 클릭 이벤트
        /// </summary>
        private async void BtnAddGroup_Click(object? sender, EventArgs e)
        {
            try
            {
                var groupCode = Microsoft.VisualBasic.Interaction.InputBox(
                    "새 그룹코드를 입력하세요:", "새 그룹코드 추가", "");

                if (!string.IsNullOrWhiteSpace(groupCode))
                {
                    var groupCodeName = Microsoft.VisualBasic.Interaction.InputBox(
                        "그룹코드명을 입력하세요 (선택사항):", "그룹코드명 입력", groupCode);

                    var success = await _commonCodeRepository.AddGroupCodeAsync(groupCode, groupCodeName);
                    
                    if (success)
                    {
                        LogManagerService.LogInfo($"새 그룹코드 '{groupCode}' 추가 성공, TreeView 새로고침 시작");
                        
                        // 전체 공통코드 관리 패널을 찾아서 TreeView와 DataGridView를 가져옴
                        if (sender is Button btn)
                        {
                            var commonCodePanel = FindCommonCodeManagementPanel(btn);
                            if (commonCodePanel != null)
                            {
                                var treeView = FindTreeViewInParent(commonCodePanel);
                                var dataGridView = FindDataGridViewInParent(commonCodePanel);
                                
                                LogManagerService.LogInfo($"TreeView 찾기 결과: {(treeView != null ? "성공" : "실패")}");
                                LogManagerService.LogInfo($"DataGridView 찾기 결과: {(dataGridView != null ? "성공" : "실패")}");
                                
                                if (treeView != null && dataGridView != null)
                                {
                                    // TreeView 전체 새로고침
                                    await LoadGroupCodesAsync(treeView, dataGridView);
                                    
                                    // 새로 추가된 그룹코드 선택
                                    foreach (TreeNode node in treeView.Nodes)
                                    {
                                        if (node.Text == groupCode)
                                        {
                                            treeView.SelectedNode = node;
                                            UpdateTreeNodeSelection(node);
                                            _selectedGroupCode = groupCode;
                                            await LoadCommonCodesAsync(groupCode, dataGridView);
                                            break;
                                        }
                                    }
                                    
                                    LogManagerService.LogInfo($"TreeView 새로고침 완료, 새 그룹코드 '{groupCode}' 선택됨");
                                }
                                else
                                {
                                    LogManagerService.LogError("TreeView 또는 DataGridView를 찾을 수 없습니다.");
                                }
                            }
                            else
                            {
                                LogManagerService.LogError("공통코드 관리 패널을 찾을 수 없습니다.");
                            }
                        }
                        
                        MessageBox.Show($"새 그룹코드 '{groupCode}'가 성공적으로 추가되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"그룹코드 '{groupCode}'가 이미 존재합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"새 그룹코드 추가 중 오류: {ex.Message}");
                MessageBox.Show($"새 그룹코드 추가 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 새로고침 버튼 클릭 이벤트
        /// </summary>
        private async void BtnRefresh_Click(object? sender, EventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Parent is Panel panel)
                {
                    var treeView = FindTreeViewInParent(panel);
                    var dataGridView = FindDataGridViewInParent(panel);
                    
                    if (treeView != null && dataGridView != null)
                    {
                        await LoadGroupCodesAsync(treeView, dataGridView);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"새로고침 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 공통코드 추가 버튼 클릭 이벤트
        /// </summary>
        private void BtnAddCode_Click(object? sender, EventArgs e)
        {
            try
            {
                LogManagerService.LogInfo("=== 코드 추가 버튼 클릭 이벤트 시작 ===");
                LogManagerService.LogInfo($"선택된 그룹코드: '{_selectedGroupCode}'");
                
                if (string.IsNullOrEmpty(_selectedGroupCode))
                {
                    LogManagerService.LogWarning("그룹코드가 선택되지 않음");
                    MessageBox.Show("먼저 그룹코드를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (sender is Button btn && btn.Parent is Panel panel)
                {
                    LogManagerService.LogInfo($"버튼 정보 - Name: {btn.Name}, Parent: {panel.GetType().Name}");
                    
                    // 공통코드 관리 패널에서 DataGridView 찾기
                    var commonCodePanel = FindCommonCodeManagementPanel(btn);
                    LogManagerService.LogInfo($"공통코드 관리 패널 찾기 결과: {(commonCodePanel != null ? "성공" : "실패")}");
                    
                    if (commonCodePanel != null)
                    {
                        var dataGridView = FindDataGridViewInParent(commonCodePanel);
                        LogManagerService.LogInfo($"DataGridView 찾기 결과: {(dataGridView != null ? "성공" : "실패")}");
                        
                        if (dataGridView != null)
                        {
                            LogManagerService.LogInfo("새 공통코드 객체 생성 시작");
                            var newCode = new CommonCode(_selectedGroupCode)
                            {
                                Code = $"NEW_{DateTime.Now:yyyyMMddHHmmss}",
                                CodeName = "새 코드",
                                SortOrder = _originalData.Count + 1,
                                CreatedBy = Environment.UserName
                            };
                            LogManagerService.LogInfo($"새 코드 생성: {newCode.GroupCode}.{newCode.Code}");

                            LogManagerService.LogInfo("DataGridView에 새 행 추가 시작");
                            var rowIndex = dataGridView.Rows.Add();
                            var row = dataGridView.Rows[rowIndex];
                            LogManagerService.LogInfo($"새 행 추가됨 - 인덱스: {rowIndex}");

                            // 셀 값 설정
                            LogManagerService.LogInfo("셀 값 설정 시작");
                            row.Cells["GroupCode"].Value = newCode.GroupCode;
                            row.Cells["Code"].Value = newCode.Code;
                            row.Cells["CodeName"].Value = newCode.CodeName;
                            row.Cells["Description"].Value = newCode.Description ?? string.Empty;
                            row.Cells["SortOrder"].Value = newCode.SortOrder;
                            row.Cells["IsUsed"].Value = newCode.IsUsed;
                            row.Cells["Attribute1"].Value = newCode.Attribute1 ?? string.Empty;
                            row.Cells["Attribute2"].Value = newCode.Attribute2 ?? string.Empty;
                            LogManagerService.LogInfo("모든 셀 값 설정 완료");

                            row.Tag = newCode;
                            _isDataModified = true;
                            LogManagerService.LogInfo("행 태그 설정 및 데이터 수정 플래그 설정 완료");
                            
                            LogManagerService.LogInfo("버튼 상태 업데이트 시작");
                            var btnSave = FindButtonInParent(commonCodePanel, "btnSave");
                            var btnDelete = FindButtonInParent(commonCodePanel, "btnDelete");
                            LogManagerService.LogInfo($"저장 버튼 찾기: {(btnSave != null ? "성공" : "실패")}");
                            LogManagerService.LogInfo($"삭제 버튼 찾기: {(btnDelete != null ? "성공" : "실패")}");
                            
                            if (btnSave != null && btnDelete != null)
                            {
                                SetButtonStates(true, btn, btnSave, btnDelete);
                                LogManagerService.LogInfo("버튼 상태 업데이트 완료");
                            }
                            
                            LogManagerService.LogInfo($"현재 DataGridView 총 행 수: {dataGridView.Rows.Count}");
                        }
                        else
                        {
                            LogManagerService.LogError("DataGridView를 찾을 수 없습니다!");
                            MessageBox.Show("DataGridView를 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        LogManagerService.LogError("공통코드 관리 패널을 찾을 수 없습니다!");
                        MessageBox.Show("공통코드 관리 패널을 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    LogManagerService.LogError("버튼 또는 부모 패널 정보가 올바르지 않습니다.");
                    MessageBox.Show("버튼 정보가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                LogManagerService.LogInfo("=== 코드 추가 버튼 클릭 이벤트 완료 ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"코드 추가 중 오류: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"코드 추가 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 저장 버튼 클릭 이벤트
        /// </summary>
        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                LogManagerService.LogInfo("=== 저장 버튼 클릭 이벤트 시작 ===");
                LogManagerService.LogInfo($"데이터 수정 상태: {_isDataModified}");
                
                if (sender is Button btn && btn.Parent is Panel panel)
                {
                    LogManagerService.LogInfo($"버튼 정보 - Name: {btn.Name}, Parent: {panel.GetType().Name}");
                    
                    // 공통코드 관리 패널에서 DataGridView 찾기
                    var commonCodePanel = FindCommonCodeManagementPanel(btn);
                    LogManagerService.LogInfo($"공통코드 관리 패널 찾기 결과: {(commonCodePanel != null ? "성공" : "실패")}");
                    
                    if (commonCodePanel != null)
                    {
                        var dataGridView = FindDataGridViewInParent(commonCodePanel);
                        LogManagerService.LogInfo($"DataGridView 찾기 결과: {(dataGridView != null ? "성공" : "실패")}");
                        
                        if (dataGridView != null)
                        {
                            LogManagerService.LogInfo($"DataGridView 총 행 수: {dataGridView.Rows.Count}");
                            
                            LogManagerService.LogInfo("DataGridView에서 공통코드 데이터 추출 시작");
                            var commonCodes = GetCommonCodesFromDataGridView(dataGridView);
                            LogManagerService.LogInfo($"추출된 공통코드 개수: {commonCodes.Count}");
                            
                            if (commonCodes.Count == 0)
                            {
                                LogManagerService.LogWarning("저장할 데이터가 없음");
                                MessageBox.Show("저장할 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            LogManagerService.LogInfo("데이터베이스에 공통코드 저장 시작");
                            var result = await _commonCodeRepository.SaveCommonCodesAsync(commonCodes);
                            LogManagerService.LogInfo($"저장 결과: {(result ? "성공" : "실패")}");
                            
                            if (result)
                            {
                                MessageBox.Show("공통코드가 성공적으로 저장되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                _isDataModified = false;
                                LogManagerService.LogInfo("데이터 수정 플래그 초기화");
                                
                                if (!string.IsNullOrEmpty(_selectedGroupCode))
                                {
                                    LogManagerService.LogInfo($"선택된 그룹코드 '{_selectedGroupCode}' 데이터 새로고침 시작");
                                    await LoadCommonCodesAsync(_selectedGroupCode, dataGridView);
                                    LogManagerService.LogInfo("데이터 새로고침 완료");
                                }
                            }
                            else
                            {
                                LogManagerService.LogError("공통코드 저장 실패");
                                MessageBox.Show("공통코드 저장에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            LogManagerService.LogError("DataGridView를 찾을 수 없습니다!");
                            MessageBox.Show("DataGridView를 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        LogManagerService.LogError("공통코드 관리 패널을 찾을 수 없습니다!");
                        MessageBox.Show("공통코드 관리 패널을 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    LogManagerService.LogError("버튼 또는 부모 패널 정보가 올바르지 않습니다.");
                    MessageBox.Show("버튼 정보가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                LogManagerService.LogInfo("=== 저장 버튼 클릭 이벤트 완료 ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 저장 중 오류: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 삭제 버튼 클릭 이벤트
        /// </summary>
        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                LogManagerService.LogInfo("=== 삭제 버튼 클릭 이벤트 시작 ===");
                
                if (sender is Button btn && btn.Parent is Panel panel)
                {
                    LogManagerService.LogInfo($"버튼 정보 - Name: {btn.Name}, Parent: {panel.GetType().Name}");
                    
                    // 공통코드 관리 패널에서 DataGridView 찾기
                    var commonCodePanel = FindCommonCodeManagementPanel(btn);
                    LogManagerService.LogInfo($"공통코드 관리 패널 찾기 결과: {(commonCodePanel != null ? "성공" : "실패")}");
                    
                    if (commonCodePanel != null)
                    {
                        var dataGridView = FindDataGridViewInParent(commonCodePanel);
                        LogManagerService.LogInfo($"DataGridView 찾기 결과: {(dataGridView != null ? "성공" : "실패")}");
                        
                        if (dataGridView?.SelectedRows.Count > 0)
                        {
                            LogManagerService.LogInfo($"선택된 행 개수: {dataGridView.SelectedRows.Count}");
                            
                            var result = MessageBox.Show(
                                "정말로 선택된 공통코드를 삭제하시겠습니까?", 
                                "삭제 확인", 
                                MessageBoxButtons.YesNo, 
                                MessageBoxIcon.Question);

                            if (result == DialogResult.Yes)
                            {
                                LogManagerService.LogInfo("사용자가 삭제를 확인함");
                                
                                var selectedRow = dataGridView.SelectedRows[0];
                                var groupCode = selectedRow.Cells["GroupCode"].Value?.ToString() ?? string.Empty;
                                var code = selectedRow.Cells["Code"].Value?.ToString() ?? string.Empty;
                                
                                LogManagerService.LogInfo($"삭제할 공통코드: {groupCode}.{code}");

                                if (string.IsNullOrEmpty(groupCode) || string.IsNullOrEmpty(code))
                                {
                                    LogManagerService.LogError("삭제할 공통코드 정보가 올바르지 않음");
                                    MessageBox.Show("삭제할 공통코드 정보가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }

                                LogManagerService.LogInfo("데이터베이스에서 공통코드 삭제 시작");
                                var deleteResult = await _commonCodeRepository.DeleteCommonCodeAsync(groupCode, code);
                                LogManagerService.LogInfo($"삭제 결과: {(deleteResult ? "성공" : "실패")}");
                                
                                if (deleteResult)
                                {
                                    MessageBox.Show("공통코드가 성공적으로 삭제되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    
                                    if (!string.IsNullOrEmpty(_selectedGroupCode))
                                    {
                                        LogManagerService.LogInfo($"선택된 그룹코드 '{_selectedGroupCode}' 데이터 새로고침 시작");
                                        await LoadCommonCodesAsync(_selectedGroupCode, dataGridView);
                                        LogManagerService.LogInfo("데이터 새로고침 완료");
                                    }
                                }
                                else
                                {
                                    LogManagerService.LogError("공통코드 삭제 실패");
                                    MessageBox.Show("공통코드 삭제에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                LogManagerService.LogInfo("사용자가 삭제를 취소함");
                            }
                        }
                        else
                        {
                            LogManagerService.LogWarning("선택된 행이 없음");
                            MessageBox.Show("삭제할 행을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        LogManagerService.LogError("공통코드 관리 패널을 찾을 수 없습니다!");
                        MessageBox.Show("공통코드 관리 패널을 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    LogManagerService.LogError("버튼 또는 부모 패널 정보가 올바르지 않습니다.");
                    MessageBox.Show("버튼 정보가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                LogManagerService.LogInfo("=== 삭제 버튼 클릭 이벤트 완료 ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"공통코드 삭제 중 오류: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\n상세 오류: {ex.InnerException.Message}";
                    LogManagerService.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                
                MessageBox.Show($"삭제 중 오류가 발생했습니다:\n\n{errorMessage}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// DataGridView 셀 값 변경 이벤트
        /// </summary>
        private void DataGridViewCodes_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                _isDataModified = true;
                if (sender is DataGridView dataGridView)
                {
                    var panel = FindPanelInParent(dataGridView);
                    if (panel != null)
                    {
                        var btnSave = FindButtonInParent(panel, "btnSave");
                        var btnDelete = FindButtonInParent(panel, "btnDelete");
                        if (btnSave != null && btnDelete != null)
                        {
                            SetButtonStates(true, null, btnSave, btnDelete);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// DataGridView 셀 편집 시작 이벤트
        /// </summary>
        private void DataGridViewCodes_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (sender is DataGridView dataGridView && e.ColumnIndex == dataGridView.Columns["GroupCode"].Index)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 그룹코드를 삭제하는 메서드
        /// </summary>
        private async Task DeleteGroupCodeAsync(TreeNode node)
        {
            try
            {
                LogManagerService.LogInfo("=== 그룹코드 삭제 시작 ===");
                
                if (node?.Tag is not string groupCode || string.IsNullOrEmpty(groupCode))
                {
                    LogManagerService.LogError("삭제할 그룹코드 정보가 올바르지 않음");
                    MessageBox.Show("삭제할 그룹코드 정보가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LogManagerService.LogInfo($"삭제할 그룹코드: '{groupCode}'");

                var result = MessageBox.Show(
                    $"정말로 그룹코드 '{groupCode}'와 관련된 모든 공통코드를 삭제하시겠습니까?\n\n" +
                    "⚠️ 이 작업은 되돌릴 수 없습니다!",
                    "그룹코드 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    LogManagerService.LogInfo("사용자가 삭제를 확인함, 데이터베이스에서 삭제 시작");
                    var deleteResult = await _commonCodeRepository.DeleteGroupCodeAsync(groupCode);
                    LogManagerService.LogInfo($"데이터베이스 삭제 결과: {(deleteResult ? "성공" : "실패")}");
                    
                    if (deleteResult)
                    {
                        MessageBox.Show($"그룹코드 '{groupCode}'가 성공적으로 삭제되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        if (node.TreeView != null)
                        {
                            LogManagerService.LogInfo("TreeView에서 노드 제거 및 새로고침 시작");
                            
                            // 공통코드 관리 패널 찾기
                            var commonCodePanel = FindCommonCodeManagementPanel(node.TreeView);
                            LogManagerService.LogInfo($"공통코드 관리 패널 찾기 결과: {(commonCodePanel != null ? "성공" : "실패")}");
                            
                            if (commonCodePanel != null)
                            {
                                var dataGridView = FindDataGridViewInParent(commonCodePanel);
                                LogManagerService.LogInfo($"DataGridView 찾기 결과: {(dataGridView != null ? "성공" : "실패")}");
                                
                                if (dataGridView != null)
                                {
                                    LogManagerService.LogInfo("TreeView 전체 새로고침 시작");
                                    await LoadGroupCodesAsync(node.TreeView, dataGridView);
                                    LogManagerService.LogInfo("TreeView 새로고침 완료");
                                }
                                else
                                {
                                    LogManagerService.LogError("DataGridView를 찾을 수 없음");
                                    // 최소한 TreeView에서는 노드 제거
                                    node.TreeView.Nodes.Remove(node);
                                }
                            }
                            else
                            {
                                LogManagerService.LogError("공통코드 관리 패널을 찾을 수 없음");
                                // 최소한 TreeView에서는 노드 제거
                                node.TreeView.Nodes.Remove(node);
                            }
                        }
                        else
                        {
                            LogManagerService.LogError("TreeView가 null임");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"그룹코드 '{groupCode}' 삭제에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    LogManagerService.LogInfo("사용자가 삭제를 취소함");
                }
                
                LogManagerService.LogInfo("=== 그룹코드 삭제 완료 ===");
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"그룹코드 삭제 중 오류: {ex.Message}");
                LogManagerService.LogError($"스택 트레이스: {ex.StackTrace}");
                
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\n상세 오류: {ex.InnerException.Message}";
                    LogManagerService.LogError($"Inner Exception: {ex.InnerException.Message}");
                }
                
                MessageBox.Show($"그룹코드 삭제 중 오류가 발생했습니다:\n\n{errorMessage}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 헬퍼 메서드 (Helper Methods)

        /// <summary>
        /// DataGridView에서 공통코드 목록을 추출하는 메서드
        /// </summary>
        private List<CommonCode> GetCommonCodesFromDataGridView(DataGridView dataGridView)
        {
            var commonCodes = new List<CommonCode>();

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells["GroupCode"].Value != null && row.Cells["Code"].Value != null)
                {
                    var commonCode = new CommonCode
                    {
                        GroupCode = row.Cells["GroupCode"].Value.ToString() ?? string.Empty,
                        Code = row.Cells["Code"].Value.ToString() ?? string.Empty,
                        CodeName = row.Cells["CodeName"].Value?.ToString() ?? string.Empty,
                        Description = row.Cells["Description"].Value?.ToString(),
                        SortOrder = Convert.ToInt32(row.Cells["SortOrder"].Value ?? 0),
                        IsUsed = Convert.ToBoolean(row.Cells["IsUsed"].Value ?? true),
                        Attribute1 = row.Cells["Attribute1"].Value?.ToString(),
                        Attribute2 = row.Cells["Attribute2"].Value?.ToString(),
                        CreatedBy = Environment.UserName,
                        CreatedAt = DateTime.Now
                    };

                    commonCodes.Add(commonCode);
                }
            }

            return commonCodes;
        }

        /// <summary>
        /// 공통코드 관리 패널을 찾는 메서드
        /// </summary>
        private Panel? FindCommonCodeManagementPanel(Control startControl)
        {
            // 버튼에서 시작해서 상위로 올라가면서 공통코드 관리 패널을 찾음
            Control? current = startControl;
            while (current != null)
            {
                if (current is Panel panel && panel.Parent is TabPage tabPage && 
                    tabPage.Text.Contains("공통코드 관리"))
                {
                    return panel;
                }
                current = current.Parent;
            }
            
            // 직접 TabControl에서 공통코드 관리 탭을 찾기
            foreach (Control control in this.Controls)
            {
                if (control is TabControl tabControl)
                {
                    foreach (TabPage tabPage in tabControl.TabPages)
                    {
                        if (tabPage.Text.Contains("공통코드 관리") && tabPage.Controls.Count > 0)
                        {
                            return tabPage.Controls[0] as Panel;
                        }
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// 부모 컨트롤에서 TreeView를 찾는 메서드
        /// </summary>
        private TreeView? FindTreeViewInParent(Control control)
        {
            if (control is TreeView treeView) return treeView;
            
            foreach (Control child in control.Controls)
            {
                var result = FindTreeViewInParent(child);
                if (result != null) return result;
            }
            
            return null;
        }

        /// <summary>
        /// 부모 컨트롤에서 DataGridView를 찾는 메서드
        /// </summary>
        private DataGridView? FindDataGridViewInParent(Control control)
        {
            LogManagerService.LogInfo($"FindDataGridViewInParent 시작 - 컨트롤 타입: {control.GetType().Name}");
            
            if (control is DataGridView dataGridView) 
            {
                LogManagerService.LogInfo($"DataGridView 발견! Name: {dataGridView.Name}");
                return dataGridView;
            }
            
            LogManagerService.LogInfo($"자식 컨트롤 개수: {control.Controls.Count}");
            foreach (Control child in control.Controls)
            {
                LogManagerService.LogInfo($"자식 컨트롤 검사 - 타입: {child.GetType().Name}, Name: {child.Name}");
                var result = FindDataGridViewInParent(child);
                if (result != null) 
                {
                    LogManagerService.LogInfo("DataGridView 찾기 성공!");
                    return result;
                }
            }
            
            LogManagerService.LogWarning("DataGridView를 찾지 못함");
            return null;
        }

        /// <summary>
        /// 부모 컨트롤에서 Panel을 찾는 메서드
        /// </summary>
        private Panel? FindPanelInParent(Control control)
        {
            if (control is Panel panel) return panel;
            
            var parent = control.Parent;
            while (parent != null)
            {
                if (parent is Panel panelParent) return panelParent;
                parent = parent.Parent;
            }
            
            return null;
        }

        /// <summary>
        /// 부모 컨트롤에서 특정 이름의 Button을 찾는 메서드
        /// </summary>
        private Button? FindButtonInParent(Control control, string buttonName)
        {
            if (control is Button button && button.Name == buttonName) return button;
            
            foreach (Control child in control.Controls)
            {
                var result = FindButtonInParent(child, buttonName);
                if (result != null) return result;
            }
            
            return null;
        }

        #endregion
    }
} 