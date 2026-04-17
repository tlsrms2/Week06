using UnityEngine;

namespace HTH
{
    /// <summary>
    /// UI 색상 관련 유틸리티 클래스.
    /// GameUIManager / CardUIView 등 여러 곳에서 공통으로 사용합니다.
    /// </summary>
    public static class UIColor
    {
        /// <summary>Hex 문자열을 Color로 변환합니다. 예: "#F5F0E1"</summary>
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        // ─── 공통 카드 색상 ──────────────────────────────────────
        public static readonly Color CardNumBg = Hex("#F5F0E1");
        public static readonly Color CardOpBg = Hex("#D4A843");
        public static readonly Color CardText = Hex("#2D2D2D");
        public static readonly Color Selected = Hex("#4FC3F7");
        public static readonly Color SlotDefault = Hex("#2A4A3A");
        public static readonly Color SlotText = Hex("#5A8A6A");
    }
}