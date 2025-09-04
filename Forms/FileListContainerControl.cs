using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using LogisticManager.Services;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 파일 목록을 ListView로 표시하는 컨트롤
    /// </summary>
    public class FileListContainerControl : UserControl
    {
        #region 필드 (Private Fields)
        
        private readonly ListView _listView;
        private readonly List<FileInfo> _fileInfos = new();
        private readonly DropboxService _dropboxService;
        
        // 색상 상수
        private readonly Color _backgroundColor = Color.White;
        private readonly Color _borderColor = Color.FromArgb(220, 220, 220);
        
        #endregion

        #region 속성 (Properties)
        
        /// <summary>
        /// 체크된 파일 정보 목록
        /// </summary>
        public IEnumerable<FileInfo> CheckedFiles => _fileInfos.Where(info => info.IsChecked);
        
        /// <summary>
        /// 모든 파일 정보 목록
        /// </summary>
        public IEnumerable<FileInfo> AllFiles => _fileInfos.AsReadOnly();
        
        /// <summary>
        /// 파일 개수
        /// </summary>
        public int FileCount => _fileInfos.Count;
        
        #endregion

        #region 생성자 (Constructor)
        
        public FileListContainerControl()
        {
            this.Size = new Size(560, 200);
            this.BackColor = _backgroundColor;
            this.BorderStyle = BorderStyle.FixedSingle;
            
            // DropboxService 초기화
            _dropboxService = DropboxService.Instance;
            
            // ListView 생성 및 설정
            _listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                MultiSelect = false,
                HideSelection = false,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(52, 73, 94),
                Font = new Font("맑은 고딕", 9),
                Dock = DockStyle.Fill
            };
            
            // 컬럼 설정
            _listView.Columns.Add("파일명", 200);
            _listView.Columns.Add("크기", 80);
            _listView.Columns.Add("업로드 시간", 150); // 실제 시간 표시를 위해 너비 증가
            _listView.Columns.Add("상태", 80);
            
            // 이벤트 핸들러 등록
            _listView.ItemChecked += ListView_ItemChecked;
            _listView.DoubleClick += ListView_DoubleClick; // 더블클릭 이벤트 추가
            
            // 컨트롤 추가
            this.Controls.Add(_listView);
            
            // 초기 빈 상태 표시
            ShowEmptyState();
        }
        
        #endregion

        #region 공개 메서드 (Public Methods)
        
        /// <summary>
        /// 파일을 목록에 추가합니다
        /// </summary>
        public void AddFileCard(string fileName, long fileSize, DateTime uploadTime, string? dropboxPath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AddFileCard 호출됨: {fileName}, 크기: {fileSize}, 시간: {uploadTime}");
                
                // 중복 파일 체크 및 제거
                var existingFiles = _fileInfos.Where(info => info.FileName == fileName).ToList();
                foreach (var file in existingFiles)
                {
                    _fileInfos.Remove(file);
                }
                
                // 새 파일 정보 생성
                var fileInfo = new FileInfo
                {
                    FileName = fileName,
                    FileSize = fileSize,
                    UploadTime = uploadTime,
                    DropboxPath = dropboxPath,
                    IsChecked = false
                };
                
                _fileInfos.Insert(0, fileInfo); // 맨 위에 추가
                System.Diagnostics.Debug.WriteLine($"파일 정보 추가됨: {_fileInfos.Count}개");
                
                // ListView에 항목 추가
                var item = new ListViewItem(fileName);
                item.SubItems.Add(FormatFileSize(fileSize));
                item.SubItems.Add(FormatRelativeTime(uploadTime));
                item.SubItems.Add("완료");
                item.Tag = fileInfo; // 파일 정보를 Tag에 저장
                item.Checked = false;
                
                _listView.Items.Insert(0, item);
                System.Diagnostics.Debug.WriteLine($"ListView 항목 추가됨: {_listView.Items.Count}개");
                
                // 빈 상태 라벨 제거 및 ListView 표시
                var existingLabels = this.Controls.OfType<Label>().Where(l => l.Text.Contains("업로드된 파일이 없습니다")).ToList();
                foreach (var label in existingLabels)
                {
                    this.Controls.Remove(label);
                    label.Dispose();
                }
                
                _listView.Show();
                _listView.BringToFront();
                
                // 최대 100개까지만 유지
                if (_listView.Items.Count > 100)
                {
                    var excessItems = _listView.Items.Cast<ListViewItem>().Skip(100).ToList();
                    foreach (var excessItem in excessItems)
                    {
                        var excessFileInfo = excessItem.Tag as FileInfo;
                        if (excessFileInfo != null)
                        {
                            _fileInfos.Remove(excessFileInfo);
                        }
                        _listView.Items.Remove(excessItem);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 추가 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 모든 파일을 제거합니다
        /// </summary>
        public void ClearAllCards()
        {
            _fileInfos.Clear();
            _listView.Items.Clear();
            ShowEmptyState();
        }
        
        /// <summary>
        /// 체크된 파일들을 제거합니다
        /// </summary>
        public void RemoveCheckedCards()
        {
            var checkedItems = _listView.CheckedItems.Cast<ListViewItem>().ToList();
            
            foreach (var item in checkedItems)
            {
                var fileInfo = item.Tag as FileInfo;
                if (fileInfo != null)
                {
                    _fileInfos.Remove(fileInfo);
                }
                _listView.Items.Remove(item);
            }
            
            // 빈 상태 표시 여부 결정
            if (_listView.Items.Count == 0)
            {
                ShowEmptyState();
            }
        }
        
        /// <summary>
        /// 모든 파일을 체크합니다
        /// </summary>
        public void CheckAllCards()
        {
            foreach (ListViewItem item in _listView.Items)
            {
                item.Checked = true;
            }
        }
        
        /// <summary>
        /// 모든 파일의 체크를 해제합니다
        /// </summary>
        public void UncheckAllCards()
        {
            foreach (ListViewItem item in _listView.Items)
            {
                item.Checked = false;
            }
        }
        
        #endregion

        #region 이벤트 핸들러 (Event Handlers)
        
        private void ListView_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            try
            {
                // Tag에서 파일 정보 가져오기
                if (e.Item.Tag is FileInfo fileInfo)
                {
                    fileInfo.IsChecked = e.Item.Checked;
                }
                
                // 체크 상태 변경 이벤트 발생
                OnCardCheckedChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"체크 상태 변경 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 파일 더블클릭 시 바로 열기
        /// </summary>
        private async void ListView_DoubleClick(object? sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;
            
            var selectedItem = _listView.SelectedItems[0];
            if (selectedItem.Tag is FileInfo fileInfo)
            {
                await OpenFileDirectly(fileInfo);
            }
        }
        
        #endregion

        #region 비공개 메서드 (Private Methods)
        
        /// <summary>
        /// 파일을 바로 열기 (다운로드 후 시스템 기본 프로그램으로 열기)
        /// </summary>
        private async Task OpenFileDirectly(FileInfo fileInfo)
        {
            try
            {
                // 상태 업데이트
                UpdateFileStatus(fileInfo.FileName, "다운로드 중...");
                
                // 임시 디렉토리 생성
                var tempDir = Path.Combine(Path.GetTempPath(), "LogisticManager");
                Directory.CreateDirectory(tempDir);
                
                // 임시 파일 경로 생성
                var tempFilePath = Path.Combine(tempDir, fileInfo.FileName);
                
                // 기존 임시 파일이 있으면 삭제
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                
                // Dropbox에서 파일 다운로드
                if (!string.IsNullOrEmpty(fileInfo.DropboxPath))
                {
                    var downloadSuccess = await _dropboxService.DownloadFileAsync(fileInfo.DropboxPath, tempFilePath);
                    
                    if (downloadSuccess && File.Exists(tempFilePath))
                    {
                        // 상태 업데이트
                        UpdateFileStatus(fileInfo.FileName, "열기 중...");
                        
                        // 시스템 기본 프로그램으로 파일 열기
                        var process = new Process();
                        process.StartInfo.FileName = tempFilePath;
                        process.StartInfo.UseShellExecute = true;
                        
                        if (process.Start())
                        {
                            UpdateFileStatus(fileInfo.FileName, "완료");
                            
                            // 파일 열기 성공 이벤트 발생
                            OnFileOpened(fileInfo, tempFilePath);
                        }
                        else
                        {
                            UpdateFileStatus(fileInfo.FileName, "열기 실패");
                            MessageBox.Show($"파일을 열 수 없습니다: {fileInfo.FileName}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        UpdateFileStatus(fileInfo.FileName, "다운로드 실패");
                        MessageBox.Show($"파일 다운로드에 실패했습니다: {fileInfo.FileName}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    UpdateFileStatus(fileInfo.FileName, "경로 없음");
                    MessageBox.Show($"파일 경로 정보가 없습니다: {fileInfo.FileName}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateFileStatus(fileInfo.FileName, "오류");
                MessageBox.Show($"파일 열기 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"파일 열기 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 파일 상태 업데이트
        /// </summary>
        private void UpdateFileStatus(string fileName, string status)
        {
            foreach (ListViewItem item in _listView.Items)
            {
                if (item.Text == fileName)
                {
                    item.SubItems[3].Text = status; // 상태 컬럼 업데이트
                    break;
                }
            }
        }
        
        /// <summary>
        /// 빈 상태를 표시합니다
        /// </summary>
        private void ShowEmptyState()
        {
            _listView.Items.Clear();
            _listView.Hide();
            
            // 기존 빈 상태 라벨 제거
            var existingLabels = this.Controls.OfType<Label>().Where(l => l.Text.Contains("업로드된 파일이 없습니다")).ToList();
            foreach (var label in existingLabels)
            {
                this.Controls.Remove(label);
                label.Dispose();
            }
            
            // 빈 상태 라벨 생성
            var emptyLabel = new Label
            {
                Text = "📁 업로드된 파일이 없습니다.\n파일을 처리하면 여기에 표시됩니다.\n\n💡 파일을 더블클릭하면 바로 열 수 있습니다.",
                Font = new Font("맑은 고딕", 9),
                ForeColor = Color.FromArgb(127, 140, 141),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            
            this.Controls.Add(emptyLabel);
            emptyLabel.BringToFront();
        }
        
        /// <summary>
        /// 파일 크기를 읽기 쉬운 형태로 변환합니다
        /// </summary>
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
        /// 실제 업로드 시간을 표시합니다
        /// </summary>
        private string FormatRelativeTime(DateTime time)
        {
            // 실제 업로드 시간을 한국 시간 형식으로 표시
            return time.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        #endregion

        #region 이벤트 (Events)
        
        /// <summary>
        /// 체크 상태가 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler? CardCheckedChanged;
        
        /// <summary>
        /// 파일이 열렸을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<FileOpenedEventArgs>? FileOpened;
        
        protected virtual void OnCardCheckedChanged()
        {
            CardCheckedChanged?.Invoke(this, EventArgs.Empty);
        }
        
        protected virtual void OnFileOpened(FileInfo fileInfo, string localFilePath)
        {
            FileOpened?.Invoke(this, new FileOpenedEventArgs(fileInfo, localFilePath));
        }
        
        #endregion

        #region 내부 클래스 (Inner Classes)
        
        /// <summary>
        /// 파일 정보를 저장하는 클래스
        /// </summary>
        public class FileInfo
        {
            public string FileName { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public DateTime UploadTime { get; set; }
            public string? DropboxPath { get; set; }
            public bool IsChecked { get; set; }
        }
        
        /// <summary>
        /// 파일 열기 이벤트 인수
        /// </summary>
        public class FileOpenedEventArgs : EventArgs
        {
            public FileInfo FileInfo { get; }
            public string LocalFilePath { get; }
            
            public FileOpenedEventArgs(FileInfo fileInfo, string localFilePath)
            {
                FileInfo = fileInfo;
                LocalFilePath = localFilePath;
            }
        }
        
        #endregion
    }
}
