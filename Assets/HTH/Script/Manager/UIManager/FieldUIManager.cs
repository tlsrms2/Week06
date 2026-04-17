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

        // ─── 내부 ────────────────────────────────────────────────
        private GameObject _dealerHiddenSlot;
        
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
            // 1단계 — 숫자 카드만 처리(슬롯 + 카드 UI 생성)
            foreach (var card in field)
            {
                if (card.cardType != CardType.Number) continue;

                if (_fieldCards.Count > 0)
                    CreateOperatorSlot(_fieldCards.Count - 1);

                var view = _cardCreate.CreateFieldCard(_playerFieldContainer, card);
                view.SetInteractable(false);
                _fieldCards.Add(view);
            }

            // 2단계 — 연산자 카드를 숫자 카드 기준 위치로 슬롯에 배치
            // 숫자 카드를 만날 때마다 카운트를 올리고
            // 연산자 카드는 직전까지 센 숫자 카드 수 - 1 이 슬롯 인덱스
            int numCount = 0;
            foreach (var card in field)
            {
                if (card.cardType == CardType.Number)
                {
                    numCount++;
                }
                else if (card.cardType == CardType.Operator)
                {
                    // numCount번째 숫자 카드 뒤 슬롯 = numCount - 1
                    int slotIndex = numCount - 1;
                    if (slotIndex >= 0 && slotIndex < _operatorSlotTexts.Count)
                        PlaceOperatorOnSlot(slotIndex, card.operatorType);
                }
            }
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
            if (card.cardType == CardType.Operator)
            {
                // 연산자 카드는 슬롯 UI를 갱신만 함 — 카드 UI 생성 안 함
                // slotIndex = 숫자 카드 기준 앞 슬롯
                int slotIndex = _fieldCards.Count; // 현재까지 추가된 숫자 카드 수
                if (slotIndex > 0 && slotIndex - 1 < _operatorSlotTexts.Count)
                    PlaceOperatorOnSlot(slotIndex - 1, card.operatorType);
                return;
            }

            // 숫자 카드 — 두 번째부터 슬롯 먼저 생성
            if (_fieldCards.Count > 0) CreateOperatorSlot(_fieldCards.Count - 1);

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
        public void RefreshDealerField(List<CardDataSO> field, bool hasHiddenCard)
        {
            ClearDealerField();
            for (int i = 0; i < field.Count; i++)
                AddCardToDealerField(field[i]);

            if (hasHiddenCard) AddHiddenCardSlot();
        }
        /// <summary>
        /// 뒷면 카드 슬롯을 생성합니다.
        /// 딜러의 비공개 카드 자리를 표시합니다.
        /// </summary>
        private void AddHiddenCardSlot()
        {
            var go = new GameObject("HiddenCard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_dealerFieldContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(65, 90);

            var img = go.GetComponent<Image>();
            img.color = UIColor.Hex("#2A4A3A"); // 뒷면 색상

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 65;
            le.preferredHeight = 90;
            le.minWidth = 65;
            le.minHeight = 90;

            // 뒷면 텍스트
            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.GetComponent<TextMeshProUGUI>();
            txt.text = "?";
            txt.fontSize = 32;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = UIColor.SlotText;
            txt.fontStyle = FontStyles.Bold;

            _dealerCards.Add(null); // 슬롯 자리만 확보 (ICardView 없음)
            _dealerHiddenSlot = go; // 별도 추적
        }

        /// <summary>딜러 필드의 모든 카드 UI를 제거합니다.</summary>
        public void ClearDealerField()
        {
            foreach (var v in _dealerCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _dealerCards.Clear();

            if(_dealerHiddenSlot != null)
            {
                Destroy(_dealerHiddenSlot);
                _dealerHiddenSlot.SetActive(false);
            }
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