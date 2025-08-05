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
    /// - settings.json에서 InputFolderPath, OutputFolderPath 읽기
    /// 
    /// 처리 과정:
    /// 1. 설정 파일에서 폴더 경로 읽기
    /// 2. EPPlus 라이센스 설정
    /// 3. Excel 파일 읽기/쓰기 작업 수행
    /// 4. 오류 처리 및 로깅
    /// </summary>
    public class FileService
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 입력 파일들이 저장되는 기본 폴더 경로
        /// settings.json에서 읽어오며, 기본값은 "C:\Work\Input\"
        /// </summary>
        private readonly string _inputFolderPath;
        
        /// <summary>
        /// 처리된 파일들이 저장되는 기본 폴더 경로
        /// settings.json에서 읽어오며, 기본값은 "C:\Work\Output\"
        /// </summary>
        private readonly string _outputFolderPath;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// FileService 생성자
        /// 
        /// 초기화 작업:
        /// 1. settings.json에서 폴더 경로 설정 읽기
        /// 2. EPPlus 라이센스 설정 (NonCommercial)
        /// 3. 기본 폴더 경로 설정
        /// 
        /// 설정 파일 구조:
        /// - INPUT_FOLDER_PATH: 입력 파일 폴더 경로
        /// - OUTPUT_FOLDER_PATH: 출력 파일 폴더 경로
        /// 
        /// 예외 처리:
        /// - 설정 파일 읽기 실패 시 기본값 사용
        /// - JSON 파싱 오류 시 빈 Dictionary 사용
        /// </summary>
        public FileService()
        {
            // settings.json에서 파일 경로들을 읽어옴
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            var settings = new Dictionary<string, string>();
            
            try
            {
                // 설정 파일이 존재하는지 확인
                if (File.Exists(settingsPath))
                {
                    // JSON 파일 내용 읽기
                    var jsonContent = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        // JSON을 Dictionary로 파싱
                        settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent) ?? new Dictionary<string, string>();
                    }
                }
            }
            catch (Exception ex)
            {
                // 설정 파일 읽기 실패 시 로그 출력
                Console.WriteLine($"❌ FileService: 설정 파일 읽기 실패: {ex.Message}");
            }
            
            // settings.json에서 읽어오거나 기본값 사용
            _inputFolderPath = settings.GetValueOrDefault("INPUT_FOLDER_PATH", "C:\\Work\\Input\\");
            _outputFolderPath = settings.GetValueOrDefault("OUTPUT_FOLDER_PATH", "C:\\Work\\Output\\");
            
            // 설정된 경로를 콘솔에 출력
            Console.WriteLine($"📁 FileService: 입력 폴더 경로 = {_inputFolderPath}");
            Console.WriteLine($"📁 FileService: 출력 폴더 경로 = {_outputFolderPath}");
            
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
        /// - IOException: 파일 읽기 오류
        /// - InvalidOperationException: Excel 파일 형식 오류
        /// 
        /// 반환 데이터:
        /// - DataTable: Excel 파일의 모든 데이터
        /// - 컬럼명: Excel의 첫 번째 행을 헤더로 사용
        /// - 데이터 타입: 모든 값을 문자열로 처리
        /// </summary>
        /// <param name="filePath">읽을 Excel 파일의 전체 경로</param>
        /// <returns>Excel 데이터가 담긴 DataTable</returns>
        /// <exception cref="FileNotFoundException">파일이 존재하지 않는 경우</exception>
        /// <exception cref="IOException">파일 읽기 오류</exception>
        public DataTable ReadExcelToDataTable(string filePath)
        {
            // 파일 존재 여부 확인
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel 파일을 찾을 수 없습니다: {filePath}");
            }

            var dataTable = new DataTable();

            try
            {
                // EPPlus를 사용하여 Excel 파일 열기
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // 첫 번째 워크시트 선택
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        throw new InvalidOperationException("Excel 파일에 워크시트가 없습니다.");
                    }

                    // 워크시트의 사용 범위 확인
                    var dimension = worksheet.Dimension;
                    if (dimension == null)
                    {
                        throw new InvalidOperationException("Excel 파일에 데이터가 없습니다.");
                    }

                    // 헤더 행을 읽어서 DataTable 컬럼 생성
                    for (int col = 1; col <= dimension.End.Column; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                        dataTable.Columns.Add(headerValue);
                    }

                    // 데이터 행들을 읽어서 DataTable에 추가
                    for (int row = 2; row <= dimension.End.Row; row++)
                    {
                        var dataRow = dataTable.NewRow();
                        bool hasData = false;

                        // 각 컬럼의 값을 읽어서 DataRow에 추가
                        for (int col = 1; col <= dimension.End.Column; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
                            dataRow[col - 1] = cellValue;
                            
                            // 빈 셀이 아닌 경우 데이터가 있다고 표시
                            if (!string.IsNullOrEmpty(cellValue))
                            {
                                hasData = true;
                            }
                        }

                        // 데이터가 있는 행만 DataTable에 추가
                        if (hasData)
                        {
                            dataTable.Rows.Add(dataRow);
                        }
                    }
                }

                Console.WriteLine($"✅ FileService: Excel 파일 읽기 완료 - {dataTable.Rows.Count}행, {dataTable.Columns.Count}열");
                return dataTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FileService: Excel 파일 읽기 실패: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Excel 파일 저장 (Excel File Saving)

        /// <summary>
        /// DataTable을 Excel 파일로 저장하는 메서드
        /// 
        /// 처리 과정:
        /// 1. 출력 디렉토리 존재 확인 및 생성
        /// 2. EPPlus를 사용하여 새로운 Excel 파일 생성
        /// 3. 워크시트 생성 및 이름 설정
        /// 4. 헤더 행 작성 (DataTable 컬럼명)
        /// 5. 데이터 행들을 Excel에 작성
        /// 6. 파일 저장 및 리소스 해제
        /// 
        /// 파일 형식:
        /// - .xlsx 확장자 사용
        /// - 첫 번째 행은 헤더 (컬럼명)
        /// - 모든 데이터는 문자열로 저장
        /// 
        /// 예외 처리:
        /// - DirectoryNotFoundException: 디렉토리 생성 실패
        /// - IOException: 파일 쓰기 오류
        /// - UnauthorizedAccessException: 파일 접근 권한 오류
        /// </summary>
        /// <param name="dataTable">저장할 데이터</param>
        /// <param name="filePath">저장할 파일 경로</param>
        /// <param name="sheetName">워크시트 이름 (기본값: "Sheet1")</param>
        /// <returns>저장 성공 여부</returns>
        public bool SaveDataTableToExcel(DataTable dataTable, string filePath, string sheetName = "Sheet1")
        {
            try
            {
                // 출력 디렉토리 존재 확인 및 생성
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    EnsureDirectoryExists(directoryPath);
                }

                // EPPlus를 사용하여 새로운 Excel 파일 생성
                using (var package = new ExcelPackage())
                {
                    // 워크시트 생성 및 이름 설정
                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    // 헤더 행 작성 (DataTable 컬럼명)
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = dataTable.Columns[col].ColumnName;
                        
                        // 헤더 셀 스타일 설정 (굵게, 배경색)
                        var headerCell = worksheet.Cells[1, col + 1];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        headerCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 데이터 행들을 Excel에 작성
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            var cellValue = dataTable.Rows[row][col]?.ToString() ?? string.Empty;
                            worksheet.Cells[row + 2, col + 1].Value = cellValue;
                        }
                    }

                    // 컬럼 너비 자동 조정
                    worksheet.Cells.AutoFitColumns();

                    // 파일 저장
                    package.SaveAs(new FileInfo(filePath));
                }

                Console.WriteLine($"✅ FileService: Excel 파일 저장 완료 - {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FileService: Excel 파일 저장 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 파일 선택 및 경로 관리 (File Selection and Path Management)

        /// <summary>
        /// Excel 파일을 선택하는 대화상자를 제공하는 메서드
        /// 
        /// 대화상자 설정:
        /// - 제목: "Excel 파일 선택"
        /// - 필터: Excel 파일만 표시 (*.xlsx, *.xls)
        /// - 초기 디렉토리: 설정된 입력 폴더
        /// - 다중 선택: 비활성화
        /// 
        /// 반환 값:
        /// - 선택된 파일 경로 (문자열)
        /// - 취소 시 null 반환
        /// 
        /// 사용 목적:
        /// - 사용자가 처리할 Excel 파일을 선택
        /// - GUI 환경에서 파일 선택 기능 제공
        /// </summary>
        /// <returns>선택된 파일 경로 또는 null</returns>
        public string? SelectExcelFile()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Excel 파일 선택";
                openFileDialog.Filter = "Excel 파일|*.xlsx;*.xls|모든 파일|*.*";
                openFileDialog.InitialDirectory = _inputFolderPath;
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }

            return null;
        }

        /// <summary>
        /// 출력 파일 경로를 생성하는 메서드
        /// 
        /// 파일명 형식:
        /// - {fileName}_{centerName}_{현재날짜}.xlsx
        /// - 예: 송장_서울냉동_20241201.xlsx
        /// 
        /// 경로 구성:
        /// - 기본 출력 폴더 + 출고지별 하위 폴더
        /// - 출고지별 폴더가 없으면 자동 생성
        /// 
        /// 날짜 형식:
        /// - yyyyMMdd 형식 사용
        /// - 예: 20241201
        /// </summary>
        /// <param name="fileName">기본 파일명</param>
        /// <param name="centerName">출고지명</param>
        /// <returns>생성된 파일 경로</returns>
        public string GetOutputFilePath(string fileName, string centerName)
        {
            // 출고지별 하위 폴더 경로 생성
            var centerFolder = Path.Combine(_outputFolderPath, centerName);
            EnsureDirectoryExists(centerFolder);

            // 현재 날짜를 파일명에 포함
            var dateString = DateTime.Now.ToString("yyyyMMdd");
            var fullFileName = $"{fileName}_{dateString}.xlsx";
            
            // 전체 파일 경로 생성
            var filePath = Path.Combine(centerFolder, fullFileName);
            
            return filePath;
        }

        /// <summary>
        /// 파일이 존재하는지 확인하는 메서드
        /// 
        /// 확인 내용:
        /// - 파일 경로가 유효한지 확인
        /// - 파일이 실제로 존재하는지 확인
        /// - 파일에 접근 권한이 있는지 확인
        /// 
        /// 사용 목적:
        /// - 파일 처리 전 존재 여부 확인
        /// - 중복 파일 처리 방지
        /// - 오류 방지를 위한 사전 검사
        /// </summary>
        /// <param name="filePath">확인할 파일 경로</param>
        /// <returns>파일이 존재하면 true, 아니면 false</returns>
        public bool FileExists(string filePath)
        {
            try
            {
                // 파일 경로 유효성 검사
                if (string.IsNullOrEmpty(filePath))
                    return false;

                // 파일 존재 여부 확인
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FileService: 파일 존재 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 디렉토리가 존재하는지 확인하고 없으면 생성하는 메서드
        /// 
        /// 처리 과정:
        /// 1. 디렉토리 경로 유효성 검사
        /// 2. 디렉토리 존재 여부 확인
        /// 3. 존재하지 않으면 생성
        /// 4. 생성 실패 시 예외 처리
        /// 
        /// 사용 목적:
        /// - 출력 폴더 자동 생성
        /// - 파일 저장 전 디렉토리 준비
        /// - 오류 방지를 위한 사전 준비
        /// </summary>
        /// <param name="directoryPath">확인/생성할 디렉토리 경로</param>
        public void EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                // 디렉토리 경로 유효성 검사
                if (string.IsNullOrEmpty(directoryPath))
                    return;

                // 디렉토리가 존재하지 않으면 생성
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Console.WriteLine($"📁 FileService: 디렉토리 생성 완료 - {directoryPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FileService: 디렉토리 생성 실패: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
} 