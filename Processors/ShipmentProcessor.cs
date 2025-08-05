using System.Data;
using LogisticManager.Models;

namespace LogisticManager.Processors
{
    /// <summary>
    /// 하나의 출고지를 처리하는 재사용 가능한 로직을 담는 클래스
    /// 
    /// 주요 기능:
    /// - 낱개/박스 상품 분류
    /// - 합포장 송장 생성
    /// - 별표 처리 (특정 주소 조건)
    /// - 특수 출고지별 맞춤 처리
    /// - 최종 데이터 병합
    /// 
    /// 처리 단계:
    /// 1. 낱개/박스 분류 (10%)
    /// 2. 추가 송장 계산 (30%)
    /// 3. 별표 처리 (20%)
    /// 4. 최종 데이터 병합 (40%)
    /// 
    /// 특수 출고지 지원:
    /// - 감천: 지역별 가격 조정, 배송비 20% 추가
    /// - 카카오: 이벤트 가격 적용, 배송비 20% 할인
    /// </summary>
    public class ShipmentProcessor
    {
        #region 필드 (Private Fields)

        /// <summary>
        /// 처리할 출고지명
        /// </summary>
        private readonly string _centerName;
        
        /// <summary>
        /// 처리할 원본 데이터
        /// </summary>
        private readonly DataTable _data;
        
        /// <summary>
        /// 해당 출고지의 배송비
        /// </summary>
        private readonly decimal _shippingCost;
        
        /// <summary>
        /// 진행 상황 메시지 콜백
        /// </summary>
        private readonly IProgress<string>? _progress;

        #endregion

        #region 생성자 (Constructor)

        /// <summary>
        /// ShipmentProcessor 생성자
        /// 
        /// 의존성 주입:
        /// - centerName: 처리할 출고지명
        /// - data: 처리할 데이터
        /// - shippingCost: 배송비
        /// - progress: 진행 상황 콜백 (선택사항)
        /// </summary>
        /// <param name="centerName">출고지명</param>
        /// <param name="data">처리할 데이터</param>
        /// <param name="shippingCost">배송비</param>
        /// <param name="progress">진행 상황 콜백 (선택사항)</param>
        public ShipmentProcessor(string centerName, DataTable data, decimal shippingCost, IProgress<string>? progress = null)
        {
            // 각 매개변수를 private 필드에 저장
            _centerName = centerName;
            _data = data;
            _shippingCost = shippingCost;
            _progress = progress;
        }

        #endregion

        #region 메인 처리 메서드 (Main Processing Method)

        /// <summary>
        /// 출고지별 데이터를 처리하는 메인 메서드
        /// 
        /// 처리 과정:
        /// 1. 낱개/박스 분류 (10%)
        /// 2. 추가 송장 계산 (30%)
        /// 3. 별표 처리 (20%)
        /// 4. 최종 데이터 병합 (40%)
        /// 
        /// 예외 처리:
        /// - 데이터 처리 오류
        /// - 계산 오류
        /// - 병합 오류
        /// </summary>
        /// <returns>처리된 DataTable</returns>
        public DataTable Process()
        {
            try
            {
                _progress?.Report($"{_centerName} 출고지 데이터 처리 시작...");

                // 1단계: 낱개/박스 분류
                var (individualItems, boxItems) = ClassifyItems();
                _progress?.Report($"{_centerName}: 낱개 {individualItems.Count}건, 박스 {boxItems.Count}건 분류 완료");

                // 2단계: 추가 송장 계산
                var additionalInvoices = CalculateAdditionalInvoices(individualItems, boxItems);
                _progress?.Report($"{_centerName}: 추가 송장 {additionalInvoices.Count}건 생성 완료");

                // 3단계: 별표 처리
                ProcessStarMarking(individualItems);
                ProcessStarMarking(boxItems);
                ProcessStarMarking(additionalInvoices);
                _progress?.Report($"{_centerName}: 별표 처리 완료");

                // 4단계: 최종 데이터 병합
                var finalData = MergeFinalData(individualItems, boxItems, additionalInvoices);
                _progress?.Report($"{_centerName}: 최종 데이터 병합 완료 (총 {finalData.Rows.Count}건)");

                return finalData;
            }
            catch (Exception ex)
            {
                _progress?.Report($"{_centerName} 처리 중 오류 발생: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 상품 분류 (Item Classification)

        /// <summary>
        /// 낱개/박스 분류 처리
        /// 
        /// 분류 기준:
        /// - 박스크기가 있으면 박스 상품
        /// - 박스크기가 없으면 낱개 상품
        /// 
        /// 처리 과정:
        /// 1. 각 DataRow를 Order 객체로 변환
        /// 2. 데이터 유효성 검사
        /// 3. 박스크기 여부에 따라 분류
        /// 4. 각각의 리스트에 추가
        /// </summary>
        /// <returns>(낱개 상품, 박스 상품) 튜플</returns>
        private (List<Order> individualItems, List<Order> boxItems) ClassifyItems()
        {
            // 낱개 상품과 박스 상품을 저장할 리스트
            var individualItems = new List<Order>();
            var boxItems = new List<Order>();

            // 각 행을 순회하며 분류
            foreach (DataRow row in _data.Rows)
            {
                // DataRow를 Order 객체로 변환
                var order = Order.FromDataRow(row);
                
                // 데이터 유효성 검사
                if (order.IsValid())
                {
                    // 박스크기가 있으면 박스 상품으로 분류
                    if (!order.IsBoxSizeEmpty())
                    {
                        boxItems.Add(order);
                    }
                    else
                    {
                        individualItems.Add(order);
                    }
                }
            }

            return (individualItems, boxItems);
        }

        #endregion

        #region 추가 송장 계산 (Additional Invoice Calculation)

        /// <summary>
        /// 추가 송장 계산 (파이썬 스크립트의 합포장 로직)
        /// 
        /// 합포장 조건:
        /// - 같은 수취인명과 주소를 가진 낱개 상품들
        /// - 2개 이상의 상품이 있는 경우
        /// 
        /// 처리 과정:
        /// 1. 같은 수취인명과 주소로 그룹화
        /// 2. 2개 이상인 그룹만 필터링
        /// 3. 각 그룹의 수량과 가격 합계 계산
        /// 4. 새로운 합포장 송장 생성
        /// 
        /// 생성되는 정보:
        /// - 주문번호: 원본번호_합포장
        /// - 상품명: 합포장 (N개 상품)
        /// - 수량: 전체 수량 합계
        /// - 단가: 전체 가격 합계 / 전체 수량
        /// - 특별비고: 포함된 상품명들
        /// </summary>
        /// <param name="individualItems">낱개 상품 목록</param>
        /// <param name="boxItems">박스 상품 목록</param>
        /// <returns>추가 송장 목록</returns>
        private List<Order> CalculateAdditionalInvoices(List<Order> individualItems, List<Order> boxItems)
        {
            // 추가 송장을 저장할 리스트
            var additionalInvoices = new List<Order>();

            // 같은 수취인명과 주소를 가진 낱개 상품들을 그룹화
            var groupedItems = individualItems
                .GroupBy(item => new { item.RecipientName, item.Address })
                .Where(group => group.Count() > 1) // 2개 이상의 상품이 있는 경우만
                .ToList();

            // 각 그룹에 대해 합포장 송장 생성
            foreach (var group in groupedItems)
            {
                // 그룹의 첫 번째 항목을 기준으로 사용
                var firstItem = group.First();
                // 전체 수량과 가격 계산
                var totalQuantity = group.Sum(item => item.Quantity);
                var totalPrice = group.Sum(item => item.TotalPrice);

                // 추가 송장 생성
                var additionalInvoice = new Order
                {
                    OrderNumber = $"{firstItem.OrderNumber}_합포장",
                    OrderDate = firstItem.OrderDate,
                    RecipientName = firstItem.RecipientName,
                    RecipientPhone = firstItem.RecipientPhone,
                    Address = firstItem.Address,
                    DetailAddress = firstItem.DetailAddress,
                    ZipCode = firstItem.ZipCode,
                    ProductCode = "합포장",
                    ProductName = $"합포장 ({group.Count()}개 상품)",
                    Quantity = totalQuantity,
                    UnitPrice = totalPrice / totalQuantity, // 평균 단가 계산
                    TotalPrice = totalPrice,
                    ShippingType = firstItem.ShippingType,
                    ShippingCenter = _centerName,
                    PaymentMethod = firstItem.PaymentMethod,
                    ShippingCost = _shippingCost,
                    BoxSize = "합포장",
                    SpecialNote = $"합포장: {string.Join(", ", group.Select(item => item.ProductName))}",
                    ProcessingStatus = "합포장",
                    StoreName = firstItem.StoreName,
                    EventType = firstItem.EventType,
                    PriceCategory = firstItem.PriceCategory,
                    Region = firstItem.Region,
                    DeliveryArea = firstItem.DeliveryArea
                };

                additionalInvoices.Add(additionalInvoice);
            }

            return additionalInvoices;
        }

        #endregion

        #region 별표 처리 (Star Marking)

        /// <summary>
        /// 별표 처리 (특정 조건에 따라 주소에 별표 추가)
        /// 
        /// 별표 추가 조건:
        /// - 주소에 특정 키워드가 포함된 경우
        /// - 키워드: "아파트", "빌라", "상가", "오피스텔", "원룸"
        /// 
        /// 처리 과정:
        /// 1. 각 주문 항목을 순회
        /// 2. 별표 추가 조건 확인
        /// 3. 조건에 맞으면 주소에 별표 추가
        /// </summary>
        /// <param name="items">처리할 주문 목록</param>
        private void ProcessStarMarking(List<Order> items)
        {
            // 각 주문 항목에 대해 별표 처리
            foreach (var item in items)
            {
                // 특정 조건에 따라 별표 추가
                if (ShouldAddStar(item))
                {
                    item.AddStarToAddress();
                }
            }
        }

        /// <summary>
        /// 별표를 추가해야 하는지 판단하는 메서드
        /// 
        /// 판단 기준:
        /// - 주소가 비어있지 않아야 함
        /// - 주소에 특정 키워드가 포함되어 있어야 함
        /// 
        /// 키워드 목록:
        /// - "아파트": 아파트 주소
        /// - "빌라": 빌라 주소
        /// - "상가": 상가 주소
        /// - "오피스텔": 오피스텔 주소
        /// - "원룸": 원룸 주소
        /// </summary>
        /// <param name="item">주문 항목</param>
        /// <returns>별표 추가 여부 (true: 추가, false: 추가하지 않음)</returns>
        private bool ShouldAddStar(Order item)
        {
            // 파이썬 스크립트의 별표 처리 로직을 C#으로 변환
            // 주소가 비어있으면 별표 추가하지 않음
            if (string.IsNullOrEmpty(item.Address))
                return false;

            // 특정 키워드가 포함된 주소에 별표 추가
            var starKeywords = new[] { "아파트", "빌라", "상가", "오피스텔", "원룸" };
            return starKeywords.Any(keyword => item.Address.Contains(keyword));
        }

        #endregion

        #region 최종 데이터 병합 (Final Data Merging)

        /// <summary>
        /// 최종 데이터 병합
        /// 
        /// 병합 순서:
        /// 1. 박스 상품 추가
        /// 2. 추가 송장 (합포장) 추가
        /// 3. 합포장되지 않은 낱개 상품만 추가
        /// 
        /// 중복 제거:
        /// - 합포장된 낱개 상품은 최종 결과에서 제외
        /// - 수취인명과 주소가 동일한 항목들만 제외
        /// 
        /// 처리 과정:
        /// 1. 원본 테이블 구조 복제
        /// 2. 박스 상품들을 먼저 추가
        /// 3. 추가 송장들을 추가
        /// 4. 합포장되지 않은 낱개 상품들만 추가
        /// </summary>
        /// <param name="individualItems">낱개 상품</param>
        /// <param name="boxItems">박스 상품</param>
        /// <param name="additionalInvoices">추가 송장</param>
        /// <returns>병합된 DataTable</returns>
        private DataTable MergeFinalData(List<Order> individualItems, List<Order> boxItems, List<Order> additionalInvoices)
        {
            // 원본 테이블의 구조를 복사하여 새로운 DataTable 생성
            var finalTable = _data.Clone();

            // 1단계: 박스 상품 추가
            foreach (var item in boxItems)
            {
                finalTable.Rows.Add(item.ToDataRow(finalTable));
            }

            // 2단계: 추가 송장 추가
            foreach (var item in additionalInvoices)
            {
                finalTable.Rows.Add(item.ToDataRow(finalTable));
            }

            // 3단계: 낱개 상품 중 합포장되지 않은 것들만 추가
            // 합포장된 수취인명과 주소 목록 생성
            var processedRecipients = additionalInvoices
                .Select(item => new { item.RecipientName, item.Address })
                .ToHashSet();

            // 합포장되지 않은 낱개 상품들만 추가
            foreach (var item in individualItems)
            {
                var key = new { item.RecipientName, item.Address };
                if (!processedRecipients.Contains(key))
                {
                    finalTable.Rows.Add(item.ToDataRow(finalTable));
                }
            }

            return finalTable;
        }

        #endregion

        #region 특수 출고지 처리 (Special Shipment Processing)

        /// <summary>
        /// 특수 출고지 처리 (감천, 카카오 등)
        /// 
        /// 지원하는 특수 출고지:
        /// - 감천: 지역별 가격 조정, 배송비 20% 추가
        /// - 카카오: 이벤트 가격 적용, 배송비 20% 할인
        /// - 기타: 기본 처리 로직 적용
        /// 
        /// 처리 방식:
        /// - 특수 타입에 따라 해당하는 전용 메서드 호출
        /// - 지원하지 않는 타입은 기본 처리 로직 사용
        /// </summary>
        /// <param name="specialType">특수 출고지 타입</param>
        /// <returns>처리된 DataTable</returns>
        public DataTable ProcessSpecialShipment(string specialType)
        {
            _progress?.Report($"{_centerName} {specialType} 특수 처리 시작...");

            // 특수 타입에 따라 해당하는 처리 메서드 호출
            switch (specialType.ToLower())
            {
                case "감천":
                    return ProcessGamcheonShipment();
                case "카카오":
                    return ProcessKakaoShipment();
                default:
                    return Process(); // 기본 처리
            }
        }

        /// <summary>
        /// 감천 특수 출고지 처리
        /// 
        /// 감천 특수 처리 내용:
        /// - 지역별 가격 조정 (부산: 10% 추가, 경남: 5% 추가)
        /// - 배송비 20% 추가 적용
        /// - 특별한 가격 계산 로직 적용
        /// 
        /// 처리 과정:
        /// 1. 원본 데이터 구조 복제
        /// 2. 각 주문에 대해 특수 가격 계산
        /// 3. 배송비 조정
        /// 4. 총 가격 재계산
        /// </summary>
        /// <returns>처리된 DataTable</returns>
        private DataTable ProcessGamcheonShipment()
        {
            // 감천 특수 로직 구현
            var processedData = _data.Clone();

            // 각 주문에 대해 감천 특수 처리
            foreach (DataRow row in _data.Rows)
            {
                // DataRow를 Order 객체로 변환
                var order = Order.FromDataRow(row);
                
                // 감천 특수 처리 로직
                if (order.IsValid())
                {
                    // 특별한 가격 계산
                    order.UnitPrice = CalculateGamcheonPrice(order);
                    order.TotalPrice = order.UnitPrice * order.Quantity;
                    
                    // 특별한 배송비 적용 (20% 추가)
                    order.ShippingCost = _shippingCost * 1.2m;
                    
                    processedData.Rows.Add(order.ToDataRow(processedData));
                }
            }

            return processedData;
        }

        /// <summary>
        /// 카카오 특수 출고지 처리
        /// 
        /// 카카오 특수 처리 내용:
        /// - 이벤트 타입별 가격 할인
        /// - 배송비 20% 할인 적용
        /// - 이벤트 상품 특별 처리
        /// 
        /// 이벤트 타입별 할인율:
        /// - 신규가입: 10% 할인
        /// - 재구매: 5% 할인
        /// - VIP: 20% 할인
        /// 
        /// 처리 과정:
        /// 1. 원본 데이터 구조 복제
        /// 2. 각 주문에 대해 이벤트 가격 계산
        /// 3. 배송비 조정
        /// 4. 총 가격 재계산
        /// </summary>
        /// <returns>처리된 DataTable</returns>
        private DataTable ProcessKakaoShipment()
        {
            // 카카오 특수 로직 구현
            var processedData = _data.Clone();

            // 각 주문에 대해 카카오 특수 처리
            foreach (DataRow row in _data.Rows)
            {
                // DataRow를 Order 객체로 변환
                var order = Order.FromDataRow(row);
                
                // 카카오 특수 처리 로직
                if (order.IsValid())
                {
                    // 이벤트 상품 처리
                    if (!string.IsNullOrEmpty(order.EventType))
                    {
                        order.UnitPrice = CalculateKakaoEventPrice(order);
                        order.TotalPrice = order.UnitPrice * order.Quantity;
                    }
                    
                    // 특별한 배송비 적용 (20% 할인)
                    order.ShippingCost = _shippingCost * 0.8m;
                    
                    processedData.Rows.Add(order.ToDataRow(processedData));
                }
            }

            return processedData;
        }

        #endregion

        #region 가격 계산 (Price Calculation)

        /// <summary>
        /// 감천 가격 계산
        /// 
        /// 지역별 가격 조정:
        /// - 부산: 기본 가격 + 10%
        /// - 경남: 기본 가격 + 5%
        /// - 기타 지역: 기본 가격 유지
        /// 
        /// 계산 방식:
        /// 1. 기본 가격을 기준으로 설정
        /// 2. 지역 정보 확인
        /// 3. 지역별 할증율 적용
        /// 4. 조정된 가격 반환
        /// </summary>
        /// <param name="order">주문 정보</param>
        /// <returns>계산된 가격</returns>
        private decimal CalculateGamcheonPrice(Order order)
        {
            // 감천 특수 가격 계산 로직
            var basePrice = order.UnitPrice;
            
            // 지역별 가격 조정
            if (!string.IsNullOrEmpty(order.Region))
            {
                switch (order.Region)
                {
                    case "부산":
                        return basePrice * 1.1m; // 10% 추가
                    case "경남":
                        return basePrice * 1.05m; // 5% 추가
                    default:
                        return basePrice; // 기본 가격 유지
                }
            }
            
            return basePrice;
        }

        /// <summary>
        /// 카카오 이벤트 가격 계산
        /// 
        /// 이벤트 타입별 할인율:
        /// - 신규가입: 10% 할인
        /// - 재구매: 5% 할인
        /// - VIP: 20% 할인
        /// - 기타: 할인 없음
        /// 
        /// 계산 방식:
        /// 1. 기본 가격을 기준으로 설정
        /// 2. 이벤트 타입 확인
        /// 3. 이벤트별 할인율 적용
        /// 4. 할인된 가격 반환
        /// </summary>
        /// <param name="order">주문 정보</param>
        /// <returns>계산된 가격</returns>
        private decimal CalculateKakaoEventPrice(Order order)
        {
            // 카카오 이벤트 가격 계산 로직
            var basePrice = order.UnitPrice;
            
            // 이벤트 타입별 가격 조정
            switch (order.EventType)
            {
                case "신규가입":
                    return basePrice * 0.9m; // 10% 할인
                case "재구매":
                    return basePrice * 0.95m; // 5% 할인
                case "VIP":
                    return basePrice * 0.8m; // 20% 할인
                default:
                    return basePrice; // 할인 없음
            }
        }

        #endregion
    }
} 