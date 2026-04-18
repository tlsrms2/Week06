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

        [Header("연산자 슬롯 3D")]
        [Tooltip("연산자 슬롯 프리팹 — OperatorSlot3D 컴포넌트 포함")]
        [SerializeField] private GameObject _operatorSlotPrefab;

        [Tooltip("슬롯 가로 크기 (카드 간격 계산용)")]
        [SerializeField] private float _slotWidth = 0.05f;

        // ─── 의존성 ───────────────────────────────────────────────
        private CardCreateManager _cardCreate;

        // ─── 카드 뷰 추적 ────────────────────────────────────────
        private readonly List<ICardView> _playerFieldCards = new();
        private readonly List<ICardView> _dealerFieldCards = new();
        private readonly List<ICardView> _playerHandCards = new();

        // 변경 — 3개 → 1개로 통합
        private readonly List<OperatorSlot3D> _operatorSlots3D = new();

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

        public void AddPlayerFieldCard(DeckSO.CardEntry entry, bool createSlot = false)
        {
            if (entry.data.cardType != CardType.Number) return;

            if (createSlot && _playerFieldCards.Count > 0)
                CreateOperatorSlot3D(_playerFieldCards.Count - 1);

            int index = _playerFieldCards.Count;
            Vector3 targetPos = CalcLocalPos(index, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);

            var view = _cardCreate.CreatePlayerFieldCard(
                _playerFieldAnchor, entry, targetPos, 
                onArrived: () => RealignPlayerFieldWithSlots(createSlot));

            if (view == null) return;

            view.SetInteractable(false);
            _playerFieldCards.Add(view);
        }
        // 추가
        private void RealignPlayerFieldWithSlots(bool hasSlots)
        {
            if (!hasSlots)
            {
                RealignCards(_playerFieldCards, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);
                return;
            }

            int cardCount = _playerFieldCards.Count;
            int slotCount = _operatorSlots3D.Count;
            if (cardCount == 0) return;

            float totalWidth = cardCount * _cardSpacingX + slotCount * _slotWidth;
            float startX = -totalWidth / 2f;
            float curX = startX;

            for (int i = 0; i < cardCount; i++)
            {
                if (_playerFieldCards[i] is MonoBehaviour mb && mb != null)
                {
                    mb.transform.localPosition = new Vector3(curX, 0f, 0f);
                    curX += _cardSpacingX;
                }

                if (i < slotCount && _operatorSlots3D[i] != null)
                {
                    _operatorSlots3D[i].transform.localPosition = new Vector3(curX, 0f, 0f);
                    curX += _slotWidth;
                }
            }
        }

        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
        {
            if (slotIndex < 0 || slotIndex >= _operatorSlots3D.Count) return;
            _operatorSlots3D[slotIndex].SetOccupied(op); // ← 컴포넌트가 처리
        }

        public void HighlightAvailableSlots(bool highlight)
        {
            foreach (var slot in _operatorSlots3D)
                slot.SetHighlight(highlight);
        }

        public void ClearPlayerField()
        {
            foreach (var v in _playerFieldCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _playerFieldCards.Clear();

            foreach (var slot in _operatorSlots3D)
                if (slot != null) Destroy(slot.gameObject);
            _operatorSlots3D.Clear();
        }

        private void CreateOperatorSlot3D(int slotIndex)
        {
            if (_operatorSlotPrefab == null) return;

            var go = Instantiate(_operatorSlotPrefab, _playerFieldAnchor);
            var slot = go.GetComponent<OperatorSlot3D>();

            if (slot == null) { Destroy(go); return; }

            slot.Initialize(slotIndex);
            slot.OnSlotClicked += idx => OnOperatorSlotClicked?.Invoke(idx);
            _operatorSlots3D.Add(slot);
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

        private void RealignCards(List<ICardView> cards, int maxPerRow, float spacingX, float spacingY)
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