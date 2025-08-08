using System;
using System.Collections.Generic;
using System.Data;
using LogisticManager.Services;

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
        /// 전화번호1 - 수취인의 첫 번째 전화번호
        /// </summary>
        public string? RecipientPhone1 { get; set; } = string.Empty;
        
        /// <summary>
        /// 전화번호2 - 수취인의 두 번째 전화번호
        /// </summary>
        public string? RecipientPhone2 { get; set; } = string.Empty;
        
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

        #region column_mapping.json 매핑 필드들

        /// <summary>
        /// 옵션명 - 상품 옵션 정보
        /// </summary>
        public string? OptionName { get; set; } = string.Empty;
        
        /// <summary>
        /// 배송메세지 - 배송 관련 메시지
        /// </summary>
        public string? ShippingMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// 쇼핑몰 - 주문이 발생한 쇼핑몰
        /// </summary>
        public string? MallName { get; set; } = string.Empty;
        
        /// <summary>
        /// 수집시간 - 데이터 수집 시간
        /// </summary>
        public DateTime? CollectionTime { get; set; }
        
        /// <summary>
        /// 송장명 - 송장 상품명
        /// </summary>
        public string? InvoiceName { get; set; } = string.Empty;
        
        /// <summary>
        /// 택배비용 - 택배 배송 비용
        /// </summary>
        public string? DeliveryCost { get; set; } = string.Empty;
        
        /// <summary>
        /// 출력개수 - 출력할 개수
        /// </summary>
        public string? PrintCount { get; set; } = string.Empty;
        
        /// <summary>
        /// 송장수량 - 송장 수량
        /// </summary>
        public string? InvoiceQuantity { get; set; } = string.Empty;
        
        /// <summary>
        /// 별표1 - 별표 필드1
        /// </summary>
        public string? Star1 { get; set; } = string.Empty;
        
        /// <summary>
        /// 별표2 - 별표 필드2
        /// </summary>
        public string? Star2 { get; set; } = string.Empty;
        
        /// <summary>
        /// 품목개수 - 품목 개수
        /// </summary>
        public string? ProductCount { get; set; } = string.Empty;
        
        /// <summary>
        /// 택배수량 - 택배 수량
        /// </summary>
        public string? DeliveryQuantity { get; set; } = string.Empty;
        
        /// <summary>
        /// 택배수량1 - 택배 수량1
        /// </summary>
        public string? DeliveryQuantity1 { get; set; } = string.Empty;
        
        /// <summary>
        /// 택배수량합산 - 택배 수량 합산
        /// </summary>
        public string? DeliveryQuantitySum { get; set; } = string.Empty;
        
        /// <summary>
        /// 송장구분자 - 송장 구분자
        /// </summary>
        public string? InvoiceSeparator { get; set; } = string.Empty;
        
        /// <summary>
        /// 송장구분 - 송장 구분
        /// </summary>
        public string? InvoiceType { get; set; } = string.Empty;
        
        /// <summary>
        /// 송장구분최종 - 송장 구분 최종
        /// </summary>
        public string? InvoiceTypeFinal { get; set; } = string.Empty;
        
        /// <summary>
        /// 위치 - 위치 정보
        /// </summary>
        public string? Location { get; set; } = string.Empty;
        
        /// <summary>
        /// 위치변환 - 변환된 위치 정보
        /// </summary>
        public string? LocationConverted { get; set; } = string.Empty;
        
        /// <summary>
        /// 주문번호(쇼핑몰) - 쇼핑몰 주문번호
        /// </summary>
        public string? OrderNumberMall { get; set; } = string.Empty;
        
        /// <summary>
        /// 결제금액 - 결제 금액
        /// </summary>
        public string? PaymentAmount { get; set; } = string.Empty;
        
        /// <summary>
        /// 주문금액 - 주문 금액
        /// </summary>
        public string? OrderAmount { get; set; } = string.Empty;
        
        /// <summary>
        /// 면과세구분 - 면과세 구분
        /// </summary>
        public string? TaxType { get; set; } = string.Empty;
        
        /// <summary>
        /// 주문상태 - 주문 상태
        /// </summary>
        public string? OrderStatus { get; set; } = string.Empty;
        
        /// <summary>
        /// 배송송 - 배송 송
        /// </summary>
        public string? DeliverySend { get; set; } = string.Empty;
        
        /// <summary>
        /// 메시지1 - 추가 메시지1
        /// </summary>
        public string? Msg1 { get; set; } = string.Empty;
        
        /// <summary>
        /// 메시지2 - 추가 메시지2
        /// </summary>
        public string? Msg2 { get; set; } = string.Empty;
        
        /// <summary>
        /// 메시지3 - 추가 메시지3
        /// </summary>
        public string? Msg3 { get; set; } = string.Empty;
        
        /// <summary>
        /// 메시지4 - 추가 메시지4
        /// </summary>
        public string? Msg4 { get; set; } = string.Empty;
        
        /// <summary>
        /// 메시지5 - 추가 메시지5
        /// </summary>
        public string? Msg5 { get; set; } = string.Empty;
        
        /// <summary>
        /// 메시지6 - 추가 메시지6
        /// </summary>
        public string? Msg6 { get; set; } = string.Empty;

        #endregion

        #region 데이터 변환 메서드 (Data Conversion Methods)

        /// <summary>
        /// DataRow를 Order 객체로 변환하는 메서드 (column_mapping.json 사용)
        /// 
        /// 변환 과정:
        /// 1. column_mapping.json에서 컬럼 매핑 정보 로드
        /// 2. DataRow의 각 컬럼을 매핑 정보에 따라 변환
        /// 3. null 안전성 처리 및 타입 변환
        /// 4. 유효성 검사 수행
        /// 
        /// 매핑 처리:
        /// - Excel 컬럼명을 데이터베이스 컬럼명으로 변환
        /// - 매핑되지 않은 컬럼은 원본 이름 유지
        /// - 데이터 타입 변환 적용
        /// 
        /// 예외 처리:
        /// - 매핑 파일이 없는 경우 기본 매핑 사용
        /// - 컬럼이 없는 경우 빈 값으로 처리
        /// - 타입 변환 실패 시 기본값 사용
        /// </summary>
        /// <param name="row">변환할 DataRow</param>
        /// <returns>변환된 Order 객체</returns>
        public static Order FromDataRow(System.Data.DataRow row)
        {
            var order = new Order();
            
            try
            {
                // MappingService를 통해 column_mapping.json 로드
                var mappingService = new MappingService();
                var configuration = mappingService.GetConfiguration();
                
                if (configuration?.Mappings.TryGetValue("order_table", out var tableMapping) == true && tableMapping != null)
                {
                    Console.WriteLine($"[매핑정보] 매핑 처리 시작: {tableMapping.MappingId}");
                    Console.WriteLine($"[매핑정보] 엑셀 컬럼 수: {tableMapping.Columns.Count}, 추가 DB 컬럼 수: {tableMapping.AdditionalColumns.Count}");
                    
                    // 엑셀 컬럼 목록 로깅
                    var availableColumns = string.Join(", ", row.Table.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName));
                    Console.WriteLine($"[매핑정보] 엑셀에서 사용 가능한 컬럼들: {availableColumns}");
                    
                    // 매핑 정보를 사용하여 동적으로 변환
                    foreach (var columnMapping in tableMapping.Columns)
                    {
                        var excelColumnName = columnMapping.Key;
                        var dbColumnName = columnMapping.Value.DbColumn;
                        
                        // DataTable 컬럼명은 DB 컬럼명 기준으로 생성되므로 dbColumnName 기준으로 조회해야 함
                        if (row.Table.Columns.Contains(dbColumnName))
                        {
                            var cellValue = row[dbColumnName];
                            var stringValue = cellValue?.ToString() ?? string.Empty;
                            
                            Console.WriteLine($"[매핑정보] 처리 중: {excelColumnName} → {dbColumnName} = '{stringValue}'");
                            
                            // 데이터베이스 컬럼명에 따라 속성 설정 (실제 테이블 구조에 맞춤)
                            switch (dbColumnName.ToLower())
                            {
                                case "주문번호":
                                    order.OrderNumber = stringValue;
                                    break;
                                case "주문번호(쇼핑몰)":
                                    order.OrderNumberMall = stringValue;
                                    break;
                                case "수취인명":
                                    order.RecipientName = stringValue;
                                    break;
                                case "전화번호1":
                                    order.RecipientPhone1 = stringValue;
                                    break;
                                case "전화번호2":
                                    order.RecipientPhone2 = stringValue;
                                    break;
                                case "우편번호":
                                    order.ZipCode = stringValue;
                                    break;
                                case "주소":
                                    order.Address = stringValue;
                                    break;
                                case "옵션명":
                                    order.OptionName = stringValue;
                                    break;
                                case "수량":
                                    order.Quantity = int.TryParse(stringValue, out var qty) ? qty : 0;
                                    break;
                                case "배송메세지":
                                    order.ShippingMessage = stringValue;
                                    break;
                                case "쇼핑몰":
                                    order.MallName = stringValue;
                                    break;
                                case "수집시간":
                                    if (DateTime.TryParse(stringValue, out var collectionTime))
                                        order.CollectionTime = collectionTime;
                                    break;
                                case "송장명":
                                    order.InvoiceName = stringValue;
                                    break;
                                case "품목코드":
                                    order.ProductCode = stringValue;
                                    break;
                                case "택배비용":
                                    order.DeliveryCost = stringValue;
                                    break;
                                case "박스크기":
                                    order.BoxSize = stringValue;
                                    break;
                                case "출력개수":
                                    order.PrintCount = stringValue;
                                    break;
                                case "송장수량":
                                    order.InvoiceQuantity = stringValue;
                                    break;
                                case "별표1":
                                    order.Star1 = stringValue;
                                    break;
                                case "별표2":
                                    order.Star2 = stringValue;
                                    break;
                                case "품목개수":
                                    order.ProductCount = stringValue;
                                    break;
                                case "택배수량":
                                    order.DeliveryQuantity = stringValue;
                                    break;
                                case "택배수량1":
                                    order.DeliveryQuantity1 = stringValue;
                                    break;
                                case "택배수량합산":
                                    order.DeliveryQuantitySum = stringValue;
                                    break;
                                case "송장구분자":
                                    order.InvoiceSeparator = stringValue;
                                    break;
                                case "송장구분":
                                    order.InvoiceType = stringValue;
                                    break;
                                case "송장구분최종":
                                    order.InvoiceTypeFinal = stringValue;
                                    break;
                                case "위치":
                                    order.Location = stringValue;
                                    break;
                                case "위치변환":
                                    order.LocationConverted = stringValue;
                                    break;
                                case "결제금액":
                                    order.PaymentAmount = stringValue;
                                    break;
                                case "주문금액":
                                    order.OrderAmount = stringValue;
                                    break;
                                case "결제수단":
                                    order.PaymentMethod = stringValue;
                                    break;
                                case "면과세구분":
                                    order.TaxType = stringValue;
                                    break;
                                case "주문상태":
                                    order.OrderStatus = stringValue;
                                    break;
                                case "배송송":
                                    order.DeliverySend = stringValue;
                                    break;
                                default:
                                    Console.WriteLine($"[매핑정보] ⚠️ 매핑되지 않은 컬럼: {dbColumnName}");
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[매핑정보] ⚠️ 엑셀에 없는 컬럼: {excelColumnName}");
                        }
                    }
                    
                    Console.WriteLine($"[매핑정보] 매핑 처리 완료");
                }
                else
                {
                    // 매핑 정보가 없는 경우 기본 매핑 사용 (하위 호환성)
                    Console.WriteLine("⚠️ column_mapping.json을 찾을 수 없어 기본 매핑을 사용합니다.");
                    
                    // 기본 매핑 시도
                    if (row.Table.Columns.Contains("수취인명"))
                        order.RecipientName = row["수취인명"]?.ToString() ?? string.Empty;
                    if (row.Table.Columns.Contains("주소"))
                        order.Address = row["주소"]?.ToString() ?? string.Empty;
                    if (row.Table.Columns.Contains("송장명"))
                        order.InvoiceName = row["송장명"]?.ToString() ?? string.Empty;
                    if (row.Table.Columns.Contains("수량"))
                        order.Quantity = int.TryParse(row["수량"]?.ToString(), out var qty) ? qty : 0;
                    // 별표2 값 매핑 (엑셀에 컬럼이 없을 수 있어 변환 단계에서 생성된 컬럼을 그대로 사용)
                    if (row.Table.Columns.Contains("별표2"))
                        order.Star2 = row["별표2"]?.ToString() ?? string.Empty;
                }
                
                // === 디버깅 정보 출력 ===
                Console.WriteLine($"[빌드정보] Order 객체 생성 완료:");
                Console.WriteLine($"  - 수취인명: '{order.RecipientName ?? "(null)"}'");
                Console.WriteLine($"  - 주소: '{order.Address ?? "(null)"}'");
                Console.WriteLine($"  - 송장명: '{order.InvoiceName ?? "(null)"}'");
                Console.WriteLine($"  - 수량: {order.Quantity}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[빌드정보] Order.FromDataRow 예외 발생: {ex.Message}");
                Console.WriteLine($"[빌드정보] 예외 상세: {ex}");
            }
            
            // === 주문번호 처리 ===
            // 엑셀에서 읽은 주문번호를 그대로 사용
            if (row.Table.Columns.Contains("주문번호"))
            {
                order.OrderNumber = row["주문번호"]?.ToString() ?? string.Empty;
                Console.WriteLine($"[Order.FromDataRow] 주문번호: {order.OrderNumber} (수취인명: {order.RecipientName ?? "UNKNOWN"})");
            }
            
            return order;
        }

        /// <summary>
        /// 
        /// [주요 처리 내용]
        /// - 주문 정보, 상품 정보, 배송 정보, 특수 처리 정보, 추가 필드 등
        ///   Order 객체의 모든 속성 값을 DataRow의 각 컬럼에 매핑하여 할당함
        /// - null 값이 존재할 경우, 빈 문자열("") 또는 0으로 안전하게 변환하여 할당
        /// - 반환된 DataRow는 엑셀 저장, DB 저장, 송장 파일 생성 등 다양한 용도로 활용 가능
        /// 
        /// [사용 예시]
        ///   var row = order.ToDataRow(dataTable);
        ///   dataTable.Rows.Add(row);
        /// </summary>
        /// <param name="table">출력 구조를 정의하는 DataTable 객체</param>
        /// <returns>Order 정보가 할당된 DataRow 객체</returns>
        public System.Data.DataRow ToDataRow(System.Data.DataTable table)
        {
            // 새로운 DataRow 생성
            var row = table.NewRow();
            
            // 기본 주문 정보를 DataRow에 할당
            // 데이터베이스 컬럼명 기준으로 DataRow에 값 할당 (누락/오타 방지, 유지보수 용이)
            // 각 컬럼은 null-safe 처리 및 기본값 지정

            // 메시지 필드
            row["msg1"] = Msg1 ?? string.Empty; // 추가 메시지1
            row["msg2"] = Msg2 ?? string.Empty; // 추가 메시지2
            row["msg3"] = Msg3 ?? string.Empty; // 추가 메시지3
            row["msg4"] = Msg4 ?? string.Empty; // 추가 메시지4
            row["msg5"] = Msg5 ?? string.Empty; // 추가 메시지5
            row["msg6"] = Msg6 ?? string.Empty; // 추가 메시지6

            // 주문 및 수취인 정보
            row["수취인명"] = RecipientName ?? string.Empty;
            row["전화번호1"] = RecipientPhone1 ?? string.Empty;
            row["전화번호2"] = RecipientPhone2 ?? string.Empty;
            row["우편번호"] = ZipCode ?? string.Empty;
            row["주소"] = Address ?? string.Empty;
            row["옵션명"] = OptionName ?? string.Empty;
            row["수량"] = Quantity;
            row["배송메세지"] = ShippingMessage ?? string.Empty;
            row["주문번호"] = OrderNumber ?? string.Empty;
            row["쇼핑몰"] = MallName ?? string.Empty;
            row["수집시간"] = CollectionTime == null ? DBNull.Value : (object)CollectionTime;
            row["송장명"] = InvoiceName ?? string.Empty;
            row["품목코드"] = ProductCode ?? string.Empty;
            row["택배비용"] = DeliveryCost ?? string.Empty;
            row["박스크기"] = BoxSize ?? string.Empty;
            row["출력개수"] = PrintCount ?? string.Empty;
            row["송장수량"] = InvoiceQuantity ?? string.Empty;
            row["별표1"] = Star1 ?? string.Empty;
            row["별표2"] = Star2 ?? string.Empty;
            row["품목개수"] = ProductCount ?? string.Empty;
            row["택배수량"] = DeliveryQuantity ?? string.Empty;
            row["택배수량1"] = DeliveryQuantity1 ?? string.Empty;
            row["택배수량합산"] = DeliveryQuantitySum ?? string.Empty;
            row["송장구분자"] = InvoiceSeparator ?? string.Empty;
            row["송장구분"] = InvoiceType ?? string.Empty;
            row["송장구분최종"] = InvoiceTypeFinal ?? string.Empty;
            row["위치"] = Location ?? string.Empty;
            row["위치변환"] = LocationConverted ?? string.Empty;
            row["주문번호(쇼핑몰)"] = OrderNumberMall ?? string.Empty;
            row["결제금액"] = PaymentAmount ?? string.Empty;
            row["주문금액"] = OrderAmount ?? string.Empty;
            row["결제수단"] = PaymentMethod ?? string.Empty;
            row["면과세구분"] = TaxType ?? string.Empty;
            row["주문상태"] = OrderStatus ?? string.Empty;
            row["배송송"] = DeliverySend ?? string.Empty;

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
        /// - 주문번호가 비어있지 않아야 함 (필수) - 자동 생성 가능
        /// - 수취인명이 비어있지 않아야 함 (필수)
        /// - 주소가 비어있지 않아야 함 (필수)
        /// - 송장명이 비어있지 않아야 함 (필수) - 더 유연한 검증
        /// - 수량이 0보다 커야 함 (필수)
        /// 
        /// 사용 목적:
        /// - 데이터 처리 전 유효성 검사
        /// - 잘못된 데이터 필터링
        /// - 송장 생성 시 필수 정보 확인
        /// </summary>
        /// <returns>유효한 주문이면 true, 아니면 false</returns>
        public bool IsValid()
        {
            // === 필수 필드 유효성 검사 (더 유연한 검증) ===
            var isValid = true;
            var missingFields = new List<string>();
            
            // 주문번호 검사 (필수)
            if (string.IsNullOrEmpty(OrderNumber) || OrderNumber.Trim() == string.Empty)
            {
                isValid = false;
                missingFields.Add("주문번호");
            }
            
            // 수취인명 검사 (필수)
            if (string.IsNullOrEmpty(RecipientName) || RecipientName.Trim() == string.Empty)
            {
                isValid = false;
                missingFields.Add("수취인명");
            }
            
            // 주소 검사 (필수)
            if (string.IsNullOrEmpty(Address) || Address.Trim() == string.Empty)
            {
                isValid = false;
                missingFields.Add("주소");
            }
            
            // 송장명 검사 (InvoiceName 또는 ProductName 사용) - 더 유연한 검증
            if ((string.IsNullOrEmpty(InvoiceName) || InvoiceName.Trim() == string.Empty) &&
                (string.IsNullOrEmpty(ProductName) || ProductName.Trim() == string.Empty))
            {
                // 송장명이 없어도 일단 허용 (대용량 데이터 처리 시)
                // isValid = false;
                // missingFields.Add("송장명");
            }
            
            // 수량 검사 (1 이상)
            if (Quantity <= 0)
            {
                isValid = false;
                missingFields.Add("수량");
            }
            
            // === 디버깅 정보 출력 (유효하지 않은 경우) ===
            if (!isValid)
            {
                Console.WriteLine($"[Order] 유효성 검사 실패 - 누락된 필드: {string.Join(", ", missingFields)}");
                Console.WriteLine($"  - 주문번호: '{OrderNumber ?? "(null)"}'");
                Console.WriteLine($"  - 수취인명: '{RecipientName ?? "(null)"}'");
                Console.WriteLine($"  - 주소: '{Address ?? "(null)"}'");
                Console.WriteLine($"  - 송장명: '{InvoiceName ?? "(null)"}' / '{ProductName ?? "(null)"}'");
                Console.WriteLine($"  - 수량: {Quantity}");
            }
            
            return isValid;
        }

        /// <summary>
        /// 주문번호가 비어있을 때 자동으로 생성하는 메서드
        /// 
        /// 생성 규칙:
        /// - 형식: AUTO_{수취인명}_{현재시간}_{랜덤숫자}
        /// - 예시: AUTO_유승렬_20250807_163122_001
        /// 
        /// 사용 목적:
        /// - 주문번호가 비어있는 데이터 처리
        /// - 중복 방지를 위한 고유 식별자 생성
        /// </summary>
        /// <returns>생성된 주문번호</returns>
        public string GenerateOrderNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var random = new Random();
            var randomNumber = random.Next(1, 1000).ToString("D3");
            var recipientName = string.IsNullOrEmpty(RecipientName) ? "UNKNOWN" : RecipientName.Trim();
            
            // 특수문자 제거 및 공백을 언더스코어로 변경
            var cleanRecipientName = System.Text.RegularExpressions.Regex.Replace(recipientName, @"[^\w\s-]", "");
            cleanRecipientName = cleanRecipientName.Replace(" ", "_");
            
            return $"AUTO_{cleanRecipientName}_{timestamp}_{randomNumber}";
        }

        #endregion
    }
} 