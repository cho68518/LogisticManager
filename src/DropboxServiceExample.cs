using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogisticManager.Services;

namespace LogisticManager.Examples
{
    /// <summary>
    /// DropboxService 사용 예시 클래스
    /// WinForms 버튼 클릭 이벤트에서 사용하는 방법을 보여줍니다.
    /// </summary>
    public class DropboxServiceExample
    {
        /// <summary>
        /// 파일 업로드 버튼 클릭 이벤트 핸들러 예시
        /// </summary>
        /// <param name="sender">이벤트 소스</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnUploadFile_Click(object sender, EventArgs e)
        {
            try
            {
                // 파일 선택 다이얼로그
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Excel 파일 (*.xlsx)|*.xlsx|모든 파일 (*.*)|*.*";
                    openFileDialog.Title = "업로드할 파일을 선택하세요";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var localFilePath = openFileDialog.FileName;
                        // App.config에서 Dropbox 폴더 경로 읽기
                        var dropboxFolderPath = System.Configuration.ConfigurationManager.AppSettings["DropboxFolderPath"] ?? "/LogisticManager/";

                        // 진행 상황 표시
                        var progressForm = new Form
                        {
                            Text = "파일 업로드 중...",
                            Size = new System.Drawing.Size(300, 100),
                            StartPosition = FormStartPosition.CenterParent,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            MaximizeBox = false,
                            MinimizeBox = false
                        };

                        var progressLabel = new Label
                        {
                            Text = "Dropbox에 파일을 업로드하고 있습니다...",
                            Location = new System.Drawing.Point(20, 30),
                            AutoSize = true
                        };

                        progressForm.Controls.Add(progressLabel);
                        progressForm.Show();

                        try
                        {
                            // DropboxService Singleton 인스턴스 사용
                            var dropboxService = DropboxService.Instance;
                            
                            // 파일 업로드 및 공유 링크 생성
                            var sharedUrl = await dropboxService.UploadFileAsync(localFilePath, dropboxFolderPath);

                            progressForm.Close();

                            // 성공 메시지 표시
                            MessageBox.Show(
                                $"파일 업로드 완료!\n\n파일: {System.IO.Path.GetFileName(localFilePath)}\n공유 링크: {sharedUrl}",
                                "업로드 성공",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                        }
                        catch (Exception ex)
                        {
                            progressForm.Close();
                            MessageBox.Show(
                                $"파일 업로드 실패: {ex.Message}",
                                "업로드 오류",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"오류 발생: {ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Dropbox 연결 테스트 버튼 클릭 이벤트 핸들러 예시
        /// </summary>
        /// <param name="sender">이벤트 소스</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.Enabled = false;
                    button.Text = "연결 테스트 중...";
                }

                // DropboxService Singleton 인스턴스 사용
                var dropboxService = DropboxService.Instance;
                
                // 연결 테스트
                var isConnected = await dropboxService.TestConnectionAsync();

                if (button != null)
                {
                    button.Enabled = true;
                    button.Text = "연결 테스트";
                }

                if (isConnected)
                {
                    MessageBox.Show(
                        "Dropbox 연결 성공!",
                        "연결 테스트",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Dropbox 연결 실패. App.config의 인증 정보를 확인해주세요.",
                        "연결 테스트",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.Enabled = true;
                    button.Text = "연결 테스트";
                }

                MessageBox.Show(
                    $"연결 테스트 오류: {ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// InvoiceProcessor에서 DropboxService 사용 예시
        /// </summary>
        /// <param name="filePath">업로드할 파일 경로</param>
        /// <param name="progress">진행 상황 보고용</param>
        /// <returns>공유 링크 URL</returns>
        public static async Task<string> UploadInvoiceFileAsync(string filePath, IProgress<string>? progress = null)
        {
            try
            {
                progress?.Report("Dropbox에 파일 업로드 중...");

                // DropboxService Singleton 인스턴스 사용
                var dropboxService = DropboxService.Instance;
                
                // App.config에서 Dropbox 폴더 경로 읽기
                var dropboxFolderPath = System.Configuration.ConfigurationManager.AppSettings["DropboxFolderPath"] ?? "/LogisticManager/";
                var uploadPath = $"{dropboxFolderPath.TrimEnd('/')}/Invoices/";
                
                // 파일 업로드 및 공유 링크 생성
                var sharedUrl = await dropboxService.UploadFileAsync(filePath, uploadPath);

                progress?.Report($"파일 업로드 완료: {sharedUrl}");

                return sharedUrl;
            }
            catch (Exception ex)
            {
                progress?.Report($"파일 업로드 실패: {ex.Message}");
                throw;
            }
        }
    }
} 