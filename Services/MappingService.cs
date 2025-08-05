using LogisticManager.Models;
using Newtonsoft.Json;
using System.Data;

namespace LogisticManager.Services
{
    /// <summary>
    /// 컬럼 매핑 설정을 관리하는 서비스
    /// 
    /// 주요 기능:
    /// - 매핑 설정 파일 로드
    /// - 동적 SQL 쿼리 생성
    /// - 데이터 검증 및 변환
    /// - 엑셀 데이터를 데이터베이스 형식으로 변환
    /// </summary>
    public class MappingService
    {
        private MappingConfiguration? _configuration;
        private readonly string _mappingFilePath;

        public MappingService()
        {
            _mappingFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "column_mapping.json");
            LoadMappingConfiguration();
        }

        /// <summary>
        /// 매핑 설정을 로드합니다.
        /// </summary>
        private void LoadMappingConfiguration()
        {
            try
            {
                if (File.Exists(_mappingFilePath))
                {
                    var jsonContent = File.ReadAllText(_mappingFilePath);
                    _configuration = JsonConvert.DeserializeObject<MappingConfiguration>(jsonContent);
                    Console.WriteLine($"✅ 매핑 설정 로드 완료: {_configuration?.Mappings.Count}개 테이블");
                }
                else
                {
                    Console.WriteLine($"⚠️ 매핑 설정 파일이 존재하지 않습니다: {_mappingFilePath}");
                    _configuration = new MappingConfiguration();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 매핑 설정 로드 실패: {ex.Message}");
                _configuration = new MappingConfiguration();
            }
        }

        /// <summary>
        /// 엑셀 컬럼명을 데이터베이스 컬럼명으로 변환합니다.
        /// </summary>
        /// <param name="excelColumn">엑셀 컬럼명</param>
        /// <param name="tableMappingKey">테이블 매핑 키</param>
        /// <returns>데이터베이스 컬럼명</returns>
        public string? GetDatabaseColumn(string excelColumn, string tableMappingKey = "order_table")
        {
            if (_configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) == true && tableMapping != null)
            {
                if (tableMapping.Columns.TryGetValue(excelColumn, out var columnMapping))
                {
                    return columnMapping.DatabaseColumn;
                }
            }
            return null;
        }

        /// <summary>
        /// 엑셀 데이터를 데이터베이스 형식으로 변환합니다.
        /// </summary>
        /// <param name="dataTable">엑셀 데이터</param>
        /// <param name="tableMappingKey">테이블 매핑 키</param>
        /// <returns>변환된 데이터</returns>
        public List<Dictionary<string, object>> TransformExcelData(DataTable dataTable, string tableMappingKey = "order_table")
        {
            var transformedData = new List<Dictionary<string, object>>();

            if (_configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) != true)
            {
                Console.WriteLine($"❌ 테이블 매핑을 찾을 수 없습니다: {tableMappingKey}");
                return transformedData;
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var transformedRow = new Dictionary<string, object>();

                foreach (DataColumn column in dataTable.Columns)
                {
                    var excelColumnName = column.ColumnName;
                    var cellValue = row[column];

                    if (tableMapping?.Columns.TryGetValue(excelColumnName, out var columnMapping) == true)
                    {
                        var dbColumnName = columnMapping.DatabaseColumn;
                        var transformedValue = TransformValue(cellValue, columnMapping, excelColumnName);
                        transformedRow[dbColumnName] = transformedValue;
                    }
                    else
                    {
                        // 매핑되지 않은 컬럼은 원본 값 그대로 사용
                        transformedRow[excelColumnName] = cellValue ?? DBNull.Value;
                    }
                }

                transformedData.Add(transformedRow);
            }

            Console.WriteLine($"✅ 데이터 변환 완료: {transformedData.Count}개 행");
            return transformedData;
        }

        /// <summary>
        /// 개별 값을 변환합니다.
        /// </summary>
        /// <param name="value">원본 값</param>
        /// <param name="columnMapping">컬럼 매핑 정보</param>
        /// <param name="excelColumnName">엑셀 컬럼명</param>
        /// <returns>변환된 값</returns>
        private object TransformValue(object value, ColumnMapping columnMapping, string excelColumnName)
        {
            if (value == null || value == DBNull.Value)
            {
                return columnMapping.DefaultValue ?? DBNull.Value;
            }

            var stringValue = value.ToString() ?? string.Empty;

            // 특별 처리 규칙 적용
            if (!string.IsNullOrEmpty(columnMapping.SpecialHandling))
            {
                switch (columnMapping.SpecialHandling)
                {
                    case "star_processing":
                        // 별표 처리 (감천, 카카오 등 특수 출고지)
                        if (excelColumnName == "주소" && !stringValue.Contains("*"))
                        {
                            stringValue = $"*{stringValue}";
                        }
                        break;
                }
            }

            // 데이터 타입 변환
            switch (columnMapping.DataType.ToLower())
            {
                case "int":
                    return int.TryParse(stringValue, out var intValue) ? intValue : columnMapping.DefaultValue ?? 0;

                case "decimal":
                    return decimal.TryParse(stringValue, out var decimalValue) ? decimalValue : columnMapping.DefaultValue ?? 0m;

                case "date":
                    return DateTime.TryParse(stringValue, out var dateValue) ? dateValue : columnMapping.DefaultValue ?? DateTime.MinValue;

                case "varchar":
                default:
                    return stringValue;
            }
        }

        /// <summary>
        /// INSERT SQL 쿼리를 동적으로 생성합니다.
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="data">삽입할 데이터</param>
        /// <returns>INSERT SQL 쿼리</returns>
        public string GenerateInsertQuery(string tableName, Dictionary<string, object> data)
        {
            var columns = string.Join(", ", data.Keys.Select(k => $"`{k}`"));
            var values = string.Join(", ", data.Values.Select(v => FormatValue(v)));

            return $"INSERT INTO `{tableName}` ({columns}) VALUES ({values})";
        }

        /// <summary>
        /// 데이터베이스 값 형식을 지정합니다.
        /// </summary>
        /// <param name="value">값</param>
        /// <returns>형식이 지정된 값</returns>
        private string FormatValue(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is string stringValue)
                return $"'{stringValue.Replace("'", "''")}'";

            if (value is DateTime dateValue)
                return $"'{dateValue:yyyy-MM-dd HH:mm:ss}'";

            if (value is int || value is decimal || value is double || value is float)
                return value.ToString() ?? "0";

            return $"'{value}'";
        }

        /// <summary>
        /// 데이터 유효성을 검사합니다.
        /// </summary>
        /// <param name="data">검사할 데이터</param>
        /// <param name="tableMappingKey">테이블 매핑 키</param>
        /// <returns>검사 결과</returns>
        public (bool IsValid, List<string> Errors) ValidateData(Dictionary<string, object> data, string tableMappingKey = "order_table")
        {
            var errors = new List<string>();

            if (_configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) != true || tableMapping == null)
            {
                errors.Add($"테이블 매핑을 찾을 수 없습니다: {tableMappingKey}");
                return (false, errors);
            }

            // 필수 필드 검사
            foreach (var columnMapping in tableMapping.Columns.Values)
            {
                if (columnMapping.Required)
                {
                    var dbColumnName = columnMapping.DatabaseColumn;
                    if (!data.ContainsKey(dbColumnName) || data[dbColumnName] == null || data[dbColumnName] == DBNull.Value)
                    {
                        errors.Add($"필수 필드가 누락되었습니다: {columnMapping.ExcelColumn} ({dbColumnName})");
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 매핑 설정을 다시 로드합니다.
        /// </summary>
        public void ReloadConfiguration()
        {
            LoadMappingConfiguration();
        }

        /// <summary>
        /// 현재 매핑 설정을 가져옵니다.
        /// </summary>
        /// <returns>매핑 설정</returns>
        public MappingConfiguration? GetConfiguration()
        {
            return _configuration;
        }
    }
} 