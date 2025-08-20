using System;
using System.IO;
using System.Data;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using LogisticManager.Models;
using System.Linq; // Added for .Where() and .ToList()
using LogisticManager.Services;

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
        public DynamicQueryBuilder(bool useReflectionFallback = false)
        {
            _useReflectionFallback = useReflectionFallback;
            // 테이블별 매핑 파일이 있는 폴더 경로 설정 (프로젝트 루트에서 찾기)
            _configPath = Path.Combine(LogPathManager.GetProjectRootDirectory(), "config", "table_mappings");
            _tableMappings = LoadTableMappings();
            
            var initLog = $"🔧 DynamicQueryBuilder 초기화 완료 - 매핑 폴더: {_configPath}";
            var countLog = $"📊 로드된 테이블 매핑 수: {_tableMappings.Count}개";
            LogManagerService.LogInfo(initLog);
            LogManagerService.LogInfo(countLog);
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

            var startLog = $"🔍 DynamicQueryBuilder: 테이블 '{tableName}'에 대한 INSERT 쿼리 생성 시작";
            var debugLog1 = $"[DEBUG] 전달받은 테이블명: {tableName}";
            var debugLog2 = $"[DEBUG] 로드된 테이블 매핑 수: {_tableMappings.Count}";
            var debugLog3 = $"[DEBUG] 사용 가능한 테이블들: {string.Join(", ", _tableMappings.Keys)}";
            LogManagerService.LogInfo(startLog);
            LogManagerService.LogDebug(debugLog1);
            LogManagerService.LogDebug(debugLog2);
            LogManagerService.LogDebug(debugLog3);

            // === 2단계: 설정 기반 매핑 시도 ===
            if (_tableMappings.TryGetValue(tableName, out var mapping))
            {
                var mappingFoundLog = $"✅ 설정 기반 매핑 발견 - 테이블: {tableName}";
                var columnCountLog = $"[DEBUG] 매핑된 컬럼 수: {mapping.Columns.Count}";
                LogManagerService.LogInfo(mappingFoundLog);
                LogManagerService.LogDebug(columnCountLog);
                return BuildFromMapping(tableName, entity, mapping);
            }

            var notFoundLog = $"❌ 테이블 '{tableName}'에 대한 매핑을 찾을 수 없음";
            LogManagerService.LogWarning(notFoundLog);

            // === 3단계: 리플렉션 기반 폴백 ===
            if (_useReflectionFallback)
            {
                var fallbackLog = $"🔄 리플렉션 기반 폴백 사용 - 테이블: {tableName}";
                LogManagerService.LogInfo(fallbackLog);
                // [사용 목적]
                // - 테이블 매핑 설정(table_mappings.json)이 없는 경우, 엔티티의 public 속성을 자동으로 DB 컬럼에 매핑하여 INSERT 쿼리를 생성한다.
                // - 신규 엔티티 타입, 임시 테이블, 테스트 데이터 등 매핑 미정의 상황에서 기본 INSERT 기능을 제공한다.
                //
                // [사용 방법]
                // - BuildInsertQuery("테이블명", entity) 호출 시, 해당 테이블에 대한 매핑이 없으면 아래 리플렉션 기반 쿼리 생성 로직이 실행된다.
                // - entity 객체의 null이 아닌 public 속성이 자동으로 컬럼으로 변환되어 INSERT 쿼리와 매개변수 딕셔너리가 반환된다.
                // - 반환값: (생성된 INSERT SQL 쿼리, 매개변수 딕셔너리)
                return BuildFromReflection<T>(tableName, entity);
            }

            // === 4단계: 매핑 실패 처리 ===
            var errorMessage = $"테이블 '{tableName}'에 대한 매핑 설정이 없습니다. " +
                             $"table_mappings.json 파일에 매핑을 추가하거나 리플렉션 폴백을 활성화하세요.";
            throw new ArgumentException(errorMessage, nameof(tableName));
        }

        /// <summary>
        /// 하이브리드 UPDATE 쿼리 생성 (설정 기반 + 리플렉션 폴백)
        /// 
        /// 📋 주요 기능:
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - WHERE 조건 동적 생성
        /// - 타입 안전성 보장
        /// - SQL 인젝션 방지
        /// - 확장 가능한 구조
        /// 
        /// 🎯 동작 순서:
        /// 1. 테이블명 유효성 검사
        /// 2. 설정 기반 매핑 시도 (table_mappings.json)
        /// 3. 설정이 없는 경우 리플렉션 기반 폴백
        /// 4. WHERE 조건 생성 (기본키 또는 지정된 조건)
        /// 5. 둘 다 실패 시 예외 발생
        /// 
        /// 🛡️ 보안 기능:
        /// - SQL 인젝션 방지 (매개변수화된 쿼리)
        /// - 테이블명 검증
        /// - 타입 안전성 보장
        /// 
        /// ⚠️ 예외 처리:
        /// - ArgumentException: 테이블명이 비어있거나 매핑 설정이 없는 경우
        /// - InvalidOperationException: 쿼리 생성 실패
        /// 
        /// 💡 사용법:
        /// ```csharp
        /// // 기본 사용법 (기본키 기반 업데이트)
        /// var (sql, parameters) = queryBuilder.BuildUpdateQuery("invoice_table", invoiceDto);
        /// 
        /// // WHERE 조건 지정
        /// var (sql, parameters) = queryBuilder.BuildUpdateQuery("invoice_table", invoiceDto, "OrderNumber = @OrderNumber");
        /// 
        /// // 복합 조건 지정
        /// var (sql, parameters) = queryBuilder.BuildUpdateQuery("invoice_table", invoiceDto, "RecipientName = @RecipientName AND OrderDate = @OrderDate");
        /// ```
        /// 
        /// 🔧 설정 파일 구조 (table_mappings.json):
        /// ```json
        /// {
        ///   "invoice_table": {
        ///     "tableName": "invoice_table",
        ///     "primaryKey": "OrderNumber",
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
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="entity">업데이트할 엔티티 객체</param>
        /// <param name="whereClause">WHERE 조건 (선택사항, 기본값: 기본키 기반)</param>
        /// <returns>SQL 쿼리와 매개변수</returns>
        /// <exception cref="ArgumentException">테이블명이 비어있거나 매핑 설정이 없는 경우</exception>
        /// <exception cref="InvalidOperationException">쿼리 생성 실패</exception>
        public (string sql, Dictionary<string, object> parameters) BuildUpdateQuery<T>(string tableName, T entity, string? whereClause = null)
        {
            // === 1단계: 입력 검증 ===
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("테이블명은 비어있을 수 없습니다.", nameof(tableName));

            if (entity == null)
                throw new ArgumentException("엔티티 객체는 null일 수 없습니다.", nameof(entity));

            var updateStartLog = $"🔍 DynamicQueryBuilder: 테이블 '{tableName}'에 대한 UPDATE 쿼리 생성 시작";
            LogManagerService.LogInfo(updateStartLog);

            // === 2단계: 설정 기반 매핑 시도 ===
            if (_tableMappings.TryGetValue(tableName, out var mapping))
            {
                var updateMappingFoundLog = $"✅ 설정 기반 매핑 발견 - 테이블: {tableName}";
                LogManagerService.LogInfo(updateMappingFoundLog);
                return BuildUpdateFromMapping(tableName, entity, mapping, whereClause);
            }

            // === 3단계: 리플렉션 기반 폴백 ===
            if (_useReflectionFallback)
            {
                var fallbackLog = $"🔄 리플렉션 기반 폴백 사용 - 테이블: {tableName}";
                LogManagerService.LogInfo(fallbackLog);
                return BuildUpdateFromReflection<T>(tableName, entity, whereClause);
            }

            // === 4단계: 매핑 실패 처리 ===
            var errorMessage = $"테이블 '{tableName}'에 대한 매핑 설정이 없습니다. " +
                             $"table_mappings.json 파일에 매핑을 추가하거나 리플렉션 폴백을 활성화하세요.";
            throw new ArgumentException(errorMessage, nameof(tableName));
        }

        /// <summary>
        /// 하이브리드 DELETE 쿼리 생성 (설정 기반 + 리플렉션 폴백)
        /// 
        /// 📋 주요 기능:
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - WHERE 조건 동적 생성
        /// - 타입 안전성 보장
        /// - SQL 인젝션 방지
        /// 
        /// 🎯 동작 순서:
        /// 1. 테이블명 유효성 검사
        /// 2. 설정 기반 매핑 시도 (table_mappings.json)
        /// 3. 설정이 없는 경우 리플렉션 기반 폴백
        /// 4. WHERE 조건 생성 (기본키 또는 지정된 조건)
        /// 5. 둘 다 실패 시 예외 발생
        /// 
        /// 💡 사용법:
        /// ```csharp
        /// // 기본 사용법 (기본키 기반 삭제)
        /// var (sql, parameters) = queryBuilder.BuildDeleteQuery("invoice_table", invoiceDto);
        /// 
        /// // WHERE 조건 지정
        /// var (sql, parameters) = queryBuilder.BuildDeleteQuery("invoice_table", invoiceDto, "OrderNumber = @OrderNumber");
        /// ```
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="entity">삭제할 엔티티 객체</param>
        /// <param name="whereClause">WHERE 조건 (선택사항, 기본값: 기본키 기반)</param>
        /// <returns>SQL 쿼리와 매개변수</returns>
        /// <exception cref="ArgumentException">테이블명이 비어있거나 매핑 설정이 없는 경우</exception>
        /// <exception cref="InvalidOperationException">쿼리 생성 실패</exception>
        public (string sql, Dictionary<string, object> parameters) BuildDeleteQuery<T>(string tableName, T entity, string? whereClause = null)
        {
            // === 1단계: 입력 검증 ===
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("테이블명은 비어있을 수 없습니다.", nameof(tableName));

            if (entity == null)
                throw new ArgumentException("엔티티 객체는 null일 수 없습니다.", nameof(entity));

            LogManagerService.LogInfo($"🔍 DynamicQueryBuilder: 테이블 '{tableName}'에 대한 DELETE 쿼리 생성 시작");

            // === 2단계: 설정 기반 매핑 시도 ===
            if (_tableMappings.TryGetValue(tableName, out var mapping))
            {
                var deleteMappingFoundLog = $"✅ 설정 기반 매핑 발견 - 테이블: {tableName}";
                LogManagerService.LogInfo(deleteMappingFoundLog);
                return BuildDeleteFromMapping(tableName, entity, mapping, whereClause);
            }

            // === 3단계: 리플렉션 기반 폴백 ===
            if (_useReflectionFallback)
            {
                var fallbackLog = $"🔄 리플렉션 기반 폴백 사용 - 테이블: {tableName}";
                LogManagerService.LogInfo(fallbackLog);
                return BuildDeleteFromReflection<T>(tableName, entity, whereClause);
            }

            // === 4단계: 매핑 실패 처리 ===
            var errorMessage = $"테이블 '{tableName}'에 대한 매핑 설정이 없습니다. " +
                             $"table_mappings.json 파일에 매핑을 추가하거나 리플렉션 폴백을 활성화하세요.";
            throw new ArgumentException(errorMessage, nameof(tableName));
        }

        /// <summary>
        /// 하이브리드 TRUNCATE TABLE 쿼리 생성 (설정 기반 + 리플렉션 폴백)
        /// 
        /// 📋 주요 기능:
        /// - 설정 기반 매핑 우선 적용 (table_mappings.json)
        /// - 리플렉션 기반 폴백 지원 (설정이 없는 경우)
        /// - 테이블명 유효성 검사
        /// - SQL 인젝션 방지
        /// - 확장 가능한 구조
        /// 
        /// 🎯 동작 순서:
        /// 1. 테이블명 유효성 검사
        /// 2. 설정 기반 매핑 시도 (table_mappings.json)
        /// 3. 설정이 없는 경우 리플렉션 기반 폴백
        /// 4. TRUNCATE TABLE 쿼리 생성
        /// 5. 둘 다 실패 시 예외 발생
        /// 
        /// 🛡️ 보안 기능:
        /// - SQL 인젝션 방지 (테이블명 검증)
        /// - 테이블명 검증
        /// - 위험한 SQL 키워드 차단
        /// 
        /// ⚠️ 예외 처리:
        /// - ArgumentException: 테이블명이 비어있거나 매핑 설정이 없는 경우
        /// - InvalidOperationException: 쿼리 생성 실패
        /// 
        /// 💡 사용법:
        /// ```csharp
        /// // 기본 사용법
        /// var (sql, parameters) = queryBuilder.BuildTruncateQuery("invoice_table");
        /// 
        /// // 커스텀 테이블명 사용
        /// var (sql, parameters) = queryBuilder.BuildTruncateQuery("custom_table_name");
        /// ```
        /// 
        /// 🔧 설정 파일 구조 (table_mappings.json):
        /// ```json
        /// {
        ///   "invoice_table": {
        ///     "tableName": "invoice_table",
        ///     "primaryKey": "OrderNumber",
        ///     "columns": [...]
        ///   }
        /// }
        /// ```
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <returns>SQL 쿼리와 매개변수</returns>
        /// <exception cref="ArgumentException">테이블명이 비어있거나 매핑 설정이 없는 경우</exception>
        /// <exception cref="InvalidOperationException">쿼리 생성 실패</exception>
        public (string sql, Dictionary<string, object> parameters) BuildTruncateQuery(string tableName)
        {
            // === 1단계: 입력 검증 ===
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("테이블명은 비어있을 수 없습니다.", nameof(tableName));

            LogManagerService.LogInfo($"🔍 DynamicQueryBuilder: 테이블 '{tableName}'에 대한 TRUNCATE 쿼리 생성 시작");

            // === 2단계: 테이블명 보안 검증 ===
            if (!IsValidTableName(tableName))
            {
                var errorMessage = $"테이블명 '{tableName}'에 위험한 문자가 포함되어 있습니다.";
                throw new ArgumentException(errorMessage, nameof(tableName));
            }

            // === 3단계: 설정 기반 매핑 확인 (선택사항) ===
            if (_tableMappings.TryGetValue(tableName, out var mapping))
            {
                var truncateMappingFoundLog = $"✅ 설정 기반 매핑 발견 - 테이블: {tableName}";
                LogManagerService.LogInfo(truncateMappingFoundLog);
                return BuildTruncateFromMapping(tableName, mapping);
            }

            // === 4단계: 기본 TRUNCATE 쿼리 생성 ===
            var defaultTruncateLog = $"🔄 기본 TRUNCATE 쿼리 생성 - 테이블: {tableName}";
            LogManagerService.LogInfo(defaultTruncateLog);
            return BuildTruncateFromReflection(tableName);
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
                if (!Directory.Exists(_configPath))
                {
                    var dirNotFoundLog = $"⚠️ 매핑 폴더가 존재하지 않음: {_configPath}";
                    LogManagerService.LogWarning(dirNotFoundLog);
                    return new Dictionary<string, DynamicTableMapping>();
                }

                var mappings = new Dictionary<string, DynamicTableMapping>();
                var jsonFiles = Directory.GetFiles(_configPath, "*.json");

                foreach (var jsonFile in jsonFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(jsonFile);
                        if (fileName == "index" || fileName == "README") continue; // 인덱스 파일 제외

                        var jsonContent = File.ReadAllText(jsonFile, Encoding.UTF8);
                        if (string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var emptyFileLog = $"⚠️ 매핑 파일이 비어있음: {fileName}";
                            LogManagerService.LogWarning(emptyFileLog);
                            continue;
                        }

                        // 테이블별 매핑 파일을 DynamicTableMapping 형식으로 변환
                        var tableMapping = ConvertTableMappingFile(jsonContent, fileName);
                        if (tableMapping != null)
                        {
                            mappings[fileName] = tableMapping;
                            var tableLog = $"  📋 테이블 로드: {fileName} ({tableMapping.Columns.Count}개 컬럼)";
                            LogManagerService.LogInfo(tableLog);
                        }
                    }
                    catch (Exception ex)
                    {
                        var fileErrorLog = $"⚠️ 매핑 파일 로드 실패 ({Path.GetFileName(jsonFile)}): {ex.Message}";
                        LogManagerService.LogWarning(fileErrorLog);
                    }
                }

                var loadCompleteLog = $"✅ 테이블 매핑 설정 로드 완료 - {mappings.Count}개 테이블";
                LogManagerService.LogInfo(loadCompleteLog);
                
                return mappings;
            }
            catch (Exception ex)
            {
                var loadFailLog = $"❌ 테이블 매핑 설정 로드 실패: {ex.Message}";
                LogManagerService.LogError(loadFailLog);
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

                                                        var startLog = $"[DynamicQueryBuilder] 설정 기반 쿼리 생성 시작 - 테이블: {tableName}";
            var columnCountLog = $"[DynamicQueryBuilder] 매핑된 컬럼 수: {mapping.Columns.Count}";
            
            LogManagerService.LogInfo(startLog);
            LogManagerService.LogDebug(columnCountLog);

            foreach (var column in mapping.Columns)
            {
                // INSERT에서 제외되는 컬럼 체크 (예: AUTO_INCREMENT 컬럼)
                if (column.ExcludeFromInsert)
                {
                    var excludeLog = $"[DynamicQueryBuilder] INSERT에서 제외: {column.PropertyName} (excludeFromInsert=true)";
                    LogManagerService.LogDebug(excludeLog);
                    continue;
                }

                var value = GetPropertyValue(entity, column.PropertyName);
                
                // 모든 컬럼을 포함 (null이어도 포함)
                // 특수문자가 포함된 컬럼명은 백틱으로 감싸기
                var safeColumnName = GetSafeColumnName(column.DatabaseColumn);
                columns.Add(safeColumnName);
                parameters.Add($"@{column.PropertyName}");
                paramDict[$"@{column.PropertyName}"] = value ?? DBNull.Value;
                
                // 디버깅: 컬럼명 변환 로그 추가
                var debugLog = $"[DynamicQueryBuilder] 컬럼명 변환: '{column.DatabaseColumn}' → '{safeColumnName}'";
                LogManagerService.LogDebug(debugLog);
                
                var mappingLog = $"[DynamicQueryBuilder] 컬럼 매핑: {column.PropertyName} → {column.DatabaseColumn} = {value ?? "(null)"}";
                LogManagerService.LogDebug(mappingLog);
            }

            if (columns.Count == 0)
            {
                throw new InvalidOperationException($"테이블 '{tableName}'에 대한 유효한 컬럼이 없습니다.");
            }

            var columnList = string.Join(", ", columns);
            var columnListLog = $"[DynamicQueryBuilder] 컬럼 목록: {columnList}";
            LogManagerService.LogDebug(columnListLog);
            
            var sql = $"INSERT INTO {tableName} ({columnList}) VALUES ({string.Join(", ", parameters)})";
            
            var sqlLog = $"[DynamicQueryBuilder] 생성된 SQL: {sql}";
            var paramLog = $"[DynamicQueryBuilder] 매개변수: {string.Join(", ", paramDict.Select(p => $"{p.Key}={p.Value}"))}";
            
            LogManagerService.LogDebug(sqlLog);
            LogManagerService.LogDebug(paramLog);
            
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

            var reflectionCompleteLog = $"✅ 리플렉션 기반 쿼리 생성 완료 - {columns.Count}개 컬럼";
            LogManagerService.LogInfo(reflectionCompleteLog);
            return (sql, paramDict);
        }

        /// <summary>
        /// 설정 기반 UPDATE 쿼리 생성
        /// 
        /// 생성 과정:
        /// 1. 매핑 설정에서 컬럼 정보 추출
        /// 2. 필수 필드 검증
        /// 3. WHERE 조건 처리
        /// 4. SQL 쿼리 구성
        /// 5. 매개변수 딕셔너리 생성
        /// 
        /// 📋 처리 로직:
        /// - 필수 필드가 null인 경우 건너뛰기
        /// - 컬럼명과 매개변수명 매핑
        /// - 타입 안전성 보장
        /// - WHERE 조건 처리
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="entity">업데이트할 엔티티 객체</param>
        /// <param name="mapping">테이블 매핑 설정</param>
        /// <param name="whereClause">WHERE 조건 (선택사항)</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildUpdateFromMapping(string tableName, object entity, DynamicTableMapping mapping, string? whereClause)
        {
            var setClauses = new List<string>();
            var paramDict = new Dictionary<string, object>();

            foreach (var column in mapping.Columns)
            {
                var value = GetPropertyValue(entity, column.PropertyName);
                
                // 필수 필드가 null인 경우 건너뛰기
                if (column.IsRequired && value == null)
                {
                    var requiredFieldNullLog = $"⚠️ 필수 필드 '{column.PropertyName}'이 null입니다. 건너뜁니다.";
                    LogManagerService.LogWarning(requiredFieldNullLog);
                    continue;
                }

                var parameterName = $"@{column.PropertyName}";
                var safeColumnName = GetSafeColumnName(column.DatabaseColumn);
                setClauses.Add($"{safeColumnName} = {parameterName}");
                paramDict[parameterName] = value ?? DBNull.Value;
            }

            if (setClauses.Count == 0)
            {
                throw new InvalidOperationException($"테이블 '{tableName}'에 업데이트할 수 있는 유효한 컬럼이 없습니다.");
            }

            var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)}";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            var updateCompleteLog = $"✅ 설정 기반 UPDATE 쿼리 생성 완료 - {setClauses.Count}개 컬럼";
            LogManagerService.LogInfo(updateCompleteLog);
            return (sql, paramDict);
        }

        /// <summary>
        /// 리플렉션 기반 UPDATE 쿼리 생성
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
        /// - WHERE 조건 처리
        /// </summary>
        /// <typeparam name="T">엔티티 타입</typeparam>
        /// <param name="tableName">테이블명</param>
        /// <param name="entity">업데이트할 엔티티 객체</param>
        /// <param name="whereClause">WHERE 조건 (선택사항)</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildUpdateFromReflection<T>(string tableName, T entity, string? whereClause)
        {
            var setClauses = new List<string>();
            var paramDict = new Dictionary<string, object>();

            foreach (var property in typeof(T).GetProperties())
            {
                if (property.CanRead)
                {
                    var value = property.GetValue(entity);
                    if (value != null)
                    {
                        var columnName = GetColumnName(property);
                        var parameterName = $"@{property.Name}";
                        
                        setClauses.Add($"{columnName} = {parameterName}");
                        paramDict[parameterName] = value;
                    }
                }
            }

            if (setClauses.Count == 0)
            {
                throw new InvalidOperationException($"엔티티 타입 '{typeof(T).Name}'에 유효한 속성이 없습니다.");
            }

            var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)}";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            var reflectionUpdateCompleteLog = $"✅ 리플렉션 기반 UPDATE 쿼리 생성 완료 - {setClauses.Count}개 컬럼";
            LogManagerService.LogInfo(reflectionUpdateCompleteLog);
            return (sql, paramDict);
        }

        /// <summary>
        /// 설정 기반 DELETE 쿼리 생성
        /// 
        /// 생성 과정:
        /// 1. 매핑 설정에서 컬럼 정보 추출
        /// 2. WHERE 조건 처리
        /// 3. SQL 쿼리 구성
        /// 4. 매개변수 딕셔너리 생성
        /// 
        /// 📋 처리 로직:
        /// - WHERE 조건 처리
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="entity">삭제할 엔티티 객체</param>
        /// <param name="mapping">테이블 매핑 설정</param>
        /// <param name="whereClause">WHERE 조건 (선택사항)</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildDeleteFromMapping(string tableName, object entity, DynamicTableMapping mapping, string? whereClause)
        {
            var paramDict = new Dictionary<string, object>();

            // WHERE 조건이 없는 경우 기본키를 사용
            if (string.IsNullOrWhiteSpace(whereClause) && !string.IsNullOrEmpty(mapping.PrimaryKey))
            {
                var primaryKeyValue = GetPropertyValue(entity, mapping.PrimaryKey);
                if (primaryKeyValue != null)
                {
                    whereClause = $"{mapping.PrimaryKey} = @{mapping.PrimaryKey}";
                    paramDict[$"@{mapping.PrimaryKey}"] = primaryKeyValue;
                }
            }

            var sql = $"DELETE FROM {tableName}";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            var deleteCompleteLog = $"✅ 설정 기반 DELETE 쿼리 생성 완료";
            LogManagerService.LogInfo(deleteCompleteLog);
            return (sql, paramDict);
        }

        /// <summary>
        /// 리플렉션 기반 DELETE 쿼리 생성
        /// 
        /// 생성 과정:
        /// 1. 엔티티 타입의 속성 정보 추출
        /// 2. null이 아닌 속성만 필터링
        /// 3. WHERE 조건 처리
        /// 4. SQL 쿼리 및 매개변수 생성
        /// 
        /// 📋 처리 로직:
        /// - WHERE 조건 처리
        /// </summary>
        /// <typeparam name="T">엔티티 타입</typeparam>
        /// <param name="tableName">테이블명</param>
        /// <param name="entity">삭제할 엔티티 객체</param>
        /// <param name="whereClause">WHERE 조건 (선택사항)</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildDeleteFromReflection<T>(string tableName, T entity, string? whereClause)
        {
            var paramDict = new Dictionary<string, object>();

            // WHERE 조건이 없는 경우 첫 번째 속성을 기본키로 가정
            if (string.IsNullOrWhiteSpace(whereClause))
            {
                var properties = typeof(T).GetProperties().Where(p => p.CanRead).ToList();
                if (properties.Count > 0)
                {
                    var firstProperty = properties.First();
                    var value = firstProperty.GetValue(entity);
                    if (value != null)
                    {
                        var columnName = GetColumnName(firstProperty);
                        var parameterName = $"@{firstProperty.Name}";
                        whereClause = $"{columnName} = {parameterName}";
                        paramDict[parameterName] = value;
                    }
                }
            }

            var sql = $"DELETE FROM {tableName}";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            var reflectionDeleteCompleteLog = $"✅ 리플렉션 기반 DELETE 쿼리 생성 완료";
            LogManagerService.LogInfo(reflectionDeleteCompleteLog);
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
                    var propertyNotFoundLog = $"⚠️ 속성 '{propertyName}'을 찾을 수 없습니다.";
                    LogManagerService.LogWarning(propertyNotFoundLog);
                    return null;
                }

                return property.GetValue(entity);
            }
            catch (Exception ex)
            {
                var propertyReadFailLog = $"❌ 속성 '{propertyName}' 값 읽기 실패: {ex.Message}";
                LogManagerService.LogError(propertyReadFailLog);
                return null;
            }
        }

        /// <summary>
        /// 속성명을 컬럼명으로 변환
        /// 
        /// 변환 규칙:
        /// 1. 한글 컬럼명 매핑 우선 적용
        /// 2. 기본 변환 규칙 적용 (카멜케이스 → 스네이크케이스)
        /// 3. 특수문자 처리
        /// 
        /// 변환 예시:
        /// - RecipientName → 수취인명 (한글 매핑)
        /// - OrderNumber → 주문번호 (한글 매핑)
        /// - ProductCode → 상품코드 (한글 매핑)
        /// </summary>
        /// <param name="property">속성 정보</param>
        /// <returns>컬럼명</returns>
        private string GetColumnName(PropertyInfo property)
        {
            // 한글 컬럼명 매핑 우선 적용
            var koreanColumnName = GetKoreanColumnName(property.Name);
            if (!string.IsNullOrEmpty(koreanColumnName))
            {
                return koreanColumnName;
            }

            // 기본 변환 규칙 적용 (카멜케이스 → 스네이크케이스)
            return ConvertToSnakeCase(property.Name);
        }

        /// <summary>
        /// 영문 속성명을 실제 데이터베이스 컬럼명으로 변환
        /// 
        /// 매핑 규칙 (실제 DB 컬럼명 기준):
        /// - RecipientName → 수취인명
        /// - OrderNumber → 주문번호
        /// - ProductCode → 품목코드
        /// - Phone1 → 전화번호1
        /// - Phone2 → 전화번호2
        /// - ZipCode → 우편번호
        /// - Address → 주소
        /// - OptionName → 옵션명
        /// - Quantity → 수량
        /// - ProductName → 송장명
        /// - ProductCount → 품목개수
        /// - SpecialNote → 배송메세지
        /// - StoreName → 쇼핑몰
        /// - CollectedAt → 수집시간
        /// - OrderAmount → 주문금액
        /// - PaymentAmount → 결제금액
        /// - PaymentMethod → 결제수단
        /// - TaxType → 면과세구분
        /// - OrderStatus → 주문상태
        /// - DeliveryCost → 택배비용
        /// - BoxSize → 박스크기
        /// - DeliveryQuantity → 택배수량
        /// - ShippingType → 배송송
        /// - PrintCount → 출력개수
        /// - InvoiceQuantity → 송장수량
        /// - InvoiceSeparator → 송장구분자
        /// - InvoiceType → 송장구분
        /// - InvoiceTypeFinal → 송장구분최종
        /// - Location → 위치
        /// - LocationConverted → 위치변환
        /// - Star1 → 별표1
        /// - Star2 → 별표2
        /// - OrderNumberMall → 주문번호(쇼핑몰)
        /// - DeliveryQuantity1 → 택배수량1
        /// - DeliveryQuantitySum → 택배수량합산
        /// </summary>
        /// <param name="propertyName">영문 속성명</param>
        /// <returns>실제 DB 컬럼명 또는 빈 문자열</returns>
        private string GetKoreanColumnName(string propertyName)
        {
            return propertyName switch
            {
                // 한글 컬럼명 (확인됨)
                "RecipientName" => "수취인명",
                "OrderNumber" => "주문번호",
                "ProductCode" => "품목코드",
                "Phone1" => "전화번호1",
                "Phone2" => "전화번호2",
                "ZipCode" => "우편번호",
                "Address" => "주소",
                "OptionName" => "옵션명",
                "Quantity" => "수량",
                
                // 영문 컬럼명 (추정)
                "ProductName" => "송장명",
                "ProductCount" => "품목개수",
                "SpecialNote" => "배송메세지",
                "StoreName" => "쇼핑몰",
                "CollectedAt" => "수집시간",
                "OrderAmount" => "주문금액",
                "PaymentAmount" => "결제금액",
                "PaymentMethod" => "결제수단",
                "TaxType" => "면과세구분",
                "OrderStatus" => "주문상태",
                "DeliveryCost" => "택배비용",
                "BoxSize" => "박스크기",
                "DeliveryQuantity" => "택배수량",
                "ShippingType" => "배송송",
                "PrintCount" => "출력개수",
                "InvoiceQuantity" => "송장수량",
                "InvoiceSeparator" => "송장구분자",
                "InvoiceType" => "송장구분",
                "InvoiceTypeFinal" => "송장구분최종",
                "Location" => "위치",
                "LocationConverted" => "위치변환",
                "Star1" => "별표1",
                "Star2" => "별표2",
                "OrderNumberMall" => "주문번호(쇼핑몰)",
                "DeliveryQuantity1" => "택배수량1",
                "DeliveryQuantitySum" => "택배수량합산",
                _ => string.Empty
            };
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

        /// <summary>
        /// 엔티티 타입의 기본키 속성명을 가져옴
        /// 
        /// 예시:
        /// - Order 클래스에 OrderId 속성이 기본키면 "OrderId" 반환
        /// - Invoice 클래스에 InvoiceId 속성이 기본키면 "InvoiceId" 반환
        /// </summary>
        /// <typeparam name="T">엔티티 타입</typeparam>
        /// <returns>기본키 속성명 또는 빈 문자열</returns>
        private string GetPrimaryKeyName<T>()
        {
            var mapping = _tableMappings.TryGetValue(typeof(T).Name, out var dynamicMapping) ? dynamicMapping : null;
            if (mapping == null)
            {
                return string.Empty;
            }

            var primaryKeyProperty = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name == mapping.PrimaryKey);

            return primaryKeyProperty?.Name ?? string.Empty;
        }

        /// <summary>
        /// 컬럼명을 안전한 SQL 형식으로 변환
        /// 
        /// 변환 규칙:
        /// 1. 모든 컬럼명을 백틱(`)으로 감싸기 (안전성 보장)
        /// 2. 특수문자(괄호, 공백, 하이픈 등)가 포함된 컬럼명도 안전하게 처리
        /// 
        /// 변환 예시:
        /// - 주문번호(쇼핑몰) → `주문번호(쇼핑몰)`
        /// - 수취인명 → `수취인명`
        /// - msg1 → `msg1`
        /// </summary>
        /// <param name="columnName">원본 컬럼명</param>
        /// <returns>안전한 SQL 컬럼명</returns>
        private string GetSafeColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return columnName;

            // 모든 컬럼명을 백틱으로 감싸기 (안전성 보장)
            return $"`{columnName}`";
        }

        /// <summary>
        /// 테이블명에 위험한 문자가 포함되어 있는지 검사
        /// 
        /// 검사 대상:
        /// - 공백 (예: "my table")
        /// - 특수 문자 (예: "my-table", "my.table")
        /// - 대문자 (예: "MyTable")
        /// - 키워드 (예: "SELECT", "INSERT")
        /// 
        /// 반환:
        /// - true: 위험한 문자가 포함되어 있음
        /// - false: 안전한 문자만 포함되어 있음
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <returns>위험 여부</returns>
        private bool IsValidTableName(string tableName)
        {
            // 공백 또는 특수 문자 검사
            if (string.IsNullOrWhiteSpace(tableName) || tableName.Contains(" ") || tableName.Contains(".") || tableName.Contains("-"))
            {
                return false;
            }

            // 대문자 또는 키워드 검사 (예시)
            var lowerCaseTableName = tableName.ToLower();
            if (lowerCaseTableName.Contains("select") || lowerCaseTableName.Contains("insert") || lowerCaseTableName.Contains("update") || lowerCaseTableName.Contains("delete"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 설정 기반 TRUNCATE 쿼리 생성
        /// 
        /// 생성 과정:
        /// 1. 매핑 설정에서 컬럼 정보 추출
        /// 2. SQL 쿼리 구성
        /// 3. 매개변수 딕셔너리 생성
        /// 
        /// 📋 처리 로직:
        /// - TRUNCATE TABLE 쿼리 구성
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <param name="mapping">테이블 매핑 설정</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildTruncateFromMapping(string tableName, DynamicTableMapping mapping)
        {
            var sql = $"TRUNCATE TABLE {tableName}";
            return (sql, new Dictionary<string, object>());
        }

        /// <summary>
        /// 리플렉션 기반 TRUNCATE 쿼리 생성
        /// 
        /// 생성 과정:
        /// 1. 엔티티 타입의 속성 정보 추출
        /// 2. null이 아닌 속성만 필터링
        /// 3. SQL 쿼리 구성
        /// 
        /// 📋 처리 로직:
        /// - TRUNCATE TABLE 쿼리 구성
        /// </summary>
        /// <param name="tableName">테이블명</param>
        /// <returns>(SQL 쿼리, 매개변수 딕셔너리) 튜플</returns>
        private (string sql, Dictionary<string, object> parameters) BuildTruncateFromReflection(string tableName)
        {
            var sql = $"TRUNCATE TABLE {tableName}";
            return (sql, new Dictionary<string, object>());
        }

        #endregion

        /// <summary>
        /// 테이블별 매핑 파일을 DynamicTableMapping 형식으로 변환
        /// </summary>
        /// <param name="jsonContent">JSON 파일 내용</param>
        /// <param name="tableName">테이블명</param>
        /// <returns>DynamicTableMapping 객체</returns>
        private static DynamicTableMapping? ConvertTableMappingFile(string jsonContent, string tableName)
        {
            try
            {
                var jsonElement = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                if (jsonElement == null) return null;

                var tableMapping = new DynamicTableMapping
                {
                    TableName = tableName,
                    Columns = new List<DynamicColumnMapping>()
                };

                // columns 속성에서 컬럼 정보 추출
                if (jsonElement.columns != null)
                {
                    foreach (var column in jsonElement.columns)
                    {
                        var columnMapping = new DynamicColumnMapping
                        {
                            PropertyName = GetPropertyNameFromColumnName(column.Name), // 엑셀 컬럼명을 영문 속성명으로 변환
                            DatabaseColumn = column.Value.db_column ?? column.Name, // DB 컬럼명
                            DataType = column.Value.data_type ?? "VARCHAR",
                            IsRequired = column.Value.required ?? false,
                            ExcludeFromInsert = false,
                            ExcludeFromUpdate = false,
                            IsAutoIncrement = false,
                            IsPrimaryKey = false
                        };

                        tableMapping.Columns.Add(columnMapping);
                    }
                }

                return tableMapping;
            }
            catch (Exception ex)
            {
                LogManagerService.LogError($"[ConvertTableMappingFile] 변환 실패 ({tableName}): {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// 엑셀 컬럼명을 영문 속성명으로 변환
        /// </summary>
        /// <param name="columnName">엑셀 컬럼명</param>
        /// <returns>영문 속성명</returns>
        private static string GetPropertyNameFromColumnName(string columnName)
        {
            return columnName switch
            {
                // 기본 메시지 필드
                "msg1" => "Msg1",
                "msg2" => "Msg2",
                "msg3" => "Msg3",
                "msg4" => "Msg4",
                "msg5" => "Msg5",
                "msg6" => "Msg6",
                
                // 수취인 정보
                "수취인명" => "RecipientName",
                "전화번호1" => "Phone1",
                "전화번호2" => "Phone2",
                "우편번호" => "ZipCode",
                "주소" => "Address",
                
                // 상품 정보
                "옵션명" => "OptionName",
                "수량" => "Quantity",
                "송장명" => "ProductName",
                "품목코드" => "ProductCode",
                "품목개수" => "ProductCount",
                
                // 주문 정보
                "배송메세지" => "SpecialNote",
                "주문번호" => "OrderNumber",
                "쇼핑몰" => "StoreName",
                "수집시간" => "CollectedAt",
                "주문번호(쇼핑몰)" => "OrderNumberMall",
                "주문금액" => "OrderAmount",
                "결제금액" => "PaymentAmount",
                "결제수단" => "PaymentMethod",
                "면과세구분" => "TaxType",
                "주문상태" => "OrderStatus",
                
                // 배송 정보
                "택배비용" => "DeliveryCost",
                "박스크기" => "BoxSize",
                "택배수량" => "DeliveryQuantity",
                "택배수량1" => "DeliveryQuantity1",
                "택배수량합산" => "DeliveryQuantitySum",
                "배송송" => "ShippingType",
                
                // 송장 정보
                "출력개수" => "PrintCount",
                "송장수량" => "InvoiceQuantity",
                "송장구분자" => "InvoiceSeparator",
                "송장구분" => "InvoiceType",
                "송장구분최종" => "InvoiceTypeFinal",
                
                // 위치 정보
                "위치" => "Location",
                "위치변환" => "LocationConverted",
                
                // 기타
                "별표1" => "Star1",
                "별표2" => "Star2",
                
                // 기본값: 원본 컬럼명 사용
                _ => columnName
            };
        }
    }

    /// <summary>
    /// 테이블 매핑 정보를 담는 클래스
    /// 
    /// 📋 주요 기능:
    /// - 테이블명 정의
    /// - 컬럼 매핑 목록 관리
    /// - 기본키 정보 관리
    /// - 설정 검증
    /// </summary>
    public class DynamicTableMapping
    {
        /// <summary>테이블명</summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>컬럼 매핑 목록</summary>
        public List<DynamicColumnMapping> Columns { get; set; } = new List<DynamicColumnMapping>();

        /// <summary>기본키 컬럼명</summary>
        public string PrimaryKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// 컬럼 매핑 설정 클래스
    /// 
    /// 📋 주요 기능:
    /// - 속성명과 데이터베이스 컬럼명 매핑
    /// - 데이터 타입 정보 저장
    /// - 필수 필드 여부 표시
    /// - INSERT/UPDATE 제외 설정
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

        /// <summary>INSERT에서 제외 여부 (AUTO_INCREMENT 등)</summary>
        public bool ExcludeFromInsert { get; set; } = false;

        /// <summary>UPDATE에서 제외 여부 (PRIMARY KEY 등)</summary>
        public bool ExcludeFromUpdate { get; set; } = false;

        /// <summary>자동 증가 컬럼 여부</summary>
        public bool IsAutoIncrement { get; set; } = false;

        /// <summary>기본키 여부</summary>
        public bool IsPrimaryKey { get; set; } = false;
    }
}
