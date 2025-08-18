using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO; // Added for file logging
using LogisticManager.Services;

namespace LogisticManager.Models
{
    /// <summary>
    /// 송장 데이터 전송 객체 (Data Transfer Object)
    /// 
    /// 주요 기능:
    /// - 송장 관련 데이터를 데이터베이스와 주고받기 위한 중간 형식
    /// - Order 객체로부터 InvoiceDto 생성
    /// - 데이터 유효성 검사
    /// - 데이터베이스 컬럼과 1:1 매핑
    /// 
    /// 데이터 구조:
    /// - 수취인 정보: 이름, 전화번호, 주소, 우편번호
    /// - 상품 정보: 옵션명, 수량, 상품명, 상품코드
    /// - 주문 정보: 주문번호, 매장명, 수집시간, 쇼핑몰 주문번호
    /// - 결제 정보: 결제금액, 주문금액, 결제수단, 면과세구분
    /// - 배송 정보: 배송비용, 박스크기, 배송수량, 배송타입
    /// - 송장 정보: 출력개수, 송장수량, 송장구분, 위치정보
    /// - 메시지 필드: msg1~msg6 (송장 출력용)
    /// 
    /// 사용 목적:
    /// - 송장 데이터의 일관된 구조 제공
    /// - 데이터베이스 CRUD 작업의 표준화
    /// - 송장 생성 및 관리 시스템의 핵심 데이터 모델
    /// </summary>
    public class InvoiceDto
    {
        #region 로깅 유틸리티 (Logging Utilities)
        
        /// <summary>
        /// app.log 파일에 로그를 기록하는 메서드
        /// </summary>
        /// <param name="message">로그 메시지</param>
        private static void WriteLog(string message)
        {
            try
            {
                var logPath = LogPathManager.AppLogPath;
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 긴 메시지는 여러 줄로 나누기
                if (message.Length > 100)
                {
                    var words = message.Split(new[] { ", " }, StringSplitOptions.None);
                    var currentLine = "";
                    
                    foreach (var word in words)
                    {
                        if ((currentLine + word).Length > 100 && !string.IsNullOrEmpty(currentLine))
                        {
                            // 현재 줄이 너무 길면 새 줄로
                            var logMessage = $"{timestamp} {currentLine.Trim()}";
                            File.AppendAllText(logPath, logMessage + Environment.NewLine);
                            currentLine = word;
                        }
                        else
                        {
                            currentLine += (string.IsNullOrEmpty(currentLine) ? "" : ", ") + word;
                        }
                    }
                    
                    // 마지막 줄 처리
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        var logMessage = $"{timestamp} {currentLine.Trim()}";
                        File.AppendAllText(logPath, logMessage + Environment.NewLine);
                    }
                }
                else
                {
                    // 짧은 메시지는 한 줄로
                    var logMessage = $"{timestamp} {message}";
                    File.AppendAllText(logPath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // 로그 쓰기 실패 시 무시하고 계속 진행
                System.Diagnostics.Debug.WriteLine($"로그 쓰기 실패: {ex.Message}");
            }
        }
        
        #endregion

        #region 메시지 필드 (테이블: msg1~msg6)
        /// <summary>메시지 1</summary>
        public string? Msg1 { get; set; }
        
        /// <summary>메시지 2</summary>
        public string? Msg2 { get; set; }
        
        /// <summary>메시지 3</summary>
        public string? Msg3 { get; set; }
        
        /// <summary>메시지 4</summary>
        public string? Msg4 { get; set; }
        
        /// <summary>메시지 5</summary>
        public string? Msg5 { get; set; }
        
        /// <summary>메시지 6</summary>
        public string? Msg6 { get; set; }
        #endregion

        #region 수취인 정보
        /// <summary>수취인명</summary>
        public string? RecipientName { get; set; }
        
        /// <summary>전화번호1</summary>
        public string? Phone1 { get; set; }
        
        /// <summary>전화번호2</summary>
        public string? Phone2 { get; set; }
        
        /// <summary>우편번호</summary>
        public string? ZipCode { get; set; }
        
        /// <summary>주소</summary>
        public string? Address { get; set; }
        #endregion

        #region 상품 정보
        /// <summary>옵션명</summary>
        public string? OptionName { get; set; }
        
        /// <summary>수량</summary>
        public int? Quantity { get; set; }
        
        /// <summary>송장명</summary>
        public string? ProductName { get; set; }
        
        /// <summary>품목코드</summary>
        public string? ProductCode { get; set; }
        
        /// <summary>품목개수</summary>
        public string? ProductCount { get; set; }
        #endregion

        #region 주문 정보
        /// <summary>배송메세지</summary>
        public string? SpecialNote { get; set; }
        
        /// <summary>주문번호</summary>
        public string? OrderNumber { get; set; }
        
        /// <summary>쇼핑몰</summary>
        public string? StoreName { get; set; }
        
        /// <summary>수집시간</summary>
        public DateTime? CollectedAt { get; set; }
        
        /// <summary>주문번호(쇼핑몰)</summary>
        public string? OrderNumberMall { get; set; }
        
        /// <summary>주문금액</summary>
        public string? OrderAmount { get; set; }
        
        /// <summary>결제금액</summary>
        public string? PaymentAmount { get; set; }
        
        /// <summary>결제수단</summary>
        public string? PaymentMethod { get; set; }
        
        /// <summary>면과세구분</summary>
        public string? TaxType { get; set; }
        
        /// <summary>주문상태</summary>
        public string? OrderStatus { get; set; }
        #endregion

        #region 배송 정보
        /// <summary>택배비용</summary>
        public string? DeliveryCost { get; set; }
        
        /// <summary>박스크기</summary>
        public string? BoxSize { get; set; }
        
        /// <summary>택배수량</summary>
        public string? DeliveryQuantity { get; set; }
        
        /// <summary>택배수량1</summary>
        public string? DeliveryQuantity1 { get; set; }
        
        /// <summary>택배수량합산</summary>
        public string? DeliveryQuantitySum { get; set; }
        
        /// <summary>배송송</summary>
        public string? ShippingType { get; set; }
        #endregion

        #region 송장 정보
        /// <summary>출력개수</summary>
        public string? PrintCount { get; set; }
        
        /// <summary>송장수량</summary>
        public string? InvoiceQuantity { get; set; }
        
        /// <summary>송장구분자</summary>
        public string? InvoiceSeparator { get; set; }
        
        /// <summary>송장구분</summary>
        public string? InvoiceType { get; set; }
        
        /// <summary>송장구분최종</summary>
        public string? InvoiceTypeFinal { get; set; }
        #endregion

        #region 위치 정보
        /// <summary>위치</summary>
        public string? Location { get; set; }
        
        /// <summary>위치변환</summary>
        public string? LocationConverted { get; set; }
        #endregion

        #region 기타 정보
        /// <summary>별표1</summary>
        public string? Star1 { get; set; }
        
        /// <summary>별표2</summary>
        public string? Star2 { get; set; }
        #endregion

        #region 정적 팩토리 메서드
        /// <summary>
        /// Order 객체로부터 InvoiceDto를 생성하는 정적 팩토리 메서드
        /// </summary>
        /// <param name="order">주문 객체</param>
        /// <returns>생성된 InvoiceDto</returns>
        public static InvoiceDto FromOrder(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order), "주문 객체가 null일 수 없습니다.");
            }

            return new InvoiceDto
            {
                // 수취인 정보
                RecipientName = order.RecipientName ?? string.Empty,
                Phone1 = order.RecipientPhone1 ?? string.Empty,
                Phone2 = order.RecipientPhone2 ?? string.Empty,
                ZipCode = order.ZipCode ?? string.Empty,
                Address = order.Address ?? string.Empty,
                
                // 상품 정보
                OptionName = order.OptionName ?? string.Empty,
                Quantity = order.Quantity,
                ProductName = order.InvoiceName ?? string.Empty,
                ProductCode = order.ProductCode ?? string.Empty,
                
                // 주문 정보
                SpecialNote = order.ShippingMessage ?? string.Empty,
                OrderNumber = order.OrderNumber ?? string.Empty,
                StoreName = order.MallName ?? string.Empty,
                CollectedAt = order.CollectionTime ?? DateTime.Now, // 수집시간이 있으면 사용, 없으면 현재 시간
                OrderNumberMall = order.OrderNumberMall ?? string.Empty,
                OrderAmount = order.OrderAmount ?? string.Empty,
                PaymentAmount = order.PaymentAmount ?? string.Empty,
                PaymentMethod = order.PaymentMethod ?? string.Empty,
                TaxType = order.TaxType ?? string.Empty,
                OrderStatus = order.OrderStatus ?? order.ProcessingStatus ?? string.Empty,
                
                // 배송 정보
                DeliveryCost = order.DeliveryCost ?? string.Empty,
                BoxSize = order.BoxSize ?? string.Empty,
                DeliveryQuantity = order.DeliveryQuantity ?? string.Empty,
                DeliveryQuantity1 = order.DeliveryQuantity1 ?? string.Empty,
                DeliveryQuantitySum = order.DeliveryQuantitySum ?? string.Empty,
                ShippingType = order.DeliverySend ?? order.ShippingType ?? string.Empty,
                
                // 송장 정보
                PrintCount = order.PrintCount ?? "1",
                InvoiceQuantity = order.InvoiceQuantity ?? "1",
                InvoiceSeparator = order.InvoiceSeparator ?? string.Empty,
                InvoiceType = order.InvoiceType ?? string.Empty,
                InvoiceTypeFinal = order.InvoiceTypeFinal ?? string.Empty,
                
                // 위치 정보
                Location = order.Location ?? "기본위치",
                LocationConverted = order.LocationConverted ?? string.Empty,
                
                // 기타 정보
                ProductCount = order.ProductCount ?? string.Empty,
                Star1 = order.Star1 ?? string.Empty,
                Star2 = order.Star2 ?? string.Empty,
                
                // 메시지 필드
                Msg1 = string.Empty,
                Msg2 = string.Empty,
                Msg3 = string.Empty,
                Msg4 = string.Empty,
                Msg5 = string.Empty,
                Msg6 = string.Empty
            };
        }
        #endregion

        #region 검증 메서드
        /// <summary>
        /// DTO 데이터 유효성 검사
        /// </summary>
        /// <returns>유효성 검사 결과</returns>
        public bool IsValid()
        {
            // === 모든 필드가 비어있는 경우만 완전히 무효로 처리 ===
            var hasAnyValidData = !string.IsNullOrWhiteSpace(RecipientName) ||
                                  !string.IsNullOrWhiteSpace(Address) ||
                                  !string.IsNullOrWhiteSpace(OrderNumber) ||
                                  !string.IsNullOrWhiteSpace(ProductName) ||
                                  Quantity > 0 ||
                                  !string.IsNullOrWhiteSpace(ProductCode) ||
                                  !string.IsNullOrWhiteSpace(Phone1) ||
                                  !string.IsNullOrWhiteSpace(Phone2) ||
                                  !string.IsNullOrWhiteSpace(ZipCode) ||
                                  !string.IsNullOrWhiteSpace(OptionName) ||
                                  !string.IsNullOrWhiteSpace(SpecialNote) ||
                                  !string.IsNullOrWhiteSpace(StoreName) ||
                                  !string.IsNullOrWhiteSpace(OrderNumberMall) ||
                                  !string.IsNullOrWhiteSpace(OrderAmount) ||
                                  !string.IsNullOrWhiteSpace(PaymentAmount) ||
                                  !string.IsNullOrWhiteSpace(PaymentMethod) ||
                                  !string.IsNullOrWhiteSpace(TaxType) ||
                                  !string.IsNullOrWhiteSpace(OrderStatus) ||
                                  !string.IsNullOrWhiteSpace(DeliveryCost) ||
                                  !string.IsNullOrWhiteSpace(BoxSize) ||
                                  !string.IsNullOrWhiteSpace(DeliveryQuantity) ||
                                  !string.IsNullOrWhiteSpace(DeliveryQuantity1) ||
                                  !string.IsNullOrWhiteSpace(DeliveryQuantitySum) ||
                                  !string.IsNullOrWhiteSpace(ShippingType) ||
                                  !string.IsNullOrWhiteSpace(PrintCount) ||
                                  !string.IsNullOrWhiteSpace(InvoiceQuantity) ||
                                  !string.IsNullOrWhiteSpace(InvoiceSeparator) ||
                                  !string.IsNullOrWhiteSpace(InvoiceType) ||
                                  !string.IsNullOrWhiteSpace(InvoiceTypeFinal) ||
                                  !string.IsNullOrWhiteSpace(Location) ||
                                  !string.IsNullOrWhiteSpace(LocationConverted) ||
                                  !string.IsNullOrWhiteSpace(ProductCount) ||
                                  !string.IsNullOrWhiteSpace(Star1) ||
                                  !string.IsNullOrWhiteSpace(Star2) ||
                                  !string.IsNullOrWhiteSpace(Msg1) ||
                                  !string.IsNullOrWhiteSpace(Msg2) ||
                                  !string.IsNullOrWhiteSpace(Msg3) ||
                                  !string.IsNullOrWhiteSpace(Msg4) ||
                                  !string.IsNullOrWhiteSpace(Msg5) ||
                                  !string.IsNullOrWhiteSpace(Msg6);
            
            if (!hasAnyValidData)
            {
                WriteLog($"[InvoiceDto] 유효성 검사 실패 - 모든 필드가 비어있음");
                return false;
            }
            
            // === 디버깅 정보 출력 ===
            WriteLog($"[InvoiceDto] 유효성 검사 통과 - 데이터 존재");
            WriteLog($"  - 수취인명: '{RecipientName ?? "(null)"}'");
            WriteLog($"  - 주소: '{Address ?? "(null)"}'");
            WriteLog($"  - 주문번호: '{OrderNumber ?? "(null)"}'");
            WriteLog($"  - 상품명: '{ProductName ?? "(null)"}'");
            WriteLog($"  - 수량: {Quantity}");
            
            return true;
        }
        #endregion
    }
}