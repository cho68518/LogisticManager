namespace LogisticManager.Models
{
    /// <summary>
    /// 사방넷 주문 데이터를 담는 모델 클래스
    /// 
    /// 주요 기능:
    /// - Excel 파일에서 읽은 주문 데이터를 객체로 변환
    /// - DataRow와 Order 객체 간의 변환
    /// - 주문 데이터 유효성 검사
    /// - 주소 별표 처리 (특정 조건에 따라)
    /// - 박스크기 여부 확인
    /// 
    /// 데이터 구조:
    /// - 기본 주문 정보: 주문번호, 주문일자, 수취인 정보
    /// - 상품 정보: 품목코드, 품목명, 수량, 가격
    /// - 배송 정보: 배송타입, 출고지, 결제방법, 배송비
    /// - 특수 처리 정보: 박스크기, 특이사항, 처리상태
    /// - 추가 필드: 매장명, 이벤트타입, 가격카테고리, 지역, 배송지역
    /// 
    /// 사용 목적:
    /// - 송장 처리 시스템의 핵심 데이터 모델
    /// - Excel 데이터와 데이터베이스 간의 중간 형식
    /// - 비즈니스 로직 처리 시 데이터 검증
    /// </summary>
    public class Order
    {
        #region 기본 주문 정보 (Basic Order Information)

        /// <summary>
        /// 주문번호 - 고유한 주문 식별자
        /// </summary>
        public string? OrderNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// 주문일자 - 주문이 발생한 날짜
        /// </summary>
        public string? OrderDate { get; set; } = string.Empty;
        
        /// <summary>
        /// 수취인명 - 상품을 받는 사람의 이름
        /// </summary>
        public string? RecipientName { get; set; } = string.Empty;
        
        /// <summary>
        /// 수취인연락처 - 수취인의 전화번호
        /// </summary>
        public string? RecipientPhone { get; set; } = string.Empty;
        
        /// <summary>
        /// 주소 - 배송지의 기본 주소
        /// </summary>
        public string? Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 상세주소 - 배송지의 상세 주소 (건물명, 호수 등)
        /// </summary>
        public string? DetailAddress { get; set; } = string.Empty;
        
        /// <summary>
        /// 우편번호 - 배송지의 우편번호
        /// </summary>
        public string? ZipCode { get; set; } = string.Empty;

        #endregion

        #region 상품 정보 (Product Information)

        /// <summary>
        /// 품목코드 - 상품의 고유 식별 코드
        /// </summary>
        public string? ProductCode { get; set; } = string.Empty;
        
        /// <summary>
        /// 품목명 - 상품의 이름
        /// </summary>
        public string? ProductName { get; set; } = string.Empty;
        
        /// <summary>
        /// 수량 - 주문한 상품의 개수
        /// </summary>
        public int Quantity { get; set; } = 0;
        
        /// <summary>
        /// 단가 - 상품 1개당 가격
        /// </summary>
        public decimal UnitPrice { get; set; } = 0;
        
        /// <summary>
        /// 총액 - 수량 × 단가로 계산된 총 가격
        /// </summary>
        public decimal TotalPrice { get; set; } = 0;

        #endregion

        #region 배송 정보 (Shipping Information)

        /// <summary>
        /// 배송타입 - 배송 방식 (일반배송, 특급배송 등)
        /// </summary>
        public string? ShippingType { get; set; } = string.Empty;
        
        /// <summary>
        /// 출고지 - 상품이 출고되는 창고 또는 센터
        /// </summary>
        public string? ShippingCenter { get; set; } = string.Empty;
        
        /// <summary>
        /// 결제방법 - 주문 결제 방식 (카드결제, 현금결제, 무통장입금 등)
        /// </summary>
        public string? PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// 배송비 - 배송에 필요한 추가 비용
        /// </summary>
        public decimal ShippingCost { get; set; } = 0;

        #endregion

        #region 특수 처리 정보 (Special Processing Information)

        /// <summary>
        /// 박스크기 - 상품의 포장 박스 크기 (박스 상품 여부 판단용)
        /// </summary>
        public string? BoxSize { get; set; } = string.Empty;
        
        /// <summary>
        /// 특이사항 - 주문에 대한 특별한 요청사항이나 메모
        /// </summary>
        public string? SpecialNote { get; set; } = string.Empty;
        
        /// <summary>
        /// 처리상태 - 주문의 현재 처리 상태 (합포장, 처리완료 등)
        /// </summary>
        public string? ProcessingStatus { get; set; } = string.Empty;

        #endregion

        #region 추가 필드 (Additional Fields)

        /// <summary>
        /// 매장명 - 주문이 발생한 매장 또는 판매처
        /// </summary>
        public string? StoreName { get; set; } = string.Empty;
        
        /// <summary>
        /// 이벤트타입 - 적용된 이벤트 종류 (신규가입, 재구매, VIP 등)
        /// </summary>
        public string? EventType { get; set; } = string.Empty;
        
        /// <summary>
        /// 가격카테고리 - 상품의 가격 등급 (일반, 프리미엄 등)
        /// </summary>
        public string? PriceCategory { get; set; } = string.Empty;
        
        /// <summary>
        /// 지역 - 배송지의 지역 정보 (부산, 경남 등)
        /// </summary>
        public string? Region { get; set; } = string.Empty;
        
        /// <summary>
        /// 배송지역 - 배송이 이루어지는 구체적인 지역
        /// </summary>
        public string? DeliveryArea { get; set; } = string.Empty;

        #endregion

        #region 데이터 변환 메서드 (Data Conversion Methods)

        /// <summary>
        /// DataRow에서 Order 객체를 생성하는 정적 메서드
        /// 
        /// 변환 과정:
        /// 1. Excel 파일에서 읽은 DataRow의 각 컬럼을 확인
        /// 2. 해당하는 Order 객체의 속성에 값을 할당
        /// 3. 숫자 데이터는 안전한 변환을 위해 TryParse 사용
        /// 4. null 값은 빈 문자열로 처리
        /// 
        /// 매핑 규칙:
        /// - Excel 컬럼명과 Order 속성명이 일치
        /// - 수량, 단가, 총액, 배송비는 숫자형으로 변환
        /// - 나머지는 문자열로 처리
        /// </summary>
        /// <param name="row">DataRow 객체 (Excel에서 읽은 데이터)</param>
        /// <returns>Order 객체</returns>
        public static Order FromDataRow(System.Data.DataRow row)
        {
            return new Order
            {
                // 기본 주문 정보 매핑
                OrderNumber = row["주문번호"]?.ToString() ?? string.Empty,
                OrderDate = row["주문일자"]?.ToString() ?? string.Empty,
                RecipientName = row["수취인명"]?.ToString() ?? string.Empty,
                RecipientPhone = row["수취인연락처"]?.ToString() ?? string.Empty,
                Address = row["주소"]?.ToString() ?? string.Empty,
                DetailAddress = row["상세주소"]?.ToString() ?? string.Empty,
                ZipCode = row["우편번호"]?.ToString() ?? string.Empty,
                
                // 상품 정보 매핑 (숫자형 데이터는 안전한 변환)
                ProductCode = row["품목코드"]?.ToString() ?? string.Empty,
                ProductName = row["품목명"]?.ToString() ?? string.Empty,
                Quantity = int.TryParse(row["수량"]?.ToString(), out var qty) ? qty : 0,
                UnitPrice = decimal.TryParse(row["단가"]?.ToString(), out var unitPrice) ? unitPrice : 0,
                TotalPrice = decimal.TryParse(row["총액"]?.ToString(), out var totalPrice) ? totalPrice : 0,
                
                // 배송 정보 매핑
                ShippingType = row["배송타입"]?.ToString() ?? string.Empty,
                ShippingCenter = row["출고지"]?.ToString() ?? string.Empty,
                PaymentMethod = row["결제방법"]?.ToString() ?? string.Empty,
                ShippingCost = decimal.TryParse(row["배송비"]?.ToString(), out var shippingCost) ? shippingCost : 0,
                
                // 특수 처리 정보 매핑
                BoxSize = row["박스크기"]?.ToString() ?? string.Empty,
                SpecialNote = row["특이사항"]?.ToString() ?? string.Empty,
                ProcessingStatus = row["처리상태"]?.ToString() ?? string.Empty,
                
                // 추가 필드 매핑
                StoreName = row["매장명"]?.ToString() ?? string.Empty,
                EventType = row["이벤트타입"]?.ToString() ?? string.Empty,
                PriceCategory = row["가격카테고리"]?.ToString() ?? string.Empty,
                Region = row["지역"]?.ToString() ?? string.Empty,
                DeliveryArea = row["배송지역"]?.ToString() ?? string.Empty
            };
        }

        /// <summary>
        /// Order 객체를 DataRow로 변환하는 메서드
        /// 
        /// 변환 과정:
        /// 1. 대상 DataTable에서 새로운 DataRow 생성
        /// 2. Order 객체의 각 속성 값을 DataRow에 할당
        /// 3. null 값은 빈 문자열로 처리
        /// 4. 완성된 DataRow 반환
        /// 
        /// 사용 목적:
        /// - 처리된 Order 데이터를 Excel 파일로 저장
        /// - 데이터베이스에 저장할 형식으로 변환
        /// - 최종 송장 파일 생성
        /// </summary>
        /// <param name="table">대상 DataTable (출력 파일의 구조)</param>
        /// <returns>DataRow 객체</returns>
        public System.Data.DataRow ToDataRow(System.Data.DataTable table)
        {
            // 새로운 DataRow 생성
            var row = table.NewRow();
            
            // 기본 주문 정보를 DataRow에 할당
            row["주문번호"] = OrderNumber ?? string.Empty;
            row["주문일자"] = OrderDate ?? string.Empty;
            row["수취인명"] = RecipientName ?? string.Empty;
            row["수취인연락처"] = RecipientPhone ?? string.Empty;
            row["주소"] = Address ?? string.Empty;
            row["상세주소"] = DetailAddress ?? string.Empty;
            row["우편번호"] = ZipCode ?? string.Empty;
            
            // 상품 정보를 DataRow에 할당
            row["품목코드"] = ProductCode ?? string.Empty;
            row["품목명"] = ProductName ?? string.Empty;
            row["수량"] = Quantity;
            row["단가"] = UnitPrice;
            row["총액"] = TotalPrice;
            
            // 배송 정보를 DataRow에 할당
            row["배송타입"] = ShippingType ?? string.Empty;
            row["출고지"] = ShippingCenter ?? string.Empty;
            row["결제방법"] = PaymentMethod ?? string.Empty;
            row["배송비"] = ShippingCost;
            
            // 특수 처리 정보를 DataRow에 할당
            row["박스크기"] = BoxSize ?? string.Empty;
            row["특이사항"] = SpecialNote ?? string.Empty;
            row["처리상태"] = ProcessingStatus ?? string.Empty;
            
            // 추가 필드를 DataRow에 할당
            row["매장명"] = StoreName ?? string.Empty;
            row["이벤트타입"] = EventType ?? string.Empty;
            row["가격카테고리"] = PriceCategory ?? string.Empty;
            row["지역"] = Region ?? string.Empty;
            row["배송지역"] = DeliveryArea ?? string.Empty;

            return row;
        }

        #endregion

        #region 유틸리티 메서드 (Utility Methods)

        /// <summary>
        /// 주소에 별표를 추가하는 메서드
        /// 
        /// 추가 조건:
        /// - 주소가 비어있지 않아야 함
        /// - 이미 별표가 포함되어 있지 않아야 함
        /// 
        /// 사용 목적:
        /// - 특정 주소 조건에 따른 별표 처리
        /// - 송장 출력 시 특별한 주소 표시
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
        /// 
        /// 제거 조건:
        /// - 주소가 비어있지 않아야 함
        /// - 주소가 별표로 시작해야 함
        /// 
        /// 사용 목적:
        /// - 별표 처리 취소
        /// - 원본 주소 복원
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
        /// 
        /// 확인 조건:
        /// - BoxSize가 null이거나 빈 문자열
        /// - 공백만 있는 경우도 빈 것으로 처리
        /// 
        /// 사용 목적:
        /// - 낱개 상품과 박스 상품 분류
        /// - 합포장 처리 시 박스 상품 제외
        /// </summary>
        /// <returns>박스크기가 비어있으면 true, 아니면 false</returns>
        public bool IsBoxSizeEmpty()
        {
            return string.IsNullOrEmpty(BoxSize) || BoxSize.Trim() == string.Empty;
        }

        /// <summary>
        /// 주문이 유효한지 확인하는 메서드
        /// 
        /// 유효성 검사 조건:
        /// - 수취인명이 비어있지 않아야 함
        /// - 주소가 비어있지 않아야 함
        /// - 품목명이 비어있지 않아야 함
        /// - 수량이 0보다 커야 함
        /// 
        /// 사용 목적:
        /// - 데이터 처리 전 유효성 검사
        /// - 잘못된 데이터 필터링
        /// - 송장 생성 시 필수 정보 확인
        /// </summary>
        /// <returns>유효한 주문이면 true, 아니면 false</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(RecipientName) && 
                   !string.IsNullOrEmpty(Address) && 
                   !string.IsNullOrEmpty(ProductName) && 
                   Quantity > 0;
        }

        #endregion
    }
} 