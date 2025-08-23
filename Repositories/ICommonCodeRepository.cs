using LogisticManager.Models;

namespace LogisticManager.Repositories
{
    /// <summary>
    /// 공통코드 데이터 접근을 위한 리포지토리 인터페이스
    /// </summary>
    public interface ICommonCodeRepository
    {
        /// <summary>
        /// 모든 그룹코드 목록을 조회합니다.
        /// </summary>
        /// <returns>그룹코드 목록</returns>
        Task<List<string>> GetAllGroupCodesAsync();

        /// <summary>
        /// 특정 그룹코드에 속한 모든 공통코드를 조회합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <returns>공통코드 목록</returns>
        Task<List<CommonCode>> GetCommonCodesByGroupAsync(string groupCode);

        /// <summary>
        /// 공통코드를 추가합니다.
        /// </summary>
        /// <param name="commonCode">추가할 공통코드</param>
        /// <returns>성공 여부</returns>
        Task<bool> AddCommonCodeAsync(CommonCode commonCode);

        /// <summary>
        /// 공통코드를 수정합니다.
        /// </summary>
        /// <param name="commonCode">수정할 공통코드</param>
        /// <returns>성공 여부</returns>
        Task<bool> UpdateCommonCodeAsync(CommonCode commonCode);

        /// <summary>
        /// 공통코드를 삭제합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <param name="code">코드</param>
        /// <returns>성공 여부</returns>
        Task<bool> DeleteCommonCodeAsync(string groupCode, string code);

        /// <summary>
        /// 여러 공통코드를 일괄 저장합니다.
        /// </summary>
        /// <param name="commonCodes">저장할 공통코드 목록</param>
        /// <returns>성공 여부</returns>
        Task<bool> SaveCommonCodesAsync(List<CommonCode> commonCodes);

        /// <summary>
        /// 특정 그룹코드가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <returns>존재 여부</returns>
        Task<bool> ExistsGroupCodeAsync(string groupCode);

        /// <summary>
        /// 특정 공통코드가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        /// <param name="code">코드</param>
        /// <returns>존재 여부</returns>
        Task<bool> ExistsCommonCodeAsync(string groupCode, string code);

        /// <summary>
        /// 새 그룹코드를 추가합니다.
        /// </summary>
        /// <param name="groupCode">새 그룹코드</param>
        /// <param name="groupCodeName">그룹코드명 (선택사항)</param>
        /// <returns>성공 여부</returns>
        Task<bool> AddGroupCodeAsync(string groupCode, string? groupCodeName = null);

        /// <summary>
        /// 그룹코드를 삭제합니다 (해당 그룹의 모든 공통코드도 함께 삭제).
        /// </summary>
        /// <param name="groupCode">삭제할 그룹코드</param>
        /// <returns>성공 여부</returns>
        Task<bool> DeleteGroupCodeAsync(string groupCode);
    }
}
