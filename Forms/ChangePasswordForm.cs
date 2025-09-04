using System;
using System.Drawing;
using System.Windows.Forms;
using LogisticManager.Services;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 비밀번호 변경 폼
    /// </summary>
    public class ChangePasswordForm : Form
    {
        private readonly AuthenticationService _authenticationService;

        private Label lblCurrent = null!; // 현재 비밀번호 라벨 (초기화는 InitializeComponents에서 수행)
        private TextBox txtCurrent = null!; // 현재 비밀번호 입력
        private Label lblNew = null!; // 새 비밀번호 라벨
        private TextBox txtNew = null!; // 새 비밀번호 입력
        private Label lblConfirm = null!; // 새 비밀번호 확인 라벨
        private TextBox txtConfirm = null!; // 새 비밀번호 확인 입력
        private Button btnOk = null!; // 확인 버튼
        private Button btnCancel = null!; // 취소 버튼
        private Label lblInfo = null!; // 안내 라벨

        public ChangePasswordForm(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "비밀번호 변경";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(360, 230);

            lblInfo = new Label
            {
                Text = "현재 비밀번호를 확인하고 새 비밀번호로 변경합니다.",
                AutoSize = false,
                Size = new Size(320, 30),
                Location = new Point(20, 10)
            };

            lblCurrent = new Label { Text = "현재 비밀번호", AutoSize = true, Location = new Point(20, 50) };
            txtCurrent = new TextBox { Location = new Point(140, 45), Size = new Size(180, 23), UseSystemPasswordChar = true };

            lblNew = new Label { Text = "새 비밀번호", AutoSize = true, Location = new Point(20, 85) };
            txtNew = new TextBox { Location = new Point(140, 80), Size = new Size(180, 23), UseSystemPasswordChar = true };

            lblConfirm = new Label { Text = "새 비밀번호 확인", AutoSize = true, Location = new Point(20, 120) };
            txtConfirm = new TextBox { Location = new Point(140, 115), Size = new Size(180, 23), UseSystemPasswordChar = true };

            btnOk = new Button { Text = "확인", Location = new Point(160, 160), Size = new Size(75, 28) };
            btnCancel = new Button { Text = "취소", Location = new Point(245, 160), Size = new Size(75, 28) };

            btnOk.Click += async (s, e) => await OnConfirmAsync();
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[]
            {
                lblInfo, lblCurrent, txtCurrent, lblNew, txtNew, lblConfirm, txtConfirm, btnOk, btnCancel
            });
        }

        private async Task OnConfirmAsync()
        {
            try
            {
                // 입력값 검증
                var currentPassword = txtCurrent.Text ?? string.Empty; // 현재 비밀번호
                var newPassword = txtNew.Text ?? string.Empty; // 새 비밀번호
                var confirmPassword = txtConfirm.Text ?? string.Empty; // 새 비밀번호 확인

                // 새 비밀번호 일치 여부 확인
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
                {
                    MessageBox.Show(this, "새 비밀번호가 일치하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 현재 비밀번호와 새 비밀번호가 동일한지 확인 (보안상 권장하지 않음)
                if (currentPassword == newPassword)
                {
                    MessageBox.Show(this, "현재 비밀번호와 새 비밀번호가 동일합니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 비밀번호 변경 시도
                var success = await _authenticationService.ChangePasswordAsync(currentPassword, newPassword);
                if (success)
                {
                    MessageBox.Show(this, "비밀번호가 변경되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show(this, "비밀번호 변경에 실패했습니다. 현재 비밀번호를 확인하세요.", "실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}


