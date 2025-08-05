using System;
using System.Windows.Forms;
using LogisticManager.Services;
using System.Linq; // Added for FirstOrDefault
using MySqlConnector; // Added for MySqlConnector
using System.Drawing.Drawing2D;
using System.Collections.Generic; // Added for List
using System.Drawing; // Added for Color, Point, Size, Font

namespace LogisticManager.Forms
{
    /// <summary>
    /// 환경 변수 및 애플리케이션 설정을 관리하는 폼
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
            this.Text = "⚙️ 애플리케이션 설정";
            this.Size = new System.Drawing.Size(700, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 244, 248);

            // 타이틀 라벨
            var titleLabel = new Label
            {
                Text = "🔧 애플리케이션 설정",
                Location = new Point(20, 20),
                Size = new Size(660, 30),
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 탭 컨트롤 생성 및 설정
            var tabControl = new TabControl();
            tabControl.Location = new Point(20, 60);
            tabControl.Size = new Size(660, 420);
            tabControl.Font = new Font("맑은 고딕", 9F);

            // 데이터베이스 설정 탭
            var dbTab = new TabPage("🗄️ 데이터베이스 설정");
            dbTab.Controls.Add(CreateDatabaseSettingsPanel());
            tabControl.TabPages.Add(dbTab);

            // API 설정 탭
            var apiTab = new TabPage("🔗 API 설정");
            apiTab.Controls.Add(CreateApiSettingsPanel());
            tabControl.TabPages.Add(apiTab);

            // 파일 경로 설정 탭
            var pathTab = new TabPage("📁 파일 경로 설정");
            pathTab.Controls.Add(CreatePathSettingsPanel());
            tabControl.TabPages.Add(pathTab);

            // 하단 버튼 패널 생성
            var buttonPanel = new Panel();
            buttonPanel.Location = new Point(20, 500);
            buttonPanel.Size = new Size(660, 50);
            buttonPanel.BackColor = Color.Transparent;

            // 버튼들의 총 너비 계산 (80 + 80 + 110 = 270px)
            // 버튼 간격: 20px
            // 총 너비: 270 + (20 * 2) = 310px
            // 시작 위치: (660 - 310) / 2 = 175px
            
            // 저장 버튼
            var saveButton = CreateModernButton("💾 저장", new Point(175, 10), new Size(80, 35), Color.FromArgb(46, 204, 113));
            saveButton.Click += SaveButton_Click;

            // 취소 버튼
            var cancelButton = CreateModernButton("❌ 취소", new Point(275, 10), new Size(80, 35), Color.FromArgb(231, 76, 60));
            cancelButton.Click += (sender, e) => this.Close();

            // 연결 테스트 버튼
            var testButton = CreateModernButton("🔍 연결 테스트", new Point(375, 10), new Size(110, 35), Color.FromArgb(52, 152, 219));
            testButton.Click += TestConnectionButton_Click;

            // 버튼들을 패널에 추가
            buttonPanel.Controls.AddRange(new Control[] { saveButton, cancelButton, testButton });

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
                Cursor = Cursors.Hand
            };

            // 둥근 모서리 설정
            button.Region = new Region(CreateRoundedRectangle(button.ClientRectangle, 8));

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
                button.BackColor = backgroundColor;
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

            // 설명 라벨
            var infoLabel = CreateLabel("💡 환경 변수를 통해 안전하게 설정값을 관리합니다.", new Point(20, 320));
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
                Padding = new Padding(20)
            };

            var controls = new List<Control>();

            // Dropbox API 설정
            controls.Add(CreateLabel("☁️ Dropbox API 키:", new Point(20, 20)));
            var txtDropboxApi = CreateTextBox("", new Point(20, 45), new Size(400, 25));
            txtDropboxApi.Name = "txtDropboxApi";
            txtDropboxApi.UseSystemPasswordChar = true;
            _textBoxes["txtDropboxApi"] = txtDropboxApi; // 컨트롤 참조 저장
            controls.Add(txtDropboxApi);

            // Kakao Work API 설정
            controls.Add(CreateLabel("💬 Kakao Work API 키:", new Point(20, 80)));
            var txtKakaoApi = CreateTextBox("", new Point(20, 105), new Size(400, 25));
            txtKakaoApi.Name = "txtKakaoApi";
            txtKakaoApi.UseSystemPasswordChar = true;
            _textBoxes["txtKakaoApi"] = txtKakaoApi; // 컨트롤 참조 저장
            controls.Add(txtKakaoApi);

            // Kakao Work 채팅방 ID 설정
            controls.Add(CreateLabel("💬 Kakao Work 채팅방 ID:", new Point(20, 140)));
            var txtKakaoChatroom = CreateTextBox("", new Point(20, 165), new Size(400, 25));
            txtKakaoChatroom.Name = "txtKakaoChatroom";
            _textBoxes["txtKakaoChatroom"] = txtKakaoChatroom; // 컨트롤 참조 저장
            controls.Add(txtKakaoChatroom);

            // 설명 라벨
            var infoLabel = CreateLabel("💡 API 키는 민감한 정보이므로 환경 변수로 관리됩니다.", new Point(20, 200));
            infoLabel.ForeColor = Color.FromArgb(127, 140, 141);
            infoLabel.Font = new Font("맑은 고딕", 8F);
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
            var txtInputPath = CreateTextBox("C:\\Work\\Input\\", new Point(20, 45), new Size(400, 25));
            txtInputPath.Name = "txtInputPath";
            _textBoxes["txtInputPath"] = txtInputPath; // 컨트롤 참조 저장
            controls.Add(txtInputPath);

            // 출력 폴더 설정
            controls.Add(CreateLabel("📤 출력 폴더 경로:", new Point(20, 80)));
            var txtOutputPath = CreateTextBox("C:\\Work\\Output\\", new Point(20, 105), new Size(400, 25));
            txtOutputPath.Name = "txtOutputPath";
            _textBoxes["txtOutputPath"] = txtOutputPath; // 컨트롤 참조 저장
            controls.Add(txtOutputPath);

            // 임시 폴더 설정
            controls.Add(CreateLabel("📁 임시 폴더 경로:", new Point(20, 140)));
            var txtTempPath = CreateTextBox("C:\\Work\\Temp\\", new Point(20, 165), new Size(400, 25));
            txtTempPath.Name = "txtTempPath";
            _textBoxes["txtTempPath"] = txtTempPath; // 컨트롤 참조 저장
            controls.Add(txtTempPath);

            // 설명 라벨
            var infoLabel = CreateLabel("💡 폴더가 존재하지 않으면 자동으로 생성됩니다.", new Point(20, 200));
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
                Size = new Size(200, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(52, 73, 94),
                BackColor = Color.Transparent
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
                SetTextBoxValue("txtServer", settings.GetValueOrDefault("DB_SERVER", "gramwonlogis.mycafe24.com"));
                SetTextBoxValue("txtDatabase", settings.GetValueOrDefault("DB_NAME", "gramwonlogis"));
                SetTextBoxValue("txtUser", settings.GetValueOrDefault("DB_USER", "gramwonlogis"));
                SetTextBoxValue("txtPassword", settings.GetValueOrDefault("DB_PASSWORD", "jung5516!"));
                SetTextBoxValue("txtPort", settings.GetValueOrDefault("DB_PORT", "3306"));

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
                // 임시 설정에서 필수 데이터베이스 설정 검증
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

                // 새 설정으로 업데이트
                settings["DB_SERVER"] = server;
                settings["DB_NAME"] = database;
                settings["DB_USER"] = user;
                settings["DB_PASSWORD"] = password;
                settings["DB_PORT"] = port;
                settings["DROPBOX_API_KEY"] = _tempSettings.GetValueOrDefault("DROPBOX_API_KEY", "");
                settings["KAKAO_WORK_API_KEY"] = _tempSettings.GetValueOrDefault("KAKAO_WORK_API_KEY", "");
                settings["KAKAO_CHATROOM_ID"] = _tempSettings.GetValueOrDefault("KAKAO_CHATROOM_ID", "");
                settings["INPUT_FOLDER_PATH"] = _tempSettings.GetValueOrDefault("INPUT_FOLDER_PATH", "");
                settings["OUTPUT_FOLDER_PATH"] = _tempSettings.GetValueOrDefault("OUTPUT_FOLDER_PATH", "");
                settings["TEMP_FOLDER_PATH"] = _tempSettings.GetValueOrDefault("TEMP_FOLDER_PATH", "");

                // JSON 파일에 저장 (Newtonsoft.Json 사용)
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.None);
                File.WriteAllText(settingsPath, jsonString);

                Console.WriteLine($"✅ 설정 저장 완료: {jsonString}");

                // 저장 성공 메시지와 함께 연결 테스트 옵션 제공
                var result = MessageBox.Show(
                    "✅ 설정이 성공적으로 저장되었습니다!\n\n저장된 설정으로 연결을 테스트하시겠습니까?",
                    "설정 저장 완료",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    // 저장된 설정으로 즉시 연결 테스트 (비동기로 실행)
                    _ = Task.Run(() =>
                    {
                        this.Invoke(() => TestConnectionWithSavedSettings());
                    });
                }
                
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
        /// 연결 테스트 버튼 클릭 이벤트 핸들러
        /// 
        /// 기능:
        /// - 현재 입력된 데이터베이스 설정으로 연결 테스트
        /// - 임시 연결 문자열 생성
        /// - MySQL 연결 시도
        /// - 성공/실패 메시지 표시
        /// 
        /// 테스트 과정:
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
                    MessageBox.Show("⚠️ 서버, 데이터베이스명, 사용자명을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 연결 정보 표시
                var connectionInfo = $"서버: {server}\n데이터베이스: {database}\n사용자: {user}\n포트: {port}";
                MessageBox.Show($"🔍 연결을 시도합니다...\n\n{connectionInfo}", "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

                    MessageBox.Show($"✅ 데이터베이스 연결이 성공했습니다!\n\n서버 버전: {version}\n현재 데이터베이스: {databaseName}", "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"❌ 데이터베이스 연결에 실패했습니다:\n\n오류: {ex.Message}";
                    
                    if (ex.InnerException != null)
                    {
                        errorMessage += $"\n\n상세 오류: {ex.InnerException.Message}";
                    }
                    
                    MessageBox.Show(errorMessage, "연결 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ 연결 테스트 중 오류가 발생했습니다:\n\n오류: {ex.Message}";
                MessageBox.Show(errorMessage, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 저장된 환경 변수 설정으로 연결을 테스트하는 메서드
        /// 
        /// 기능:
        /// - 환경 변수에서 데이터베이스 설정을 읽어옴
        /// - SecurityService를 통해 안전한 연결 문자열 생성
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
                // 환경 변수에서 설정 읽기
                var server = SecurityService.GetEnvironmentVariable("DB_SERVER");
                var database = SecurityService.GetEnvironmentVariable("DB_NAME");
                var user = SecurityService.GetEnvironmentVariable("DB_USER");
                var port = SecurityService.GetEnvironmentVariable("DB_PORT");

                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(user))
                {
                    MessageBox.Show("⚠️ 저장된 데이터베이스 설정이 완전하지 않습니다.\n환경 변수를 확인해주세요.", "설정 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 연결 정보 표시
                var connectionInfo = $"서버: {server}\n데이터베이스: {database}\n사용자: {user}\n포트: {port}";
                MessageBox.Show($"🔍 저장된 설정으로 연결을 시도합니다...\n\n{connectionInfo}", "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // SecurityService를 통해 안전한 연결 문자열 생성
                var connectionString = SecurityService.GetSecureConnectionString();

                // 동기적으로 연결 테스트 실행
                try
                {
                    using var connection = new MySqlConnector.MySqlConnection(connectionString);
                    connection.Open();

                    // 서버 버전 확인
                    using var command = new MySqlConnector.MySqlCommand("SELECT VERSION() as version", connection);
                    var version = command.ExecuteScalar();

                    MessageBox.Show($"✅ 저장된 설정으로 데이터베이스 연결이 성공했습니다!\n\n서버 버전: {version}\n\n이제 애플리케이션에서 저장된 설정을 사용합니다.", "연결 테스트 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
} 