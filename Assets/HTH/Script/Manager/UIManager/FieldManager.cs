using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 플레이어 필드 / 딜러 필드 / 플레이어 손패를 통합 관리합니다.
    /// 연산자 슬롯 클릭 이벤트를 직접 담당합니다.
    /// 카드 공개/비공개 상태를 관리합니다.
    /// </summary>
    public class FieldManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("패널")]
        [Tooltip("플레이어 턴 패널 루트 — 필드/손패/버튼이 포함된 패널")]
        [SerializeField] private GameObject _fieldPanelRoot;

        [Header("플레이어 영역")]
        [Tooltip("플레이어 필드 컨테이너")]
        [SerializeField] private RectTransform _playerFieldContainer;

        [Tooltip("플레이어 손패 컨테이너")]
        [SerializeField] private RectTransform _playerHandContainer;

        [Header("딜러 영역")]
        [Tooltip("딜러 필드 컨테이너")]
        [SerializeField] private RectTransform _dealerFieldContainer;

        // ─── 의존성 ───────────────────────────────────────────────
        private CardCreateManager _cardCreate;

        // ─── 카드 뷰 추적 ────────────────────────────────────────
        private readonly List<ICardView> _playerFieldCards = new();
        private readonly List<ICardView> _dealerFieldCards = new();
        private readonly List<ICardView> _playerHandCards = new();
        private readonly List<GameObject> _operatorSlots = new();
        private readonly List<Image> _operatorSlotImages = new();
        private readonly List<TextMeshProUGUI> _operatorSlotTexts = new();

        private GameObject _dealerHiddenSlot;

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>연산자 슬롯 클릭 시 발행 (슬롯 인덱스)</summary>
        public event Action<int> OnOperatorSlotClicked;

        /// <summary>손패 카드 클릭 시 발행 (손패 인덱스)</summary>
        public event Action<int> OnHandCardClicked;

        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>CardCreateManager를 주입합니다.</summary>
        public void Initialize(CardCreateManager cardCreate)
        {
            _cardCreate = cardCreate;
        }

        // ─── 플레이어 필드 ────────────────────────────────────────

        /// <summary>
        /// 필드 패널을 활성화합니다.
        /// VisionBettingManager의 Confirm 버튼 확정 후 호출됩니다.
        /// </summary>
        public void Show() => _fieldPanelRoot?.SetActive(true);

        /// <summary>필드 패널을 비활성화합니다.</summary>
        public void Hide() => _fieldPanelRoot?.SetActive(false);

        /// <summary>
        /// 플레이어 필드 UI를 갱신합니다.
        /// 1단계 숫자 카드로 슬롯과 카드 UI를 생성합니다.
        /// 2단계 연산자 카드를 숫자 카드 기준 슬롯에 배치합니다.
        /// </summary>
        public void RefreshPlayerField(List<CardDataSO> field)
        {
            ClearPlayerField();

            // 1단계 — 숫자 카드
            foreach (var card in field)
            {
                if (card.cardType != CardType.Number) continue;

                if (_playerFieldCards.Count > 0)
                    CreateOperatorSlot(_playerFieldCards.Count - 1);

                var view = _cardCreate.CreateFieldCard(_playerFieldContainer, card);
                view.SetInteractable(false);
                _playerFieldCards.Add(view);
            }

            // 2단계 — 연산자 카드
            int numCount = 0;
            foreach (var card in field)
            {
                if (card.cardType == CardType.Number)
                {
                    numCount++;
                }
                else if (card.cardType == CardType.Operator)
                {
                    int slotIndex = numCount - 1;
                    if (slotIndex >= 0 && slotIndex < _operatorSlotTexts.Count)
                        PlaceOperatorOnSlot(slotIndex, card.operatorType);
                }
            }
        }

        /// <summary>플레이어 필드와 슬롯을 모두 제거합니다.</summary>
        public void ClearPlayerField()
        {
            foreach (var v in _playerFieldCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _playerFieldCards.Clear();

            foreach (var go in _operatorSlots)
                if (go != null) Destroy(go);
            _operatorSlots.Clear();
            _operatorSlotImages.Clear();
            _operatorSlotTexts.Clear();
        }

        /// <summary>연산자 슬롯을 생성합니다.</summary>
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

            int captured = slotIndex;
            slotGo.GetComponent<Button>().onClick.AddListener(
                () => OnOperatorSlotClicked?.Invoke(captured));

            _operatorSlots.Add(slotGo);
            _operatorSlotImages.Add(slotImg);
            _operatorSlotTexts.Add(slotTxt);
        }

        /// <summary>연산자 슬롯에 연산자를 배치합니다.</summary>
        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
        {
            if (slotIndex < 0 || slotIndex >= _operatorSlotTexts.Count) return;

            _operatorSlotTexts[slotIndex].text = op switch
            {
                OperatorType.Subtract => "−",
                OperatorType.Multiply => "×",
                OperatorType.Divide => "÷",
                _ => "+"
            };
            _operatorSlotTexts[slotIndex].color = UIColor.CardText;
            _operatorSlotImages[slotIndex].color = UIColor.CardOpBg;
        }

        /// <summary>배치 가능한 슬롯을 하이라이트합니다.</summary>
        public void HighlightAvailableSlots(bool highlight)
        {
            foreach (var img in _operatorSlotImages)
                img.color = highlight ? UIColor.Selected : UIColor.SlotDefault;
        }

        // ─── 딜러 필드 ───────────────────────────────────────────

        /// <summary>
        /// 딜러 필드 UI를 갱신합니다.
        /// hasHiddenCard = true면 뒷면 카드 슬롯을 추가합니다.
        /// </summary>
        public void RefreshDealerField(List<CardDataSO> field, bool hasHiddenCard = false)
        {
            ClearDealerField();

            foreach (var card in field)
                AddDealerCard(card, isOpen: true);

            if (hasHiddenCard)
                AddDealerHiddenSlot();
        }

        /// <summary>딜러 필드 카드를 추가합니다.</summary>
        private void AddDealerCard(CardDataSO card, bool isOpen)
        {
            var view = _cardCreate.CreateFieldCard(_dealerFieldContainer, card);
            view.SetInteractable(false);
            _dealerFieldCards.Add(view);
        }

        /// <summary>딜러 비공개 카드 슬롯을 생성합니다.</summary>
        private void AddDealerHiddenSlot()
        {
            var go = new GameObject("HiddenCard",
                typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_dealerFieldContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(65, 90);

            var img = go.GetComponent<Image>();
            img.color = UIColor.Hex("#2A4A3A");

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 65;
            le.preferredHeight = 90;
            le.minWidth = 65;
            le.minHeight = 90;

            var txtGo = new GameObject("Label",
                typeof(RectTransform), typeof(TextMeshProUGUI));
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

            _dealerHiddenSlot = go;
        }

        /// <summary>딜러 필드를 초기화합니다.</summary>
        public void ClearDealerField()
        {
            foreach (var v in _dealerFieldCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _dealerFieldCards.Clear();

            if (_dealerHiddenSlot != null)
            {
                Destroy(_dealerHiddenSlot);
                _dealerHiddenSlot = null;
            }
        }

        // ─── 플레이어 손패 ────────────────────────────────────────

        /// <summary>플레이어 손패 UI를 갱신합니다.</summary>
        public void RefreshPlayerHand(List<CardDataSO> hand)
        {
            ClearPlayerHand();
            for (int i = 0; i < hand.Count; i++)
                AddHandCard(hand[i], i);
        }

        /// <summary>손패 카드를 추가합니다.</summary>
        private void AddHandCard(CardDataSO card, int index)
        {
            int captured = index;
            var view = _cardCreate.CreateHandCard(_playerHandContainer, card);
            view.SetInteractable(true);
            view.OnClicked += _ => OnHandCardClicked?.Invoke(captured);
            _playerHandCards.Add(view);
        }

        /// <summary>손패를 초기화합니다.</summary>
        public void ClearPlayerHand()
        {
            foreach (var v in _playerHandCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _playerHandCards.Clear();
        }

        /// <summary>
        /// 손패에서 카드를 제거하고 콜백을 재등록합니다.
        /// </summary>
        public void RemoveFromHand(int index)
        {
            if (index < 0 || index >= _playerHandCards.Count) return;

            var view = _playerHandCards[index];
            if (view is MonoBehaviour mb && mb != null)
                Destroy(mb.gameObject);

            _playerHandCards.RemoveAt(index);
            RebuildHandCallbacks();
        }

        /// <summary>손패 콜백을 현재 인덱스 기준으로 재등록합니다.</summary>
        private void RebuildHandCallbacks()
        {
            for (int i = 0; i < _playerHandCards.Count; i++)
            {
                int captured = i;
                _playerHandCards[i].ClearClickListeners();
                _playerHandCards[i].OnClicked +=
                    _ => OnHandCardClicked?.Invoke(captured);
            }
        }

        /// <summary>손패 카드 선택 상태를 갱신합니다.</summary>
        public void HighlightHandCard(int index)
        {
            for (int i = 0; i < _playerHandCards.Count; i++)
                _playerHandCards[i].SetSelected(i == index);
        }
    }
}