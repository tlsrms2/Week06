using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
        [Tooltip("플레이어 한 줄 최대 카드 수")]
        [SerializeField] private int _maxCardsPerRow = 10;

        [Tooltip("딜러 한 줄 최대 카드 수")]
        [SerializeField] private int _dealerMaxCardsPerRow = 10;

        [Header("딜러 카드 뒤집기")]
        [Tooltip("딜러 비공개 카드 뒤집기 시간 (초)")]
        [SerializeField] private float _revealFlipDuration = 0.4f;

        [Header("손패 UI (Canvas 안 RectTransform)")]
        [SerializeField] private RectTransform _playerHandContainer;

        // ─── 의존성 ───────────────────────────────────────────────
        private CardCreateManager _cardCreate;

        // ─── 카드 뷰 추적 ────────────────────────────────────────
        private readonly List<ICardView> _playerFieldCards = new();
        private readonly List<ICardView> _dealerFieldCards = new();
        private readonly List<ICardView> _playerHandCards = new();
        private ICardView _dealerHiddenCardView;

        // ─── 이벤트 ──────────────────────────────────────────────
        public event Action<int> OnHandCardClicked;
        /// <summary>드래그 드롭으로 연산자가 배치될 슬롯 인덱스 발행</summary>
        public event Action<int> OnOperatorDropped;

        // ─── 드래그 spread 상태 ───────────────────────────────────
        private int _hoverSlotIndex = -1;   // 드래그 중 호버 중인 슬롯 인덱스
        private float _spreadAmount = 0.12f;  // 카드가 벌어지는 양 (인스펙터에서 추후 노출 가능)

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
                onArrived: () => RealignAllFieldCards());

            if (view == null) return;

            view.SetInteractable(false);
            _playerFieldCards.Add(view);
        }

        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
        {
            if (_playerHandCards.Count == 0) return;

            var handView = _playerHandCards[0];
            if (handView is not MonoBehaviour handMb || handMb == null)
                return;

            handMb.transform.SetParent(_playerFieldAnchor, worldPositionStays: true);
            handView.SetInteractable(false);
            handMb.transform.localRotation = Quaternion.Euler(-180f, 0f, 0f); // 필드 카드와 동일한 각도로 설정
            _playerHandCards.RemoveAt(0);

            // _playerFieldCards 내부의 숫자 카드 기준 slotIndex 위치 찾기
            int numberIndex = 0;
            int insertIndex = _playerFieldCards.Count;
            for (int i = 0; i < _playerFieldCards.Count; i++)
            {
                if (_playerFieldCards[i] is MonoBehaviour mb && 
                    mb.GetComponent<Card3DView>()?.Data?.cardType == CardType.Number)
                {
                    if (numberIndex == slotIndex + 1)
                    {
                        insertIndex = i;
                        break;
                    }
                    numberIndex++;
                }
            }

            _playerFieldCards.Insert(insertIndex, handView);

            // 드래그 드롭 직후이므로, 카드가 도착 지점에 근접해 있을 것.
            // 즉시 정렬하여 snap 시킵니다.
            RealignAllFieldCards();
        }

        /// <summary>
        /// 숫자 카드 + 연산자 카드를 포함한 전체 필드를 중앙 정렬합니다.
        /// </summary>
        private void RealignAllFieldCards()
        {
            RealignCards(_playerFieldCards, _maxCardsPerRow, _cardSpacingX, _cardSpacingY);
        }

        public void HighlightAvailableSlots(bool highlight)
        {
            // 드래그 방식으로 전환 — 슬롯 하이라이트는 spread로 대체됨
        }

        // ─── 드래그 드롭 API ─────────────────────────────────────

        /// <summary>
        /// 손패 연산자 카드(조커)에 드래그 이벤트를 연결합니다.
        /// 손패에 카드가 추가될 때 호출합니다.
        /// </summary>
        public void EnableHandCardDrag()
        {
            foreach (var v in _playerHandCards)
            {
                if (v is Card3DView c3d && c3d.Data?.cardType == CardType.Operator)
                {
                    c3d.EnableDrag();
                    c3d.OnDragStarted += OnOperatorDragStarted;
                    c3d.OnDragging   += OnOperatorDragging;
                    c3d.OnDropped    += OnOperatorDragDropped;
                }
            }
        }

        /// <summary>드래그 연결을 해제합니다.</summary>
        public void DisableHandCardDrag()
        {
            foreach (var v in _playerHandCards)
            {
                if (v is Card3DView c3d)
                {
                    c3d.DisableDrag();
                    c3d.OnDragStarted -= OnOperatorDragStarted;
                    c3d.OnDragging    -= OnOperatorDragging;
                    c3d.OnDropped     -= OnOperatorDragDropped;
                }
            }
        }

        private void OnOperatorDragStarted(Card3DView card)
        {
            _hoverSlotIndex = -1;
        }

        private void OnOperatorDragging(Card3DView card, Vector3 worldPos)
        {
            // 월드 포지션 → playerFieldAnchor 기준 로컬 X 구하기
            Vector3 localPos = _playerFieldAnchor.InverseTransformPoint(worldPos);
            int nearestSlot = FindNearestSlotIndex(localPos.x);

            if (nearestSlot != _hoverSlotIndex)
            {
                _hoverSlotIndex = nearestSlot;
                ApplySpread(_hoverSlotIndex);
            }
        }

        private void OnOperatorDragDropped(Card3DView card, Vector3 worldPos)
        {
            // spread 원복
            _hoverSlotIndex = -1;
            ApplySpread(-1);

            Vector3 localPos = _playerFieldAnchor.InverseTransformPoint(worldPos);
            int slotIndex = FindNearestSlotIndex(localPos.x);

            if (slotIndex >= 0)
                OnOperatorDropped?.Invoke(slotIndex);
            else
            {
                // 유효한 슬롯 없음 — 카드를 원위치로 돌려보냄
                if (_playerHandCards.Count > 0 && _playerHandCards[0] is MonoBehaviour mb && mb != null)
                    StartCoroutine(ReturnHandCardToAnchor(mb.gameObject));
            }
        }

        /// <summary>숫자 카드 사이 슬롯 중 localX 에 가장 가까운 슬롯 인덱스를 반환합니다.</summary>
        private int FindNearestSlotIndex(float localX)
        {
            var numberCards = GetNumberCardMBs();
            int slotCount = numberCards.Count - 1;
            if (slotCount <= 0) return -1;

            int best = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < slotCount; i++)
            {
                // 이미 연산자가 차있는지 확인
                int idxA = _playerFieldCards.FindIndex(v => v is MonoBehaviour mb && mb == numberCards[i]);
                int idxB = _playerFieldCards.FindIndex(v => v is MonoBehaviour mb && mb == numberCards[i + 1]);
                if (idxA >= 0 && idxB >= 0 && Mathf.Abs(idxA - idxB) > 1) 
                    continue; // 사이에 무언가(연산자)가 이미 있음

                float midX = (numberCards[i].transform.localPosition.x +
                              numberCards[i + 1].transform.localPosition.x) / 2f;
                float dist = Mathf.Abs(localX - midX);
                
                // 마우스가 카드 사이에 충분히 가까울 때만 슬롯으로 판정
                if (dist < bestDist && dist < _cardSpacingX * 1.5f)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// slotIndex 기준으로 왼쪽 카드는 왼쪽으로, 오른쪽 카드는 오른쪽으로 벌립니다.
        /// slotIndex == -1 이면 원래 위치로 복원합니다.
        /// DOTween을 사용하여 부드럽게 이동시킵니다.
        /// </summary>
        private void ApplySpread(int slotIndex)
        {
            if (_playerFieldCards.Count == 0) return;

            // 1. 기본 정렬 목표 위치 계산
            int totalCards = _playerFieldCards.Count;
            Dictionary<MonoBehaviour, Vector3> targets = new Dictionary<MonoBehaviour, Vector3>();
            for (int i = 0; i < totalCards; i++)
            {
                if (_playerFieldCards[i] is not MonoBehaviour mb || mb == null) continue;
                int row = i / _maxCardsPerRow;
                int col = i % _maxCardsPerRow;
                int rowStart = row * _maxCardsPerRow;
                int rowEnd = Mathf.Min(rowStart + _maxCardsPerRow, totalCards);
                int countInRow = rowEnd - rowStart;

                float rowWidth = (countInRow - 1) * _cardSpacingX;
                float centerOffX = -rowWidth / 2f;

                float x = centerOffX + col * _cardSpacingX;
                float y = -row * _cardSpacingY;

                targets[mb] = new Vector3(x, y, 0f);
            }

            // 2. Spread 반영
            if (slotIndex >= 0)
            {
                var numberCards = GetNumberCardMBs();
                if (slotIndex < numberCards.Count)
                {
                    MonoBehaviour targetNumberCard = numberCards[slotIndex];
                    int cutoffIndex = _playerFieldCards.FindIndex(v => v is MonoBehaviour mb && mb == targetNumberCard);

                    if (cutoffIndex >= 0)
                    {
                        for (int i = 0; i < totalCards; i++)
                        {
                            if (_playerFieldCards[i] is MonoBehaviour mb && mb != null)
                            {
                                var p = targets[mb];
                                if (i <= cutoffIndex) p.x -= _spreadAmount;
                                else p.x += _spreadAmount;
                                targets[mb] = p;
                            }
                        }
                    }
                }
            }

            // 3. DOTween으로 부드럽게 이동
            foreach (var kvp in targets)
            {
                kvp.Key.transform.DOKill();
                kvp.Key.transform.DOLocalMove(kvp.Value, 0.15f).SetEase(Ease.OutQuad);
            }
        }

        private List<MonoBehaviour> GetNumberCardMBs()
        {
            var result = new List<MonoBehaviour>();
            foreach (var v in _playerFieldCards)
                if (v is MonoBehaviour mb && mb != null &&
                    mb.GetComponent<Card3DView>()?.Data?.cardType == CardType.Number)
                    result.Add(mb);
            return result;
        }

        private IEnumerator ReturnHandCardToAnchor(GameObject go)
        {
            if (go == null) yield break;
            var start = go.transform.position;
            var end   = go.transform.parent != null
                        ? go.transform.parent.position
                        : start;
            float elapsed = 0f, dur = 0.25f;
            while (elapsed < dur)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                go.transform.position = Vector3.Lerp(start, end,
                    Mathf.SmoothStep(0f, 1f, elapsed / dur));
                yield return null;
            }
            if (go != null) go.transform.localPosition = Vector3.zero;
        }

        public void ClearPlayerField()
        {
            foreach (var v in _playerFieldCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _playerFieldCards.Clear();
        }

        // ─── 딜러 필드 (3D) ──────────────────────────────────────

        public void AddDealerFieldCard(DeckSO.CardEntry entry, bool isHidden = false)
        {
            int index = _dealerFieldCards.Count;
            Vector3 targetPos = CalcLocalPos(
                index, _dealerMaxCardsPerRow, _cardSpacingX, _cardSpacingY);

            var view = _cardCreate.CreateDealerFieldCard(
                _dealerFieldAnchor, entry, targetPos, isHidden,
                onArrived: () => RealignCards(
                    _dealerFieldCards, _dealerMaxCardsPerRow, _cardSpacingX, _cardSpacingY));

            if (view == null) return;

            view.SetInteractable(false);
            _dealerFieldCards.Add(view);

            if (isHidden)
                _dealerHiddenCardView = view;
        }

        public void InsertDealerFieldCard(int index, DeckSO.CardEntry entry)
        {
            // 주어진 index에 삽입되었을 때의 최종 목표 위치를 임시로 계산 파라미터로 사용
            Vector3 targetPos = CalcLocalPos(
                index, _dealerMaxCardsPerRow, _cardSpacingX, _cardSpacingY);

            // 카드를 생성하고 도착 시 전체 배열을 다시 정렬하도록 함
            var view = _cardCreate.CreateDealerFieldCard(
                _dealerFieldAnchor, entry, targetPos, isHidden: false,
                onArrived: () => RealignCards(
                    _dealerFieldCards, _dealerMaxCardsPerRow, _cardSpacingX, _cardSpacingY));

            if (view == null) return;
            view.SetInteractable(false);
            
            // 지정된 위치에 뷰(Card)를 삽입!
            if (index < _dealerFieldCards.Count)
                _dealerFieldCards.Insert(index, view);
            else
                _dealerFieldCards.Add(view);
                
            // 즉시 재정렬을 호출하여 UI가 꼬이지 않도록 합니다
            RealignCards(_dealerFieldCards, _dealerMaxCardsPerRow, _cardSpacingX, _cardSpacingY);
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

                mb.transform.DOKill();
                mb.transform.DOLocalMove(new Vector3(x, y, 0f), 0.15f).SetEase(Ease.OutQuad);
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