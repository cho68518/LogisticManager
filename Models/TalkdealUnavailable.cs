using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogisticManager.Models
{
    /// <summary>
    /// 송장출력_톡딜불가 테이블 모델
    /// 
    /// 🎯 주요 목적:
    /// - 톡딜 배송이 불가능한 상품들을 체계적으로 분류하고 관리
    /// - 필수코드 기반의 계층적 상품 분류 체계 제공
    /// - 쇼핑몰별 상품 분류 데이터 통합 관리
    /// 
    /// 📋 핵심 기능:
    /// - 3단계 계층적 필수코드 분류 (필수코드1, 2, 3)
    /// - 쇼핑몰별 상품 분류 체계 지원
    /// - 품목코드 기반 상품 고유 식별
    /// - 데이터 품질 검증 및 관리
    /// 
    /// 💡 사용 목적:
    /// - 톡딜 배송 제한 상품 사전 식별
    /// - 상품 분류 체계의 일관성 유지
    /// - 배송 정책 수립을 위한 데이터 기반 제공
    /// </summary>
    [Table("송장출력_톡딜불가")]
    public class TalkdealUnavailable
    {
        /// <summary>
        /// 기본 키 (자동 증가)
        /// </summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 쇼핑몰명
        /// </summary>
        [Column("쇼핑몰")]
        [StringLength(255)]
        public string? ShoppingMall { get; set; }

        /// <summary>
        /// 상품 품목 코드
        /// </summary>
        [Column("품목코드")]
        [StringLength(255)]
        public string? ProductCode { get; set; }

        /// <summary>
        /// 상품명
        /// </summary>
        [Column("상품명")]
        [StringLength(255)]
        public string? ProductName { get; set; }

        /// <summary>
        /// 1차 필수 분류 코드
        /// </summary>
        [Column("필수코드1")]
        [StringLength(255)]
        public string? RequiredCode1 { get; set; }

        /// <summary>
        /// 1차 필수 분류 코드에 해당하는 상품명
        /// </summary>
        [Column("필수코드1상품명")]
        [StringLength(255)]
        public string? RequiredCode1ProductName { get; set; }

        /// <summary>
        /// 2차 필수 분류 코드
        /// </summary>
        [Column("필수코드2")]
        [StringLength(255)]
        public string? RequiredCode2 { get; set; }

        /// <summary>
        /// 2차 필수 분류 코드에 해당하는 상품명
        /// </summary>
        [Column("필수코드2상품명")]
        [StringLength(255)]
        public string? RequiredCode2ProductName { get; set; }

        /// <summary>
        /// 3차 필수 분류 코드
        /// </summary>
        [Column("필수코드3")]
        [StringLength(255)]
        public string? RequiredCode3 { get; set; }

        /// <summary>
        /// 3차 필수 분류 코드에 해당하는 상품명
        /// </summary>
        [Column("필수코드3상품명")]
        [StringLength(255)]
        public string? RequiredCode3ProductName { get; set; }

        /// <summary>
        /// 데이터 생성 시간
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 데이터 수정 시간
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        #region 비즈니스 로직 메서드

        /// <summary>
        /// 계층적 분류 코드의 유효성을 검증
        /// </summary>
        /// <returns>유효성 검증 결과</returns>
        public bool ValidateHierarchicalCodes()
        {
            // 필수코드2가 있으면 필수코드1도 있어야 함
            if (!string.IsNullOrEmpty(RequiredCode2) && string.IsNullOrEmpty(RequiredCode1))
                return false;

            // 필수코드3이 있으면 필수코드2도 있어야 함
            if (!string.IsNullOrEmpty(RequiredCode3) && string.IsNullOrEmpty(RequiredCode2))
                return false;

            return true;
        }

        /// <summary>
        /// 전체 분류 경로를 문자열로 반환
        /// </summary>
        /// <returns>분류 경로 문자열</returns>
        public string GetFullClassificationPath()
        {
            var path = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(RequiredCode1))
            {
                path.Append(RequiredCode1);
                if (!string.IsNullOrEmpty(RequiredCode1ProductName))
                    path.Append($"({RequiredCode1ProductName})");
            }

            if (!string.IsNullOrEmpty(RequiredCode2))
            {
                path.Append(" > ").Append(RequiredCode2);
                if (!string.IsNullOrEmpty(RequiredCode2ProductName))
                    path.Append($"({RequiredCode2ProductName})");
            }

            if (!string.IsNullOrEmpty(RequiredCode3))
            {
                path.Append(" > ").Append(RequiredCode3);
                if (!string.IsNullOrEmpty(RequiredCode3ProductName))
                    path.Append($"({RequiredCode3ProductName})");
            }

            return path.ToString();
        }

        /// <summary>
        /// 상품이 특정 분류 코드에 속하는지 확인
        /// </summary>
        /// <param name="code">검색할 분류 코드</param>
        /// <returns>속함 여부</returns>
        public bool BelongsToClassification(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            return RequiredCode1 == code || RequiredCode2 == code || RequiredCode3 == code;
        }

        /// <summary>
        /// 상품 정보의 요약을 반환
        /// </summary>
        /// <returns>상품 요약 정보</returns>
        public string GetProductSummary()
        {
            var summary = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(ShoppingMall))
                summary.Append($"[{ShoppingMall}] ");

            if (!string.IsNullOrEmpty(ProductName))
                summary.Append(ProductName);

            if (!string.IsNullOrEmpty(ProductCode))
                summary.Append($" (코드: {ProductCode})");

            return summary.ToString();
        }

        #endregion

        #region 오버라이드 메서드

        /// <summary>
        /// 객체의 문자열 표현을 반환
        /// </summary>
        /// <returns>문자열 표현</returns>
        public override string ToString()
        {
            return GetProductSummary();
        }

        /// <summary>
        /// 객체의 동등성을 비교
        /// </summary>
        /// <param name="obj">비교할 객체</param>
        /// <returns>동등 여부</returns>
        public override bool Equals(object? obj)
        {
            if (obj is TalkdealUnavailable other)
            {
                return Id == other.Id && 
                       ProductCode == other.ProductCode && 
                       ShoppingMall == other.ShoppingMall;
            }
            return false;
        }

        /// <summary>
        /// 해시 코드를 반환
        /// </summary>
        /// <returns>해시 코드</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, ProductCode, ShoppingMall);
        }

        #endregion
    }
}
