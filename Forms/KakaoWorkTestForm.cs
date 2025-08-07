using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogisticManager.Services;
using LogisticManager.Models;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic; // Added for List

namespace LogisticManager.Forms
{
    /// <summary>
    /// KakaoWork 연결 테스트 및 알림 전송 기능을 제공하는 폼
    /// 
    /// 주요 기능:
    /// - KakaoWork 연결 상태 테스트
    /// - 파일 선택 및 알림 전송
    /// - 다양한 알림 종류별 테스트
    /// - 전송 결과 확인
    /// </summary>
    public class KakaoWorkTestForm : Form
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// KakaoWork 서비스 인스턴스
        /// </summary>
        private readonly KakaoWorkService _kakaoWorkService;

        /// <summary>
        /// Dropbox 서비스 인스턴스
        /// </summary>
        private readonly DropboxService _dropboxService;

        /// <summary>
        /// 선택된 파일 경로
        /// </summary>
        private string? _selectedFilePath;

        /// <summary>
        /// 선택된 알림 종류
        /// </summary>
        private NotificationType _selectedNotificationType = NotificationType.SeoulFrozen;

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
        /// 알림 전송 버튼
        /// </summary>
        private Button btnSendNotification = null!;

        /// <summary>
        /// 알림 종류 선택 콤보박스
        /// </summary>
        private ComboBox cboNotificationType = null!;

        /// <summary>
        /// 선택된 파일 경로 표시 라벨
        /// </summary>
        private Label lblFilePath = null!;

        /// <summary>
        /// 배치 정보 입력 텍스트박스
        /// </summary>
        private TextBox txtBatch = null!;

        /// <summary>
        /// 송장 개수 입력 텍스트박스
        /// </summary>
        private TextBox txtInvoiceCount = null!;

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
        /// KakaoWorkTestForm 생성자
        /// </summary>
        public KakaoWorkTestForm()
        {
            _kakaoWorkService = KakaoWorkService.Instance;
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
            this.Text = "KakaoWork 연결 테스트";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 타이틀 라벨
            lblTitle = new Label
            {
                Text = "📱 KakaoWork 연결 테스트",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(860, 40),
                Location = new Point(20, 20)
            };

            // 연결 테스트 버튼
            btnTestConnection = CreateModernButton("🔗 연결 테스트", new Point(20, 80), new Size(150, 40), Color.FromArgb(52, 152, 219));

            // 파일 선택 버튼
            btnSelectFile = CreateModernButton("📁 파일 선택", new Point(190, 80), new Size(150, 40), Color.FromArgb(46, 204, 113));

            // 알림 전송 버튼
            btnSendNotification = CreateModernButton("📤 알림 전송", new Point(360, 80), new Size(150, 40), Color.FromArgb(155, 89, 182));

            // 알림 종류 선택 라벨
            var lblNotificationType = new Label
            {
                Text = "알림 종류:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Size = new Size(80, 20),
                Location = new Point(20, 140)
            };

            // 알림 종류 선택 콤보박스
            cboNotificationType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(200, 25),
                Location = new Point(110, 140),
                Font = new Font("Segoe UI", 9)
            };
            
            // App.config에서 알림 종류별 한글 이름 읽어와서 콤보박스에 추가
            var notificationTypes = new List<string>();
            foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
            {
                var configKey = $"KakaoWork.NotificationType.{type}.Name";
                var koreanName = System.Configuration.ConfigurationManager.AppSettings[configKey];
                if (!string.IsNullOrEmpty(koreanName))
                {
                    notificationTypes.Add(koreanName);
                }
            }
            
            // App.config에 설정이 없으면 기본값 사용
            if (notificationTypes.Count == 0)
            {
                notificationTypes.AddRange(new string[]
                {
                    "서울냉동",
                    "경기냉동", 
                    "서울공산",
                    "경기공산",
                    "부산청과",
                    "감천냉동",
                    "판매입력",
                    "통합송장",
                    "모니터링체크용(봇방)"
                });
            }
            
            cboNotificationType.Items.AddRange(notificationTypes.ToArray());
            cboNotificationType.SelectedIndex = 0;
            cboNotificationType.SelectedIndexChanged += CboNotificationType_SelectedIndexChanged;

            // 배치 정보 라벨
            var lblBatch = new Label
            {
                Text = "배치 정보:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Size = new Size(80, 20),
                Location = new Point(330, 140)
            };

            // 배치 정보 입력 텍스트박스
            txtBatch = new TextBox
            {
                Text = $"BATCH_{DateTime.Now:yyyyMMdd_HHmmss}",
                Size = new Size(200, 25),
                Location = new Point(420, 140),
                Font = new Font("Segoe UI", 9)
            };

            // 송장 개수 라벨
            var lblInvoiceCount = new Label
            {
                Text = "송장 개수:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Size = new Size(80, 20),
                Location = new Point(640, 140)
            };

            // 송장 개수 입력 텍스트박스
            txtInvoiceCount = new TextBox
            {
                Text = "10",
                Size = new Size(100, 25),
                Location = new Point(730, 140),
                Font = new Font("Segoe UI", 9)
            };

            // 선택된 파일 경로 라벨
            lblFilePath = new Label
            {
                Text = "선택된 파일: 없음",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Size = new Size(740, 20),
                Location = new Point(20, 180)
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
                Size = new Size(860, 350),
                Location = new Point(20, 220)
            };

            // 진행률 표시바
            progressBar = new ProgressBar
            {
                Size = new Size(860, 20),
                Location = new Point(20, 590),
                Style = ProgressBarStyle.Continuous
            };

            // 상태 라벨
            lblStatus = new Label
            {
                Text = "대기 중...",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Size = new Size(740, 20),
                Location = new Point(20, 620)
            };

            // 닫기 버튼
            btnClose = CreateModernButton("❌ 닫기", new Point(780, 620), new Size(100, 30), Color.FromArgb(231, 76, 60));

            // 컨트롤들을 폼에 추가
            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                btnTestConnection,
                btnSelectFile,
                btnSendNotification,
                lblNotificationType,
                cboNotificationType,
                lblBatch,
                txtBatch,
                lblInvoiceCount,
                txtInvoiceCount,
                lblFilePath,
                txtLog,
                progressBar,
                lblStatus,
                btnClose
            });

            // 이벤트 핸들러 등록
            btnTestConnection.Click += BtnTestConnection_Click;
            btnSelectFile.Click += BtnSelectFile_Click;
            btnSendNotification.Click += BtnSendNotification_Click;
            btnClose.Click += BtnClose_Click;

            // 초기 상태 설정
            btnSendNotification.Enabled = false;
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

                LogMessage("🔗 KakaoWork 연결 테스트를 시작합니다...");

                // KakaoWorkService의 TestConnectionAsync 메서드 사용
                var result = await _kakaoWorkService.TestConnectionAsync();

                if (result)
                {
                    LogMessage("✅ KakaoWork 연결 테스트 성공!");
                    lblStatus.Text = "연결 성공";
                    btnSendNotification.Enabled = true;
                }
                else
                {
                    LogMessage("❌ KakaoWork 연결 테스트 실패!");
                    LogMessage("💡 App.config에서 KakaoWork 인증 정보를 확인해주세요.");
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
                Title = "알림 전송할 파일 선택",
                Filter = "모든 파일 (*.*)|*.*|Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls|텍스트 파일 (*.txt)|*.txt",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _selectedFilePath = openFileDialog.FileName;
                lblFilePath.Text = $"선택된 파일: {Path.GetFileName(_selectedFilePath)}";
                LogMessage($"📁 파일 선택됨: {_selectedFilePath}");
                btnSendNotification.Enabled = true;
            }
        }

        /// <summary>
        /// 알림 전송 버튼 클릭 이벤트
        /// </summary>
        private async void BtnSendNotification_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("전송할 파일을 먼저 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!int.TryParse(txtInvoiceCount.Text, out int invoiceCount))
            {
                MessageBox.Show("송장 개수를 올바른 숫자로 입력해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnSendNotification.Enabled = false;
                lblStatus.Text = "알림 전송 중...";
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 30;

                var batch = txtBatch.Text;
                LogMessage($"📤 알림 전송을 시작합니다: {_selectedNotificationType}");
                LogMessage($"📁 파일: {Path.GetFileName(_selectedFilePath)}");
                LogMessage($"📊 배치: {batch}, 송장 개수: {invoiceCount}");

                // 1. Dropbox에 파일 업로드
                LogMessage("☁️ Dropbox에 파일 업로드 중...");
                var dropboxFolderPath = System.Configuration.ConfigurationManager.AppSettings["DropboxFolderPath"] ?? "/LogisticManager/";
                var uploadPath = $"{dropboxFolderPath.TrimEnd('/')}/{_selectedNotificationType}/";
                
                var fileUrl = await _dropboxService.UploadFileAsync(_selectedFilePath, uploadPath);
                LogMessage($"✅ 파일 업로드 완료: {fileUrl}");


                // 2. KakaoWork로 알림 전송
                LogMessage("📱 KakaoWork로 알림 전송 중...");
                LogMessage($"알림 종류: {_selectedNotificationType}");
                LogMessage($"파일 URL: {fileUrl}");
                await _kakaoWorkService.SendInvoiceNotificationAsync(
                    _selectedNotificationType,
                    batch,
                    invoiceCount,
                    fileUrl); // Dropbox에서 업로드된 파일 URL 전달

                LogMessage("✅ 알림 전송 완료!");
                lblStatus.Text = "전송 완료";

                MessageBox.Show($"알림 전송이 완료되었습니다!\n\n알림 종류: {_selectedNotificationType}\n파일: {Path.GetFileName(_selectedFilePath)}\n배치: {batch}\n송장 개수: {invoiceCount}", 
                    "전송 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 알림 전송 오류: {ex.Message}");
                lblStatus.Text = "전송 실패";
                MessageBox.Show($"알림 전송 중 오류가 발생했습니다:\n{ex.Message}", 
                    "전송 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSendNotification.Enabled = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.MarqueeAnimationSpeed = 0;
                progressBar.Value = 0;
            }
        }

        /// <summary>
        /// 알림 종류 선택 변경 이벤트
        /// </summary>
        private void CboNotificationType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboNotificationType.SelectedIndex >= 0)
            {
                var selectedText = cboNotificationType.SelectedItem?.ToString() ?? "서울냉동";
                
                // App.config에서 한글 이름에 해당하는 NotificationType 찾기
                _selectedNotificationType = NotificationType.SeoulFrozen; // 기본값
                foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
                {
                    var configKey = $"KakaoWork.NotificationType.{type}.Name";
                    var koreanName = System.Configuration.ConfigurationManager.AppSettings[configKey];
                    if (koreanName == selectedText)
                    {
                        _selectedNotificationType = type;
                        break;
                    }
                }

                LogMessage($"📋 알림 종류 변경: {selectedText} ({_selectedNotificationType})");
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