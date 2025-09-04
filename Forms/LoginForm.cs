using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LogisticManager.Services;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 사용자 로그인 폼
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly AuthenticationService _authService;
        private readonly Action? _onLoginSuccess; // 로그인 성공 시 콜백
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Button btnCancel = null!;
        private Label lblUsername = null!;
        private Label lblPassword = null!;
        private Label lblTitle = null!;
        private CheckBox chkRememberMe = null!;



        public LoginForm(AuthenticationService authService, Action? onLoginSuccess = null)
        {
            _authService = authService;
            _onLoginSuccess = onLoginSuccess;
            InitializeUI();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // App.config에서 Login 설정 읽기
            string loginSetting = System.Configuration.ConfigurationManager.AppSettings["Login"] ?? "N";
            bool showUsername = loginSetting.ToUpper() == "Y";
            
            // 로그에 설정값 출력
            LogManagerService.LogInfo($"🔍 LoginForm: Login 설정값 = '{loginSetting}', showUsername = {showUsername}");
            


            // 폼 기본 설정
            this.Text = "송장 처리 시스템";
            this.Size = new Size(400, showUsername ? 300 : 250); // 사용자명 표시 여부에 따라 폼 크기 조정
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 244, 248);

            // 제목 라벨
            lblTitle = new Label
            {
                Text = "🔐 로그인",
                Location = new Point(20, 20),
                Size = new Size(320, 30),
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 사용자명 라벨 (Login 설정에 따라 표시/숨김)
            lblUsername = new Label
            {
                Text = "사용자명:",
                Location = new Point(50, showUsername ? 80 : 60),
                Size = new Size(80, 20),
                Font = new Font("맑은 고딕", 10F),
                ForeColor = Color.FromArgb(52, 73, 0),
                Visible = showUsername // Login 설정에 따라 표시 여부 결정
            };

            // 사용자명 입력 필드 (Login 설정에 따라 표시/숨김)
            txtUsername = new TextBox
            {
                Location = new Point(140, showUsername ? 80 : 60),
                Size = new Size(200, 25),
                Font = new Font("맑은 고딕", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = showUsername // Login 설정에 따라 표시 여부 결정
            };

            // 비밀번호 라벨 (사용자명 표시 여부에 따라 위치 조정)
            lblPassword = new Label
            {
                Text = "비밀번호:",
                Location = new Point(50, showUsername ? 120 : 100),
                Size = new Size(80, 20),
                Font = new Font("맑은 고딕", 10F),
                ForeColor = Color.FromArgb(52, 73, 0)
            };

            // 비밀번호 입력 필드 (사용자명 표시 여부에 따라 위치 조정)
            txtPassword = new TextBox
            {
                Location = new Point(140, showUsername ? 120 : 100),
                Size = new Size(200, 25),
                Font = new Font("맑은 고딕", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                UseSystemPasswordChar = true
            };

            // 로그인 기억하기 체크박스 (사용자명 표시 여부에 따라 위치 조정)
            chkRememberMe = new CheckBox
            {
                Text = "로그인 정보 기억하기",
                Location = new Point(140, showUsername ? 155 : 135),
                Size = new Size(200, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(52, 73, 0)
            };

            // 로그인 버튼 (사용자명 표시 여부에 따라 위치 조정)
            btnLogin = new Button
            {
                Text = "로그인",
                Location = new Point(140, showUsername ? 190 : 170),
                Size = new Size(90, 35),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.Click += BtnLogin_Click;

            // 취소 버튼 (사용자명 표시 여부에 따라 위치 조정)
            btnCancel = new Button
            {
                Text = "취소",
                Location = new Point(250, showUsername ? 190 : 170),
                Size = new Size(90, 35),
                Font = new Font("맑은 고딕", 10F),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += BtnCancel_Click;

            // 컨트롤들을 폼에 추가 (사용자명 표시 여부에 따라 조건부 추가)
            var controlsToAdd = new List<Control>
            {
                lblTitle,
                lblPassword,
                txtPassword,
                chkRememberMe,
                btnLogin,
                btnCancel
            };
            
            // 사용자명이 표시되는 경우에만 사용자명 관련 컨트롤 추가
            if (showUsername)
            {
                controlsToAdd.Add(lblUsername);
                controlsToAdd.Add(txtUsername);
            }
            
            this.Controls.AddRange(controlsToAdd.ToArray());

            // Enter 키 이벤트 설정
            txtPassword.KeyPress += (sender, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    BtnLogin_Click(sender, EventArgs.Empty);
                }
            };

            // 포커스 설정 (사용자명 표시 여부에 따라)
            if (showUsername)
            {
                txtUsername.Focus();
            }
            else
            {
                txtPassword.Focus();
            }
        }

        /// <summary>
        /// 로그인 버튼 클릭 이벤트
        /// </summary>
        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            try
            {
                // App.config에서 Login 설정 읽기
                string loginSetting = System.Configuration.ConfigurationManager.AppSettings["Login"] ?? "N";
                bool showUsername = loginSetting.ToUpper() == "Y";

                // 입력값 검증
                if (showUsername && string.IsNullOrEmpty(txtUsername.Text.Trim()))
                {
                    MessageBox.Show("사용자명을 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(txtPassword.Text))
                {
                    MessageBox.Show("비밀번호를 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return;
                }

                // 로그인 처리
                btnLogin.Enabled = false;
                btnCancel.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var username = showUsername ? txtUsername.Text.Trim() : "default"; // 사용자명이 표시되지 않는 경우 기본값 사용
                var password = txtPassword.Text;

                if (await _authService.LoginAsync(username, password))
                {
                    // 로그인 성공 시 사용자명 저장
                    SaveUsername();
                    
                    // 로그인 성공 (메시지 표시하지 않음)
                    // MessageBox.Show($"환영합니다, {_authService.CurrentUser?.Username}님!", "로그인 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 로그인 성공 콜백 호출
                    _onLoginSuccess?.Invoke();
                    
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    // 로그인 실패
                    MessageBox.Show("사용자명 또는 비밀번호가 올바르지 않습니다.", "로그인 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그인 처리 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnCancel.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 취소 버튼 클릭 이벤트
        /// </summary>
        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            try
            {
                // 취소 시 사용자에게 확인 메시지 표시
                var result = MessageBox.Show(
                    //"로그인을 취소하시겠습니까?\n프로그램이 종료됩니다.", 
                    "로그인을 취소하시겠습니까?", 
                    "로그인 취소", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // DialogResult 설정 및 폼 닫기
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
                // No를 선택한 경우 아무것도 하지 않음 (로그인 폼 유지)
            }
            catch (Exception ex)
            {
                // 오류 발생 시 강제로 폼 닫기
                System.Diagnostics.Debug.WriteLine($"취소 버튼 클릭 중 오류: {ex.Message}");
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        /// <summary>
        /// 폼 로드 시 이벤트
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // 저장된 사용자명 불러오기
            LoadSavedUsername();
            
            // 사용자명 필드에 포커스
            txtUsername.Focus();
        }

        /// <summary>
        /// 저장된 사용자명 불러오기
        /// </summary>
        private void LoadSavedUsername()
        {
            try
            {
                var settingsPath = Path.Combine(Application.StartupPath, "login_settings.txt");
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    if (lines.Length >= 2)
                    {
                        var savedUsername = lines[0];
                        var rememberMe = bool.Parse(lines[1]);
                        
                        if (!string.IsNullOrEmpty(savedUsername) && rememberMe)
                        {
                            txtUsername.Text = savedUsername;
                            chkRememberMe.Checked = true;
                            txtPassword.Focus(); // 비밀번호 필드로 포커스 이동
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 설정 로드 실패 시 무시 (기본값 사용)
                System.Diagnostics.Debug.WriteLine($"사용자명 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 사용자명 저장하기
        /// </summary>
        private void SaveUsername()
        {
            try
            {
                var settingsPath = Path.Combine(Application.StartupPath, "login_settings.txt");
                var username = chkRememberMe.Checked ? txtUsername.Text.Trim() : "";
                var rememberMe = chkRememberMe.Checked;
                
                var content = $"{username}\n{rememberMe}";
                File.WriteAllText(settingsPath, content);
            }
            catch (Exception ex)
            {
                // 설정 저장 실패 시 무시
                System.Diagnostics.Debug.WriteLine($"사용자명 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 폼이 닫힐 때 이벤트
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            // 폼이 닫힐 때 사용자명 저장
            SaveUsername();
            
            // DialogResult가 설정되지 않은 경우 Cancel로 설정
            if (this.DialogResult == DialogResult.None)
            {
                this.DialogResult = DialogResult.Cancel;
            }
        }
    }
}
