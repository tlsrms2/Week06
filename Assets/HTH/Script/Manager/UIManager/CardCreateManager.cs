using System;
using System.Collections;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 카드 뭉치에서 카드를 생성하고 필드로 이동시키는 팩토리입니다.
    ///
    /// 플레이어 카드 흐름
    /// 1. 덱 위치에서 생성 (뒷면)
    /// 2. _centerPoint(화면 중앙)로 이동 + 앞면으로 뒤집기
    /// 3. PlayerField 목표 위치로 이동
    ///
    /// 딜러 카드 흐름
    /// └── 덱 위치에서 DealerField 목표 위치로 직행
    ///     첫 번째 → 앞면 / 두 번째 → 뒷면
    ///
    /// 손패 카드
    /// └── UI 카드로 생성 (RectTransform)
    /// </summary>
    public class CardCreateManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("위치")]
        [Tooltip("카드가 생성되는 시작 위치 (덱)")]
        [SerializeField] private Transform _deckTransform;

        [Tooltip("플레이어 카드가 뒤집히는 중앙 위치")]
        [SerializeField] private Transform _centerPoint;

        [Header("연산자 위치")]
        [Tooltip("연산자 카드 손패 배치 기준점")]
        [SerializeField] private Transform _handAnchor;

        [Tooltip("카드 뒤집기 애니메이션 시간 (초)")]
        [SerializeField] private float _flipDuration = 0.3f;

        [Header("이동 설정")]
        [Tooltip("덱 → 중앙 이동 시간 (초)")]
        [SerializeField] private float _moveToCenterDuration = 0.3f;

        [Tooltip("중앙 → 필드 이동 시간 (초)")]
        [SerializeField] private float _moveToFieldDuration = 0.3f;

        [Tooltip("딜러 덱 → 필드 이동 시간 (초)")]
        [SerializeField] private float _dealerMoveDuration = 0.4f;

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>
        /// 플레이어 필드용 카드를 생성합니다.
        /// 덱 → 중앙(뒤집기) → 목표 위치 순서로 이동합니다.
        /// targetPos : 필드에서의 최종 localPosition
        /// </summary>
        public ICardView CreatePlayerFieldCard(Transform parent, DeckSO.CardEntry entry, 
            Vector3 targetLocalPos, Action onArrived = null)
        {
            var go = SpawnCard(entry, out Card3DView view);
            if (go == null) return null;

            StartCoroutine(MovePlayerCard(go, view, parent, targetLocalPos, onArrived));
            return view;
        }

        /// <summary>
        /// 딜러 필드용 카드를 생성합니다.
        /// 덱 → 목표 위치로 직행합니다.
        /// isHidden = true면 뒷면으로 배치됩니다.
        /// targetPos : 필드에서의 최종 localPosition
        /// </summary>
        public ICardView CreateDealerFieldCard(Transform parent, DeckSO.CardEntry entry, 
            Vector3 targetLocalPos, bool isHidden = false, Action onArrived = null)
        {
            var go = SpawnCard(entry, out Card3DView view);
            if (go == null) return null;

            if (isHidden) view.SetFaceDown();
            else view.SetFaceUp();

            StartCoroutine(MoveDealerCard(go, parent, targetLocalPos, onArrived));
            return view;
        }

        /// <summary>
        /// 손패용 UI 카드를 생성합니다.
        /// parent : Canvas 안 RectTransform
        /// </summary>
        public ICardView CreateHandCard(RectTransform parent, DeckSO.CardEntry entry, Action onArrived = null) // ← onArrived 콜백 추가
        {
            var go = SpawnCard(entry, out Card3DView view);
            if (go == null) return null;

            StartCoroutine(MoveHandCard(go, view, onArrived)); // ← 코루틴으로 이동
            return view;
        }
        private IEnumerator MoveHandCard(GameObject go, Card3DView view, Action onArrived)
        {
            if (go == null) yield break;

            var anchor = _handAnchor != null ? _handAnchor : _deckTransform;
            var targetPos = anchor.position;

            // 1. 덱 → _handAnchor 이동 (비공개 상태)
            yield return StartCoroutine(
                MoveWorld(go, go.transform.position, targetPos, _moveToFieldDuration));

            if (go == null) yield break;

            // 2. 부모 설정
            go.transform.SetParent(anchor, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;

            // 3. 도착 후 앞면으로 뒤집기
            yield return StartCoroutine(FlipCard(go, view, _flipDuration));

            if (go == null) yield break;

            // 4. 클릭 가능 상태로 전환
            view.SetInteractable(true);
            onArrived?.Invoke();
        }

        // ─── 카드 공통 생성 ───────────────────────────────────────

        /// <summary>
        /// 덱 위치에서 카드를 Instantiate하고 Card3DView를 반환합니다.
        /// </summary>
        private GameObject SpawnCard(DeckSO.CardEntry entry, out Card3DView view)
        {
            view = null;

            GameObject prefab;
            Material mat;
            Vector2 offset;
            Vector2 scale;

            if (entry.data.cardType == CardType.Operator)
            {
                prefab = entry.data.jokerPrefab;
                mat = entry.data.operatorMaterial;
                offset = entry.data.operatorUvOffset;
                scale = entry.data.operatorUvScale;
            }
            else
            {
                var suitData = entry.suitData;
                if (suitData == null)
                {
                    Debug.LogWarning($"[CardCreateManager] {entry.data.displayLabel} SuitDataSO 없음");
                    return null;
                }

                prefab = suitData.GetPrefab(entry.data.numberValue);
                mat = suitData.sharedMaterial;
                offset = suitData.GetOffset(entry.data.numberValue);
                scale = suitData.uvScale;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[CardCreateManager] {entry.data.displayLabel} 프리팹 없음");
                return null;
            }

            var startPos = _deckTransform != null
                ? _deckTransform.position
                : Vector3.zero;
            
            // 카드 생성
            var go = Instantiate(prefab, startPos, Quaternion.identity);
            go.transform.rotation = Quaternion.identity;

            var rigid = go.GetComponent<Rigidbody>();
            if (rigid != null)
            {
                rigid.isKinematic = true;
                rigid.useGravity = false;
            }

            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && mat != null)
            {
                var matInstance = new Material(mat);
                matInstance.SetTextureOffset("_BaseMap", offset);
                matInstance.SetTextureScale("_BaseMap", scale);
                meshRenderer.material = matInstance;
            }

            view = go.AddComponent<Card3DView>();
            
            view.Initialize(entry.data);

            // 카드 뒤집힌 상태로 생성
            view.SetFaceDown();

            return go;
        }

        // ─── 이동 코루틴 ─────────────────────────────────────────

        /// <summary>
        /// 플레이어 카드 이동.
        /// 덱 → 중앙(뒤집기) → 목표 localPosition
        /// </summary>
        private IEnumerator MovePlayerCard(GameObject go, Card3DView view, Transform parent, Vector3 targetLocalPos, Action OnArrived)
        {
            if (go == null) yield break;
            AudioManager.instance.PlaySfx(AudioManager.Sfx.cardSlide);
            // 1. 덱 → 중앙
            var centerPos = _centerPoint != null ? _centerPoint.position : Vector3.zero;

            yield return StartCoroutine(MoveWorld(go, go.transform.position, centerPos, _moveToCenterDuration));

            if (go == null) yield break;

            // 2. 중앙에서 뒤집기 애니메이션
            yield return StartCoroutine(FlipCard(go, view, _flipDuration));
            AudioManager.instance.PlaySfx(AudioManager.Sfx.cardFlip);

            if (go == null) yield break;

            // 3. 부모 설정 후 목표 localPosition으로 이동
            go.transform.SetParent(parent, worldPositionStays: true);

            var targetWorldPos = parent.TransformPoint(targetLocalPos);

            yield return StartCoroutine(MoveWorld(go, go.transform.position, targetWorldPos, _moveToFieldDuration));

            if (go == null) yield break;

            go.transform.localPosition = targetLocalPos;

            OnArrived?.Invoke();
        }
        /// <summary>카드를 X축으로 180도 회전시켜 뒤집습니다.</summary>
        private IEnumerator FlipCard(GameObject go, Card3DView view, float duration)
        {
            if (go == null) yield break;

            var startRot = go.transform.rotation;
            var endRot = startRot * Quaternion.Euler(0f, 180f, 0f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                go.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            if (go == null) yield break;

            // 뒤집기 완료 후 localRotation을 FaceUp으로 고정
            view.SetFaceUp();
        }

        /// <summary>
        /// 딜러 카드 이동.
        /// 덱 → 목표 localPosition 직행
        /// </summary>
        private IEnumerator MoveDealerCard(GameObject go, Transform parent, Vector3 targetLocalPos, Action onArrived)
        {
            if (go == null) yield break;

            var startPos = go.transform.position;
            var targetWorldPos = parent.TransformPoint(targetLocalPos);

            yield return StartCoroutine(MoveWorld(go, startPos, targetWorldPos, _dealerMoveDuration));

            if (go == null) yield break;

            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.localPosition = targetLocalPos;

            onArrived?.Invoke();
        }

        /// <summary>월드 좌표 기준 SmoothStep 이동 코루틴</summary>
        private IEnumerator MoveWorld(GameObject go, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                go.transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }
            if (go != null)
                go.transform.position = to;
        }
    }
}