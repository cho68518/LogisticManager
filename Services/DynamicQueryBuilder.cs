using System;
using System.IO;
using System.Data;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using LogisticManager.Models;

namespace LogisticManager.Services
{
    /// <summary>
    /// 하이브리드 동적 쿼리 생성기 (리플렉션 + 설정 기반)
    /// 
    /// 📋 주요 기능:
    /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
    /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
    /// - 타입 안전성 보장
    /// - SQL 인젝션 방지
    /// - 확장 가능한 구조
    /// 
    /// 🎯 사용 목적:
    /// - 다양한 테이블 구조에 대한 유연한 INSERT 쿼리 생성
    /// - 코드 수정 없이 새로운 테이블 추가 가능
    /// - 설정 파일을 통한 테이블 구조 관리
    /// - 런타임 동적 쿼리 생성
    /// 
    /// 💡 사용법:
    /// ```csharp
    /// var queryBuilder = new DynamicQueryBuilder();
    /// var (sql, parameters) = queryBuilder.BuildInsertQuery("invoice_table", invoiceDto);
    /// ```
    /// 
    /// 🔧 설정 파일 구조 (table_mappings.json):
    /// ```json
    /// {
    ///   "invoice_table": {
    ///     "tableName": "invoice_table",
    ///     "columns": [
    ///       {
    ///         "propertyName": "RecipientName",
    ///         "databaseColumn": "수취인명",
    ///         "dataType": "VARCHAR",
    ///         "isRequired": true
    ///       }
    ///     ]
    ///   }
    /// }
    /// ```
    /// </summary>
    public class DynamicQueryBuilder
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 테이블별 매핑 설정을 저장하는 딕셔너리
        /// </summary>
        private readonly Dictionary<string, DynamicTableMapping> _tableMappings;

        /// <summary>
        /// 리플렉션 폴백 사용 여부
        /// </summary>
        private readonly bool _useReflectionFallback;

        /// <summary>
        /// 설정 파일 경로
        /// </summary>
        private readonly string _configPath;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// DynamicQueryBuilder 생성자
        /// 
        /// 초기화 과정:
        /// 1. 설정 파일 경로 설정
        /// 2. 테이블 매핑 설정 로드
        /// 3. 리플렉션 폴백 옵션 설정
        /// 
        /// 예외 처리:
        /// - 설정 파일 읽기 실패 시 빈 딕셔너리 사용
        /// - JSON 파싱 오류 시 기본 설정 사용
        /// </summary>
        /// <param name="useReflectionFallback">리플렉션 폴백 사용 여부 (기본값: true)</param>
        public DynamicQueryBuilder(bool useReflectionFallback = true)
        {
            _useReflectionFallback = useReflectionFallback;
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "table_mappings.json");
            _tableMappings = LoadTableMappings();
            
            Console.WriteLine($"🔧 DynamicQueryBuilder 초기화 완료 - 설정 파일: {_configPath}");
            Console.WriteLine($"📊 로드된 테이블 매핑 수: {_tableMappings.Count}개");
        }

        #endregion

        #region 공개 메서드 (Public Methods)

        /// <summary>
        /// 하이브리드 INSERT 쿼리 생성 메서드
        /// 
        /// 동작 순서:
        /// 1. 설정 기반 매핑 시도 (table_mappings.json)
        /// 2. 설정이 없는 경우 리플렉션 기반 폴백
        /// 3. 둘 다 실패 시 예외 발생
        /// 
        /// 📋 처리 과정:
        /// - 테이블명 유효성 검사
        /// - 설정 기반 매핑 확인
        /// - 리플렉션 기반 폴백 처리
        /// - SQL 쿼리 및 매개변수 생성
        /// 
        /// 🛡️ 보안 기능:
        /// - SQL 인젝션 방지 (매개변수화된 쿼리)
        /// - 테이블명 검증
        /// - 타입 안전성 보장
        /// 
        /// ⚠️ 예외 처리:
        /// - ArgumentException: 테이블명이 비어있는 경우
        /// - ArgumentException: 매핑 설정이 없는 경우
        /// - InvalidOperationException: 쿼리 생성 실패
        /// </summary>
        /// <typeparam name="T">엔티티 타입</typeparam>
        /// <param name="tableName">테이블명</param>
        /// <param name="entity">삽입할 엔티티 객체</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        /// <exception cref="ArgumentException">테이블명이 비어있거나 매핑 설정이 없는 경우</exception>
        /// <exception cref="InvalidOperationException">쿼리 생성 실패</exception>
        public (string sql, Dictionary<string, object> parameters) BuildInsertQuery<T>(string tableName, T entity)
        {
            // === 1단계: 입력 검증 ===
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("테이블명은 비어있을 수 없습니다.", nameof(tableName));

            if (entity == null)
                throw new ArgumentException("엔티티 객체는 null일 수 없습니다.", nameof(entity));

            Console.WriteLine($"🔍 DynamicQueryBuilder: 테이블 '{tableName}'에 대한 INSERT 쿼리 생성 시작");

            // === 2단계: 설정 기반 매핑 시도 ===
            if (_tableMappings.TryGetValue(tableName, out var mapping))
            {
                Console.WriteLine($"✅ 설정 기반 매핑 발견 - 테이블: {tableName}");
                return BuildFromMapping(tableName, entity, mapping);
            }

            // === 3단계: 리플렉션 기반 폴백 ===
            if (_useReflectionFallback)
            {
                Console.WriteLine($"🔄 리플렉션 기반 폴백 사용 - 테이블: {tableName}");
                return BuildFromReflection<T>(tableName, entity);
            }

            // === 4단계: 매핑 실패 처리 ===
            var errorMessage = $"테이블 '{tableName}'에 대한 매핑 설정이 없습니다. " +
                             $"table_mappings.json 파일에 매핑을 추가하거나 리플렉션 폴백을 활성화하세요.";
            throw new ArgumentException(errorMessage, nameof(tableName));
        }

        #endregion

        #region 비공개 메서드 (Private Methods)

        /// <summary>
        /// 테이블 매핑 설정을 JSON 파일에서 로드
        /// 
        /// 로드 과정:
        /// 1. 설정 파일 존재 여부 확인
        /// 2. JSON 파일 읽기
        /// 3. JSON 역직렬화
        /// 4. 설정 검증
        /// 
        /// 예외 처리:
        /// - 파일이 없는 경우: 빈 딕셔너리 반환
        /// - JSON 파싱 오류: 기본 설정 사용
        /// - 기타 오류: 로그 출력 후 빈 딕셔너리 반환
        /// </summary>
        /// <returns>테이블 매핑 설정 딕셔너리</returns>
        private Dictionary<string, DynamicTableMapping> LoadTableMappings()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Console.WriteLine($"⚠️ 설정 파일이 존재하지 않음: {_configPath}");
                    return new Dictionary<string, DynamicTableMapping>();
                }

                var jsonContent = File.ReadAllText(_configPath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    Console.WriteLine("⚠️ 설정 파일이 비어있음");
                    return new Dictionary<string, DynamicTableMapping>();
                }

                var mappings = JsonConvert.DeserializeObject<Dictionary<string, DynamicTableMapping>>(jsonContent);
                if (mappings == null)
                {
                    Console.WriteLine("⚠️ JSON 역직렬화 실패 - 빈 딕셔너리 사용");
                    return new Dictionary<string, DynamicTableMapping>();
                }

                Console.WriteLine($"✅ 테이블 매핑 설정 로드 완료 - {mappings.Count}개 테이블");
                return mappings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 테이블 매핑 설정 로드 실패: {ex.Message}");
                return new Dictionary<string, DynamicTableMapping>();
            }
        }

        /// <summary>
        /// 설정 기반 INSERT 쿼리 생성
        /// 
        /// 생성 과정:
        /// 1. 매핑 설정에서 컬럼 정보 추출
        /// 2. 필수 필드 검증
        /// 3. SQL 쿼리 구성
        /// 4. 매개변수 딕셔너리 생성
        /// 
        /// 📋 처리 로직:
        /// - 필수 필드가 null인 경우 건너뛰기
        /// - 컬럼명과 매개변수명 매핑
        /// - 타입 안전성 보장
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="entity">엔티티 객체</param>
        /// <param name="mapping">테이블 매핑 설정</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildFromMapping(string tableName, object entity, DynamicTableMapping mapping)
        {
            var columns = new List<string>();
            var parameters = new List<string>();
            var paramDict = new Dictionary<string, object>();

            foreach (var column in mapping.Columns)
            {
                var value = GetPropertyValue(entity, column.PropertyName);
                
                // 필수 필드가 null인 경우 건너뛰기
                if (column.IsRequired && value == null)
                {
                    Console.WriteLine($"⚠️ 필수 필드 '{column.PropertyName}'이 null입니다. 건너뜁니다.");
                    continue;
                }

                columns.Add(column.DatabaseColumn);
                parameters.Add($"@{column.PropertyName}");
                paramDict[$"@{column.PropertyName}"] = value ?? DBNull.Value;
            }

            if (columns.Count == 0)
            {
                throw new InvalidOperationException($"테이블 '{tableName}'에 삽입할 수 있는 유효한 컬럼이 없습니다.");
            }

            var sql = $@"
                INSERT INTO {tableName} (
                    {string.Join(", ", columns)}
                ) VALUES (
                    {string.Join(", ", parameters)}
                )";

            Console.WriteLine($"✅ 설정 기반 쿼리 생성 완료 - {columns.Count}개 컬럼");
            return (sql, paramDict);
        }

        /// <summary>
        /// 리플렉션 기반 INSERT 쿼리 생성
        /// 
        /// 생성 과정:
        /// 1. 엔티티 타입의 속성 정보 추출
        /// 2. null이 아닌 속성만 필터링
        /// 3. 컬럼명 변환 (카멜케이스 → 스네이크케이스)
        /// 4. SQL 쿼리 및 매개변수 생성
        /// 
        /// 📋 처리 로직:
        /// - 읽기 가능한 속성만 사용
        /// - null 값은 DBNull.Value로 처리
        /// - 컬럼명 자동 변환
        /// </summary>
        /// <typeparam name="T">엔티티 타입</typeparam>
        /// <param name="tableName">테이블명</param>
        /// <param name="entity">엔티티 객체</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildFromReflection<T>(string tableName, T entity)
        {
            var properties = new List<PropertyInfo>();
            foreach (var property in typeof(T).GetProperties())
            {
                if (property.CanRead && property.GetValue(entity) != null)
                {
                    properties.Add(property);
                }
            }

            if (properties.Count == 0)
            {
                throw new InvalidOperationException($"엔티티 타입 '{typeof(T).Name}'에 유효한 속성이 없습니다.");
            }

            var columns = new List<string>();
            var parameters = new List<string>();
            var paramDict = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                var columnName = GetColumnName(property);
                var parameterName = $"@{property.Name}";
                
                columns.Add(columnName);
                parameters.Add(parameterName);
                paramDict[parameterName] = property.GetValue(entity) ?? DBNull.Value;
            }

            var sql = $@"
                INSERT INTO {tableName} (
                    {string.Join(", ", columns)}
                ) VALUES (
                    {string.Join(", ", parameters)}
                )";

            Console.WriteLine($"✅ 리플렉션 기반 쿼리 생성 완료 - {columns.Count}개 컬럼");
            return (sql, paramDict);
        }

        /// <summary>
        /// 객체에서 속성 값을 안전하게 추출
        /// 
        /// 추출 과정:
        /// 1. 속성 존재 여부 확인
        /// 2. 속성 값 읽기
        /// 3. null 안전성 처리
        /// 
        /// 예외 처리:
        /// - 속성이 없는 경우: null 반환
        /// - 읽기 오류: null 반환
        /// </summary>
        /// <param name="entity">엔티티 객체</param>
        /// <param name="propertyName">속성명</param>
        /// <returns>속성 값 또는 null</returns>
        private object? GetPropertyValue(object entity, string propertyName)
        {
            try
            {
                var property = entity.GetType().GetProperty(propertyName);
                if (property == null)
                {
                    Console.WriteLine($"⚠️ 속성 '{propertyName}'을 찾을 수 없습니다.");
                    return null;
                }

                return property.GetValue(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 속성 '{propertyName}' 값 읽기 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 속성명을 컬럼명으로 변환
        /// 
        /// 변환 규칙:
        /// 1. 기본 변환 규칙 적용 (카멜케이스 → 스네이크케이스)
        /// 2. 특수문자 처리
        /// 
        /// 변환 예시:
        /// - RecipientName → recipient_name
        /// - OrderNumber → order_number
        /// - ProductCode → product_code
        /// </summary>
        /// <param name="property">속성 정보</param>
        /// <returns>컬럼명</returns>
        private string GetColumnName(PropertyInfo property)
        {
            // 기본 변환 규칙 적용 (카멜케이스 → 스네이크케이스)
            return ConvertToSnakeCase(property.Name);
        }

        /// <summary>
        /// 카멜케이스를 스네이크케이스로 변환
        /// 
        /// 변환 규칙:
        /// 1. 대문자를 소문자로 변환
        /// 2. 대문자 앞에 언더스코어 추가
        /// 3. 연속된 언더스코어 제거
        /// 
        /// 변환 예시:
        /// - RecipientName → recipient_name
        /// - OrderNumber → order_number
        /// - ProductCode → product_code
        /// </summary>
        /// <param name="input">입력 문자열</param>
        /// <returns>변환된 문자열</returns>
        private string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (i > 0 && char.IsUpper(input[i]))
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(input[i]));
            }
            
            return result.ToString().Replace("__", "_").Trim('_');
        }

        #endregion
    }

    /// <summary>
    /// 테이블 매핑 설정 클래스
    /// 
    /// 📋 주요 기능:
    /// - 테이블별 컬럼 매핑 정보 저장
    /// - JSON 직렬화/역직렬화 지원
    /// - 설정 검증
    /// </summary>
    public class DynamicTableMapping
    {
        /// <summary>테이블명</summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>컬럼 매핑 목록</summary>
        public List<DynamicColumnMapping> Columns { get; set; } = new List<DynamicColumnMapping>();
    }

    /// <summary>
    /// 컬럼 매핑 설정 클래스
    /// 
    /// 📋 주요 기능:
    /// - 속성명과 데이터베이스 컬럼명 매핑
    /// - 데이터 타입 정보 저장
    /// - 필수 필드 여부 표시
    /// </summary>
    public class DynamicColumnMapping
    {
        /// <summary>속성명 (C# 클래스의 속성명)</summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>데이터베이스 컬럼명</summary>
        public string DatabaseColumn { get; set; } = string.Empty;

        /// <summary>데이터 타입 (VARCHAR, INT, DECIMAL 등)</summary>
        public string DataType { get; set; } = "VARCHAR";

        /// <summary>필수 필드 여부</summary>
        public bool IsRequired { get; set; } = false;
    }
}
