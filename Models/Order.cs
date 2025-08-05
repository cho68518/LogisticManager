namespace LogisticManager.Models
{
    /// <summary>
    /// 사방넷 주문 데이터를 담는 모델 클래스
    /// </summary>
    public class Order
    {
        // 기본 주문 정보
        public string? OrderNumber { get; set; } = string.Empty;           // 주문번호
        public string? OrderDate { get; set; } = string.Empty;             // 주문일자
        public string? RecipientName { get; set; } = string.Empty;         // 수취인명
        public string? RecipientPhone { get; set; } = string.Empty;        // 수취인연락처
        public string? Address { get; set; } = string.Empty;               // 주소
        public string? DetailAddress { get; set; } = string.Empty;         // 상세주소
        public string? ZipCode { get; set; } = string.Empty;               // 우편번호

        // 상품 정보
        public string? ProductCode { get; set; } = string.Empty;           // 품목코드
        public string? ProductName { get; set; } = string.Empty;           // 품목명
        public int Quantity { get; set; } = 0;                             // 수량
        public decimal UnitPrice { get; set; } = 0;                        // 단가
        public decimal TotalPrice { get; set; } = 0;                       // 총액

        // 배송 정보
        public string? ShippingType { get; set; } = string.Empty;          // 배송타입
        public string? ShippingCenter { get; set; } = string.Empty;        // 출고지
        public string? PaymentMethod { get; set; } = string.Empty;         // 결제방법
        public decimal ShippingCost { get; set; } = 0;                     // 배송비

        // 특수 처리 정보
        public string? BoxSize { get; set; } = string.Empty;               // 박스크기
        public string? SpecialNote { get; set; } = string.Empty;           // 특이사항
        public string? ProcessingStatus { get; set; } = string.Empty;      // 처리상태

        // 추가 필드 (파이썬 스크립트에서 사용되는 컬럼들)
        public string? StoreName { get; set; } = string.Empty;             // 매장명
        public string? EventType { get; set; } = string.Empty;             // 이벤트타입
        public string? PriceCategory { get; set; } = string.Empty;         // 가격카테고리
        public string? Region { get; set; } = string.Empty;                // 지역
        public string? DeliveryArea { get; set; } = string.Empty;          // 배송지역

        /// <summary>
        /// DataRow에서 Order 객체를 생성하는 정적 메서드
        /// </summary>
        /// <param name="row">DataRow 객체</param>
        /// <returns>Order 객체</returns>
        public static Order FromDataRow(System.Data.DataRow row)
        {
            return new Order
            {
                OrderNumber = row["주문번호"]?.ToString() ?? string.Empty,
                OrderDate = row["주문일자"]?.ToString() ?? string.Empty,
                RecipientName = row["수취인명"]?.ToString() ?? string.Empty,
                RecipientPhone = row["수취인연락처"]?.ToString() ?? string.Empty,
                Address = row["주소"]?.ToString() ?? string.Empty,
                DetailAddress = row["상세주소"]?.ToString() ?? string.Empty,
                ZipCode = row["우편번호"]?.ToString() ?? string.Empty,
                ProductCode = row["품목코드"]?.ToString() ?? string.Empty,
                ProductName = row["품목명"]?.ToString() ?? string.Empty,
                Quantity = int.TryParse(row["수량"]?.ToString(), out var qty) ? qty : 0,
                UnitPrice = decimal.TryParse(row["단가"]?.ToString(), out var unitPrice) ? unitPrice : 0,
                TotalPrice = decimal.TryParse(row["총액"]?.ToString(), out var totalPrice) ? totalPrice : 0,
                ShippingType = row["배송타입"]?.ToString() ?? string.Empty,
                ShippingCenter = row["출고지"]?.ToString() ?? string.Empty,
                PaymentMethod = row["결제방법"]?.ToString() ?? string.Empty,
                ShippingCost = decimal.TryParse(row["배송비"]?.ToString(), out var shippingCost) ? shippingCost : 0,
                BoxSize = row["박스크기"]?.ToString() ?? string.Empty,
                SpecialNote = row["특이사항"]?.ToString() ?? string.Empty,
                ProcessingStatus = row["처리상태"]?.ToString() ?? string.Empty,
                StoreName = row["매장명"]?.ToString() ?? string.Empty,
                EventType = row["이벤트타입"]?.ToString() ?? string.Empty,
                PriceCategory = row["가격카테고리"]?.ToString() ?? string.Empty,
                Region = row["지역"]?.ToString() ?? string.Empty,
                DeliveryArea = row["배송지역"]?.ToString() ?? string.Empty
            };
        }

        /// <summary>
        /// Order 객체를 DataRow로 변환하는 메서드
        /// </summary>
        /// <param name="table">대상 DataTable</param>
        /// <returns>DataRow 객체</returns>
        public System.Data.DataRow ToDataRow(System.Data.DataTable table)
        {
            var row = table.NewRow();
            
            row["주문번호"] = OrderNumber ?? string.Empty;
            row["주문일자"] = OrderDate ?? string.Empty;
            row["수취인명"] = RecipientName ?? string.Empty;
            row["수취인연락처"] = RecipientPhone ?? string.Empty;
            row["주소"] = Address ?? string.Empty;
            row["상세주소"] = DetailAddress ?? string.Empty;
            row["우편번호"] = ZipCode ?? string.Empty;
            row["품목코드"] = ProductCode ?? string.Empty;
            row["품목명"] = ProductName ?? string.Empty;
            row["수량"] = Quantity;
            row["단가"] = UnitPrice;
            row["총액"] = TotalPrice;
            row["배송타입"] = ShippingType ?? string.Empty;
            row["출고지"] = ShippingCenter ?? string.Empty;
            row["결제방법"] = PaymentMethod ?? string.Empty;
            row["배송비"] = ShippingCost;
            row["박스크기"] = BoxSize ?? string.Empty;
            row["특이사항"] = SpecialNote ?? string.Empty;
            row["처리상태"] = ProcessingStatus ?? string.Empty;
            row["매장명"] = StoreName ?? string.Empty;
            row["이벤트타입"] = EventType ?? string.Empty;
            row["가격카테고리"] = PriceCategory ?? string.Empty;
            row["지역"] = Region ?? string.Empty;
            row["배송지역"] = DeliveryArea ?? string.Empty;

            return row;
        }

        /// <summary>
        /// 주소에 별표를 추가하는 메서드
        /// </summary>
        public void AddStarToAddress()
        {
            if (!string.IsNullOrEmpty(Address) && !Address.Contains("*"))
            {
                Address = $"*{Address}";
            }
        }

        /// <summary>
        /// 주소에서 별표를 제거하는 메서드
        /// </summary>
        public void RemoveStarFromAddress()
        {
            if (!string.IsNullOrEmpty(Address) && Address.StartsWith("*"))
            {
                Address = Address.Substring(1);
            }
        }

        /// <summary>
        /// 박스크기가 비어있는지 확인하는 메서드
        /// </summary>
        /// <returns>박스크기가 비어있으면 true</returns>
        public bool IsBoxSizeEmpty()
        {
            return string.IsNullOrEmpty(BoxSize) || BoxSize.Trim() == string.Empty;
        }

        /// <summary>
        /// 주문이 유효한지 확인하는 메서드
        /// </summary>
        /// <returns>유효한 주문이면 true</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(RecipientName) && 
                   !string.IsNullOrEmpty(Address) && 
                   !string.IsNullOrEmpty(ProductName) && 
                   Quantity > 0;
        }
    }
} 