using System.Collections.Generic;

namespace LogisticManager.Models
{
    /// <summary>
    /// 엑셀 컬럼과 데이터베이스 컬럼 간의 매핑 정보를 담는 모델
    /// </summary>
    public class ColumnMapping
    {
        /// <summary>
        /// 엑셀 컬럼명
        /// </summary>
        public string ExcelColumn { get; set; } = string.Empty;

        /// <summary>
        /// 데이터베이스 컬럼명
        /// </summary>
        public string DatabaseColumn { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 타입
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 필수 필드 여부
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 기본값
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 특별 처리 규칙
        /// </summary>
        public string? SpecialHandling { get; set; }
    }

    /// <summary>
    /// 테이블 매핑 정보를 담는 모델
    /// </summary>
    public class TableMapping
    {
        /// <summary>
        /// 테이블명
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 컬럼 매핑 정보
        /// </summary>
        public Dictionary<string, ColumnMapping> Columns { get; set; } = new Dictionary<string, ColumnMapping>();
    }

    /// <summary>
    /// 매핑 설정 전체를 담는 모델
    /// </summary>
    public class MappingConfiguration
    {
        /// <summary>
        /// 버전
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 테이블 매핑 정보
        /// </summary>
        public Dictionary<string, TableMapping> Mappings { get; set; } = new Dictionary<string, TableMapping>();

        /// <summary>
        /// 검증 규칙
        /// </summary>
        public ValidationRules ValidationRules { get; set; } = new ValidationRules();

        /// <summary>
        /// 데이터 변환 규칙
        /// </summary>
        public Dictionary<string, DataTransformation> DataTransformations { get; set; } = new Dictionary<string, DataTransformation>();
    }

    /// <summary>
    /// 검증 규칙을 담는 모델
    /// </summary>
    public class ValidationRules
    {
        /// <summary>
        /// 필수 필드 목록
        /// </summary>
        public List<string> RequiredFields { get; set; } = new List<string>();

        /// <summary>
        /// 숫자 필드 목록
        /// </summary>
        public List<string> NumericFields { get; set; } = new List<string>();

        /// <summary>
        /// 날짜 필드 목록
        /// </summary>
        public List<string> DateFields { get; set; } = new List<string>();
    }

    /// <summary>
    /// 데이터 변환 규칙을 담는 모델
    /// </summary>
    public class DataTransformation
    {
        /// <summary>
        /// 특별 처리 규칙
        /// </summary>
        public string? SpecialHandling { get; set; }

        /// <summary>
        /// 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 타입 변환
        /// </summary>
        public string? DataTypeConversion { get; set; }

        /// <summary>
        /// 기본값
        /// </summary>
        public object? DefaultValue { get; set; }
    }
} 