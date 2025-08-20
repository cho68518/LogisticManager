using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogisticManager.Models
{
    /// <summary>
    /// 카카오워크 메시지 타입을 정의하는 열거형
    /// 각 타입은 다른 메시지 구조와 채팅방을 사용합니다.
    /// </summary>
    public enum KakaoWorkMessageType
    {
        /// <summary>
        /// 판매입력 타입 - 판매입력_이카운트자료 전달용
        /// </summary>
        SalesInput,
        
        /// <summary>
        /// 운송장 수집 타입 - 운송장 수집 완료 알림용
        /// </summary>
        Shipment,
        
        /// <summary>
        /// 소분 프린트 자료 타입 - 소분 프린트 자료 전달용
        /// </summary>
        PrintMaterial,
        
        /// <summary>
        /// 통합 송장 타입 - 통합 송장 수집 완료 알림용
        /// </summary>
        IntegratedInvoice
    }

    /// <summary>
    /// 카카오워크 메시지 블록의 기본 구조
    /// </summary>
    public abstract class KakaoWorkBlock
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }

    /// <summary>
    /// 헤더 블록
    /// </summary>
    public class HeaderBlock : KakaoWorkBlock
    {
        [JsonPropertyName("type")]
        public override string Type => "header";
        
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("style")]
        public string Style { get; set; } = "blue";
    }

    /// <summary>
    /// 텍스트 블록
    /// </summary>
    public class TextBlock : KakaoWorkBlock
    {
        [JsonPropertyName("type")]
        public override string Type => "text";
        
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("markdown")]
        public bool Markdown { get; set; } = true;
    }

    /// <summary>
    /// 버튼 블록
    /// </summary>
    public class ButtonBlock : KakaoWorkBlock
    {
        [JsonPropertyName("type")]
        public override string Type => "button";
        
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("style")]
        public string Style { get; set; } = "default";
        
        [JsonPropertyName("action_type")]
        public string ActionType { get; set; } = "open_system_browser";
        
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 구분선 블록
    /// </summary>
    public class DividerBlock : KakaoWorkBlock
    {
        [JsonPropertyName("type")]
        public override string Type => "divider";
    }

    /// <summary>
    /// 카카오워크 메시지 페이로드
    /// </summary>
    public class KakaoWorkMessagePayload
    {
        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;
        
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("blocks")]
        public List<object> Blocks { get; set; } = new List<object>();
    }

    /// <summary>
    /// 카카오워크 메시지 빌더
    /// 팩토리 패턴을 사용하여 메시지 타입별로 적절한 메시지를 생성합니다.
    /// </summary>
    public static class KakaoWorkMessageBuilder
    {
        /// <summary>
        /// 메시지 타입에 따라 적절한 카카오워크 메시지를 생성합니다.
        /// </summary>
        /// <param name="type">메시지 타입</param>
        /// <param name="batch">배치 정보 (예: "2차")</param>
        /// <param name="fileUrl">파일 다운로드 URL</param>
        /// <param name="centerName">출고지 이름 (운송장 타입에서 사용)</param>
        /// <param name="uniqueCount">송장 개수 (운송장 타입에서 사용)</param>
        /// <returns>카카오워크 메시지 페이로드</returns>
        public static KakaoWorkMessagePayload Build(
            KakaoWorkMessageType type, 
            string batch, 
            string fileUrl, 
            string? centerName = null, 
            int? uniqueCount = null)
        {
            return type switch
            {
                KakaoWorkMessageType.SalesInput => BuildSalesInputMessage(batch, fileUrl),
                KakaoWorkMessageType.Shipment => BuildShipmentMessage(batch, fileUrl, centerName, uniqueCount),
                KakaoWorkMessageType.PrintMaterial => BuildPrintMaterialMessage(batch, fileUrl, centerName),
                KakaoWorkMessageType.IntegratedInvoice => BuildShipmentMessage(batch, fileUrl, centerName, uniqueCount),
                _ => throw new ArgumentException($"지원하지 않는 메시지 타입입니다: {type}")
            };
        }

        /// <summary>
        /// 판매입력 타입 메시지를 생성합니다.
        /// </summary>
        /// <param name="batch">배치 정보</param>
        /// <param name="fileUrl">파일 다운로드 URL</param>
        /// <returns>판매입력 메시지</returns>
        private static KakaoWorkMessagePayload BuildSalesInputMessage(string batch, string fileUrl)
        {
            return new KakaoWorkMessagePayload
            {
                Text = $"{batch} - 판매입력_이카운트자료",
                Blocks = new List<object>
                {
                    new HeaderBlock
                    {
                        Text = $"{batch} - 판매입력_이카운트자료",
                        Style = "blue"
                    },
                    new TextBlock
                    {
                        Text = "파일 다운로드 후 DB로 한번 더 돌려주세요",
                        Markdown = true
                    },
                    new ButtonBlock
                    {
                        Text = "판매입력 파일 다운로드",
                        Style = "default",
                        ActionType = "open_system_browser",
                        Value = fileUrl
                    }
                }
            };
        }

        /// <summary>
        /// 운송장 수집 타입 메시지를 생성합니다.
        /// </summary>
        /// <param name="batch">배치 정보</param>
        /// <param name="fileUrl">파일 다운로드 URL</param>
        /// <param name="centerName">출고지 이름</param>
        /// <param name="uniqueCount">송장 개수</param>
        /// <returns>운송장 수집 메시지</returns>
        private static KakaoWorkMessagePayload BuildShipmentMessage(
            string batch, 
            string fileUrl, 
            string? centerName, 
            int? uniqueCount)
        {
            var centerDisplayName = !string.IsNullOrEmpty(centerName) ? centerName : "운송장";
            var normalizedCenter = centerDisplayName.Replace(" ", "");
            var isIntegrated = string.Equals(normalizedCenter, "통합송장", StringComparison.Ordinal);
            var titleText = isIntegrated 
                ? $"{batch} - 통합송장 수집 완료"
                : $"{batch} - {centerDisplayName} 운송장 수집 완료";
            
            var blocks = new List<object>
            {
                new HeaderBlock
                {
                    Text = titleText,
                    Style = "blue"
                },
                new TextBlock
                {
                    Text = "아래 링크에서 파일을 다운로드하세요!",
                    Markdown = true
                },
                new ButtonBlock
                {
                    Text = "파일 다운로드",
                    Style = "default",
                    ActionType = "open_system_browser",
                    Value = fileUrl
                },
                new DividerBlock(),
                new TextBlock
                {
                    Text = $"*송장 개수:* {uniqueCount ?? 0}건",
                    Markdown = true
                }
            };

            if (!isIntegrated)
            {
                blocks.Add(new DividerBlock());
                blocks.Add(new TextBlock
                {
                    Text = "*송장넘기기*\n아이디: `gram`\n비번: `3535`\n[👉 송장 관리 페이지 바로가기](https://gramwon.me/orders/transfer)",
                    Markdown = true
                });
                blocks.Add(new DividerBlock());
            }

            return new KakaoWorkMessagePayload
            {
                Text = titleText,
                Blocks = blocks
            };
        }

        /// <summary>
        /// 소분 프린트 자료 타입 메시지를 생성합니다.
        /// </summary>
        /// <param name="batch">배치 정보</param>
        /// <param name="fileUrl">파일 다운로드 URL</param>
        /// <param name="centerName">출고지 이름</param>
        /// <returns>소분 프린트 자료 메시지</returns>
        private static KakaoWorkMessagePayload BuildPrintMaterialMessage(
            string batch, 
            string fileUrl, 
            string? centerName)
        {
            var centerDisplayName = !string.IsNullOrEmpty(centerName) ? centerName : "소분 프린트";
            var isBusanCheonggwaPrint = string.Equals(centerDisplayName, "부산청과", StringComparison.Ordinal);
            var headerText = isBusanCheonggwaPrint
                ? $"{batch} - 소분 프린트 자료"
                : $"{batch} - {centerDisplayName} 소분 프린트 자료";
            
            var blocks = new List<object>
            {
                new HeaderBlock
                {
                    Text = headerText,
                    Style = "blue"
                },
                new TextBlock
                {
                    Text = "아래 링크에서 파일을 다운로드하세요!",
                    Markdown = true
                },
                new ButtonBlock
                {
                    Text = "파일 다운로드",
                    Style = "default",
                    ActionType = "open_system_browser",
                    Value = fileUrl
                }
            };

            return new KakaoWorkMessagePayload
            {
                Text = headerText,
                Blocks = blocks
            };
        }

        /// <summary>
        /// 통합 송장 타입 메시지를 생성합니다.
        /// </summary>
        /// <param name="batch">배치 정보</param>
        /// <param name="fileUrl">파일 다운로드 URL</param>
        /// <param name="uniqueCount">송장 개수</param>
        /// <returns>통합 송장 메시지</returns>
        private static KakaoWorkMessagePayload BuildIntegratedInvoiceMessage(
            string batch, 
            string fileUrl, 
            int? uniqueCount)
        {
            var blocks = new List<object>
            {
                new HeaderBlock
                {
                    Text = $"{batch} - 통합 송장 수집 완료",
                    Style = "blue"
                },
                new TextBlock
                {
                    Text = "모든 출고지의 통합 송장 자료가 준비되었습니다!",
                    Markdown = true
                },
                new ButtonBlock
                {
                    Text = "통합 송장 자료 다운로드",
                    Style = "default",
                    ActionType = "open_system_browser",
                    Value = fileUrl
                },
                new DividerBlock(),
                new TextBlock
                {
                    Text = $"*전체 송장 개수:* {uniqueCount ?? 0}건",
                    Markdown = true
                },
                new DividerBlock(),
                new TextBlock
                {
                    Text = "*포함된 출고지*\n- 서울냉동\n- 경기냉동\n- 서울공산\n- 경기공산\n- 부산청과\n- 감천냉동",
                    Markdown = true
                },
                new DividerBlock(),
                new TextBlock
                {
                    Text = "*송장넘기기*\n아이디: `gram`\n비번: `3535`\n[👉 송장 관리 페이지 바로가기](https://gramwon.me/orders/transfer)",
                    Markdown = true
                },
                new DividerBlock()
            };

            return new KakaoWorkMessagePayload
            {
                Text = $"{batch} - 통합 송장 수집 완료",
                Blocks = blocks
            };
        }
    }
} 