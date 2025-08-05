using OfficeOpenXml;
using System.Data;
using System.Configuration;

namespace LogisticManager.Services
{
    /// <summary>
    /// 파일 처리(Excel 읽기/쓰기)를 담당하는 서비스 클래스
    /// 
    /// 주요 기능:
    /// - Excel 파일을 DataTable로 읽기
    /// - DataTable을 Excel 파일로 저장
    /// - 파일 선택 대화상자 제공
    /// - 출력 파일 경로 생성
    /// - 디렉토리 존재 확인 및 생성
    /// 
    /// 사용 라이브러리:
    /// - EPPlus (Excel 파일 처리)
    /// - System.Data (DataTable 사용)
    /// 
    /// 설정 파일:
    /// - App.config에서 InputFolderPath, OutputFolderPath 읽기
    /// </summary>
    public class FileService
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 입력 파일들이 저장되는 기본 폴더 경로
        /// App.config에서 읽어오며, 기본값은 "C:\Work\Input\"
        /// </summary>
        private readonly string _inputFolderPath;
        
        /// <summary>
        /// 처리된 파일들이 저장되는 기본 폴더 경로
        /// App.config에서 읽어오며, 기본값은 "C:\Work\Output\"
        /// </summary>
        private readonly string _outputFolderPath;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// FileService 생성자
        /// 
        /// 초기화 작업:
        /// 1. App.config에서 폴더 경로 설정 읽기
        /// 2. EPPlus 라이센스 설정 (NonCommercial)
        /// </summary>
        public FileService()
        {
            // App.config에서 파일 경로들을 읽어옴
            _inputFolderPath = ConfigurationManager.AppSettings["InputFolderPath"] ?? "C:\\Work\\Input\\";
            _outputFolderPath = ConfigurationManager.AppSettings["OutputFolderPath"] ?? "C:\\Work\\Output\\";
            
            // EPPlus 라이센스 설정 (상업용 사용 시 필요)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        #endregion

        #region Excel 파일 읽기 (Excel File Reading)

        /// <summary>
        /// Excel 파일을 읽어서 DataTable로 변환하는 메서드
        /// 
        /// 처리 과정:
        /// 1. 파일 존재 여부 확인
        /// 2. EPPlus를 사용하여 Excel 파일 열기
        /// 3. 첫 번째 워크시트 선택
        /// 4. 헤더 행을 읽어서 DataTable 컬럼 생성
        /// 5. 데이터 행들을 읽어서 DataTable에 추가
        /// 6. 빈 행은 제외하고 유효한 데이터만 반환
        /// 
        /// 예외 처리:
        /// - FileNotFoundException: 파일이 존재하지 않는 경우
        /// - InvalidOperationException: 워크시트가 없거나 데이터가 없는 경우
        /// </summary>
        /// <param name="filePath">읽을 Excel 파일의 전체 경로</param>
        /// <returns>Excel 데이터를 담은 DataTable 객체</returns>
        /// <exception cref="FileNotFoundException">파일이 존재하지 않는 경우</exception>
        /// <exception cref="InvalidOperationException">Excel 파일에 워크시트가 없거나 데이터가 없는 경우</exception>
        public DataTable ReadExcelToDataTable(string filePath)
        {
            try
            {
                // 파일 존재 여부 확인
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Excel 파일을 찾을 수 없습니다: {filePath}");
                }

                // DataTable 객체 생성
                var dataTable = new DataTable();

                // EPPlus를 사용하여 Excel 파일 열기
                using var package = new ExcelPackage(new FileInfo(filePath));
                
                // 첫 번째 워크시트 선택 (없으면 예외 발생)
                var worksheet = package.Workbook.Worksheets.FirstOrDefault() 
                    ?? throw new InvalidOperationException("Excel 파일에 워크시트가 없습니다.");

                // 워크시트의 크기 정보 가져오기
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;

                // 데이터가 없는 경우 예외 발생
                if (rowCount == 0 || colCount == 0)
                {
                    throw new InvalidOperationException("Excel 파일에 데이터가 없습니다.");
                }

                // 헤더 행을 읽어서 컬럼 생성 (첫 번째 행)
                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                    dataTable.Columns.Add(headerValue);
                }

                // 데이터 행들을 읽어서 DataTable에 추가 (두 번째 행부터)
                for (int row = 2; row <= rowCount; row++)
                {
                    var dataRow = dataTable.NewRow();
                    bool hasData = false;  // 빈 행 체크용 플래그

                    // 각 셀의 값을 읽어서 DataRow에 설정
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
                        dataRow[col - 1] = cellValue;  // DataTable은 0부터 시작
                        
                        // 빈 행 체크 (하나라도 데이터가 있으면 유효한 행)
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            hasData = true;
                        }
                    }

                    // 빈 행이 아닌 경우만 DataTable에 추가
                    if (hasData)
                    {
                        dataTable.Rows.Add(dataRow);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                // 예외를 래핑하여 더 명확한 오류 메시지 제공
                throw new InvalidOperationException($"Excel 파일 읽기 중 오류 발생: {ex.Message}", ex);
            }
        }

        #endregion

        #region Excel 파일 저장 (Excel File Writing)

        /// <summary>
        /// DataTable을 Excel 파일로 저장하는 메서드
        /// 
        /// 처리 과정:
        /// 1. 출력 디렉토리 존재 여부 확인 및 생성
        /// 2. EPPlus를 사용하여 새로운 Excel 패키지 생성
        /// 3. 워크시트 생성 및 이름 설정
        /// 4. 헤더 행 작성 (굵은 글씨로 설정)
        /// 5. 데이터 행들을 셀에 작성
        /// 6. 자동 열 너비 조정
        /// 7. 파일로 저장
        /// 
        /// 파일 형식:
        /// - .xlsx (Excel 2007 이상)
        /// - 자동 열 너비 조정
        /// - 헤더는 굵은 글씨로 표시
        /// </summary>
        /// <param name="dataTable">저장할 데이터가 담긴 DataTable</param>
        /// <param name="filePath">저장할 파일의 전체 경로</param>
        /// <param name="sheetName">워크시트 이름 (기본값: "Sheet1")</param>
        /// <returns>저장 성공 여부 (true: 성공, false: 실패)</returns>
        /// <exception cref="InvalidOperationException">파일 저장 중 오류가 발생한 경우</exception>
        public bool SaveDataTableToExcel(DataTable dataTable, string filePath, string sheetName = "Sheet1")
        {
            try
            {
                // 출력 디렉토리가 없으면 생성
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // EPPlus를 사용하여 새로운 Excel 패키지 생성
                using var package = new ExcelPackage();
                
                // 워크시트 생성 및 이름 설정
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                // 헤더 행 작성 (첫 번째 행)
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = dataTable.Columns[col].ColumnName;
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true;  // 굵은 글씨로 설정
                }

                // 데이터 행들을 셀에 작성 (두 번째 행부터)
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        // DataTable의 값을 셀에 설정 (null 처리 포함)
                        worksheet.Cells[row + 2, col + 1].Value = dataTable.Rows[row][col]?.ToString() ?? string.Empty;
                    }
                }

                // 자동 열 너비 조정 (내용에 맞게)
                worksheet.Cells.AutoFitColumns();

                // 파일로 저장
                package.SaveAs(new FileInfo(filePath));
                return true;
            }
            catch (Exception ex)
            {
                // 예외를 래핑하여 더 명확한 오류 메시지 제공
                throw new InvalidOperationException($"Excel 파일 저장 중 오류 발생: {ex.Message}", ex);
            }
        }

        #endregion

        #region 파일 관리 (File Management)

        /// <summary>
        /// 파일 선택 대화상자를 통해 Excel 파일을 선택하는 메서드
        /// 
        /// 기능:
        /// - Excel 파일만 필터링 (.xlsx, .xls)
        /// - 기본 디렉토리를 입력 폴더로 설정
        /// - 사용자가 취소하면 null 반환
        /// 
        /// 반환값:
        /// - 선택된 파일의 전체 경로 (문자열)
        /// - 취소한 경우 null
        /// </summary>
        /// <returns>선택된 파일의 전체 경로 또는 null (취소한 경우)</returns>
        public string? SelectExcelFile()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls|모든 파일 (*.*)|*.*",
                Title = "사방넷 주문 파일을 선택하세요",
                InitialDirectory = _inputFolderPath
            };

            return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// 출력 파일 경로를 생성하는 메서드
        /// 
        /// 파일명 형식:
        /// - {출고지명}_{파일명}_{타임스탬프}.xlsx
        /// - 예: "서울냉동_송장_서울냉동_20241201_143022.xlsx"
        /// 
        /// 타임스탬프 형식:
        /// - yyyyMMdd_HHmmss
        /// - 예: 20241201_143022
        /// </summary>
        /// <param name="fileName">기본 파일명 (확장자 제외)</param>
        /// <param name="centerName">출고지명 (파일명에 포함됨)</param>
        /// <returns>생성된 파일의 전체 경로</returns>
        public string GetOutputFilePath(string fileName, string centerName)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputFileName = $"{centerName}_{fileName}_{timestamp}.xlsx";
            return Path.Combine(_outputFolderPath, outputFileName);
        }

        /// <summary>
        /// 파일이 존재하는지 확인하는 메서드
        /// 
        /// 사용 목적:
        /// - 파일 업로드 전 존재 여부 확인
        /// - 파일 처리 전 유효성 검사
        /// </summary>
        /// <param name="filePath">확인할 파일의 전체 경로</param>
        /// <returns>파일 존재 여부 (true: 존재, false: 존재하지 않음)</returns>
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// 디렉토리가 존재하는지 확인하고 없으면 생성하는 메서드
        /// 
        /// 기능:
        /// - 디렉토리 존재 여부 확인
        /// - 존재하지 않으면 자동 생성
        /// - 중첩된 디렉토리도 자동 생성
        /// 
        /// 사용 시점:
        /// - 파일 저장 전 출력 디렉토리 확인
        /// - 임시 파일 저장 전 디렉토리 준비
        /// </summary>
        /// <param name="directoryPath">확인할 디렉토리의 전체 경로</param>
        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        #endregion
    }
} 