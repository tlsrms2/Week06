using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 플레이어 필드 / 딜러 필드 / 플레이어 손패를 통합 관리합니다.
    /// </summary>
    public class FieldManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("패널")]
        [SerializeField] private GameObject _fieldPanelRoot;

        [Header("3D 카드 배치 기준점 (일반 Transform)")]
        [SerializeField] private Transform _playerFieldAnchor;
        [SerializeField] private Transform _dealerFieldAnchor;

        [Header("카드 정렬 설정")]
        [SerializeField] private float _cardSpacingX = 0.15f;
        [SerializeField] private float _cardSpacingY = 0.25f;
        [SerializeField] private int _maxCardsPerRow = 5;

        [Header("딜러 카드 뒤집기")]
        [Tooltip("딜러 비공개 카드 뒤집기 시간 (초)")]
        [SerializeField] private float _revealFlipDuration = 0.4f;

        [Header("손패 / 연산자 슬롯 UI (Canvas 안 RectTransform)")]
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

        public void AddPlayerFieldCard(DeckSO.CardEntry entry)
        {
            if (entry.data.cardType != CardType.Number) return;

            if (_playerFieldCards.Count > 0)
                CreateOperatorSlot(_playerFieldCards.Count - 1);

            int index = _playerFieldCards.Count;
            Vector3 targetPos = CalcLocalPos(index, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);

            var view = _cardCreate.CreatePlayerFieldCard(
                _playerFieldAnchor, entry, targetPos,
                onArrived: () => RealignCards(
                    _playerFieldCards, _maxCardsPerRow, _cardSpacingX, _cardSpacingY));

            if (view == null) return;

            view.SetInteractable(false);
            _playerFieldCards.Add(view);
        }

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

        public void HighlightAvailableSlots(bool highlight)
        {
            foreach (var img in _operatorSlotImages)
                img.color = highlight ? UIColor.Selected : UIColor.SlotDefault;
        }

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

        public void AddDealerFieldCard(DeckSO.CardEntry entry, bool isHidden = false)
        {
            int index = _dealerFieldCards.Count;
            Vector3 targetPos = CalcLocalPos(index, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);

            var view = _cardCreate.CreateDealerFieldCard(
                _dealerFieldAnchor, entry, targetPos, isHidden,
                onArrived: () => RealignCards(
                    _dealerFieldCards, _maxCardsPerRow, _cardSpacingX, _cardSpacingY));

            if (view == null) return;

            view.SetInteractable(false);
            _dealerFieldCards.Add(view);

            if (isHidden)
                _dealerHiddenCardView = view;
        }

        /// <summary>
        /// 딜러 비공개 카드를 애니메이션으로 뒤집습니다.
        /// onComplete : 뒤집기 완료 후 콜백 (다음 카드 드로우 등)
        /// </summary>
        public void RevealDealerHiddenCard(Action onComplete = null)
        {
            if (_dealerHiddenCardView == null)
            {
                onComplete?.Invoke();
                return;
            }

            var view = _dealerHiddenCardView;
            _dealerHiddenCardView = null;

            StartCoroutine(FlipToFaceUp(view, onComplete));
        }

        /// <summary>
        /// 카드를 FaceDown → FaceUp으로 애니메이션 뒤집습니다.
        /// FaceDown : Euler(90,  0, 90)
        /// FaceUp   : Euler(-90, 0, 90)
        /// X축 기준 180도 회전 (Z=90 고정)
        /// </summary>
        private IEnumerator FlipToFaceUp(ICardView cardView, Action onComplete)
        {
            if (cardView is not MonoBehaviour mb || mb == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var t = mb.transform;
            var startRot = t.localRotation;
            // FaceUp 목표 각도
            var targetRot = Quaternion.Euler(-180f, 0f, 0f);
            float elapsed = 0f;

            while (elapsed < _revealFlipDuration)
            {
                if (mb == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / _revealFlipDuration);
                t.localRotation = Quaternion.Slerp(startRot, targetRot, progress);
                yield return null;
            }

            if (mb != null)
                mb.transform.localRotation = targetRot;

            onComplete?.Invoke();
        }

        public void ClearDealerField()
        {
            foreach (var v in _dealerFieldCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _dealerFieldCards.Clear();
            _dealerHiddenCardView = null;
        }

        // ─── 정렬 계산 ────────────────────────────────────────────

        private Vector3 CalcLocalPos(int index, int maxPerRow, float spacingX, float spacingY)
        {
            int row = index / maxPerRow;
            int col = index % maxPerRow;
            float x = col * spacingX;
            float y = -row * spacingY;
            return new Vector3(x, y, 0f);
        }

        private void RealignCards(
            List<ICardView> cards,
            int maxPerRow,
            float spacingX,
            float spacingY)
        {
            int totalCards = cards.Count;
            if (totalCards == 0) return;

            for (int i = 0; i < totalCards; i++)
            {
                if (cards[i] is not MonoBehaviour mb || mb == null) continue;

                int row = i / maxPerRow;
                int col = i % maxPerRow;
                int rowStart = row * maxPerRow;
                int rowEnd = Mathf.Min(rowStart + maxPerRow, totalCards);
                int countInRow = rowEnd - rowStart;

                float rowWidth = (countInRow - 1) * spacingX;
                float centerOffX = -rowWidth / 2f;

                float x = centerOffX + col * spacingX;
                float y = -row * spacingY;

                mb.transform.localPosition = new Vector3(x, y, 0f);
            }
        }

        // ─── 플레이어 손패 (UI) ───────────────────────────────────

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

        public void RemoveFromHand(int index)
        {
            if (index < 0 || index >= _playerHandCards.Count) return;

            var view = _playerHandCards[index];
            if (view is MonoBehaviour mb && mb != null)
                Destroy(mb.gameObject);

            _playerHandCards.RemoveAt(index);
            RebuildHandCallbacks();
        }

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

        public void HighlightHandCard(int index)
        {
            for (int i = 0; i < _playerHandCards.Count; i++)
                _playerHandCards[i].SetSelected(i == index);
        }

        public void ClearAll()
        {
            ClearPlayerField();
            ClearDealerField();
            ClearPlayerHand();
        }
    }
}