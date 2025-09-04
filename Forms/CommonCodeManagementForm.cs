using LogisticManager.Models;
using LogisticManager.Repositories;
using LogisticManager.Services;
using System.ComponentModel;

namespace LogisticManager.Forms
{
    /// <summary>
    /// 공통코드 관리를 위한 폼
    /// 
    /// 주요 기능:
    /// - 그룹코드별 공통코드 관리 (Master-Detail 구조)
    /// - 공통코드 추가/수정/삭제
    /// - 일괄 저장 기능
    /// 
    /// 화면 구성:
    /// - 왼쪽: TreeView (그룹코드 목록)
    /// - 오른쪽: DataGridView (공통코드 상세 목록)
    /// - 하단: 추가/저장/삭제 버튼
    /// </summary>
    public partial class CommonCodeManagementForm : Form
    {
        #region 필드 (Private Fields)

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

        #region UI 컨트롤 (UI Controls)

        /// <summary>
        /// 그룹코드 목록을 표시하는 TreeView
        /// </summary>
        private TreeView treeViewGroupCodes = null!;

        /// <summary>
        /// 공통코드 상세 목록을 표시하는 DataGridView
        /// </summary>
        private DataGridView dataGridViewCodes = null!;

        /// <summary>
        /// 새 그룹코드 추가 버튼
        /// </summary>
        private Button btnAddGroup = null!;

        /// <summary>
        /// 공통코드 추가 버튼
        /// </summary>
        private Button btnAddCode = null!;

        /// <summary>
        /// 저장 버튼
        /// </summary>
        private Button btnSave = null!;

        /// <summary>
        /// 삭제 버튼
        /// </summary>
        private Button btnDelete = null!;

        /// <summary>
        /// 새로고침 버튼
        /// </summary>
        private Button btnRefresh = null!;

        /// <summary>
        /// 닫기 버튼
        /// </summary>
        private Button btnClose = null!;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// CommonCodeManagementForm 생성자
        /// </summary>
        public CommonCodeManagementForm()
        {
            // 서비스 초기화
            _databaseService = new DatabaseService();
            _commonCodeRepository = new CommonCodeRepository(_databaseService);

            InitializeComponent();
            InitializeUI();
            LoadDataAsync();
        }

        #endregion

        #region UI 초기화 (UI Initialization)

        /// <summary>
        /// 폼 기본 설정을 초기화하는 메서드
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
            this.Text = "🔧 공통코드 관리";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new Size(1000, 700);
            this.BackColor = Color.FromArgb(240, 244, 248);

            // 타이틀 라벨
            var titleLabel = new Label
            {
                Text = "🔧 공통코드 관리 시스템",
                Location = new Point(20, 20),
                Size = new Size(1160, 40),
                Font = new Font("맑은 고딕", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 왼쪽 패널 (TreeView)
            var leftPanel = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(300, 600),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // TreeView 라벨
            var treeViewLabel = new Label
            {
                Text = "📁 그룹코드 목록",
                Location = new Point(10, 10),
                Size = new Size(280, 25),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            // TreeView 생성
            treeViewGroupCodes = new TreeView
            {
                Location = new Point(10, 40),
                Size = new Size(280, 520),
                Font = new Font("맑은 고딕", 9F),
                FullRowSelect = true,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                HideSelection = false,  // 포커스가 없어도 선택 상태 유지
                BackColor = Color.White,
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            treeViewGroupCodes.AfterSelect += TreeViewGroupCodes_AfterSelect;
            treeViewGroupCodes.DoubleClick += TreeViewGroupCodes_DoubleClick;
            treeViewGroupCodes.MouseClick += TreeViewGroupCodes_MouseClick;
            
            // TreeView 색상 테마 설정
            treeViewGroupCodes.SelectedImageIndex = 0;
            treeViewGroupCodes.SelectedNode = null;

            // 새 그룹코드 추가 버튼
            btnAddGroup = CreateModernButton("➕ 새 그룹", new Point(10, 570), new Size(135, 25), Color.FromArgb(52, 152, 219));
            btnAddGroup.Click += BtnAddGroup_Click;

            // 그룹코드 새로고침 버튼
            btnRefresh = CreateModernButton("🔄 새로고침", new Point(155, 570), new Size(135, 25), Color.FromArgb(155, 89, 182));
            btnRefresh.Click += BtnRefresh_Click;

            // 왼쪽 패널에 컨트롤 추가
            leftPanel.Controls.AddRange(new Control[] { treeViewLabel, treeViewGroupCodes, btnAddGroup, btnRefresh });

            // 오른쪽 패널 (DataGridView)
            var rightPanel = new Panel
            {
                Location = new Point(340, 80),
                Size = new Size(840, 600),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // DataGridView 라벨
            var dataGridViewLabel = new Label
            {
                Text = "📋 공통코드 상세",
                Location = new Point(10, 10),
                Size = new Size(820, 25),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            // DataGridView 생성
            dataGridViewCodes = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(820, 480),
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
            dataGridViewCodes.CellValueChanged += DataGridViewCodes_CellValueChanged;
            dataGridViewCodes.CellBeginEdit += DataGridViewCodes_CellBeginEdit;

            // 하단 버튼 패널
            var buttonPanel = new Panel
            {
                Location = new Point(10, 530),
                Size = new Size(820, 60),
                BackColor = Color.Transparent
            };

            // 공통코드 추가 버튼
            btnAddCode = CreateModernButton("➕ 코드 추가", new Point(10, 15), new Size(120, 35), Color.FromArgb(46, 204, 113));
            btnAddCode.Click += BtnAddCode_Click;

            // 저장 버튼
            btnSave = CreateModernButton("💾 저장", new Point(150, 15), new Size(120, 35), Color.FromArgb(52, 152, 219));
            btnSave.Click += BtnSave_Click;

            // 삭제 버튼
            btnDelete = CreateModernButton("🗑️ 삭제", new Point(290, 15), new Size(120, 35), Color.FromArgb(231, 76, 60));
            btnDelete.Click += BtnDelete_Click;

            // 닫기 버튼
            btnClose = CreateModernButton("❌ 닫기", new Point(690, 15), new Size(120, 35), Color.FromArgb(149, 165, 166));
            btnClose.Click += (sender, e) => this.Close();

            // 버튼들을 패널에 추가
            buttonPanel.Controls.AddRange(new Control[] { btnAddCode, btnSave, btnDelete, btnClose });

            // 오른쪽 패널에 컨트롤 추가
            rightPanel.Controls.AddRange(new Control[] { dataGridViewLabel, dataGridViewCodes, buttonPanel });

            // 모든 컨트롤을 폼에 추가
            this.Controls.AddRange(new Control[] { titleLabel, leftPanel, rightPanel });

            // 초기 상태 설정
            SetButtonStates(false);
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
                FlatAppearance = { BorderSize = 0 },
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 호버 효과
            button.MouseEnter += (sender, e) =>
            {
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

            return button;
        }

        #endregion

        #region 데이터 로딩 (Data Loading)

        /// <summary>
        /// 데이터를 비동기로 로드하는 메서드
        /// </summary>
        private async void LoadDataAsync()
        {
            try
            {
                // LogManagerService.LogInfo("공통코드 데이터 로드 시작");
                
                // 데이터베이스 연결 테스트
                await TestDatabaseConnectionAsync();
                
                await LoadGroupCodesAsync();
                // LogManagerService.LogInfo("공통코드 데이터 로드 완료");
            }
            catch (Exception _)
            {
                // LogManagerService.LogError($"공통코드 데이터 로드 중 오류: {_.Message}");
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {_.Message}\n\n" +
                    "해결 방법:\n" +
                    "1. 설정 탭에서 '테이블 생성' 버튼을 먼저 클릭하세요.\n" +
                    "2. 데이터베이스 연결을 확인하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 데이터베이스 연결 및 테이블 존재 여부를 테스트하는 메서드
        /// </summary>
        private async Task TestDatabaseConnectionAsync()
        {
            try
            {
                // 테이블 존재 여부 확인
                var checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'CommonCode'";

                var result = await _databaseService.ExecuteScalarAsync(checkTableQuery);
                var tableExists = Convert.ToInt32(result) > 0;

                // LogManagerService.LogInfo($"CommonCode 테이블 존재 여부: {tableExists}");

                if (!tableExists)
                {
                    throw new Exception("CommonCode 테이블이 존재하지 않습니다. 설정 탭에서 '테이블 생성' 버튼을 먼저 클릭하세요.");
                }

                // 데이터 개수 확인
                var countQuery = "SELECT COUNT(*) FROM CommonCode";
                var dataCount = await _databaseService.ExecuteScalarAsync(countQuery);
                // LogManagerService.LogInfo($"CommonCode 테이블 데이터 개수: {dataCount}");

                if (Convert.ToInt32(dataCount) == 0)
                {
                    // LogManagerService.LogWarning("CommonCode 테이블에 데이터가 없습니다. 샘플 데이터를 추가하겠습니다.");
                    await InsertSampleDataIfEmptyAsync();
                }
            }
            catch
            {
                // LogManagerService.LogError($"데이터베이스 연결 테스트 중 오류");
                throw;
            }
        }

        /// <summary>
        /// 데이터가 없을 때 샘플 데이터를 추가하는 메서드
        /// </summary>
        private async Task InsertSampleDataIfEmptyAsync()
        {
            try
            {
                var sampleDataQuery = @"
                    INSERT INTO CommonCode (GroupCode, Code, CodeName, Description, SortOrder, IsUsed, CreatedBy, CreatedAt) VALUES
                    ('USER_ROLE', 'ADMIN', '관리자', '시스템 관리자 권한', 1, 1, 'SYSTEM', NOW()),
                    ('USER_ROLE', 'USER', '일반사용자', '일반 사용자 권한', 2, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'PENDING', '주문접수', '주문이 접수된 상태', 1, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'PROCESSING', '처리중', '주문이 처리 중인 상태', 2, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'COMPLETED', '완료', '주문 처리가 완료된 상태', 3, 1, 'SYSTEM', NOW()),
                    ('ORDER_STATUS', 'CANCELLED', '취소', '주문이 취소된 상태', 4, 1, 'SYSTEM', NOW())";

                var result = await _databaseService.ExecuteNonQueryAsync(sampleDataQuery);
                // LogManagerService.LogInfo($"샘플 데이터 {result}개 추가 완료");
            }
            catch
            {
                // LogManagerService.LogError($"샘플 데이터 추가 중 오류");
                // 샘플 데이터 추가 실패는 치명적이지 않으므로 예외를 다시 던지지 않음
            }
        }

        /// <summary>
        /// 그룹코드 목록을 로드하는 메서드
        /// </summary>
        private async Task LoadGroupCodesAsync()
        {
            try
            {
                // LogManagerService.LogInfo("그룹코드 목록 로드 시작");
                treeViewGroupCodes.Nodes.Clear();
                var groupCodes = await _commonCodeRepository.GetAllGroupCodesAsync();

                // LogManagerService.LogInfo($"그룹코드 {groupCodes.Count}개 조회됨: [{string.Join(", ", groupCodes)}]");

                foreach (var groupCode in groupCodes)
                {
                    var node = new TreeNode(groupCode)
                    {
                        Tag = groupCode
                    };
                    treeViewGroupCodes.Nodes.Add(node);
                    // LogManagerService.LogInfo($"TreeView에 그룹코드 '{groupCode}' 노드 추가");
                }

                if (groupCodes.Count > 0)
                {
                    // 첫 번째 노드를 선택하고 해당 그룹의 공통코드 로드
                    var firstNode = treeViewGroupCodes.Nodes[0];
                    treeViewGroupCodes.SelectedNode = firstNode;
                    // LogManagerService.LogInfo($"첫 번째 노드 '{firstNode.Text}' 선택");
                    
                    // 첫 번째 노드 선택 시 색상 적용
                    UpdateTreeNodeSelection(firstNode);
                    
                    // 수동으로 첫 번째 그룹의 공통코드 로드
                    if (firstNode.Tag is string firstGroupCode)
                    {
                        _selectedGroupCode = firstGroupCode;
                        // LogManagerService.LogInfo($"첫 번째 그룹코드 '{firstGroupCode}'의 공통코드 로드 시작");
                        await LoadCommonCodesAsync(firstGroupCode);
                    }
                }
                else
                {
                    // LogManagerService.LogWarning("그룹코드가 없습니다. 그리드를 초기화합니다.");
                    // 그룹코드가 없을 때 그리드 초기화
                    ConfigureDataGridView();
                    dataGridViewCodes.Rows.Clear();
                    SetButtonStates(false);
                    
                    MessageBox.Show("공통코드 데이터가 없습니다.\n설정 탭에서 '테이블 생성' 버튼을 클릭하여 샘플 데이터를 추가하세요.", 
                        "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception _)
            {
                // LogManagerService.LogError($"그룹코드 로드 중 오류: {_.Message}");
                MessageBox.Show($"그룹코드 로드 중 오류가 발생했습니다: {_.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 오류 발생 시 그리드 초기화
                ConfigureDataGridView();
                dataGridViewCodes.Rows.Clear();
                SetButtonStates(false);
            }
        }

        /// <summary>
        /// 선택된 그룹코드의 공통코드 목록을 로드하는 메서드
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        private async Task LoadCommonCodesAsync(string groupCode)
        {
            try
            {
                // LogManagerService.LogInfo($"공통코드 로드 시작 - 그룹코드: '{groupCode}'");

                if (string.IsNullOrEmpty(groupCode))
                {
                    // LogManagerService.LogWarning("그룹코드가 비어있음. 그리드를 초기화합니다.");
                    ConfigureDataGridView();
                    dataGridViewCodes.Rows.Clear();
                    SetButtonStates(false);
                    return;
                }

                var commonCodes = await _commonCodeRepository.GetCommonCodesByGroupAsync(groupCode);
                // LogManagerService.LogInfo($"그룹코드 '{groupCode}'에서 {commonCodes.Count}개의 공통코드 조회됨");

                if (commonCodes.Count > 0)
                {
                    foreach (var code in commonCodes)
                    {
                        // LogManagerService.LogInfo($"  - {code.Code}: {code.CodeName}");
                    }
                }

                _originalData = commonCodes.Select(c => c.Clone()).ToList();
                
                // LogManagerService.LogInfo("DataGridView 구성 시작");
                ConfigureDataGridView();
                
                // LogManagerService.LogInfo("DataGridView에 데이터 채우기 시작");
                PopulateDataGridView(commonCodes);
                
                // LogManagerService.LogInfo("버튼 상태 설정");
                SetButtonStates(true);
                
                // LogManagerService.LogInfo($"그룹코드 '{groupCode}'의 공통코드 {commonCodes.Count}개 로드 완료");
            }
            catch (Exception _)
            {
                // LogManagerService.LogError($"공통코드 로드 중 오류 (그룹코드: {groupCode}): {_.Message}");
                // LogManagerService.LogError($"스택 트레이스: {_.StackTrace}");
                MessageBox.Show($"공통코드 로드 중 오류가 발생했습니다: {_.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 오류 발생 시 그리드 초기화
                ConfigureDataGridView();
                dataGridViewCodes.Rows.Clear();
                SetButtonStates(false);
            }
        }

        /// <summary>
        /// DataGridView를 구성하는 메서드
        /// </summary>
        private void ConfigureDataGridView()
        {
            dataGridViewCodes.Columns.Clear();

            // 컬럼 추가
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
        /// <param name="commonCodes">공통코드 목록</param>
        private void PopulateDataGridView(List<CommonCode> commonCodes)
        {
            try
            {
                // LogManagerService.LogInfo($"DataGridView에 {commonCodes.Count}개의 행 추가 시작");
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

                    // 태그에 원본 데이터 저장
                    row.Tag = commonCode;
                    
                    // LogManagerService.LogInfo($"  행 {rowIndex + 1}: {commonCode.GroupCode}.{commonCode.Code} - {commonCode.CodeName}");
                }

                // LogManagerService.LogInfo($"DataGridView에 총 {dataGridViewCodes.Rows.Count}개 행 추가 완료");
            }
            catch
            {
                // LogManagerService.LogError($"DataGridView 데이터 채우기 중 오류");
                throw;
            }
        }

        #endregion

        #region 이벤트 핸들러 (Event Handlers)

        /// <summary>
        /// TreeView에서 그룹코드 선택 시 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void TreeViewGroupCodes_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            try
            {
                // 선택된 노드의 색상 업데이트
                UpdateTreeNodeSelection(e.Node);

                if (e.Node?.Tag is string groupCode && !string.IsNullOrEmpty(groupCode))
                {
                    _selectedGroupCode = groupCode;
                    await LoadCommonCodesAsync(groupCode);
                }
            }
            catch (Exception _)
            {
                // LogManagerService.LogError($"TreeView 선택 이벤트 처리 중 오류: {_.Message}");
                MessageBox.Show($"그룹코드 선택 중 오류가 발생했습니다: {_.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// TreeView에서 그룹코드 더블클릭 시 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void TreeViewGroupCodes_DoubleClick(object? sender, EventArgs e)
        {
            try
            {
                if (treeViewGroupCodes.SelectedNode?.Tag is string groupCode && !string.IsNullOrEmpty(groupCode))
                {
                    _selectedGroupCode = groupCode;
                    await LoadCommonCodesAsync(groupCode);
                }
            }
            catch (Exception _)
            {
                // LogManagerService.LogError($"TreeView 더블클릭 이벤트 처리 중 오류: {_.Message}");
                MessageBox.Show($"그룹코드 더블클릭 중 오류가 발생했습니다: {_.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// TreeView에서 마우스 클릭 시 이벤트 (우클릭 메뉴 처리)
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void TreeViewGroupCodes_MouseClick(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    // 클릭된 위치의 노드 찾기
                    var clickedNode = treeViewGroupCodes.GetNodeAt(e.X, e.Y);
                    if (clickedNode != null)
                    {
                        // 노드 선택
                        treeViewGroupCodes.SelectedNode = clickedNode;
                        
                        // 우클릭 메뉴 표시
                        ShowTreeNodeContextMenu(clickedNode, e.Location);
                    }
                }
            }
            catch
            {
                // LogManagerService.LogError($"TreeView 마우스 클릭 이벤트 처리 중 오류");
            }
        }

        /// <summary>
        /// TreeView 노드 우클릭 메뉴를 표시하는 메서드
        /// </summary>
        /// <param name="node">선택된 노드</param>
        /// <param name="location">메뉴 표시 위치</param>
        private void ShowTreeNodeContextMenu(TreeNode node, Point location)
        {
            try
            {
                var contextMenu = new ContextMenuStrip();
                
                // 그룹코드 삭제 메뉴
                var deleteMenuItem = new ToolStripMenuItem("🗑️ 그룹코드 삭제", null, async (sender, e) =>
                {
                    await DeleteGroupCodeAsync(node);
                });
                // [한글 주석] 요구사항: 삭제 기능은 제거하지 말고 사용 불가 처리 (메뉴 숨김)
                deleteMenuItem.Visible = false;
                
                contextMenu.Items.Add(deleteMenuItem);
                
                // 메뉴 표시
                contextMenu.Show(treeViewGroupCodes, location);
            }
            catch
            {
                // LogManagerService.LogError($"우클릭 메뉴 표시 중 오류");
            }
        }

        /// <summary>
        /// 그룹코드를 삭제하는 메서드
        /// </summary>
        /// <param name="node">삭제할 그룹코드 노드</param>
        private async Task DeleteGroupCodeAsync(TreeNode node)
        {
            try
            {
                if (node?.Tag is not string groupCode || string.IsNullOrEmpty(groupCode))
                {
                    MessageBox.Show("삭제할 그룹코드 정보가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var result = MessageBox.Show(
                    $"정말로 그룹코드 '{groupCode}'와 관련된 모든 공통코드를 삭제하시겠습니까?\n\n" +
                    "⚠️ 이 작업은 되돌릴 수 없습니다!",
                    "그룹코드 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // LogManagerService.LogInfo($"그룹코드 삭제 시작: {groupCode}");
                    
                    var deleteResult = await _commonCodeRepository.DeleteGroupCodeAsync(groupCode);
                    
                    if (deleteResult)
                    {
                        MessageBox.Show($"그룹코드 '{groupCode}'가 성공적으로 삭제되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // TreeView에서 노드 제거
                        treeViewGroupCodes.Nodes.Remove(node);
                        
                        // 그룹코드 목록 새로고침
                        await LoadGroupCodesAsync();
                    }
                    else
                    {
                        MessageBox.Show($"그룹코드 '{groupCode}' 삭제에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception _)
            {
                // LogManagerService.LogError($"그룹코드 삭제 중 오류: {_.Message}");
                // LogManagerService.LogError($"스택 트레이스: {_.StackTrace}");
                
                var errorMessage = _.Message;
                if (_.InnerException != null)
                {
                    errorMessage += $"\n\n상세 오류: {_.InnerException.Message}";
                }
                
                MessageBox.Show($"그룹코드 삭제 중 오류가 발생했습니다:\n\n{errorMessage}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 새 그룹코드 추가 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnAddGroup_Click(object? sender, EventArgs e)
        {
            try
            {
                var groupCode = Microsoft.VisualBasic.Interaction.InputBox(
                    "새 그룹코드를 입력하세요:", "새 그룹코드 추가", "");

                if (!string.IsNullOrWhiteSpace(groupCode))
                {
                    // 그룹코드명도 입력받기
                    var groupCodeName = Microsoft.VisualBasic.Interaction.InputBox(
                        "그룹코드명을 입력하세요 (선택사항):", "그룹코드명 입력", groupCode);

                    // 데이터베이스에 새 그룹코드 추가
                    var success = await _commonCodeRepository.AddGroupCodeAsync(groupCode, groupCodeName);
                    
                    if (success)
                    {
                        // TreeView에 노드 추가
                        var node = new TreeNode(groupCode) { Tag = groupCode };
                        treeViewGroupCodes.Nodes.Add(node);
                        treeViewGroupCodes.SelectedNode = node;
                        
                        // 새로 추가된 노드 선택 시 색상 적용
                        UpdateTreeNodeSelection(node);
                        
                        // 새로 추가된 그룹의 공통코드 로드
                        _selectedGroupCode = groupCode;
                        await LoadCommonCodesAsync(groupCode);
                        
                        MessageBox.Show($"새 그룹코드 '{groupCode}'가 성공적으로 추가되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"그룹코드 '{groupCode}'가 이미 존재합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception _)
            {
                // LogManagerService.LogError($"새 그룹코드 추가 중 오류: {_.Message}");
                MessageBox.Show($"새 그룹코드 추가 중 오류가 발생했습니다: {_.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 새로고침 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnRefresh_Click(object? sender, EventArgs e)
        {
            try
            {
                await LoadGroupCodesAsync();
                if (!string.IsNullOrEmpty(_selectedGroupCode))
                {
                    await LoadCommonCodesAsync(_selectedGroupCode);
                }
            }
            catch (Exception _)
            {
                MessageBox.Show($"새로고침 중 오류가 발생했습니다: {_.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 공통코드 추가 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void BtnAddCode_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedGroupCode))
            {
                MessageBox.Show("먼저 그룹코드를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var newCode = new CommonCode(_selectedGroupCode)
            {
                Code = $"NEW_{DateTime.Now:yyyyMMddHHmmss}",
                CodeName = "새 코드",
                SortOrder = _originalData.Count + 1,
                CreatedBy = Environment.UserName
            };

            var rowIndex = dataGridViewCodes.Rows.Add();
            var row = dataGridViewCodes.Rows[rowIndex];

            row.Cells["GroupCode"].Value = newCode.GroupCode;
            row.Cells["Code"].Value = newCode.Code;
            row.Cells["CodeName"].Value = newCode.CodeName;
            row.Cells["Description"].Value = newCode.Description ?? string.Empty;
            row.Cells["SortOrder"].Value = newCode.SortOrder;
            row.Cells["IsUsed"].Value = newCode.IsUsed;
            row.Cells["Attribute1"].Value = newCode.Attribute1 ?? string.Empty;
            row.Cells["Attribute2"].Value = newCode.Attribute2 ?? string.Empty;

            row.Tag = newCode;
            _isDataModified = true;
            SetButtonStates(true);
        }

        /// <summary>
        /// 저장 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                // 비밀번호 확인
                if (!ValidatePassword())
                {
                    return;
                }

                var commonCodes = GetCommonCodesFromDataGridView();
                
                if (commonCodes.Count == 0)
                {
                    MessageBox.Show("저장할 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = await _commonCodeRepository.SaveCommonCodesAsync(commonCodes);
                
                if (result)
                {
                    MessageBox.Show("공통코드가 성공적으로 저장되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _isDataModified = false;
                    
                    // 데이터 새로고침
                    if (!string.IsNullOrEmpty(_selectedGroupCode))
                    {
                        await LoadCommonCodesAsync(_selectedGroupCode);
                    }
                }
                else
                {
                    MessageBox.Show("공통코드 저장에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception _)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {_.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // LogManagerService.LogError($"공통코드 저장 중 오류: {_.Message}");
            }
        }

        /// <summary>
        /// 삭제 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dataGridViewCodes.SelectedRows.Count == 0)
            {
                MessageBox.Show("삭제할 행을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 비밀번호 확인
            if (!ValidatePassword())
            {
                return;
            }

            var result = MessageBox.Show(
                "정말로 선택된 공통코드를 삭제하시겠습니까?", 
                "삭제 확인", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var selectedRow = dataGridViewCodes.SelectedRows[0];
                    var groupCode = selectedRow.Cells["GroupCode"].Value?.ToString() ?? string.Empty;
                    var code = selectedRow.Cells["Code"].Value?.ToString() ?? string.Empty;

                    // LogManagerService.LogInfo($"공통코드 삭제 시작: {groupCode}.{code}");

                    // 삭제 전 유효성 검사
                    if (string.IsNullOrEmpty(groupCode) || string.IsNullOrEmpty(code))
                    {
                        MessageBox.Show("삭제할 공통코드 정보가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var deleteResult = await _commonCodeRepository.DeleteCommonCodeAsync(groupCode, code);
                    
                    if (deleteResult)
                    {
                        MessageBox.Show("공통코드가 성공적으로 삭제되었습니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // 데이터 새로고침
                        if (!string.IsNullOrEmpty(_selectedGroupCode))
                        {
                            await LoadCommonCodesAsync(_selectedGroupCode);
                        }
                    }
                    else
                    {
                        MessageBox.Show("공통코드 삭제에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception _)
                {
                    // LogManagerService.LogError($"공통코드 삭제 중 오류: {_.Message}");
                    // LogManagerService.LogError($"스택 트레이스: {_.StackTrace}");
                    
                    var errorMessage = _.Message;
                    if (_.InnerException != null)
                    {
                        errorMessage += $"\n\n상세 오류: {_.InnerException.Message}";
                    }
                    
                    MessageBox.Show($"삭제 중 오류가 발생했습니다:\n\n{errorMessage}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// DataGridView 셀 값 변경 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void DataGridViewCodes_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                _isDataModified = true;
                SetButtonStates(true);
            }
        }

        /// <summary>
        /// DataGridView 셀 편집 시작 이벤트
        /// </summary>
        /// <param name="sender">이벤트 발생 객체</param>
        /// <param name="e">이벤트 인수</param>
        private void DataGridViewCodes_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            // GroupCode는 편집 불가
            if (e.ColumnIndex == dataGridViewCodes.Columns["GroupCode"].Index)
            {
                e.Cancel = true;
            }
        }

        #endregion

        #region 헬퍼 메서드 (Helper Methods)

        /// <summary>
        /// DataGridView에서 공통코드 목록을 추출하는 메서드
        /// </summary>
        /// <returns>공통코드 목록</returns>
        private List<CommonCode> GetCommonCodesFromDataGridView()
        {
            var commonCodes = new List<CommonCode>();

            foreach (DataGridViewRow row in dataGridViewCodes.Rows)
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
        /// 버튼 상태를 설정하는 메서드
        /// </summary>
        /// <param name="hasData">데이터가 있는지 여부</param>
        private void SetButtonStates(bool hasData)
        {
            btnAddCode.Enabled = hasData;
            btnSave.Enabled = hasData && _isDataModified;
            btnDelete.Enabled = hasData && dataGridViewCodes.SelectedRows.Count > 0;
        }

        /// <summary>
        /// TreeView 노드의 색상을 설정하는 메서드
        /// </summary>
        /// <param name="node">색상을 설정할 노드</param>
        /// <param name="isSelected">선택된 상태인지 여부</param>
        private void SetTreeNodeColor(TreeNode node, bool isSelected)
        {
            if (node == null) return;

            if (isSelected)
            {
                // 선택된 노드: 파란색 배경, 흰색 텍스트
                node.BackColor = Color.FromArgb(52, 152, 219);
                node.ForeColor = Color.White;
            }
            else
            {
                // 기본 상태: 흰색 배경, 검은색 텍스트
                node.BackColor = Color.White;
                node.ForeColor = Color.FromArgb(52, 73, 94);
            }
        }

        /// <summary>
        /// 모든 TreeView 노드의 색상을 기본 상태로 초기화하는 메서드
        /// </summary>
        private void ResetAllTreeNodeColors()
        {
            foreach (TreeNode node in treeViewGroupCodes.Nodes)
            {
                SetTreeNodeColor(node, false);
            }
        }

        /// <summary>
        /// TreeView 노드 선택 시 색상을 업데이트하는 메서드
        /// </summary>
        /// <param name="selectedNode">선택된 노드</param>
        private void UpdateTreeNodeSelection(TreeNode? selectedNode)
        {
            // 모든 노드 색상 초기화
            ResetAllTreeNodeColors();

            // 선택된 노드 색상 변경
            if (selectedNode != null)
            {
                SetTreeNodeColor(selectedNode, true);
            }
        }

        /// <summary>
        /// 비밀번호 확인 메서드
        /// </summary>
        /// <returns>비밀번호가 올바르면 true, 그렇지 않으면 false</returns>
        private bool ValidatePassword()
        {
            const string correctPassword = "gram0904";
            
            // 커스텀 비밀번호 입력 다이얼로그 생성
            var passwordForm = new Form
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                Text = "비밀번호 확인"
            };

            var label = new Label
            {
                Text = "관리자 비밀번호를 입력하세요:",
                Left = 20,
                Top = 20,
                Width = 260
            };

            var textBox = new TextBox
            {
                Left = 20,
                Top = 45,
                Width = 260,
                PasswordChar = '*',
                UseSystemPasswordChar = true
            };

            var okButton = new Button
            {
                Text = "확인",
                Left = 100,
                Top = 75,
                Width = 80,
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "취소",
                Left = 190,
                Top = 75,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            passwordForm.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            passwordForm.AcceptButton = okButton;
            passwordForm.CancelButton = cancelButton;

            // 텍스트박스에 포커스 설정
            passwordForm.Load += (s, e) => textBox.Focus();

            var result = passwordForm.ShowDialog();
            var inputPassword = textBox.Text;

            if (result != DialogResult.OK || string.IsNullOrEmpty(inputPassword))
            {
                MessageBox.Show("비밀번호를 입력해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (inputPassword != correctPassword)
            {
                MessageBox.Show("비밀번호가 올바르지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        #endregion
    }
}
