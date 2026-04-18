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

        [Tooltip("연산자 카드 슬롯 이동 시간 (초)")]
        [SerializeField] private float _operatorMoveToSlotDuration = 0.3f;

        [Tooltip("슬롯 가로 크기 (카드 간격 계산용)")]
        [SerializeField] private float _slotWidth = 0.05f;

        // ─── 의존성 ───────────────────────────────────────────────
        private CardCreateManager _cardCreate;

        // ─── 카드 뷰 추적 ────────────────────────────────────────
        private readonly List<ICardView> _playerFieldCards = new();
        private readonly List<ICardView> _dealerFieldCards = new();
        private readonly List<ICardView> _playerHandCards = new();

        private readonly List<GameObject> _placedOperatorObjects = new();

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

            int index = _playerFieldCards.Count;
            Vector3 targetPos = CalcLocalPos(index, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);

            var view = _cardCreate.CreatePlayerFieldCard(
                _playerFieldAnchor, entry, targetPos,
                onArrived: () =>
                {
                    if (createSlot && _playerFieldCards.Count > 1)
                        CreateOperatorSlot3D(_playerFieldCards.Count - 2);

                    // ← 통합 정렬
                    RealignAllFieldCards();

                    // ← 슬롯도 재배치
                    RealignSlots();
                });

            if (view == null) return;

            view.SetInteractable(false);
            _playerFieldCards.Add(view);
        }
        /// <summary>
        /// 슬롯을 카드 사이에 배치합니다.
        /// 슬롯[i]는 카드[i]와 카드[i+1] 사이에 위치합니다.
        /// </summary>
        private void RealignSlots()
        {
            var numberCards = new List<ICardView>();
            foreach (var v in _playerFieldCards)
                if (v is MonoBehaviour mb && mb != null &&
                    mb.GetComponent<Card3DView>()?.Data?.cardType == CardType.Number)
                    numberCards.Add(v);

            for (int i = 0; i < _operatorSlots3D.Count; i++)
            {
                var slot = _operatorSlots3D[i];
                if (slot == null) continue;

                if (i >= numberCards.Count - 1) continue;

                // 줄이 다르면 슬롯 배치 스킵
                int rowA = i / _maxCardsPerRow;
                int rowB = (i + 1) / _maxCardsPerRow;
                if (rowA != rowB) continue;

                if (numberCards[i] is MonoBehaviour mbA &&
                    numberCards[i + 1] is MonoBehaviour mbB)
                {
                    float midX = (mbA.transform.localPosition.x +
                                  mbB.transform.localPosition.x) / 2f;
                    float y = mbA.transform.localPosition.y;
                    slot.transform.localPosition = new Vector3(midX, y, 0f);
                }
            }
        }
        // 추가
        private void RealignPlayerFieldWithSlots(bool hasSlots)
        {
            int cardCount = _playerFieldCards.Count;
            if (cardCount == 0) return;

            if (!hasSlots)
            {
                RealignCards(_playerFieldCards, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);
                return;
            }

            // 슬롯 전체 수 (null 포함) — null은 연산자 카드가 차지한 자리
            int slotCount = _operatorSlots3D.Count;

            float totalWidth = (cardCount - 1) * _cardSpacingX
                             + slotCount * (_slotWidth + _cardSpacingX * 0.5f);
            float curX = -totalWidth / 2f;

            for (int i = 0; i < cardCount; i++)
            {
                // 카드 배치
                if (_playerFieldCards[i] is MonoBehaviour mb && mb != null)
                {
                    mb.transform.localPosition = new Vector3(curX, 0f, 0f);
                    curX += _cardSpacingX;
                }

                // 슬롯 또는 연산자 카드 자리
                if (i < slotCount)
                {
                    var slot = _operatorSlots3D[i];
                    if (slot != null)
                    {
                        // 빈 슬롯
                        slot.transform.localPosition = new Vector3(curX, 0f, 0f);
                    }
                    // slot == null이면 연산자 카드가 이미 해당 위치에 있음
                    // MoveToSlot에서 localPosition을 직접 설정했으므로 건드리지 않음
                    curX += _slotWidth + _cardSpacingX * 0.5f;
                }
            }
        }

        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
        {
            if (slotIndex < 0 || slotIndex >= _operatorSlots3D.Count) return;
            if (_playerHandCards.Count == 0) return;

            var slot = _operatorSlots3D[slotIndex];
            var handView = _playerHandCards[0];

            if (slot == null || handView is not MonoBehaviour handMb || handMb == null)
                return;

            var slotPos = slot.transform.localPosition;

            Destroy(slot.gameObject);
            _operatorSlots3D[slotIndex] = null;

            handMb.transform.SetParent(_playerFieldAnchor, worldPositionStays: true);
            _playerHandCards.RemoveAt(0);

            // ← 연산자 카드 오브젝트 추적
            // slotIndex 위치에 대응하도록 리스트 크기 맞춤
            while (_placedOperatorObjects.Count <= slotIndex)
                _placedOperatorObjects.Add(null);
            _placedOperatorObjects[slotIndex] = handMb.gameObject;

            StartCoroutine(MoveToSlotAndRealign(handMb.gameObject, slotPos));
        }
        private IEnumerator MoveToSlotAndRealign(GameObject go, Vector3 targetLocalPos)
        {
            if (go == null) yield break;

            var startLocal = go.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < _operatorMoveToSlotDuration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _operatorMoveToSlotDuration);
                go.transform.localPosition = Vector3.Lerp(startLocal, targetLocalPos, t);
                yield return null;
            }

            if (go == null) yield break;

            go.transform.localPosition = targetLocalPos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // ← 배열에 포함된 상태로 전체 재정렬
            RealignAllFieldCards();
        }
        /// <summary>
        /// 숫자 카드 + 연산자 카드를 포함한 전체 필드를 중앙 정렬합니다.
        /// _playerFieldCards에 모든 카드가 포함된 상태로 호출됩니다.
        /// </summary>
        private void RealignAllFieldCards()
        {
            int cardCount = _playerFieldCards.Count;
            if (cardCount == 0) return;

            int slotIdx = 0;
            int cardIdx = 0;

            while (cardIdx < cardCount)
            {
                int row = cardIdx / _maxCardsPerRow;
                int rowCardCount = Mathf.Min(_maxCardsPerRow, cardCount - row * _maxCardsPerRow);
                bool isLastRow = (row + 1) * _maxCardsPerRow >= cardCount;
                int availableSlots = Mathf.Max(0, _operatorSlots3D.Count - slotIdx);

                int rowSlotCount;
                if (isLastRow)
                {
                    // 마지막 줄 — 경계 슬롯 없음
                    rowSlotCount = Mathf.Min(rowCardCount - 1, availableSlots);
                }
                else
                {
                    // 중간 줄 — 마지막 슬롯은 경계 슬롯 → 너비 계산 제외
                    rowSlotCount = Mathf.Max(0, Mathf.Min(rowCardCount - 2, availableSlots));
                }

                // 경계 슬롯 제외한 너비 계산
                float rowWidth = (rowCardCount - 1) * _cardSpacingX + rowSlotCount * _slotWidth;
                float curX = -rowWidth / 2f;
                float curY = -row * _cardSpacingY;

                for (int col = 0; col < rowCardCount && cardIdx < cardCount; col++, cardIdx++)
                {
                    if (_playerFieldCards[cardIdx] is MonoBehaviour mb && mb != null)
                    {
                        mb.transform.localPosition = new Vector3(curX, curY, 0f);
                        curX += _cardSpacingX;
                    }

                    if (slotIdx >= _operatorSlots3D.Count) continue;

                    bool isLastInRow = col == rowCardCount - 1;
                    bool isLastCard = cardIdx == cardCount - 1;
                    bool isBoundary = isLastInRow && !isLastCard;

                    var slot = _operatorSlots3D[slotIdx];
                    var opObj = slotIdx < _placedOperatorObjects.Count
                        ? _placedOperatorObjects[slotIdx]
                        : null;

                    if (isBoundary)
                    {
                        // ← SetActive(false) 제거
                        // 줄 1 마지막 카드 오른쪽에 배치
                        if (slot != null)
                        {
                            slot.gameObject.SetActive(true);
                            slot.transform.localPosition = new Vector3(curX, curY, 0f);
                        }
                        else if (opObj != null)
                        {
                            opObj.SetActive(true);
                            opObj.transform.localPosition = new Vector3(curX, curY, 0f);
                        }
                        // curX는 너비 계산에 포함 안 됐으므로 갱신 불필요
                        slotIdx++;
                    }
                    else if (!isLastInRow)
                    {
                        // 줄 내부 슬롯 정상 배치
                        if (slot != null)
                        {
                            slot.gameObject.SetActive(true);
                            slot.transform.localPosition = new Vector3(curX, curY, 0f);
                        }
                        else if (opObj != null)
                        {
                            opObj.SetActive(true);
                            opObj.transform.localPosition = new Vector3(curX, curY, 0f);
                        }
                        curX += _slotWidth;
                        slotIdx++;
                    }
                }
            }
        }

        //손패 카드를 슬롯 위치로 부드럽게 이동
        private IEnumerator MoveToSlot(GameObject go, Vector3 targetLocalPos, float duration)
        {
            var startLocal = go.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                go.transform.localPosition = Vector3.Lerp(startLocal, targetLocalPos, t);
                yield return null;
            }

            go.transform.localPosition = targetLocalPos;

            // ← 도착 후 rotation 고정 (FaceUp 각도)
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            RealignPlayerFieldWithSlots(true);
        }

        public void HighlightAvailableSlots(bool highlight)
        {
            foreach (var slot in _operatorSlots3D)
                if(slot != null) slot.SetHighlight(highlight);
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

            _placedOperatorObjects.Clear();
        }

        private void CreateOperatorSlot3D(int slotIndex)
        {
            Debug.Log($"[FieldManager] CreateOperatorSlot3D 호출 — slotIndex:{slotIndex}");

            if (_operatorSlotPrefab == null)
            {
                Debug.LogError("[FieldManager] _operatorSlotPrefab 없음");
                return;
            }

            var go = Instantiate(_operatorSlotPrefab, _playerFieldAnchor);
            var slot = go.GetComponent<OperatorSlot3D>();

            if (slot == null)
            {
                Debug.LogError("[FieldManager] OperatorSlot3D 컴포넌트 없음");
                Destroy(go);
                return;
            }

            slot.Initialize(slotIndex);
            slot.OnSlotClicked += idx => OnOperatorSlotClicked?.Invoke(idx);
            _operatorSlots3D.Add(slot);

            Debug.Log($"[FieldManager] 슬롯 생성 완료 — 총 슬롯 수:{_operatorSlots3D.Count}");
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
        // FieldManager 추가
        /// <summary>
        /// 손패 카드를 제거하고 3D 오브젝트를 Destroy합니다.
        /// 연산자 카드 버리기 시 호출됩니다.
        /// </summary>
        public void RemoveHandCard()
        {
            if (_playerHandCards.Count == 0) return;

            var view = _playerHandCards[0];
            if (view is MonoBehaviour mb && mb != null)
                Destroy(mb.gameObject);

            _playerHandCards.RemoveAt(0);
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