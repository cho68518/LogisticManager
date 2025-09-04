using System;

namespace LogisticManager.Models
{
    /// <summary>
    /// 사용자 정보 모델
    /// </summary>
    public class User
    {
        /// <summary>
        /// 사용자 고유 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 사용자명 (로그인 ID)
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 사용자 표시명 (Users 테이블의 name 컬럼)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 이메일 주소
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 계정 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 마지막 로그인 시간
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// 로그인 성공 여부 (인증용)
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// 사용자 표시명 (사용자명과 이메일 조합)
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(Email) ? $"{Username} ({Email})" : Username;
    }
}
