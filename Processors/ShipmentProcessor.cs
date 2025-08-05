using System.Data;
using LogisticManager.Models;

namespace LogisticManager.Processors
{
    /// <summary>
    /// 하나의 출고지를 처리하는 재사용 가능한 로직을 담는 클래스
    /// </summary>
    public class ShipmentProcessor
    {
        private readonly string _centerName;
        private readonly DataTable _data;
        private readonly decimal _shippingCost;
        private readonly IProgress<string>? _progress;

        public ShipmentProcessor(string centerName, DataTable data, decimal shippingCost, IProgress<string>? progress = null)
        {
            _centerName = centerName;
            _data = data;
            _shippingCost = shippingCost;
            _progress = progress;
        }

        /// <summary>
        /// 출고지별 데이터를 처리하는 메인 메서드
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

        /// <summary>
        /// 낱개/박스 분류 처리
        /// </summary>
        /// <returns>(낱개 상품, 박스 상품) 튜플</returns>
        private (List<Order> individualItems, List<Order> boxItems) ClassifyItems()
        {
            var individualItems = new List<Order>();
            var boxItems = new List<Order>();

            foreach (DataRow row in _data.Rows)
            {
                var order = Order.FromDataRow(row);
                
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

        /// <summary>
        /// 추가 송장 계산 (파이썬 스크립트의 합포장 로직)
        /// </summary>
        /// <param name="individualItems">낱개 상품 목록</param>
        /// <param name="boxItems">박스 상품 목록</param>
        /// <returns>추가 송장 목록</returns>
        private List<Order> CalculateAdditionalInvoices(List<Order> individualItems, List<Order> boxItems)
        {
            var additionalInvoices = new List<Order>();

            // 같은 수취인명과 주소를 가진 낱개 상품들을 그룹화
            var groupedItems = individualItems
                .GroupBy(item => new { item.RecipientName, item.Address })
                .Where(group => group.Count() > 1) // 2개 이상의 상품이 있는 경우만
                .ToList();

            foreach (var group in groupedItems)
            {
                var firstItem = group.First();
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
                    UnitPrice = totalPrice / totalQuantity,
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

        /// <summary>
        /// 별표 처리 (특정 조건에 따라 주소에 별표 추가)
        /// </summary>
        /// <param name="items">처리할 주문 목록</param>
        private void ProcessStarMarking(List<Order> items)
        {
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
        /// </summary>
        /// <param name="item">주문 항목</param>
        /// <returns>별표 추가 여부</returns>
        private bool ShouldAddStar(Order item)
        {
            // 파이썬 스크립트의 별표 처리 로직을 C#으로 변환
            if (string.IsNullOrEmpty(item.Address))
                return false;

            // 특정 키워드가 포함된 주소에 별표 추가
            var starKeywords = new[] { "아파트", "빌라", "상가", "오피스텔", "원룸" };
            return starKeywords.Any(keyword => item.Address.Contains(keyword));
        }

        /// <summary>
        /// 최종 데이터 병합
        /// </summary>
        /// <param name="individualItems">낱개 상품</param>
        /// <param name="boxItems">박스 상품</param>
        /// <param name="additionalInvoices">추가 송장</param>
        /// <returns>병합된 DataTable</returns>
        private DataTable MergeFinalData(List<Order> individualItems, List<Order> boxItems, List<Order> additionalInvoices)
        {
            var finalTable = _data.Clone(); // 원본 테이블의 구조를 복사

            // 박스 상품 추가
            foreach (var item in boxItems)
            {
                finalTable.Rows.Add(item.ToDataRow(finalTable));
            }

            // 추가 송장 추가
            foreach (var item in additionalInvoices)
            {
                finalTable.Rows.Add(item.ToDataRow(finalTable));
            }

            // 낱개 상품 중 합포장되지 않은 것들만 추가
            var processedRecipients = additionalInvoices
                .Select(item => new { item.RecipientName, item.Address })
                .ToHashSet();

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

        /// <summary>
        /// 특수 출고지 처리 (감천, 카카오 등)
        /// </summary>
        /// <param name="specialType">특수 출고지 타입</param>
        /// <returns>처리된 DataTable</returns>
        public DataTable ProcessSpecialShipment(string specialType)
        {
            _progress?.Report($"{_centerName} {specialType} 특수 처리 시작...");

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
        /// </summary>
        /// <returns>처리된 DataTable</returns>
        private DataTable ProcessGamcheonShipment()
        {
            // 감천 특수 로직 구현
            var processedData = _data.Clone();

            foreach (DataRow row in _data.Rows)
            {
                var order = Order.FromDataRow(row);
                
                // 감천 특수 처리 로직
                if (order.IsValid())
                {
                    // 특별한 가격 계산
                    order.UnitPrice = CalculateGamcheonPrice(order);
                    order.TotalPrice = order.UnitPrice * order.Quantity;
                    
                    // 특별한 배송비 적용
                    order.ShippingCost = _shippingCost * 1.2m; // 20% 추가
                    
                    processedData.Rows.Add(order.ToDataRow(processedData));
                }
            }

            return processedData;
        }

        /// <summary>
        /// 카카오 특수 출고지 처리
        /// </summary>
        /// <returns>처리된 DataTable</returns>
        private DataTable ProcessKakaoShipment()
        {
            // 카카오 특수 로직 구현
            var processedData = _data.Clone();

            foreach (DataRow row in _data.Rows)
            {
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
                    
                    // 특별한 배송비 적용
                    order.ShippingCost = _shippingCost * 0.8m; // 20% 할인
                    
                    processedData.Rows.Add(order.ToDataRow(processedData));
                }
            }

            return processedData;
        }

        /// <summary>
        /// 감천 가격 계산
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
                        return basePrice;
                }
            }
            
            return basePrice;
        }

        /// <summary>
        /// 카카오 이벤트 가격 계산
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
                    return basePrice;
            }
        }
    }
} 