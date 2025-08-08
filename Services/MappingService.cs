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
            // 프로젝트 루트 디렉토리에서 설정 파일 찾기
            _mappingFilePath = Path.Combine(Directory.GetCurrentDirectory(), "column_mapping.json");
            Console.WriteLine($"[MappingService] 설정 파일 경로: {_mappingFilePath}");
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
                    
                    // 활성화된 매핑 정보 출력
                    var activeMappings = _configuration?.Mappings.Values.Where(m => m.IsActive).ToList();
                    if (activeMappings?.Any() == true)
                    {
                        Console.WriteLine($"📋 활성화된 매핑:");
                        foreach (var mapping in activeMappings.OrderBy(m => m.ProcessingOrder))
                        {
                            Console.WriteLine($"  - {mapping.MappingId}: {mapping.Description} (순서: {mapping.ProcessingOrder})");
                        }
                    }
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
            Console.WriteLine($"[MappingService] 매핑 요청: '{excelColumn}' (테이블: {tableMappingKey})");
            
            if (_configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) == true && tableMapping != null)
            {
                Console.WriteLine($"[MappingService] 테이블 매핑 찾음: {tableMapping.MappingId}, 컬럼 수: {tableMapping.Columns.Count}");
                
                // 사용 가능한 모든 컬럼명 출력
                var availableColumns = string.Join(", ", tableMapping.Columns.Keys);
                Console.WriteLine($"[MappingService] 사용 가능한 컬럼들: {availableColumns}");
                
                if (tableMapping.Columns.TryGetValue(excelColumn, out var columnMapping))
                {
                    Console.WriteLine($"[MappingService] ✅ 매핑 성공: '{excelColumn}' → '{columnMapping.DbColumn}'");
                    return columnMapping.DbColumn;
                }
                else
                {
                    Console.WriteLine($"[MappingService] ❌ 매핑 실패: '{excelColumn}' 컬럼을 찾을 수 없음");
                }
            }
            else
            {
                Console.WriteLine($"[MappingService] ❌ 테이블 매핑 '{tableMappingKey}'를 찾을 수 없음");
                if (_configuration?.Mappings != null)
                {
                    var availableTables = string.Join(", ", _configuration.Mappings.Keys);
                    Console.WriteLine($"[MappingService] 사용 가능한 테이블들: {availableTables}");
                }
            }
            return null;
        }

        /// <summary>
        /// 활성화된 매핑 목록을 가져옵니다.
        /// </summary>
        /// <returns>활성화된 매핑 목록</returns>
        public List<TableMapping> GetActiveMappings()
        {
            if (_configuration?.Mappings == null)
                return new List<TableMapping>();

            return _configuration.Mappings.Values
                .Where(m => m.IsActive)
                .OrderBy(m => m.ProcessingOrder)
                .ToList();
        }

        /// <summary>
        /// 특정 매핑을 가져옵니다.
        /// </summary>
        /// <param name="tableMappingKey">테이블 매핑 키</param>
        /// <returns>테이블 매핑 정보</returns>
        public TableMapping? GetMapping(string tableMappingKey)
        {
            if (_configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) == true)
            {
                return tableMapping;
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

            if (_configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) != true || tableMapping == null)
            {
                Console.WriteLine($"❌ 테이블 매핑을 찾을 수 없습니다: {tableMappingKey}");
                return transformedData;
            }

            if (!tableMapping.IsActive)
            {
                Console.WriteLine($"⚠️ 매핑이 비활성화되어 있습니다: {tableMappingKey}");
                return transformedData;
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var transformedRow = new Dictionary<string, object>();

                // 기본 컬럼 매핑
                foreach (DataColumn column in dataTable.Columns)
                {
                    var excelColumnName = column.ColumnName;
                    var cellValue = row[column];

                    if (tableMapping.Columns.TryGetValue(excelColumnName, out var columnMapping))
                    {
                        var dbColumnName = columnMapping.DbColumn;
                        var transformedValue = TransformValue(cellValue, columnMapping, excelColumnName);
                        transformedRow[dbColumnName] = transformedValue;
                    }
                    else
                    {
                        // 매핑되지 않은 컬럼은 원본 값 그대로 사용
                        transformedRow[excelColumnName] = cellValue ?? DBNull.Value;
                    }
                }

                // 추가 컬럼에 기본값 설정
                foreach (var additionalColumn in tableMapping.AdditionalColumns)
                {
                    var dbColumnName = additionalColumn.Value.DbColumn;
                    if (!transformedRow.ContainsKey(dbColumnName))
                    {
                        transformedRow[dbColumnName] = additionalColumn.Value.DefaultValue ?? DBNull.Value;
                    }
                }

                transformedData.Add(transformedRow);
            }

            Console.WriteLine($"✅ 데이터 변환 완료: {transformedData.Count}개 행 (매핑: {tableMapping.MappingId})");
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

            // 데이터 타입 변환
            switch (columnMapping.DataType.ToLower())
            {
                case "int":
                    return int.TryParse(stringValue, out var intValue) ? intValue : (columnMapping.DefaultValue ?? 0);

                case "decimal":
                    return decimal.TryParse(stringValue, out var decimalValue) ? decimalValue : (columnMapping.DefaultValue ?? 0m);

                case "datetime":
                    return DateTime.TryParse(stringValue, out var dateValue) ? dateValue : (columnMapping.DefaultValue ?? DateTime.MinValue);

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
                    var dbColumnName = columnMapping.DbColumn;
                    if (!data.ContainsKey(dbColumnName) || data[dbColumnName] == null || data[dbColumnName] == DBNull.Value)
                    {
                        errors.Add($"필수 필드가 누락되었습니다: {dbColumnName}");
                    }
                }
            }

            // 검증 규칙 검사
            if (tableMapping.ValidationRules != null)
            {
                // 필수 필드 검사
                foreach (var requiredField in tableMapping.ValidationRules.RequiredFields)
                {
                    if (!data.ContainsKey(requiredField) || data[requiredField] == null || data[requiredField] == DBNull.Value)
                    {
                        errors.Add($"필수 필드가 누락되었습니다: {requiredField}");
                    }
                }

                // 숫자 필드 검사
                foreach (var numericField in tableMapping.ValidationRules.NumericFields)
                {
                    if (data.ContainsKey(numericField) && data[numericField] != null && data[numericField] != DBNull.Value)
                    {
                        if (!decimal.TryParse(data[numericField].ToString(), out _))
                        {
                            errors.Add($"숫자 필드 형식이 잘못되었습니다: {numericField}");
                        }
                    }
                }

                // 날짜 필드 검사
                foreach (var dateField in tableMapping.ValidationRules.DateFields)
                {
                    if (data.ContainsKey(dateField) && data[dateField] != null && data[dateField] != DBNull.Value)
                    {
                        if (!DateTime.TryParse(data[dateField].ToString(), out _))
                        {
                            errors.Add($"날짜 필드 형식이 잘못되었습니다: {dateField}");
                        }
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

        /// <summary>
        /// 매핑 요약 정보를 출력합니다.
        /// </summary>
        public void PrintMappingSummary()
        {
            if (_configuration?.Mappings == null)
            {
                Console.WriteLine("❌ 매핑 설정이 없습니다.");
                return;
            }

            Console.WriteLine("📊 매핑 설정 요약:");
            Console.WriteLine($"  - 총 매핑 수: {_configuration.Mappings.Count}");
            
            var activeMappings = _configuration.Mappings.Values.Where(m => m.IsActive).ToList();
            Console.WriteLine($"  - 활성화된 매핑 수: {activeMappings.Count}");
            
            foreach (var mapping in activeMappings.OrderBy(m => m.ProcessingOrder))
            {
                Console.WriteLine($"    • {mapping.MappingId}: {mapping.Description}");
                Console.WriteLine($"      - 테이블: {mapping.TableName}");
                Console.WriteLine($"      - 엑셀 파일 패턴: {mapping.ExcelFilePattern}");
                Console.WriteLine($"      - 처리 순서: {mapping.ProcessingOrder}");
                Console.WriteLine($"      - 컬럼 수: {mapping.Columns.Count} (매핑) + {mapping.AdditionalColumns.Count} (추가)");
                
                // 상세 매핑 정보 출력
                Console.WriteLine($"      📋 엑셀 컬럼 매핑:");
                foreach (var column in mapping.Columns.OrderBy(c => c.Value.ExcelColumnIndex))
                {
                    Console.WriteLine($"        {column.Value.ExcelColumnIndex:00}. {column.Key} → {column.Value.DbColumn} ({column.Value.DataType})");
                }
                
                if (mapping.AdditionalColumns.Any())
                {
                    Console.WriteLine($"      🔧 추가 DB 컬럼:");
                    foreach (var additionalColumn in mapping.AdditionalColumns)
                    {
                        Console.WriteLine($"        - {additionalColumn.Value.DbColumn} ({additionalColumn.Value.DataType})");
                    }
                }
            }
        }

        /// <summary>
        /// 특정 매핑의 상세 정보를 출력합니다.
        /// </summary>
        /// <param name="tableMappingKey">테이블 매핑 키</param>
        public void PrintDetailedMapping(string tableMappingKey = "order_table")
        {
            if (_configuration?.Mappings.TryGetValue(tableMappingKey, out var tableMapping) != true || tableMapping == null)
            {
                Console.WriteLine($"❌ 매핑을 찾을 수 없습니다: {tableMappingKey}");
                return;
            }

            Console.WriteLine($"🔍 상세 매핑 정보: {tableMapping.MappingId}");
            Console.WriteLine($"  - 테이블명: {tableMapping.TableName}");
            Console.WriteLine($"  - 설명: {tableMapping.Description}");
            Console.WriteLine($"  - 활성화: {tableMapping.IsActive}");
            Console.WriteLine($"  - 처리 순서: {tableMapping.ProcessingOrder}");
            Console.WriteLine($"  - 엑셀 파일 패턴: {tableMapping.ExcelFilePattern}");
            Console.WriteLine($"  - 엑셀 시트명: {tableMapping.ExcelSheetName}");
            
            Console.WriteLine($"\n📊 컬럼 매핑 현황:");
            Console.WriteLine($"  - 엑셀 컬럼 수: {tableMapping.Columns.Count}");
            Console.WriteLine($"  - DB 컬럼 수: {tableMapping.Columns.Count + tableMapping.AdditionalColumns.Count}");
            Console.WriteLine($"  - 추가 DB 컬럼 수: {tableMapping.AdditionalColumns.Count}");
            
            Console.WriteLine($"\n📋 엑셀 컬럼 → DB 컬럼 매핑:");
            foreach (var column in tableMapping.Columns.OrderBy(c => c.Value.ExcelColumnIndex))
            {
                var required = column.Value.Required ? " (필수)" : "";
                Console.WriteLine($"  {column.Value.ExcelColumnIndex:00}. {column.Key,-15} → {column.Value.DbColumn,-15} ({column.Value.DataType}){required}");
            }
            
            if (tableMapping.AdditionalColumns.Any())
            {
                Console.WriteLine($"\n🔧 추가 DB 컬럼 (엑셀에 없음):");
                foreach (var additionalColumn in tableMapping.AdditionalColumns)
                {
                    var defaultValue = additionalColumn.Value.DefaultValue?.ToString() ?? "null";
                    Console.WriteLine($"  - {additionalColumn.Value.DbColumn,-15} ({additionalColumn.Value.DataType}) 기본값: {defaultValue}");
                }
            }
            
            if (tableMapping.ValidationRules != null)
            {
                Console.WriteLine($"\n✅ 검증 규칙:");
                if (tableMapping.ValidationRules.RequiredFields.Any())
                    Console.WriteLine($"  - 필수 필드: {string.Join(", ", tableMapping.ValidationRules.RequiredFields)}");
                if (tableMapping.ValidationRules.NumericFields.Any())
                    Console.WriteLine($"  - 숫자 필드: {string.Join(", ", tableMapping.ValidationRules.NumericFields)}");
                if (tableMapping.ValidationRules.DateFields.Any())
                    Console.WriteLine($"  - 날짜 필드: {string.Join(", ", tableMapping.ValidationRules.DateFields)}");
            }
        }
    }
} 