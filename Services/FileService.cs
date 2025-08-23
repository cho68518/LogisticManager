using OfficeOpenXml;
using System.Data;
using System.Configuration;
using LogisticManager.Models;
using System.Linq;

namespace LogisticManager.Services
{
    /// <summary>
    /// 파일 처리(Excel 읽기/쓰기)를 담당하는 서비스 클래스
    /// 
    /// 주요 기능:
    /// - Excel 파일을 DataTable로 읽기 (ColumnMapping 적용)
    /// - 읽어온 데이터의 값 변환 및 정규화 (DataTransformationService 사용)
    /// - DataTable을 Excel 파일로 저장
    /// - 파일 선택 대화상자 제공
    /// - 출력 파일 경로 생성
    /// - 디렉토리 존재 확인 및 생성
    /// 
    /// 사용 라이브러리:
    /// - EPPlus (Excel 파일 처리)
    /// - System.Data (DataTable 사용)
    /// - MappingService (컬럼 매핑 처리)
    /// - DataTransformationService (데이터 값 변환 및 정규화)
    /// 
    /// 설정 파일:
    /// - settings.json에서 InputFolderPath, OutputFolderPath 읽기
    /// - column_mapping.json에서 매핑 설정 읽기
    /// 
    /// 처리 과정:
    /// 1. 설정 파일에서 폴더 경로 읽기
    /// 2. EPPlus 라이센스 설정
    /// 3. Excel 파일 읽기 (매핑 적용)
    /// 4. 데이터 값 변환 및 정규화 수행
    /// 5. Excel 파일 쓰기 작업 수행
    /// 6. 오류 처리 및 로깅
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

        /// <summary>
        /// 컬럼 매핑 설정을 관리하는 서비스
        /// Excel 컬럼명과 데이터베이스 컬럼명 간의 매핑 처리
        /// </summary>
        private readonly MappingService _mappingService;

        /// <summary>
        /// 데이터 변환 서비스 인스턴스
        /// 엑셀에서 읽어온 데이터의 값을 변환하고 정규화하는 서비스
        /// </summary>
        private readonly DataTransformationService _transformationService;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// FileService 생성자
        /// 
        /// 초기화 작업:
        /// 1. settings.json에서 폴더 경로 설정 읽기
        /// 2. EPPlus 라이센스 설정 (NonCommercial)
        /// 3. 기본 폴더 경로 설정
        /// 4. MappingService 인스턴스 생성
        /// 5. DataTransformationService 인스턴스 생성
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
            
            // MappingService 인스턴스 생성
            _mappingService = new MappingService();
            
            // DataTransformationService 인스턴스 생성
            _transformationService = new DataTransformationService();
        }

        #endregion

        #region Excel 파일 읽기 (Excel File Reading)

        /// <summary>
        /// Excel 파일을 읽어서 DataTable로 변환하는 메서드 (ColumnMapping 적용)
        /// 
        /// 처리 과정:
        /// 1. 파일 존재 여부 확인
        /// 2. EPPlus를 사용하여 Excel 파일 열기
        /// 3. 첫 번째 워크시트 선택
        /// 4. 헤더 행을 읽어서 매핑 설정 확인
        /// 5. 매핑된 컬럼명으로 DataTable 컬럼 생성
        /// 6. 데이터 행들을 읽어서 DataTable에 추가
        /// 7. 빈 행은 제외하고 유효한 데이터만 반환
        /// 
        /// 매핑 처리:
        /// - Excel 컬럼명을 데이터베이스 컬럼명으로 변환
        /// - 매핑되지 않은 컬럼은 원본 이름 유지
        /// - 데이터 타입 변환 적용
        /// 
        /// 예외 처리:
        /// - FileNotFoundException: 파일이 존재하지 않는 경우
        /// - IOException: 파일 읽기 오류
        /// - InvalidOperationException: Excel 파일 형식 오류
        /// 
        /// 반환 데이터:
        /// - DataTable: Excel 파일의 모든 데이터 (매핑 적용)
        /// - 컬럼명: 매핑 설정에 따른 데이터베이스 컬럼명
        /// - 데이터 타입: 매핑 설정에 따른 타입 변환
        /// </summary>
        /// <param name="filePath">읽을 Excel 파일의 전체 경로</param>
        /// <param name="tableMappingKey">테이블 매핑 키 (기본값: "order_table")</param>
        /// <returns>Excel 데이터가 담긴 DataTable (매핑 적용)</returns>
        /// <exception cref="FileNotFoundException">파일이 존재하지 않는 경우</exception>
        /// <exception cref="IOException">파일 읽기 오류</exception>
        public DataTable ReadExcelToDataTable(string filePath, string? tableMappingKey = null)
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

                    // 헤더 행을 읽어서 매핑된 컬럼명으로 DataTable 컬럼 생성
                    for (int col = 1; col <= dimension.End.Column; col++)
                    {
                        try
                        {
                            var cell = worksheet.Cells[1, col];
                            var cellValue = cell.Value;
                            var excelColumnName = cellValue?.ToString() ?? $"Column{col}";
                            
                            // 매핑 없이 Excel 컬럼명 그대로 사용
                            var columnName = excelColumnName;
                            
                            // 모든 컬럼을 문자열로 처리
                            var dataType = typeof(string);
                            dataTable.Columns.Add(columnName, dataType);
                            
                            Console.WriteLine($"📋 FileService: 컬럼 생성 - Excel: {excelColumnName} (String)");
                        }
                        catch
                        {
                            // 셀 접근 실패 시 기본 컬럼명 사용
                            var excelColumnName = $"Column{col}";
                            var columnName = excelColumnName;
                            var dataType = typeof(string);
                            dataTable.Columns.Add(columnName, dataType);
                            Console.WriteLine($"📋 FileService: 컬럼 생성 - Excel: {excelColumnName} (String) - 기본값 사용");
                        }
                    }

                    // 데이터 행들을 읽어서 DataTable에 추가
                    for (int row = 2; row <= dimension.End.Row; row++)
                    {
                        var dataRow = dataTable.NewRow();
                        bool hasData = false;

                        // 각 컬럼의 값을 읽어서 DataRow에 추가
                        for (int col = 1; col <= dimension.Columns; col++)
                        {
                            try
                            {
                                var headerCell = worksheet.Cells[1, col];
                                var headerCellValue = headerCell.Value;
                                var excelColumnName = headerCellValue?.ToString() ?? $"Column{col}";
                                var rowCell = worksheet.Cells[row, col];
                                var rowCellValue = rowCell.Value;
                                var cellValue = rowCellValue?.ToString() ?? string.Empty;
                                
                                // 매핑 없이 Excel 컬럼명 그대로 사용
                                var columnName = excelColumnName;
                                
                                // 문자열 값 그대로 사용 (변환 없음)
                                var convertedValue = cellValue;
                                
                                // 결제수단 컬럼 특별 디버깅 (첫 번째 행)
                                if (row == 2 && excelColumnName == "결제수단")
                                {
                                    var rawValue = rowCellValue;
                                    var rawValueType = rawValue?.GetType().Name ?? "NULL";
                                    var rawValueString = rawValue?.ToString() ?? "NULL";
                                    
                                    Console.WriteLine($"[FileService] 🔍 결제수단 컬럼 디버깅 (첫 번째 행):");
                                    Console.WriteLine($"  - 원본 값: '{rawValueString}' (타입: {rawValueType})");
                                    Console.WriteLine($"  - cellValue: '{cellValue}' (길이: {cellValue?.Length ?? 0})");
                                    Console.WriteLine($"  - convertedValue: '{convertedValue}' (타입: {convertedValue?.GetType().Name ?? "NULL"})");
                                    Console.WriteLine($"  - 셀 주소: {rowCell.Address}");
                                    
                                    // 셀의 상세 정보 확인
                                    Console.WriteLine($"  - 셀 형식: {rowCell.Style.Numberformat.Format}");
                                    Console.WriteLine($"  - 셀 값 타입: {rowCellValue?.GetType().Name ?? "NULL"}");
                                }
                                
                                // 컬럼명으로 데이터 설정
                                if (dataTable.Columns.Contains(columnName))
                                {
                                    dataRow[columnName] = convertedValue;
                                    
                                    // 디버깅을 위한 로그 추가 (쇼핑몰 컬럼 특별 처리)
                                    if (row <= 3 || columnName == "쇼핑몰") // 처음 몇 행만 로깅 + 쇼핑몰 컬럼은 항상 로깅
                                    {
                                        Console.WriteLine($"[FileService] 행{row} 컬럼 '{excelColumnName}' → '{columnName}': '{cellValue}' → '{convertedValue}'");
                                    }
                                }
                                else
                                {
                                    // 컬럼이 존재하지 않는 경우 로깅
                                    if (row <= 3)
                                    {
                                        Console.WriteLine($"[FileService] ⚠️ 행{row} 컬럼 '{columnName}'이 DataTable에 존재하지 않음");
                                    }
                                }
                                
                                // 빈 셀이 아닌 경우 데이터가 있다고 표시
                                if (!string.IsNullOrEmpty(cellValue))
                                {
                                    hasData = true;
                                }
                            }
                            catch
                            {
                                // 셀 접근 실패 시 건너뛰기
                                if (row <= 3)
                                {
                                    Console.WriteLine($"[FileService] ⚠️ 행{row} 컬럼 {col} 접근 실패 - 건너뛰기");
                                }
                            }
                        }

                        // 데이터가 있는 행만 DataTable에 추가
                        if (hasData)
                        {
                            dataTable.Rows.Add(dataRow);
                        }
                    }
                }

                Console.WriteLine($"✅ FileService: Excel 파일 읽기 완료 (매핑 적용) - {dataTable.Rows.Count}행, {dataTable.Columns.Count}열");
                
                // 📊 데이터 변환 및 정규화 수행 (주석 처리 - 나중에 사용 가능)
                // Console.WriteLine($"🔄 FileService: 데이터 변환 및 정규화 시작...");
                // dataTable = _transformationService.TransformData(dataTable);
                // Console.WriteLine($"✨ FileService: 데이터 변환 및 정규화 완료");
                
                return dataTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FileService: Excel 파일 읽기 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 컬럼의 데이터 타입을 가져오는 메서드
        /// </summary>
        /// <param name="excelColumnName">Excel 컬럼명</param>
        /// <param name="tableMappingKey">테이블 매핑 키</param>
        /// <returns>데이터 타입</returns>
        private Type GetColumnDataType(string excelColumnName, string tableMappingKey)
        {
            // 매핑 설정에서 데이터 타입 확인
            var configuration = _mappingService.GetConfiguration();
            if (configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) == true)
            {
                if (tableMapping.Columns.TryGetValue(excelColumnName, out var columnMapping))
                {
                    return columnMapping.DataType.ToLower() switch
                    {
                        "int" => typeof(int),
                        "decimal" => typeof(decimal),
                        "double" => typeof(double),
                        "date" => typeof(DateTime),
                        "datetime" => typeof(DateTime),
                        "bool" => typeof(bool),
                        _ => typeof(string)
                    };
                }
            }
            
            // 기본값은 문자열
            return typeof(string);
        }

        /// <summary>
        /// 셀 값을 데이터 타입에 맞게 변환하는 메서드
        /// </summary>
        /// <param name="cellValue">원본 셀 값</param>
        /// <param name="excelColumnName">Excel 컬럼명</param>
        /// <param name="tableMappingKey">테이블 매핑 키</param>
        /// <returns>변환된 값</returns>
        private object ConvertCellValue(string cellValue, string excelColumnName, string tableMappingKey)
        {
            if (string.IsNullOrEmpty(cellValue))
            {
                // 기본값 확인
                var configuration = _mappingService.GetConfiguration();
                if (configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) == true)
                {
                    if (tableMapping.Columns.TryGetValue(excelColumnName, out var columnMapping))
                    {
                        return columnMapping.DefaultValue ?? DBNull.Value;
                    }
                }
                return DBNull.Value;
            }

            // 매핑 설정에서 데이터 타입 확인
            var dataType = GetColumnDataType(excelColumnName, tableMappingKey);
            
            try
            {
                return dataType.Name switch
                {
                    "Int32" => int.TryParse(cellValue, out var intValue) ? intValue : 0,
                    "Decimal" => decimal.TryParse(cellValue, out var decimalValue) ? decimalValue : 0m,
                    "Double" => double.TryParse(cellValue, out var doubleValue) ? doubleValue : 0.0,
                    "DateTime" => DateTime.TryParse(cellValue, out var dateValue) ? dateValue : DateTime.MinValue,
                    "Boolean" => bool.TryParse(cellValue, out var boolValue) ? boolValue : false,
                    _ => cellValue
                };
            }
            catch
            {
                // 변환 실패 시 원본 값 반환
                return cellValue;
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

        #region Excel 파일 저장 (헤더 없음) (Excel File Saving without Header)

        /// <summary>
        /// DataTable을 Excel 파일로 저장하는 메서드 (헤더 없음)
        /// 
        /// 처리 과정:
        /// 1. 출력 디렉토리 존재 확인 및 생성
        /// 2. EPPlus를 사용하여 새로운 Excel 파일 생성
        /// 3. 워크시트 생성 및 이름 설정
        /// 4. 헤더 행 작성하지 않음 (header=False)
        /// 5. 데이터 행들을 Excel에 작성
        /// 6. 파일 저장 및 리소스 해제
        /// 
        /// 파일 형식:
        /// - .xlsx 확장자 사용
        /// - 첫 번째 행부터 데이터 시작 (헤더 없음)
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
        public bool SaveDataTableToExcelWithoutHeader(DataTable dataTable, string filePath, string sheetName = "Sheet1")
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

                    // 헤더 행 작성하지 않음 (header=False)
                    // 데이터 행들을 Excel에 작성 (첫 번째 행부터 시작)
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            var cellValue = dataTable.Rows[row][col]?.ToString() ?? string.Empty;
                            worksheet.Cells[row + 1, col + 1].Value = cellValue; // +1이 아닌 +1 (헤더 없음)
                        }
                    }

                    // 컬럼 너비 자동 조정
                    worksheet.Cells.AutoFitColumns();

                    // 파일 저장
                    package.SaveAs(new FileInfo(filePath));
                }

                Console.WriteLine($"✅ FileService: Excel 파일 저장 완료 (헤더 없음) - {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FileService: Excel 파일 저장 실패 (헤더 없음): {ex.Message}");
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

        #region Excel 데이터를 프로시저로 전달 (Excel Data to Procedure)

        /// <summary>
        /// Excel 파일을 읽어서 DataTable로 변환하고 지정된 프로시저로 전달하는 공용 메서드
        /// 
        /// 🎯 주요 기능:
        /// - Excel 파일을 DataTable로 읽기 (기존 ReadExcelToDataTable 메서드 활용)
        /// - 지정된 프로시저명으로 프로시저 호출
        /// - DataTable을 프로시저 파라미터로 전달
        /// - 컬럼명은 자동으로 전달됨 (별도 전달 불필요)
        /// 
        /// 📋 처리 과정:
        /// 1. Excel 파일을 DataTable로 읽기
        /// 2. 프로시저명 유효성 검증
        /// 3. DatabaseService를 통한 프로시저 호출
        /// 4. DataTable을 프로시저 파라미터로 전달
        /// 5. 결과 반환 및 오류 처리
        /// 
        /// ⚙️ 설정 파일:
        /// - App.config에서 프로시저명 정의
        /// - <add key="ExcelProcessor.Proc1" value="sp_Excel_Proc1" />
        /// 
        /// 🔄 재사용성:
        /// - 다양한 Excel 파일과 프로시저 조합으로 사용 가능
        /// - 공용 메서드로 여러 곳에서 호출 가능
        /// 
        /// ⚠️ 예외 처리:
        /// - FileNotFoundException: Excel 파일이 존재하지 않는 경우
        /// - ArgumentException: 프로시저명이 유효하지 않은 경우
        /// - InvalidOperationException: 프로시저 실행 실패
        /// 
        /// 💡 사용 예시:
        /// ```csharp
        /// var fileService = new FileService();
        /// var result = await fileService.ReadExcelToDataTableWithProcedure(
        ///     "C:\\Work\\Input\\data.xlsx", 
        ///     "ExcelProcessor.Proc1"
        /// );
        /// ```
        /// </summary>
        /// <param name="filePath">읽을 Excel 파일의 전체 경로</param>
        /// <param name="procedureConfigKey">프로시저 설정 키 (App.config의 key 값)</param>
        /// <returns>프로시저 실행 결과 (성공/실패)</returns>
        /// <exception cref="FileNotFoundException">Excel 파일이 존재하지 않는 경우</exception>
        /// <exception cref="ArgumentException">프로시저명이 유효하지 않은 경우</exception>
        /// <exception cref="InvalidOperationException">프로시저 실행 실패</exception>
        public async Task<bool> ReadExcelToDataTableWithProcedure(string filePath, string procedureConfigKey)
        {
            // 메서드명과 프로시저명 상수 정의
            const string METHOD_NAME = "ReadExcelToDataTableWithProcedure";

            try
            {
                LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일을 프로시저로 전달하는 작업 시작");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 파일 경로: {filePath}");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 프로시저 설정 키: {procedureConfigKey}");

                // 1단계: Excel 파일을 DataTable로 읽기 (기존 메서드 호출)
                LogManagerService.LogInfo($"[{METHOD_NAME}] 1단계: Excel 파일 읽기 시작");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 파일 경로: {filePath}");
                
                // 파일 존재 여부 확인
                if (!File.Exists(filePath))
                {
                    var errorMsg = $"Excel 파일이 존재하지 않습니다: {filePath}";
                    LogManagerService.LogError($"[{METHOD_NAME}] {errorMsg}");
                    return false;
                }
                
                // 파일 크기 확인
                var fileInfo = new FileInfo(filePath);
                LogManagerService.LogInfo($"[{METHOD_NAME}] 파일 크기: {fileInfo.Length:N0} bytes");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 파일 수정 시간: {fileInfo.LastWriteTime}");
                
                var dataTable = ReadExcelToDataTable(filePath); // 매핑 없이 처리
                
                if (dataTable == null)
                {
                    var errorMsg = $"Excel 파일을 읽어서 DataTable로 변환할 수 없습니다: {filePath}";
                    LogManagerService.LogError($"[{METHOD_NAME}] {errorMsg}");
                    return false;
                }
                
                if (dataTable.Rows.Count == 0)
                {
                    var errorMsg = $"Excel 파일에 데이터가 없습니다: {filePath}";
                    LogManagerService.LogWarning($"[{METHOD_NAME}] {errorMsg}");
                    return false;
                }
                
                LogManagerService.LogInfo($"[{METHOD_NAME}] 1단계 완료: Excel 파일 읽기 성공");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 데이터 정보: {dataTable.Rows.Count:N0}행, {dataTable.Columns.Count}열");
                
                // 컬럼 정보 로깅
                var columnList = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select((col, i) => $"{i + 1}: {col.ColumnName} ({col.DataType.Name})"));
                LogManagerService.LogInfo($"[{METHOD_NAME}] 컬럼 목록: {columnList}");
                
                // 샘플 데이터 로깅 (처음 3행)
                for (int row = 0; row < Math.Min(3, dataTable.Rows.Count); row++)
                {
                    var sampleData = string.Join(" | ", dataTable.Columns.Cast<DataColumn>().Select(col => $"{col.ColumnName}: {dataTable.Rows[row][col]?.ToString() ?? "NULL"}"));
                    LogManagerService.LogInfo($"[{METHOD_NAME}] 샘플 데이터 행 {row + 1}: {sampleData}");
                }

                // 2단계: 프로시저명 설정에서 가져오기
                LogManagerService.LogInfo($"[{METHOD_NAME}] 2단계: 프로시저명 설정 확인");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 설정 키: {procedureConfigKey}");
                
                // App.config에서 프로시저명 조회
                var procedureName = ConfigurationManager.AppSettings[procedureConfigKey];
                LogManagerService.LogInfo($"[{METHOD_NAME}] App.config에서 조회된 값: '{procedureName ?? "NULL"}'");
                
                if (string.IsNullOrEmpty(procedureName))
                {
                    var errorMessage = $"프로시저 설정 키 '{procedureConfigKey}'에 해당하는 프로시저명을 찾을 수 없습니다. App.config를 확인해주세요.";
                    LogManagerService.LogError($"[{METHOD_NAME}] {errorMessage}");
                    throw new ArgumentException(errorMessage, nameof(procedureConfigKey));
                }
                
                LogManagerService.LogInfo($"[{METHOD_NAME}] 2단계 완료: 프로시저명 확인");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 실행할 프로시저: {procedureName}");
                
                // 프로시저명 유효성 검사
                if (procedureName.Trim().Length == 0)
                {
                    var errorMessage = $"프로시저명이 빈 문자열입니다: '{procedureConfigKey}' = '{procedureName}'";
                    LogManagerService.LogError($"[{METHOD_NAME}] {errorMessage}");
                    throw new ArgumentException(errorMessage, nameof(procedureConfigKey));
                }

                // 3단계: DatabaseService를 통한 프로시저 호출
                LogManagerService.LogInfo($"[{METHOD_NAME}] 3단계: 프로시저 호출 시작");
                LogManagerService.LogInfo($"[{METHOD_NAME}] DatabaseService 인스턴스 생성 중...");
                
                // DatabaseService 인스턴스 생성
                var databaseService = new DatabaseService();
                LogManagerService.LogInfo($"[{METHOD_NAME}] DatabaseService 인스턴스 생성 완료");
                
                // 프로시저 호출 전 최종 확인
                LogManagerService.LogInfo($"[{METHOD_NAME}] 프로시저 호출 정보:");
                LogManagerService.LogInfo($"[{METHOD_NAME}]   - 프로시저명: {procedureName}");
                LogManagerService.LogInfo($"[{METHOD_NAME}]   - 데이터 행수: {dataTable.Rows.Count:N0}행");
                LogManagerService.LogInfo($"[{METHOD_NAME}]   - 데이터 컬럼수: {dataTable.Columns.Count}열");
                LogManagerService.LogInfo($"[{METHOD_NAME}]   - 첫 번째 컬럼명: {dataTable.Columns[0]?.ColumnName ?? "NULL"}");
                LogManagerService.LogInfo($"[{METHOD_NAME}]   - 마지막 컬럼명: {dataTable.Columns[dataTable.Columns.Count - 1]?.ColumnName ?? "NULL"}");
                
                // 프로시저 호출 (DataTable을 파라미터로 전달)
                LogManagerService.LogInfo($"[{METHOD_NAME}] ExecuteProcedureWithDataTable 메서드 호출 시작...");
                var result = await ExecuteProcedureWithDataTable(databaseService, procedureName, dataTable);
                
                if (result)
                {
                    LogManagerService.LogInfo($"[{METHOD_NAME}] 3단계 완료: 프로시저 실행 성공");
                    LogManagerService.LogInfo($"[{METHOD_NAME}] 프로시저 '{procedureName}' 실행 성공 - {dataTable.Rows.Count:N0}행 처리 완료");
                }
                else
                {
                    var errorMsg = $"프로시저 '{procedureName}' 실행 실패 - 데이터가 테이블에 삽입되지 않았습니다";
                    LogManagerService.LogError($"[{METHOD_NAME}] 3단계 실패: {errorMsg}");
                    
                    // 프로시저 실행 실패 시 상세 오류 정보를 포함한 예외 발생
                    throw new InvalidOperationException(errorMsg);
                }

                LogManagerService.LogInfo($"[{METHOD_NAME}] Excel 파일을 프로시저로 전달하는 작업 완료");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 최종 결과: {(result ? "성공" : "실패")}");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 처리된 파일: {filePath}");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 실행된 프로시저: {procedureName}");
                LogManagerService.LogInfo($"[{METHOD_NAME}] 처리된 데이터: {dataTable.Rows.Count:N0}행, {dataTable.Columns.Count}열");
                
                return result;
            }
            catch (FileNotFoundException ex)
            {
                var errorMsg = $"Excel 파일을 찾을 수 없습니다: {ex.Message}";
                LogManagerService.LogError($"[{METHOD_NAME}] {errorMsg} - 파일: {filePath}");
                throw;
            }
            catch (ArgumentException ex)
            {
                var errorMsg = $"잘못된 매개변수: {ex.Message}";
                LogManagerService.LogError($"[{METHOD_NAME}] {errorMsg} - 설정키: {procedureConfigKey}");
                throw;
            }
            catch (Exception ex)
            {
                var errorMsg = $"예상치 못한 오류 발생: {ex.Message}";
                
                // 상세 오류 정보를 로그에 기록
                LogManagerService.LogError($"[{METHOD_NAME}] {errorMsg}");
                LogManagerService.LogError($"[{METHOD_NAME}] 오류 상세: {ex.StackTrace}");
                LogManagerService.LogError($"[{METHOD_NAME}] 파일 경로: {filePath}");
                LogManagerService.LogError($"[{METHOD_NAME}] 프로시저 설정키: {procedureConfigKey}");
                
                throw new InvalidOperationException($"Excel 파일을 프로시저로 전달하는 중 오류가 발생했습니다: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// DatabaseService를 통해 프로시저를 실행하는 내부 메서드
        /// 
        /// 🎯 주요 기능:
        /// - 지정된 프로시저명으로 프로시저 호출
        /// - DataTable을 프로시저 파라미터로 전달
        /// - 컬럼명은 자동으로 전달됨
        /// - 비동기 실행으로 성능 최적화
        /// 
        /// 📋 처리 과정:
        /// 1. 프로시저 실행 준비
        /// 2. DataTable을 프로시저 파라미터로 전달
        /// 3. 프로시저 실행 및 결과 확인
        /// 4. 결과 반환 및 오류 처리
        /// 
        /// ⚠️ 주의사항:
        /// - DataTable의 컬럼명은 자동으로 전달됨
        /// - 프로시저에서 컬럼 구조를 동적으로 파악 가능
        /// - 대량 데이터 처리 시 성능 고려 필요
        /// </summary>
        /// <param name="databaseService">데이터베이스 서비스 인스턴스</param>
        /// <param name="procedureName">실행할 프로시저명</param>
        /// <param name="dataTable">프로시저로 전달할 DataTable</param>
        /// <returns>프로시저 실행 성공 여부</returns>
        private async Task<bool> ExecuteProcedureWithDataTable(DatabaseService databaseService, string procedureName, DataTable dataTable)
        {
            try
            {
                Console.WriteLine($"🔄 프로시저 '{procedureName}' 실행 시작...");
                Console.WriteLine($"📊 전달할 데이터: {dataTable.Rows.Count}행, {dataTable.Columns.Count}열");

                // DataTable의 컬럼 정보 로깅 (디버깅용)
                var columnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
                Console.WriteLine($"📋 컬럼명 목록: {string.Join(", ", columnNames)}");

                // 프로시저 실행 (DataTable을 파라미터로 전달)
                // DatabaseService에서 DataTable을 프로시저 파라미터로 처리하는 메서드 호출
                var result = await databaseService.ExecuteProcedureWithDataTableAsync(procedureName, dataTable);
                
                Console.WriteLine($"✅ 프로시저 '{procedureName}' 실행 완료 - 결과: {(result ? "성공" : "실패")}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 프로시저 '{procedureName}' 실행 중 오류 발생: {ex.Message}");
                Console.WriteLine($"📋 오류 상세: {ex.StackTrace}");
                return false;
            }
        }

        #endregion
    }
} 