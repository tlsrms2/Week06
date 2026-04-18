using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 플레이어 필드 / 딜러 필드 / 플레이어 손패를 통합 관리합니다.
    ///
    /// 카드 정렬
    /// └── 코드로 직접 계산 (HorizontalLayoutGroup 미사용)
    ///     카드 추가될 때마다 전체 중앙 정렬 재계산
    ///     maxCardsPerRow 초과 시 줄바꿈
    ///
    /// 좌표계 분리
    /// ├── 3D 카드  → _playerFieldAnchor / _dealerFieldAnchor (일반 Transform)
    /// └── 손패 UI  → _playerHandContainer (Canvas 안 RectTransform)
    /// </summary>
    public class FieldManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("패널")]
        [Tooltip("플레이어 턴 패널 루트")]
        [SerializeField] private GameObject _fieldPanelRoot;

        [Header("3D 카드 배치 기준점 (일반 Transform)")]
        [Tooltip("플레이어 카드 배치 기준점 — 이 Transform이 중앙")]
        [SerializeField] private Transform _playerFieldAnchor;

        [Tooltip("딜러 카드 배치 기준점 — 이 Transform이 중앙")]
        [SerializeField] private Transform _dealerFieldAnchor;

        [Header("카드 정렬 설정")]
        [Tooltip("카드 가로 간격 (월드 단위)")]
        [SerializeField] private float _cardSpacingX = 0.15f;

        [Tooltip("카드 세로 간격 — 줄바꿈 시 (월드 단위)")]
        [SerializeField] private float _cardSpacingY = 0.25f;

        [Tooltip("한 줄에 표시할 최대 카드 수")]
        [SerializeField] private int _maxCardsPerRow = 5;

        [Header("손패 / 연산자 슬롯 UI (Canvas 안 RectTransform)")]
        [Tooltip("플레이어 손패 카드 UI 컨테이너")]
        [SerializeField] private RectTransform _playerHandContainer;

        // ─── 의존성 ───────────────────────────────────────────────
        private CardCreateManager _cardCreate;

        // ─── 카드 뷰 추적 ────────────────────────────────────────
        private readonly List<ICardView> _playerFieldCards = new();
        private readonly List<ICardView> _dealerFieldCards = new();
        private readonly List<ICardView> _playerHandCards = new();
        private readonly List<GameObject> _operatorSlots = new();
        private readonly List<Image> _operatorSlotImages = new();
        private readonly List<TextMeshProUGUI> _operatorSlotTexts = new();

        private ICardView _dealerHiddenCardView;

        // ─── 이벤트 ──────────────────────────────────────────────
        public event Action<int> OnOperatorSlotClicked;
        public event Action<int> OnHandCardClicked;

        // ─── 초기화 ───────────────────────────────────────────────

        public void Initialize(CardCreateManager cardCreate)
        {
            _cardCreate = cardCreate;
        }

        // ─── 패널 ────────────────────────────────────────────────

        public void Show() => _fieldPanelRoot?.SetActive(true);
        public void Hide() => _fieldPanelRoot?.SetActive(false);

        // ─── 플레이어 필드 (3D) ───────────────────────────────────

        /// <summary>
        /// 플레이어 필드에 카드를 추가합니다.
        /// 추가 후 전체 카드 위치를 중앙 기준으로 재정렬합니다.
        /// </summary>
        public void AddPlayerFieldCard(DeckSO.CardEntry entry)
        {
            if (entry.data.cardType != CardType.Number) return;

            // 두 번째 카드부터 연산자 슬롯 생성
            if (_playerFieldCards.Count > 0)
                CreateOperatorSlot(_playerFieldCards.Count - 1);

            // 이 카드의 목표 localPosition 계산
            int index = _playerFieldCards.Count;
            Vector3 targetPos = CalcLocalPos(index, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);

            var view = _cardCreate.CreatePlayerFieldCard(_playerFieldAnchor, entry, targetPos, onArrived: () =>
            {
                RealignCards(_playerFieldCards, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);
            });

            if (view == null) return;

            view.SetInteractable(false);
            _playerFieldCards.Add(view);
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

        /// <summary>플레이어 필드와 슬롯을 초기화합니다.</summary>
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

        private void CreateOperatorSlot(int slotIndex)
        {
            var slotGo = new GameObject(
                $"OpSlot_{slotIndex}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));

            slotGo.transform.SetParent(_playerHandContainer, false);

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

        // ─── 딜러 필드 (3D) ──────────────────────────────────────

        /// <summary>
        /// 딜러 필드에 카드를 추가합니다.
        /// 추가 후 전체 카드 위치를 중앙 기준으로 재정렬합니다.
        /// </summary>
        public void AddDealerFieldCard(DeckSO.CardEntry entry, bool isHidden = false)
        {
            int index = _dealerFieldCards.Count;
            Vector3 targetPos = CalcLocalPos(index, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);

            var view = _cardCreate.CreateDealerFieldCard(
                _dealerFieldAnchor, entry, targetPos, isHidden, onArrived: () =>
                {
                    RealignCards(_dealerFieldCards, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);
                });

            if (view == null) return;

            view.SetInteractable(false);
            _dealerFieldCards.Add(view);

            if (isHidden)
                _dealerHiddenCardView = view;

        }

        /// <summary>딜러 비공개 카드를 앞면으로 뒤집습니다.</summary>
        public void RevealDealerHiddenCard()
        {
            if (_dealerHiddenCardView == null) return;
            _dealerHiddenCardView.SetFaceUp();
            _dealerHiddenCardView = null;
        }

        /// <summary>딜러 필드를 초기화합니다.</summary>
        public void ClearDealerField()
        {
            foreach (var v in _dealerFieldCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _dealerFieldCards.Clear();
            _dealerHiddenCardView = null;
        }

        // ─── 정렬 계산 ────────────────────────────────────────────

        /// <summary>
        /// 인덱스 기준으로 카드의 localPosition을 계산합니다.
        /// 한 줄 중앙 정렬 + maxCardsPerRow 초과 시 줄바꿈.
        /// </summary>
        private Vector3 CalcLocalPos(
            int index,
            int maxPerRow,
            float spacingX,
            float spacingY)
        {
            int row = index / maxPerRow;
            int col = index % maxPerRow;

            // 이 줄의 카드 수 (마지막 줄은 남은 카드 수)
            // 전체 카드 기준이 아닌 현재 추가 중이므로 col + 1 기준
            float x = col * spacingX;
            float y = -row * spacingY;

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// 전체 카드를 중앙 기준으로 재정렬합니다.
        /// 각 줄별로 중앙 오프셋을 계산합니다.
        /// </summary>
        private void RealignCards(List<ICardView> cards, int maxPerRow, float spacingX, float spacingY)
        {
            int totalCards = cards.Count;
            if (totalCards == 0) return;

            for (int i = 0; i < totalCards; i++)
            {
                if (cards[i] is not MonoBehaviour mb || mb == null) continue;

                int row = i / maxPerRow;
                int col = i % maxPerRow;

                // 이 줄의 카드 수
                int rowStart = row * maxPerRow;
                int rowEnd = Mathf.Min(rowStart + maxPerRow, totalCards);
                int countInRow = rowEnd - rowStart;

                // 줄 중앙 오프셋
                float rowWidth = (countInRow - 1) * spacingX;
                float centerOffX = -rowWidth / 2f;

                float x = centerOffX + col * spacingX;
                float y = -row * spacingY;

                mb.transform.localPosition = new Vector3(x, y, 0f);
            }
        }

        // ─── 플레이어 손패 (UI) ───────────────────────────────────

        /// <summary>손패에 연산자 카드 UI를 추가합니다.</summary>
        public void AddHandCard(DeckSO.CardEntry entry)
        {
            int index = _playerHandCards.Count;
            int captured = index;

            var view = _cardCreate.CreateHandCard(_playerHandContainer, entry);
            if (view == null) return;

            view.SetInteractable(true);
            view.OnClicked += _ => OnHandCardClicked?.Invoke(captured);
            _playerHandCards.Add(view);
        }

        /// <summary>손패에서 카드를 제거하고 콜백을 재등록합니다.</summary>
        public void RemoveFromHand(int index)
        {
            if (index < 0 || index >= _playerHandCards.Count) return;

            var view = _playerHandCards[index];
            if (view is MonoBehaviour mb && mb != null)
                Destroy(mb.gameObject);

            _playerHandCards.RemoveAt(index);
            RebuildHandCallbacks();
        }

        /// <summary>손패를 초기화합니다.</summary>
        public void ClearPlayerHand()
        {
            foreach (var v in _playerHandCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _playerHandCards.Clear();
        }

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

        /// <summary>전체 초기화 — 라운드 시작 시 호출합니다.</summary>
        public void ClearAll()
        {
            ClearPlayerField();
            ClearDealerField();
            ClearPlayerHand();
        }
    }
}