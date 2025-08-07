using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogisticManager.Services;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LogisticManager.Forms
{
    /// <summary>
    /// Dropbox 연결 테스트 및 파일 업로드 기능을 제공하는 폼
    /// 
    /// 주요 기능:
    /// - Dropbox 연결 상태 테스트
    /// - 파일 선택 및 업로드
    /// - 업로드 결과 확인
    /// - 공유 링크 생성
    /// </summary>
    public class DropboxTestForm : Form
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// Dropbox 서비스 인스턴스
        /// </summary>
        private readonly DropboxService _dropboxService;

        /// <summary>
        /// 선택된 파일 경로
        /// </summary>
        private string? _selectedFilePath;

        #endregion

        #region UI 컨트롤 (UI Controls)

        /// <summary>
        /// 타이틀 라벨
        /// </summary>
        private Label lblTitle = null!;

        /// <summary>
        /// 연결 테스트 버튼
        /// </summary>
        private Button btnTestConnection = null!;

        /// <summary>
        /// 파일 선택 버튼
        /// </summary>
        private Button btnSelectFile = null!;

        /// <summary>
        /// 파일 업로드 버튼
        /// </summary>
        private Button btnUploadFile = null!;

        /// <summary>
        /// 선택된 파일 경로 표시 라벨
        /// </summary>
        private Label lblFilePath = null!;

        /// <summary>
        /// 로그 메시지 출력 텍스트박스
        /// </summary>
        private TextBox txtLog = null!;

        /// <summary>
        /// 진행률 표시바
        /// </summary>
        private ProgressBar progressBar = null!;

        /// <summary>
        /// 상태 라벨
        /// </summary>
        private Label lblStatus = null!;

        /// <summary>
        /// 닫기 버튼
        /// </summary>
        private Button btnClose = null!;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// DropboxTestForm 생성자
        /// </summary>
        public DropboxTestForm()
        {
            _dropboxService = DropboxService.Instance;
            InitializeComponent();
            InitializeUI();
        }

        #endregion

        #region UI 초기화 (UI Initialization)

        /// <summary>
        /// 기본 폼 설정을 초기화하는 메서드
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        /// <summary>
        /// UI 컨트롤들을 초기화하고 배치하는 메서드
        /// </summary>
        private void InitializeUI()
        {
            // 폼 기본 설정
            this.Text = "Dropbox 연결 테스트";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 타이틀 라벨
            lblTitle = new Label
            {
                Text = "📁 Dropbox 연결 테스트",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(760, 40),
                Location = new Point(20, 20)
            };

            // 연결 테스트 버튼
            btnTestConnection = CreateModernButton("🔗 연결 테스트", new Point(20, 80), new Size(150, 40), Color.FromArgb(52, 152, 219));

            // 파일 선택 버튼
            btnSelectFile = CreateModernButton("📁 파일 선택", new Point(190, 80), new Size(150, 40), Color.FromArgb(46, 204, 113));

            // 파일 업로드 버튼
            btnUploadFile = CreateModernButton("☁️ 파일 업로드", new Point(360, 80), new Size(150, 40), Color.FromArgb(155, 89, 182));

            // 선택된 파일 경로 라벨
            lblFilePath = new Label
            {
                Text = "선택된 파일: 없음",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Size = new Size(540, 20),
                Location = new Point(20, 140)
            };

            // 로그 텍스트박스
            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(0, 255, 0),
                Font = new Font("Consolas", 9),
                Size = new Size(760, 300),
                Location = new Point(20, 180)
            };

            // 진행률 표시바
            progressBar = new ProgressBar
            {
                Size = new Size(760, 20),
                Location = new Point(20, 500),
                Style = ProgressBarStyle.Continuous
            };

            // 상태 라벨
            lblStatus = new Label
            {
                Text = "대기 중...",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Size = new Size(540, 20),
                Location = new Point(20, 530)
            };

            // 닫기 버튼
            btnClose = CreateModernButton("❌ 닫기", new Point(680, 530), new Size(100, 30), Color.FromArgb(231, 76, 60));

            // 컨트롤들을 폼에 추가
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                btnTestConnection,
                btnSelectFile,
                btnUploadFile,
                lblFilePath,
                txtLog,
                progressBar,
                lblStatus,
                btnClose
            });

            // 이벤트 핸들러 등록
            btnTestConnection.Click += BtnTestConnection_Click;
            btnSelectFile.Click += BtnSelectFile_Click;
            btnUploadFile.Click += BtnUploadFile_Click;
            btnClose.Click += BtnClose_Click;

            // 초기 상태 설정
            btnUploadFile.Enabled = false;
        }

        /// <summary>
        /// 모던한 스타일의 버튼을 생성하는 메서드
        /// </summary>
        private Button CreateModernButton(string text, Point location, Size size, Color? backgroundColor = null)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = backgroundColor ?? Color.FromArgb(52, 152, 219),
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Max(0, backgroundColor?.R - 20 ?? 32),
                Math.Max(0, backgroundColor?.G - 20 ?? 132),
                Math.Max(0, backgroundColor?.B - 20 ?? 199)
            );

            return button;
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// 연결 테스트 버튼 클릭 이벤트
        /// </summary>
        private async void BtnTestConnection_Click(object? sender, EventArgs e)
        {
            try
            {
                btnTestConnection.Enabled = false;
                lblStatus.Text = "연결 테스트 중...";
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 30;

                LogMessage("🔗 Dropbox 연결 테스트를 시작합니다...");

                var result = await _dropboxService.TestConnectionAsync();

                if (result)
                {
                    LogMessage("✅ Dropbox 연결 테스트 성공!");
                    LogMessage("☁️ 파일 업로드 기능을 사용할 수 있습니다.");
                    lblStatus.Text = "연결 성공";
                    btnUploadFile.Enabled = true;
                }
                else
                {
                    LogMessage("❌ Dropbox 연결 테스트 실패!");
                    LogMessage("💡 App.config에서 Dropbox 인증 정보를 확인해주세요.");
                    LogMessage("💡 Dropbox.AppKey, Dropbox.AppSecret, Dropbox.RefreshToken을 확인해주세요.");
                    lblStatus.Text = "연결 실패";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 연결 테스트 오류: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogMessage($"🔍 상세 오류: {ex.InnerException.Message}");
                }
                
                var errorMessage = "Dropbox 연결 테스트 중 오류가 발생했습니다.";
                if (ex.Message.Contains("unauthorized") || ex.Message.Contains("인증"))
                {
                    errorMessage += "\n\n💡 Dropbox 인증 정보를 확인해주세요.";
                }
                else if (ex.Message.Contains("network") || ex.Message.Contains("연결"))
                {
                    errorMessage += "\n\n💡 네트워크 연결을 확인해주세요.";
                }
                
                LogMessage(errorMessage);
                lblStatus.Text = "연결 오류";
            }
            finally
            {
                btnTestConnection.Enabled = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.MarqueeAnimationSpeed = 0;
                progressBar.Value = 0;
            }
        }

        /// <summary>
        /// 파일 선택 버튼 클릭 이벤트
        /// </summary>
        private void BtnSelectFile_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Title = "업로드할 파일 선택",
                Filter = "모든 파일 (*.*)|*.*|Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls|텍스트 파일 (*.txt)|*.txt",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _selectedFilePath = openFileDialog.FileName;
                lblFilePath.Text = $"선택된 파일: {Path.GetFileName(_selectedFilePath)}";
                LogMessage($"📁 파일 선택됨: {_selectedFilePath}");
                btnUploadFile.Enabled = true;
            }
        }

        /// <summary>
        /// 파일 업로드 버튼 클릭 이벤트
        /// </summary>
        private async void BtnUploadFile_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("업로드할 파일을 먼저 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(_selectedFilePath))
            {
                MessageBox.Show("선택된 파일이 존재하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                btnUploadFile.Enabled = false;
                lblStatus.Text = "파일 업로드 중...";
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 30;

                LogMessage($"☁️ 파일 업로드를 시작합니다: {Path.GetFileName(_selectedFilePath)}");

                // App.config에서 Dropbox 폴더 경로 읽기
                var dropboxFolderPath = System.Configuration.ConfigurationManager.AppSettings["DropboxFolderPath"] ?? "/LogisticManager/";
                LogMessage($"📁 업로드 폴더: {dropboxFolderPath}");
                
                // 파일 업로드 시도 (최대 3회 재시도)
                string? sharedUrl = null;
                Exception? lastException = null;
                
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        LogMessage($"🔄 업로드 시도 {attempt}/3...");
                        sharedUrl = await _dropboxService.UploadFileAsync(_selectedFilePath, dropboxFolderPath);
                        LogMessage($"✅ 파일 업로드 완료! (시도 {attempt}/3)");
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        LogMessage($"⚠️ 업로드 시도 {attempt}/3 실패: {ex.Message}");
                        
                        if (attempt < 3)
                        {
                            LogMessage("🔄 3초 후 재시도합니다...");
                            await Task.Delay(3000);
                        }
                    }
                }

                if (sharedUrl == null)
                {
                    throw lastException ?? new InvalidOperationException("파일 업로드에 실패했습니다.");
                }

                LogMessage($"🔗 공유 링크: {sharedUrl}");
                lblStatus.Text = "업로드 완료";
                
                // 클립보드에 링크 복사
                try
                {
                    Clipboard.SetText(sharedUrl);
                    LogMessage("📋 공유 링크가 클립보드에 복사되었습니다.");
                }
                catch (Exception clipboardEx)
                {
                    LogMessage($"⚠️ 클립보드 복사 실패: {clipboardEx.Message}");
                }

                MessageBox.Show($"파일 업로드가 완료되었습니다!\n\n파일: {Path.GetFileName(_selectedFilePath)}\n공유 링크가 클립보드에 복사되었습니다.", 
                    "업로드 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 파일 업로드 오류: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogMessage($"🔍 상세 오류: {ex.InnerException.Message}");
                }
                lblStatus.Text = "업로드 실패";
                
                var errorMessage = $"파일 업로드 중 오류가 발생했습니다:\n{ex.Message}";
                if (ex.Message.Contains("unauthorized") || ex.Message.Contains("인증"))
                {
                    errorMessage += "\n\n💡 Dropbox 인증 정보를 확인해주세요.";
                }
                else if (ex.Message.Contains("network") || ex.Message.Contains("연결"))
                {
                    errorMessage += "\n\n💡 네트워크 연결을 확인해주세요.";
                }
                
                MessageBox.Show(errorMessage, "업로드 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnUploadFile.Enabled = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.MarqueeAnimationSpeed = 0;
                progressBar.Value = 0;
            }
        }

        /// <summary>
        /// 닫기 버튼 클릭 이벤트
        /// </summary>
        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region 유틸리티 메서드 (Utility Methods)

        /// <summary>
        /// 로그 메시지를 텍스트박스에 추가하는 메서드
        /// </summary>
        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}";
            
            txtLog.AppendText(logMessage + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        #endregion
    }
} 