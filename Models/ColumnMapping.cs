using System.Collections.Generic;
using Newtonsoft.Json;

namespace LogisticManager.Models
{
    /// <summary>
    /// 엑셀 컬럼과 데이터베이스 컬럼 간의 매핑 정보를 담는 모델
    /// </summary>
    public class ColumnMapping
    {
        /// <summary>
        /// 데이터베이스 컬럼명
        /// </summary>
        [JsonProperty("db_column")]
        public string DbColumn { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 타입
        /// </summary>
        [JsonProperty("data_type")]
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 필수 필드 여부
        /// </summary>
        [JsonProperty("required")]
        public bool Required { get; set; } = false;

        /// <summary>
        /// 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 엑셀 컬럼 인덱스
        /// </summary>
        [JsonProperty("excel_column_index")]
        public int ExcelColumnIndex { get; set; } = 0;

        /// <summary>
        /// 기본값
        /// </summary>
        [JsonProperty("default_value")]
        public object? DefaultValue { get; set; }

        // 하위 호환성을 위한 속성
        [JsonIgnore]
        public string DatabaseColumn => DbColumn;

        [JsonIgnore]
        public string ExcelColumn => string.Empty; // 새로운 구조에서는 키가 엑셀 컬럼명
    }

    /// <summary>
    /// 추가 컬럼 정보를 담는 모델
    /// </summary>
    public class AdditionalColumn
    {
        /// <summary>
        /// 데이터베이스 컬럼명
        /// </summary>
        [JsonProperty("db_column")]
        public string DbColumn { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 타입
        /// </summary>
        [JsonProperty("data_type")]
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 필수 필드 여부
        /// </summary>
        [JsonProperty("required")]
        public bool Required { get; set; } = false;

        /// <summary>
        /// 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 기본값
        /// </summary>
        [JsonProperty("default_value")]
        public object? DefaultValue { get; set; }
    }

    /// <summary>
    /// 테이블 매핑 정보를 담는 모델
    /// </summary>
    public class TableMapping
    {
        /// <summary>
        /// 매핑 ID
        /// </summary>
        [JsonProperty("mapping_id")]
        public string MappingId { get; set; } = string.Empty;

        /// <summary>
        /// 테이블명
        /// </summary>
        [JsonProperty("table_name")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 엑셀 파일 패턴
        /// </summary>
        [JsonProperty("excel_file_pattern")]
        public string ExcelFilePattern { get; set; } = string.Empty;

        /// <summary>
        /// 엑셀 시트명
        /// </summary>
        [JsonProperty("excel_sheet_name")]
        public string ExcelSheetName { get; set; } = string.Empty;

        /// <summary>
        /// 처리 순서
        /// </summary>
        [JsonProperty("processing_order")]
        public int ProcessingOrder { get; set; } = 0;

        /// <summary>
        /// 활성화 여부
        /// </summary>
        [JsonProperty("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 컬럼 매핑 정보
        /// </summary>
        [JsonProperty("columns")]
        public Dictionary<string, ColumnMapping> Columns { get; set; } = new Dictionary<string, ColumnMapping>();

        /// <summary>
        /// 추가 컬럼 정보
        /// </summary>
        [JsonProperty("additional_columns")]
        public Dictionary<string, AdditionalColumn> AdditionalColumns { get; set; } = new Dictionary<string, AdditionalColumn>();

        /// <summary>
        /// 검증 규칙
        /// </summary>
        [JsonProperty("validation_rules")]
        public ValidationRules ValidationRules { get; set; } = new ValidationRules();

        /// <summary>
        /// 데이터 변환 규칙
        /// </summary>
        [JsonProperty("data_transformations")]
        public Dictionary<string, DataTransformation> DataTransformations { get; set; } = new Dictionary<string, DataTransformation>();

        /// <summary>
        /// 컬럼 매핑 요약
        /// </summary>
        [JsonProperty("column_mapping_summary")]
        public ColumnMappingSummary ColumnMappingSummary { get; set; } = new ColumnMappingSummary();
    }

    /// <summary>
    /// table_mappings.json에 맞는 테이블 매핑 정보를 담는 모델 클래스
    /// </summary>
    public class TableMappingInfo
    {
        /// <summary>
        /// 테이블명
        /// </summary>
        [JsonProperty("tableName")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 컬럼 정보 리스트
        /// </summary>
        [JsonProperty("columns")]
        public List<TableColumnInfo> Columns { get; set; } = new List<TableColumnInfo>();

        /// <summary>
        /// 테이블 설명
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 비즈니스 목적
        /// </summary>
        [JsonProperty("businessPurpose")]
        public string? BusinessPurpose { get; set; }

        /// <summary>
        /// 데이터 소스
        /// </summary>
        [JsonProperty("dataSource")]
        public string? DataSource { get; set; }

        /// <summary>
        /// 업데이트 빈도
        /// </summary>
        [JsonProperty("updateFrequency")]
        public string? UpdateFrequency { get; set; }

        /// <summary>
        /// 데이터 보관 기간
        /// </summary>
        [JsonProperty("dataRetention")]
        public string? DataRetention { get; set; }
    }

    /// <summary>
    /// 테이블 컬럼 정보를 담는 모델 클래스
    /// </summary>
    public class TableColumnInfo
    {
        /// <summary>
        /// 속성명 (C# 프로퍼티명)
        /// </summary>
        [JsonProperty("propertyName")]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// 데이터베이스 컬럼명
        /// </summary>
        [JsonProperty("databaseColumn")]
        public string DatabaseColumn { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 타입
        /// </summary>
        [JsonProperty("dataType")]
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 필수 여부
        /// </summary>
        [JsonProperty("isRequired")]
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 자동 증가 여부
        /// </summary>
        [JsonProperty("isAutoIncrement")]
        public bool IsAutoIncrement { get; set; } = false;

        /// <summary>
        /// 기본키 여부
        /// </summary>
        [JsonProperty("isPrimaryKey")]
        public bool IsPrimaryKey { get; set; } = false;

        /// <summary>
        /// INSERT에서 제외 여부
        /// </summary>
        [JsonProperty("excludeFromInsert")]
        public bool ExcludeFromInsert { get; set; } = false;

        /// <summary>
        /// 컬럼 설명
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 컬럼 매핑 요약 정보
    /// </summary>
    public class ColumnMappingSummary
    {
        /// <summary>
        /// 엑셀 컬럼 수
        /// </summary>
        [JsonProperty("excel_columns")]
        public int ExcelColumns { get; set; } = 0;

        /// <summary>
        /// 데이터베이스 컬럼 수
        /// </summary>
        [JsonProperty("db_columns")]
        public int DbColumns { get; set; } = 0;

        /// <summary>
        /// 매핑된 컬럼 수
        /// </summary>
        [JsonProperty("mapped_columns")]
        public int MappedColumns { get; set; } = 0;

        /// <summary>
        /// 추가 데이터베이스 컬럼 수
        /// </summary>
        [JsonProperty("additional_db_columns")]
        public int AdditionalDbColumns { get; set; } = 0;

        /// <summary>
        /// 매핑 상태
        /// </summary>
        [JsonProperty("mapping_status")]
        public string MappingStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// 매핑 설정 전체를 담는 모델
    /// </summary>
    public class MappingConfiguration
    {
        /// <summary>
        /// 버전
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 생성 날짜
        /// </summary>
        [JsonProperty("created_date")]
        public string CreatedDate { get; set; } = string.Empty;

        /// <summary>
        /// 마지막 수정 날짜
        /// </summary>
        [JsonProperty("last_updated")]
        public string LastUpdated { get; set; } = string.Empty;

        /// <summary>
        /// 테이블 매핑 정보
        /// </summary>
        [JsonProperty("mappings")]
        public Dictionary<string, TableMapping> Mappings { get; set; } = new Dictionary<string, TableMapping>();

        /// <summary>
        /// 글로벌 설정
        /// </summary>
        [JsonProperty("global_settings")]
        public GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();

        /// <summary>
        /// 매핑 템플릿
        /// </summary>
        [JsonProperty("mapping_templates")]
        public Dictionary<string, MappingTemplate> MappingTemplates { get; set; } = new Dictionary<string, MappingTemplate>();

        /// <summary>
        /// 사용 가이드
        /// </summary>
        [JsonProperty("usage_guide")]
        public UsageGuide UsageGuide { get; set; } = new UsageGuide();
    }

    /// <summary>
    /// 글로벌 설정
    /// </summary>
    public class GlobalSettings
    {
        /// <summary>
        /// 기본 데이터 타입
        /// </summary>
        [JsonProperty("default_data_types")]
        public Dictionary<string, string> DefaultDataTypes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 지원 파일 형식
        /// </summary>
        [JsonProperty("supported_file_formats")]
        public List<string> SupportedFileFormats { get; set; } = new List<string>();

        /// <summary>
        /// 인코딩
        /// </summary>
        [JsonProperty("encoding")]
        public string Encoding { get; set; } = "UTF-8";

        /// <summary>
        /// 배치 크기
        /// </summary>
        [JsonProperty("batch_size")]
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// 에러 처리 설정
        /// </summary>
        [JsonProperty("error_handling")]
        public ErrorHandling ErrorHandling { get; set; } = new ErrorHandling();
    }

    /// <summary>
    /// 에러 처리 설정
    /// </summary>
    public class ErrorHandling
    {
        /// <summary>
        /// 에러 발생 시 계속 진행 여부
        /// </summary>
        [JsonProperty("continue_on_error")]
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// 에러 로그 기록 여부
        /// </summary>
        [JsonProperty("log_errors")]
        public bool LogErrors { get; set; } = true;

        /// <summary>
        /// 잘못된 행 건너뛰기 여부
        /// </summary>
        [JsonProperty("skip_invalid_rows")]
        public bool SkipInvalidRows { get; set; } = true;
    }

    /// <summary>
    /// 매핑 템플릿
    /// </summary>
    public class MappingTemplate
    {
        /// <summary>
        /// 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 템플릿 타입
        /// </summary>
        [JsonProperty("template_type")]
        public string TemplateType { get; set; } = string.Empty;

        /// <summary>
        /// 버전
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// 사용 가이드
    /// </summary>
    public class UsageGuide
    {
        /// <summary>
        /// 새 매핑 추가 방법
        /// </summary>
        [JsonProperty("how_to_add_new_mapping")]
        public Dictionary<string, string> HowToAddNewMapping { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 필드 설명
        /// </summary>
        [JsonProperty("field_descriptions")]
        public Dictionary<string, string> FieldDescriptions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 데이터 타입 매핑
        /// </summary>
        [JsonProperty("data_type_mapping")]
        public Dictionary<string, string> DataTypeMapping { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 특별 처리 타입
        /// </summary>
        [JsonProperty("special_handling_types")]
        public Dictionary<string, string> SpecialHandlingTypes { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 검증 규칙을 담는 모델
    /// </summary>
    public class ValidationRules
    {
        /// <summary>
        /// 필수 필드 목록
        /// </summary>
        [JsonProperty("required_fields")]
        public List<string> RequiredFields { get; set; } = new List<string>();

        /// <summary>
        /// 숫자 필드 목록
        /// </summary>
        [JsonProperty("numeric_fields")]
        public List<string> NumericFields { get; set; } = new List<string>();

        /// <summary>
        /// 날짜 필드 목록
        /// </summary>
        [JsonProperty("date_fields")]
        public List<string> DateFields { get; set; } = new List<string>();

        /// <summary>
        /// 소수점 필드 목록
        /// </summary>
        [JsonProperty("decimal_fields")]
        public List<string> DecimalFields { get; set; } = new List<string>();
    }

    /// <summary>
    /// 데이터 변환 규칙을 담는 모델
    /// </summary>
    public class DataTransformation
    {
        /// <summary>
        /// 특별 처리 규칙
        /// </summary>
        [JsonProperty("special_handling")]
        public string? SpecialHandling { get; set; }

        /// <summary>
        /// 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 타입 변환
        /// </summary>
        [JsonProperty("data_type_conversion")]
        public string? DataTypeConversion { get; set; }

        /// <summary>
        /// 기본값
        /// </summary>
        [JsonProperty("default_value")]
        public object? DefaultValue { get; set; }
    }
} 