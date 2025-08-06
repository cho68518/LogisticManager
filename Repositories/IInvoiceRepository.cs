using LogisticManager.Models;
using System.Data;

namespace LogisticManager.Repositories
{
    /// <summary>
    /// 송장 데이터 저장소 인터페이스 - Repository 패턴 적용
    /// 
    /// 📋 주요 기능:
    /// - 데이터 액세스 로직 추상화
    /// - 테스트 가능성 향상 (Mock 지원)
    /// - 의존성 역전 원칙 적용
    /// - 단일 책임 원칙 준수
    /// 
    /// 🎯 장점:
    /// - 비즈니스 로직과 데이터 액세스 로직 분리
    /// - 데이터베이스 변경 시 영향 최소화
    /// - 단위 테스트 용이성
    /// - 코드 재사용성 향상
    /// 
    /// 💡 사용법:
    /// var repository = new InvoiceRepository(databaseService);
    /// await repository.InsertBatchAsync(invoices);
    /// </summary>
    public interface IInvoiceRepository
    {
        #region 기본 CRUD 작업

        /// <summary>
        /// 송장 데이터 배치 삽입 - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 사용
        /// - 대량 데이터 배치 처리
        /// - 트랜잭션 처리
        /// - 성능 최적화
        /// 
        /// 💡 사용법:
        /// await repository.InsertBatchAsync(invoices, progress);
        /// </summary>
        /// <param name="invoices">삽입할 송장 목록</param>
        /// <param name="progress">진행률 콜백</param>
        /// <param name="batchSize">배치 크기 (기본값: 500)</param>
        /// <returns>삽입된 행 수</returns>
        Task<int> InsertBatchAsync(IEnumerable<InvoiceDto> invoices, IProgress<string>? progress = null, int batchSize = 500);

        /// <summary>
        /// 송장 데이터 배치 삽입 - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블에 데이터 삽입
        /// - 대량 데이터 배치 처리
        /// - 트랜잭션 처리
        /// - 성능 최적화
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// await repository.InsertBatchAsync("custom_table", invoices, progress);
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="invoices">삽입할 송장 목록</param>
        /// <param name="progress">진행률 콜백</param>
        /// <param name="batchSize">배치 크기 (기본값: 500)</param>
        /// <returns>삽입된 행 수</returns>
        Task<int> InsertBatchAsync(string tableName, IEnumerable<InvoiceDto> invoices, IProgress<string>? progress = null, int batchSize = 500);

        /// <summary>
        /// 테이블 초기화 (TRUNCATE) - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 초기화
        /// - 테이블 데이터 완전 삭제
        /// - 자동 증가 값 초기화
        /// - 빠른 삭제 성능
        /// 
        /// 💡 사용법:
        /// await repository.TruncateTableAsync();
        /// </summary>
        /// <returns>작업 성공 여부</returns>
        Task<bool> TruncateTableAsync();

        /// <summary>
        /// 테이블 초기화 (TRUNCATE) - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블명으로 초기화 실행
        /// - 테이블 데이터 완전 삭제
        /// - 자동 증가 값 초기화
        /// - 빠른 삭제 성능
        /// - 테이블명 유효성 검사
        /// 
        /// ⚠️ 주의사항:
        /// - 테이블명 검증으로 SQL 인젝션 방지
        /// - 허용된 테이블명만 처리 권장
        /// 
        /// 💡 사용법:
        /// await repository.TruncateTableAsync("custom_table_name");
        /// </summary>
        /// <param name="tableName">초기화할 테이블명</param>
        /// <returns>작업 성공 여부</returns>
        Task<bool> TruncateTableAsync(string tableName);

        /// <summary>
        /// 전체 데이터 조회 - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 조회
        /// - 테이블의 모든 데이터 조회
        /// - 페이징 지원 (선택적)
        /// - 정렬 지원
        /// 
        /// 💡 사용법:
        /// var invoices = await repository.GetAllAsync();
        /// </summary>
        /// <param name="limit">조회 제한 수 (0 = 제한 없음)</param>
        /// <param name="offset">시작 위치</param>
        /// <returns>송장 목록</returns>
        Task<IEnumerable<InvoiceDto>> GetAllAsync(int limit = 0, int offset = 0);

        /// <summary>
        /// 전체 데이터 조회 - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블의 모든 데이터 조회
        /// - 페이징 지원 (선택적)
        /// - 정렬 지원
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// var invoices = await repository.GetAllAsync("custom_table", 100, 0);
        /// </summary>
        /// <param name="tableName">조회할 테이블명</param>
        /// <param name="limit">조회 제한 수 (0 = 제한 없음)</param>
        /// <param name="offset">시작 위치</param>
        /// <returns>송장 목록</returns>
        Task<IEnumerable<InvoiceDto>> GetAllAsync(string tableName, int limit = 0, int offset = 0);

        /// <summary>
        /// 조건별 데이터 조회 - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 조회
        /// - WHERE 조건 지원
        /// - 동적 쿼리 생성
        /// - 매개변수화된 쿼리
        /// 
        /// 💡 사용법:
        /// var invoices = await repository.GetByConditionAsync("품목코드 = @code", new { code = "7710" });
        /// </summary>
        /// <param name="whereClause">WHERE 조건절</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>조건에 맞는 송장 목록</returns>
        Task<IEnumerable<InvoiceDto>> GetByConditionAsync(string whereClause, object? parameters = null);

        /// <summary>
        /// 조건별 데이터 조회 - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블에서 조건별 데이터 조회
        /// - WHERE 조건 지원
        /// - 동적 쿼리 생성
        /// - 매개변수화된 쿼리
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// var invoices = await repository.GetByConditionAsync("custom_table", "품목코드 = @code", new { code = "7710" });
        /// </summary>
        /// <param name="tableName">조회할 테이블명</param>
        /// <param name="whereClause">WHERE 조건절</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>조건에 맞는 송장 목록</returns>
        Task<IEnumerable<InvoiceDto>> GetByConditionAsync(string tableName, string whereClause, object? parameters = null);

        /// <summary>
        /// 데이터 개수 조회 - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블의 레코드 수 조회
        /// - 테이블의 총 레코드 수 조회
        /// - 조건별 개수 조회 지원
        /// - 성능 최적화된 COUNT 쿼리
        /// 
        /// 💡 사용법:
        /// var count = await repository.GetCountAsync();
        /// </summary>
        /// <param name="whereClause">WHERE 조건절 (선택적)</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>데이터 개수</returns>
        Task<int> GetCountAsync(string? whereClause = null, object? parameters = null);

        /// <summary>
        /// 데이터 개수 조회 - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블의 레코드 수 조회
        /// - 조건별 개수 조회 지원
        /// - 성능 최적화된 COUNT 쿼리
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// var count = await repository.GetCountAsync("custom_table", "품목코드 = @code", new { code = "7710" });
        /// </summary>
        /// <param name="tableName">조회할 테이블명</param>
        /// <param name="whereClause">WHERE 조건절 (선택적)</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>데이터 개수</returns>
        Task<int> GetCountAsync(string tableName, string? whereClause = null, object? parameters = null);

        #endregion

        #region 1차 데이터 가공 작업

        /// <summary>
        /// 특정 품목코드의 주소에 별표(*) 추가 - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 대상
        /// - 품목코드 7710, 7720 대상
        /// - 주소 필드에 '*' 접미사 추가
        /// - 대량 업데이트 최적화
        /// 
        /// 💡 사용법:
        /// await repository.AddStarToAddressAsync(new[] { "7710", "7720" });
        /// </summary>
        /// <param name="productCodes">대상 품목코드 목록</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> AddStarToAddressAsync(IEnumerable<string> productCodes);

        /// <summary>
        /// 특정 품목코드의 주소에 별표(*) 추가 - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블 대상
        /// - 품목코드 7710, 7720 대상
        /// - 주소 필드에 '*' 접미사 추가
        /// - 대량 업데이트 최적화
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// await repository.AddStarToAddressAsync("custom_table", new[] { "7710", "7720" });
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="productCodes">대상 품목코드 목록</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> AddStarToAddressAsync(string tableName, IEnumerable<string> productCodes);

        /// <summary>
        /// 송장명 일괄 변경 (BS_ → GC_) - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 대상
        /// - 송장명 접두사 변경
        /// - 정규식 기반 변경 지원
        /// - 대량 업데이트 최적화
        /// 
        /// 💡 사용법:
        /// await repository.ReplacePrefixAsync("송장명", "BS_", "GC_");
        /// </summary>
        /// <param name="fieldName">대상 필드명</param>
        /// <param name="oldPrefix">기존 접두사</param>
        /// <param name="newPrefix">새 접두사</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> ReplacePrefixAsync(string fieldName, string oldPrefix, string newPrefix);

        /// <summary>
        /// 송장명 일괄 변경 (BS_ → GC_) - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블 대상
        /// - 송장명 접두사 변경
        /// - 정규식 기반 변경 지원
        /// - 대량 업데이트 최적화
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// await repository.ReplacePrefixAsync("custom_table", "송장명", "BS_", "GC_");
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="fieldName">대상 필드명</param>
        /// <param name="oldPrefix">기존 접두사</param>
        /// <param name="newPrefix">새 접두사</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> ReplacePrefixAsync(string tableName, string fieldName, string oldPrefix, string newPrefix);

        /// <summary>
        /// 필드 값 일괄 변경 - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 대상
        /// - 특정 조건의 필드 값 변경
        /// - 다중 조건 지원
        /// - 매개변수화된 쿼리
        /// 
        /// 💡 사용법:
        /// await repository.UpdateFieldAsync("수취인명", "난난", "수취인명 = 'nan'");
        /// </summary>
        /// <param name="fieldName">변경할 필드명</param>
        /// <param name="newValue">새 값</param>
        /// <param name="whereClause">WHERE 조건절</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> UpdateFieldAsync(string fieldName, object newValue, string whereClause, object? parameters = null);

        /// <summary>
        /// 필드 값 일괄 변경 - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블 대상
        /// - 특정 조건의 필드 값 변경
        /// - 다중 조건 지원
        /// - 매개변수화된 쿼리
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// await repository.UpdateFieldAsync("custom_table", "수취인명", "난난", "수취인명 = 'nan'");
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="fieldName">변경할 필드명</param>
        /// <param name="newValue">새 값</param>
        /// <param name="whereClause">WHERE 조건절</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> UpdateFieldAsync(string tableName, string fieldName, object newValue, string whereClause, object? parameters = null);

        /// <summary>
        /// 문자열 필드에서 특정 문자 제거 - 기본 테이블
        /// 
        /// 📋 기능:
        /// - Repository가 관리하는 기본 테이블 대상
        /// - REPLACE 함수 사용
        /// - 특정 문자열 패턴 제거
        /// - 대량 처리 최적화
        /// 
        /// 💡 사용법:
        /// await repository.RemoveCharacterAsync("주소", "·");
        /// </summary>
        /// <param name="fieldName">대상 필드명</param>
        /// <param name="targetChar">제거할 문자</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> RemoveCharacterAsync(string fieldName, string targetChar);

        /// <summary>
        /// 문자열 필드에서 특정 문자 제거 - 커스텀 테이블
        /// 
        /// 📋 기능:
        /// - 지정된 테이블 대상
        /// - REPLACE 함수 사용
        /// - 특정 문자열 패턴 제거
        /// - 대량 처리 최적화
        /// - 테이블명 유효성 검사
        /// 
        /// 💡 사용법:
        /// await repository.RemoveCharacterAsync("custom_table", "주소", "·");
        /// </summary>
        /// <param name="tableName">대상 테이블명</param>
        /// <param name="fieldName">대상 필드명</param>
        /// <param name="targetChar">제거할 문자</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> RemoveCharacterAsync(string tableName, string fieldName, string targetChar);

        #endregion

        #region 특수 처리 작업

        /// <summary>
        /// 제주도 주소 마킹
        /// 
        /// 📋 기능:
        /// - 제주도 관련 주소 패턴 검색
        /// - 별표2 필드에 '제주' 마킹
        /// - 다중 패턴 지원
        /// 
        /// 💡 사용법:
        /// await repository.MarkJejuAddressAsync(new[] { "%제주특별%", "%제주 제주%" });
        /// </summary>
        /// <param name="addressPatterns">제주도 주소 패턴 목록</param>
        /// <returns>마킹된 행 수</returns>
        Task<int> MarkJejuAddressAsync(IEnumerable<string> addressPatterns);

        /// <summary>
        /// 박스 상품 명칭 변경
        /// 
        /// 📋 기능:
        /// - 박스 관련 상품 검색
        /// - 송장명에 특수 접두사 추가
        /// - 패턴 매칭 지원
        /// 
        /// 💡 사용법:
        /// await repository.AddBoxPrefixAsync("▨▧▦ ", "%박스%");
        /// </summary>
        /// <param name="prefix">추가할 접두사</param>
        /// <param name="pattern">박스 상품 패턴</param>
        /// <returns>업데이트된 행 수</returns>
        Task<int> AddBoxPrefixAsync(string prefix, string pattern);

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 커스텀 쿼리 실행 (SELECT)
        /// 
        /// 📋 기능:
        /// - 복잡한 조회 쿼리 실행
        /// - DataTable 반환
        /// - 매개변수 지원
        /// 
        /// 💡 사용법:
        /// var result = await repository.ExecuteQueryAsync("SELECT * FROM table WHERE id = @id", new { id = 1 });
        /// </summary>
        /// <param name="sql">실행할 SQL 쿼리</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>쿼리 결과</returns>
        Task<DataTable> ExecuteQueryAsync(string sql, object? parameters = null);

        /// <summary>
        /// 커스텀 쿼리 실행 (UPDATE/INSERT/DELETE)
        /// 
        /// 📋 기능:
        /// - 데이터 변경 쿼리 실행
        /// - 영향받은 행 수 반환
        /// - 매개변수 지원
        /// 
        /// 💡 사용법:
        /// var affected = await repository.ExecuteNonQueryAsync("UPDATE table SET field = @value", new { value = "test" });
        /// </summary>
        /// <param name="sql">실행할 SQL 쿼리</param>
        /// <param name="parameters">쿼리 매개변수</param>
        /// <returns>영향받은 행 수</returns>
        Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null);

        #endregion
    }
}