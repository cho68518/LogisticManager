using System.Collections.Generic;
using Newtonsoft.Json;

namespace LogisticManager.Models
{
    /// <summary>
    /// KakaoWork 메시지 페이로드 클래스
    /// </summary>
    public class KakaoWorkPayload
    {
        [JsonProperty("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("blocks")]
        public List<IBlock> Blocks { get; set; } = new List<IBlock>();
    }

    /// <summary>
    /// Block Kit 블록의 기본 인터페이스
    /// </summary>
    public interface IBlock
    {
        [JsonProperty("type")]
        string Type { get; }
    }

    /// <summary>
    /// 헤더 블록 - 제목 표시용
    /// </summary>
    public class HeaderBlock : IBlock
    {
        [JsonProperty("type")]
        public string Type => "header";

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("style")]
        public string Style { get; set; } = "blue";
    }

    /// <summary>
    /// 텍스트 블록 - 일반 텍스트 표시용
    /// </summary>
    public class TextBlock : IBlock
    {
        [JsonProperty("type")]
        public string Type => "text";

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("markdown")]
        public bool Markdown { get; set; } = true;
    }

    /// <summary>
    /// 버튼 블록 - 클릭 가능한 버튼
    /// </summary>
    public class ButtonBlock : IBlock
    {
        [JsonProperty("type")]
        public string Type => "button";

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;

        [JsonProperty("style")]
        public string Style { get; set; } = "default";

        [JsonProperty("action_type")]
        public string ActionType { get; set; } = "open_system_browser";
    }

    /// <summary>
    /// 구분선 블록 - 시각적 구분용
    /// </summary>
    public class DividerBlock : IBlock
    {
        [JsonProperty("type")]
        public string Type => "divider";
    }

    /// <summary>
    /// 설명 블록 - 키-값 쌍 표시용
    /// </summary>
    public class DescriptionBlock : IBlock
    {
        [JsonProperty("type")]
        public string Type => "description";

        [JsonProperty("term")]
        public string Term { get; set; } = string.Empty;

        [JsonProperty("content")]
        public IBlock Content { get; set; } = new TextBlock();
    }
} 