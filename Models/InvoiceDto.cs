using System.ComponentModel.DataAnnotations;

namespace LogisticManager.Models
{
    /// <summary>
    /// 송장 데이터 전송 객체 (DTO) - 데이터베이스와 애플리케이션 간 데이터 전송용
    /// 
    /// 📋 주요 기능:
    /// - 송장 데이터의 구조화된 표현
    /// - 데이터 검증 (Data Annotations 사용)
    /// - 타입 안전성 보장
    /// - null 안전성 처리
    /// 
    /// 💡 사용법:
    /// var invoiceDto = new InvoiceDto { RecipientName = "홍길동", ... };
    /// </summary>
    public class InvoiceDto
    {
        #region 기본 정보

        /// <summary>수취인명 (필수)</summary>
        [Required(ErrorMessage = "수취인명은 필수입니다.")]
        [MaxLength(100, ErrorMessage = "수취인명은 100자를 초과할 수 없습니다.")]
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>전화번호1 (필수)</summary>
        [Required(ErrorMessage = "전화번호는 필수입니다.")]
        [MaxLength(20, ErrorMessage = "전화번호는 20자를 초과할 수 없습니다.")]
        public string Phone1 { get; set; } = string.Empty;

        /// <summary>전화번호2 (선택)</summary>
        [MaxLength(20, ErrorMessage = "전화번호2는 20자를 초과할 수 없습니다.")]
        public string Phone2 { get; set; } = string.Empty;

        /// <summary>우편번호 (필수)</summary>
        [Required(ErrorMessage = "우편번호는 필수입니다.")]
        [MaxLength(10, ErrorMessage = "우편번호는 10자를 초과할 수 없습니다.")]
        public string ZipCode { get; set; } = string.Empty;

        /// <summary>주소 (필수)</summary>
        [Required(ErrorMessage = "주소는 필수입니다.")]
        [MaxLength(500, ErrorMessage = "주소는 500자를 초과할 수 없습니다.")]
        public string Address { get; set; } = string.Empty;

        #endregion

        #region 주문 정보

        /// <summary>옵션명</summary>
        [MaxLength(200, ErrorMessage = "옵션명은 200자를 초과할 수 없습니다.")]
        public string OptionName { get; set; } = string.Empty;

        /// <summary>수량</summary>
        [Range(1, int.MaxValue, ErrorMessage = "수량은 1 이상이어야 합니다.")]
        public int Quantity { get; set; } = 1;

        /// <summary>배송메세지 (특이사항)</summary>
        [MaxLength(500, ErrorMessage = "배송메세지는 500자를 초과할 수 없습니다.")]
        public string SpecialNote { get; set; } = string.Empty;

        /// <summary>주문번호 (필수)</summary>
        [Required(ErrorMessage = "주문번호는 필수입니다.")]
        [MaxLength(100, ErrorMessage = "주문번호는 100자를 초과할 수 없습니다.")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>쇼핑몰 (매장명)</summary>
        [MaxLength(100, ErrorMessage = "쇼핑몰명은 100자를 초과할 수 없습니다.")]
        public string StoreName { get; set; } = string.Empty;

        #endregion

        #region 상품 정보

        /// <summary>수집시간</summary>
        public DateTime CollectedAt { get; set; } = DateTime.Now;

        /// <summary>송장명 (품목명)</summary>
        [MaxLength(200, ErrorMessage = "송장명은 200자를 초과할 수 없습니다.")]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>품목코드</summary>
        [MaxLength(50, ErrorMessage = "품목코드는 50자를 초과할 수 없습니다.")]
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>주문번호(쇼핑몰)</summary>
        [MaxLength(100, ErrorMessage = "주문번호(쇼핑몰)는 100자를 초과할 수 없습니다.")]
        public string OrderNumberMall { get; set; } = string.Empty;

        #endregion

        #region 결제 정보

        /// <summary>결제금액</summary>
        [Range(0, double.MaxValue, ErrorMessage = "결제금액은 0 이상이어야 합니다.")]
        public decimal PaymentAmount { get; set; }

        /// <summary>주문금액</summary>
        [Range(0, double.MaxValue, ErrorMessage = "주문금액은 0 이상이어야 합니다.")]
        public decimal OrderAmount { get; set; }

        /// <summary>결제수단</summary>
        [MaxLength(255, ErrorMessage = "결제수단은 255자를 초과할 수 없습니다.")]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>면과세구분 (가격카테고리)</summary>
        [MaxLength(50, ErrorMessage = "면과세구분은 50자를 초과할 수 없습니다.")]
        public string TaxType { get; set; } = string.Empty;

        /// <summary>주문상태 (처리상태)</summary>
        [MaxLength(50, ErrorMessage = "주문상태는 50자를 초과할 수 없습니다.")]
        public string OrderStatus { get; set; } = string.Empty;

        /// <summary>배송송 (배송타입)</summary>
        [MaxLength(50, ErrorMessage = "배송송은 50자를 초과할 수 없습니다.")]
        public string ShippingType { get; set; } = string.Empty;

        #endregion

        #region 생성 메서드

        /// <summary>
        /// Order 모델에서 InvoiceDto로 변환하는 팩토리 메서드
        /// 
        /// 📋 기능:
        /// - Order 모델의 데이터를 InvoiceDto로 안전하게 변환
        /// - null 안전성 보장
        /// - 타입 변환 처리
        /// 
        /// 💡 사용법:
        /// var dto = InvoiceDto.FromOrder(order);
        /// </summary>
        /// <param name="order">변환할 Order 모델</param>
        /// <returns>변환된 InvoiceDto</returns>
        public static InvoiceDto FromOrder(Order order)
        {
            return new InvoiceDto
            {
                RecipientName = order.RecipientName ?? string.Empty,
                Phone1 = order.RecipientPhone ?? string.Empty,
                Phone2 = string.Empty,
                ZipCode = order.ZipCode ?? string.Empty,
                Address = order.Address ?? string.Empty,
                OptionName = string.Empty,
                Quantity = order.Quantity,
                SpecialNote = order.SpecialNote ?? string.Empty,
                OrderNumber = order.OrderNumber ?? string.Empty,
                StoreName = order.StoreName ?? string.Empty,
                CollectedAt = DateTime.Now,
                ProductName = order.ProductName ?? string.Empty,
                ProductCode = order.ProductCode ?? string.Empty,
                OrderNumberMall = order.OrderNumber ?? string.Empty,
                PaymentAmount = order.TotalPrice,
                OrderAmount = order.TotalPrice,
                PaymentMethod = order.PaymentMethod ?? string.Empty,
                TaxType = order.PriceCategory ?? string.Empty,
                OrderStatus = order.ProcessingStatus ?? string.Empty,
                ShippingType = order.ShippingType ?? string.Empty
            };
        }

        #endregion

        #region 검증 메서드

        /// <summary>
        /// DTO 데이터 유효성 검사
        /// 
        /// 📋 기능:
        /// - 필수 필드 검증
        /// - 데이터 형식 검증
        /// - 비즈니스 규칙 검증
        /// 
        /// 💡 사용법:
        /// if (dto.IsValid()) { ... }
        /// </summary>
        /// <returns>유효성 검사 결과</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(RecipientName) &&
                   !string.IsNullOrWhiteSpace(Phone1) &&
                   !string.IsNullOrWhiteSpace(ZipCode) &&
                   !string.IsNullOrWhiteSpace(Address) &&
                   !string.IsNullOrWhiteSpace(OrderNumber) &&
                   Quantity > 0 &&
                   PaymentAmount >= 0 &&
                   OrderAmount >= 0;
        }

        #endregion
    }
}