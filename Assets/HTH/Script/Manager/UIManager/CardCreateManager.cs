using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// ICardView 구현체를 생성하는 팩토리입니다.
    /// 카드 프리팹이 없으면 코드로 UI 카드를 생성합니다.
    /// 카드 프리팹이 있으면 프리팹을 Instantiate해서 사용합니다.
    /// 3D 전환 시 Card3DPrefab을 연결하면 자동으로 교체됩니다.
    /// </summary>
    public class CardCreateManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("카드 프리팹 (없으면 코드로 생성)")]
        [Tooltip("UI 카드 프리팹 — ICardView를 구현한 컴포넌트가 포함되어야 합니다.")]
        [SerializeField] private GameObject _cardUIPrefab;

        [Tooltip("3D 카드 프리팹 — ICardView를 구현한 컴포넌트가 포함되어야 합니다.")]
        [SerializeField] private GameObject _card3DPrefab;

        [Header("코드 생성 카드 기본 크기 (프리팹 없을 때만 사용)")]
        [Tooltip("필드 카드 너비")]
        [SerializeField] private float _fieldCardWidth = 65f;

        [Tooltip("필드 카드 높이")]
        [SerializeField] private float _fieldCardHeight = 90f;

        [Tooltip("필드 카드 폰트 크기")]
        [SerializeField] private int _fieldCardFontSize = 32;

        [Tooltip("손패 카드 너비")]
        [SerializeField] private float _handCardWidth = 55f;

        [Tooltip("손패 카드 높이")]
        [SerializeField] private float _handCardHeight = 75f;

        [Tooltip("손패 카드 폰트 크기")]
        [SerializeField] private int _handCardFontSize = 30;

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>
        /// 필드용 카드 뷰를 생성합니다.
        /// 프리팹이 있으면 Instantiate, 없으면 코드로 생성합니다.
        /// </summary>
        public ICardView CreateFieldCard(RectTransform parent, CardDataSO data)
            => Create(parent, data, _fieldCardWidth, _fieldCardHeight, _fieldCardFontSize);

        /// <summary>
        /// 손패용 카드 뷰를 생성합니다.
        /// 프리팹이 있으면 Instantiate, 없으면 코드로 생성합니다.
        /// </summary>
        public ICardView CreateHandCard(RectTransform parent, CardDataSO data)
            => Create(parent, data, _handCardWidth, _handCardHeight, _handCardFontSize);

        // ─── 내부 생성 로직 ───────────────────────────────────────

        /// <summary>
        /// 프리팹 유무에 따라 카드 생성 방식을 결정합니다.
        /// 3D 프리팹 → UI 프리팹 → 코드 생성 순으로 우선순위를 가집니다.
        /// </summary>
        private ICardView Create(
            RectTransform parent,
            CardDataSO data,
            float width,
            float height,
            int fontSize)
        {
            // 3D 프리팹 우선
            if (_card3DPrefab != null)
                return CreateFromPrefab(_card3DPrefab, parent, data);

            // UI 프리팹
            if (_cardUIPrefab != null)
                return CreateFromPrefab(_cardUIPrefab, parent, data);

            // 프리팹 없음 → 코드로 생성
            return CreateFromCode(parent, data, width, height, fontSize);
        }

        /// <summary>
        /// 프리팹을 Instantiate해서 ICardView를 반환합니다.
        /// 프리팹에 ICardView 구현체가 없으면 경고 후 null 반환합니다.
        /// </summary>
        private ICardView CreateFromPrefab(
            GameObject prefab,
            RectTransform parent,
            CardDataSO data)
        {
            var go = Instantiate(prefab, parent);
            var view = go.GetComponent<ICardView>();

            if (view == null)
            {
                Debug.LogError(
                    $"[CardCreateManager] 프리팹 '{prefab.name}'에 " +
                    $"ICardView 구현체가 없습니다.");
                Destroy(go);
                return null;
            }

            view.Initialize(data);
            return view;
        }

        /// <summary>
        /// 코드로 UI 카드 GameObject를 생성합니다.
        /// 프리팹이 없을 때의 임시 구현입니다.
        /// </summary>
        private ICardView CreateFromCode(
            RectTransform parent,
            CardDataSO data,
            float width,
            float height,
            int fontSize)
        {
            // ── 루트 GameObject ──
            var go = new GameObject(
                $"Card_{data.displayLabel}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);

            // ── 크기 설정 ──
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;

            // ── 텍스트 자식 먼저 생성 ──
            var txtGo = new GameObject("Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.GetComponent<TextMeshProUGUI>();
            txt.fontSize = fontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.overflowMode = TextOverflowModes.Overflow;

            // ── 자식 생성 후 CardUIView 추가 → Awake 시점에 자식 존재 ──
            var view = go.AddComponent<CardUIView>();
            view.Initialize(data);

            return view;
        }
    }
}