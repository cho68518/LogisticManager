using System.ComponentModel;

namespace LogisticManager.Models
{
    /// <summary>
    /// 공통코드 정보를 담는 모델 클래스
    /// </summary>
    public class CommonCode
    {
        /// <summary>
        /// 코드 그룹 (예: USER_ROLE, ORDER_STATUS)
        /// </summary>
        [DisplayName("코드 그룹")]
        public string GroupCode { get; set; } = string.Empty;

        /// <summary>
        /// 개별 코드 값 (예: ADMIN, USER, PENDING)
        /// </summary>
        [DisplayName("코드")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 코드의 표시 이름 (예: 관리자, 일반사용자, 주문접수)
        /// </summary>
        [DisplayName("코드명")]
        public string CodeName { get; set; } = string.Empty;

        /// <summary>
        /// 코드에 대한 상세 설명
        /// </summary>
        [DisplayName("설명")]
        public string? Description { get; set; }

        /// <summary>
        /// 정렬 순서 (낮은 숫자가 먼저 표시됨)
        /// </summary>
        [DisplayName("정렬순서")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 사용 여부 (TRUE: 사용, FALSE: 미사용)
        /// </summary>
        [DisplayName("사용여부")]
        public bool IsUsed { get; set; } = true;

        /// <summary>
        /// 추가 속성 1
        /// </summary>
        [DisplayName("추가속성1")]
        public string? Attribute1 { get; set; }

        /// <summary>
        /// 추가 속성 2
        /// </summary>
        [DisplayName("추가속성2")]
        public string? Attribute2 { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        [DisplayName("생성자")]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 생성 일시
        /// </summary>
        [DisplayName("생성일시")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 수정자
        /// </summary>
        [DisplayName("수정자")]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// 수정 일시
        /// </summary>
        [DisplayName("수정일시")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 그룹코드명
        /// </summary>
        [DisplayName("그룹코드명")]
        public string? GroupCodeNm { get; set; }

        /// <summary>
        /// 새로운 공통코드 인스턴스를 생성합니다.
        /// </summary>
        public CommonCode() { }

        /// <summary>
        /// 지정된 그룹코드로 새로운 공통코드 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="groupCode">그룹코드</param>
        public CommonCode(string groupCode)
        {
            GroupCode = groupCode;
            Code = string.Empty;
            CodeName = string.Empty;
            Description = string.Empty;
            SortOrder = 0;
            IsUsed = true;
            Attribute1 = string.Empty;
            Attribute2 = string.Empty;
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// 공통코드의 복사본을 생성합니다.
        /// </summary>
        /// <returns>복사된 공통코드 객체</returns>
        public CommonCode Clone()
        {
            return new CommonCode
            {
                GroupCode = this.GroupCode,
                Code = this.Code,
                CodeName = this.CodeName,
                Description = this.Description,
                SortOrder = this.SortOrder,
                IsUsed = this.IsUsed,
                Attribute1 = this.Attribute1,
                Attribute2 = this.Attribute2,
                CreatedBy = this.CreatedBy,
                CreatedAt = this.CreatedAt,
                UpdatedBy = this.UpdatedBy,
                UpdatedAt = this.UpdatedAt,
                GroupCodeNm = this.GroupCodeNm
            };
        }
    }
}
