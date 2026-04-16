using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 플레이어 필드와 딜러 필드의 카드 UI를 담당합니다.
    /// 연산자 슬롯 생성/갱신/하이라이트도 이 클래스가 처리합니다.
    /// </summary>
    public class FieldUIManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Tooltip("플레이어 카드 컨테이너")]
        [SerializeField] private RectTransform _playerFieldContainer;

        [Tooltip("딜러 카드 컨테이너")]
        [SerializeField] private RectTransform _dealerFieldContainer;

        // ─── 의존성 ───────────────────────────────────────────────
        private CardCreateManager _cardCreate;

        // ─── 카드 뷰 추적 ────────────────────────────────────────
        private readonly List<ICardView> _fieldCards = new();
        private readonly List<ICardView> _dealerCards = new();
        private readonly List<GameObject> _operatorSlots = new();
        private readonly List<Image> _operatorSlotImages = new();
        private readonly List<TextMeshProUGUI> _operatorSlotTexts = new();

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>연산자 슬롯 클릭 시 발행 (슬롯 인덱스)</summary>
        public event Action<int> OnOperatorSlotClicked;

        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>CardViewFactory를 주입합니다. GameUIManager가 Awake에서 호출합니다.</summary>
        public void Initialize(CardCreateManager cardCreate)
        {
            _cardCreate = cardCreate;
        }

        // ─── 플레이어 필드 ────────────────────────────────────────

        /// <summary>
        /// 플레이어 필드 UI를 갱신합니다.
        /// GameUIManager.RefreshPlayerArea에서 호출합니다.
        /// </summary>
        public void RefreshField(List<CardDataSO> field)
        {
            ClearField();
            for (int i = 0; i < field.Count; i++)
                AddCardToField(field[i], i);
        }

        /// <summary>플레이어 필드와 슬롯 UI를 모두 제거합니다.</summary>
        public void ClearField()
        {
            foreach (var v in _fieldCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _fieldCards.Clear();

            foreach (var go in _operatorSlots)
                if (go != null) Destroy(go);
            _operatorSlots.Clear();
            _operatorSlotImages.Clear();
            _operatorSlotTexts.Clear();
        }

        /// <summary>
        /// 필드에 카드 UI를 추가합니다.
        /// 두 번째 카드부터 앞에 연산자 슬롯을 자동 생성합니다.
        /// </summary>
        private void AddCardToField(CardDataSO card, int index)
        {
            if (index > 0) CreateOperatorSlot(index - 1);

            var view = _cardCreate.CreateFieldCard(_playerFieldContainer, card);
            view.SetInteractable(false);
            _fieldCards.Add(view);
        }

        /// <summary>연산자 슬롯 GameObject를 생성합니다.</summary>
        private void CreateOperatorSlot(int slotIndex)
        {
            var slotGo = new GameObject(
                $"OpSlot_{slotIndex}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            slotGo.transform.SetParent(_playerFieldContainer, false);

            var slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(45, 65);

            var slotImg = slotGo.GetComponent<Image>();
            slotImg.color = UIColor.SlotDefault;

            // 슬롯 텍스트
            var slotTxtGo = new GameObject("Label",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            slotTxtGo.transform.SetParent(slotGo.transform, false);

            var slotTxtRt = slotTxtGo.GetComponent<RectTransform>();
            slotTxtRt.anchorMin = Vector2.zero;
            slotTxtRt.anchorMax = Vector2.one;
            slotTxtRt.offsetMin = Vector2.zero;
            slotTxtRt.offsetMax = Vector2.zero;

            var slotTxt = slotTxtGo.GetComponent<TextMeshProUGUI>();
            slotTxt.text = "+";
            slotTxt.fontSize = 28;
            slotTxt.color = UIColor.SlotText;
            slotTxt.alignment = TextAlignmentOptions.Center;
            slotTxt.fontStyle = FontStyles.Bold;

            var le = slotGo.AddComponent<LayoutElement>();
            le.preferredWidth = 45;
            le.preferredHeight = 65;
            le.minWidth = 45;
            le.minHeight = 65;

            var slotBtn = slotGo.GetComponent<Button>();
            int captured = slotIndex;
            slotBtn.onClick.AddListener(
                () => OnOperatorSlotClicked?.Invoke(captured));

            _operatorSlots.Add(slotGo);
            _operatorSlotImages.Add(slotImg);
            _operatorSlotTexts.Add(slotTxt);
        }

        /// <summary>
        /// 연산자 슬롯에 연산자를 배치하고 색상과 텍스트를 갱신합니다.
        /// </summary>
        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
        {
            if (slotIndex < 0 || slotIndex >= _operatorSlotTexts.Count) return;

            string symbol = op switch
            {
                OperatorType.Subtract => "−",
                OperatorType.Multiply => "×",
                OperatorType.Divide => "÷",
                _ => "+"
            };

            _operatorSlotTexts[slotIndex].text = symbol;
            _operatorSlotTexts[slotIndex].color = UIColor.CardText;
            _operatorSlotImages[slotIndex].color = UIColor.CardOpBg;
        }

        /// <summary>
        /// 배치 가능한 슬롯을 하이라이트합니다.
        /// </summary>
        public void HighlightAvailableSlots(bool highlight)
        {
            foreach (var img in _operatorSlotImages)
                img.color = highlight ? UIColor.Selected : UIColor.SlotDefault;
        }

        // ─── 딜러 필드 ───────────────────────────────────────────

        /// <summary>
        /// 딜러 필드 UI를 갱신합니다.
        /// GameUIManager.RefreshDealerArea에서 호출합니다.
        /// </summary>
        public void RefreshDealerField(List<CardDataSO> field)
        {
            ClearDealerField();
            for (int i = 0; i < field.Count; i++)
                AddCardToDealerField(field[i]);
        }

        /// <summary>딜러 필드의 모든 카드 UI를 제거합니다.</summary>
        public void ClearDealerField()
        {
            foreach (var v in _dealerCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _dealerCards.Clear();
        }

        /// <summary>딜러 필드에 카드 UI를 추가합니다.</summary>
        private void AddCardToDealerField(CardDataSO card)
        {
            var view = _cardCreate.CreateFieldCard(_dealerFieldContainer, card);
            view.SetInteractable(false);
            _dealerCards.Add(view);
        }

        // ─── 블러 적용 ───────────────────────────────────────────

        /// <summary>
        /// 시야 비율에 따라 필드/딜러 카드 블러를 갱신합니다.
        /// HUDManager.HandleVisionChanged에서 호출합니다.
        /// </summary>
        public void ApplyBlur(float visionRatio)
        {
            float blur = visionRatio > 0.2f
                ? 0f
                : Mathf.InverseLerp(0.2f, 0f, visionRatio);

            foreach (var v in _fieldCards) v.SetBlurLevel(blur);
            foreach (var v in _dealerCards) v.SetBlurLevel(blur);
        }
    }
}