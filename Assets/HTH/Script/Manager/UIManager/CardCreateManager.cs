using System.Collections;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 카드 뭉치에서 카드를 생성하는 팩토리입니다.
    ///
    /// 생성 방식 분리
    /// ├── CreateFieldCard  → 3D 프리팹 (일반 Transform 기준점)
    /// ├── CreateHiddenCard → 3D 프리팹, 뒷면 상태
    /// └── CreateHandCard   → 항상 UI 카드 (RectTransform 컨테이너)
    /// </summary>
    public class CardCreateManager : MonoBehaviour
    {
        [Header("카드 뭉치")]
        [Tooltip("카드가 생성되는 시작 위치")]
        [SerializeField] private Transform _deckTransform;

        [Header("이동 설정")]
        [Tooltip("카드가 필드로 이동하는 시간 (초)")]
        [SerializeField] private float _moveDuration = 0.5f;

        [Tooltip("카드 사이 간격 (월드 단위)")]
        [SerializeField] private float _cardSpacing = 0.15f;

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>필드용 3D 카드를 생성합니다. parent : 일반 Transform</summary>
        public ICardView CreateFieldCard(
            Transform parent,
            DeckSO.CardEntry entry,
            int cardIndex = 0)
            => Create3D(parent, entry, isOpen: true, cardIndex);

        /// <summary>딜러 비공개 3D 카드를 생성합니다. 뒷면 상태.</summary>
        public ICardView CreateHiddenCard(
            Transform parent,
            DeckSO.CardEntry entry,
            int cardIndex = 0)
            => Create3D(parent, entry, isOpen: false, cardIndex);

        /// <summary>손패용 UI 카드를 생성합니다. parent : RectTransform</summary>
        public ICardView CreateHandCard(
            Transform parent,
            DeckSO.CardEntry entry,
            int cardIndex = 0)
            => CreateUI(parent, entry.data);

        // ─── 3D 카드 생성 ─────────────────────────────────────────

        private ICardView Create3D(
            Transform parent,
            DeckSO.CardEntry entry,
            bool isOpen,
            int cardIndex)
        {
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
                    Debug.LogWarning(
                        $"[CardCreateManager] {entry.data.displayLabel} SuitDataSO 없음");
                    return null;
                }

                prefab = suitData.GetPrefab(entry.data.numberValue);
                mat = suitData.sharedMaterial;
                offset = suitData.GetOffset(entry.data.numberValue);
                scale = suitData.uvScale;
            }

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[CardCreateManager] {entry.data.displayLabel} 프리팹 없음");
                return null;
            }

            // 덱 위치에서 생성
            var startPos = _deckTransform != null
                ? _deckTransform.position
                : Vector3.zero;

            var go = Instantiate(prefab, startPos, Quaternion.identity);

            // Rigidbody 비활성
            var rigid = go.GetComponent<Rigidbody>();
            if (rigid != null)
            {
                rigid.isKinematic = true;
                rigid.useGravity = false;
            }

            // Material UV 적용
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && mat != null)
            {
                var matInstance = new Material(mat);
                matInstance.SetTextureOffset("_BaseMap", offset);
                matInstance.SetTextureScale("_BaseMap", scale);
                meshRenderer.material = matInstance;
            }

            // Card3DView 추가 및 초기화
            var view = go.AddComponent<Card3DView>();
            view.Initialize(entry.data);

            // 앞뒤면 상태 기록 — SetParent 이후에도 유지하기 위해
            bool faceUp = isOpen;

            // 목표 위치 계산
            var targetPos = parent.position + parent.right * (cardIndex * _cardSpacing);

            // 이동 코루틴 — 도착 후 SetParent + rotation 재적용
            StartCoroutine(MoveToTarget(go, view, parent, targetPos, _moveDuration, faceUp));

            return view;
        }

        /// <summary>
        /// 카드를 목표 위치까지 이동시킵니다.
        /// 도착 후 SetParent하고 앞뒤면 rotation을 재적용합니다.
        /// </summary>
        private IEnumerator MoveToTarget(
            GameObject go,
            Card3DView view,
            Transform parent,
            Vector3 targetPos,
            float duration,
            bool faceUp)
        {
            if (go == null) yield break;

            var startPos = go.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                go.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if (go == null) yield break;

            go.transform.position = targetPos;

            // SetParent — worldPositionStays: true로 위치 유지
            go.transform.SetParent(parent, worldPositionStays: true);

            // ← SetParent 후 rotation 재적용
            if (faceUp) view.SetFaceUp();
            else view.SetFaceDown();
        }

        // ─── UI 카드 생성 ─────────────────────────────────────────

        private ICardView CreateUI(Transform parent, CardDataSO data)
        {
            var go = new GameObject(
                $"Card_{data.displayLabel}",
                typeof(Transform),
                typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button));

            if (parent != null)
                go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(65f, 90f);

            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = 65f;
            le.preferredHeight = 90f;

            var txtGo = new GameObject("Label",
                typeof(RectTransform),
                typeof(TMPro.TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
            txt.fontSize = 32;
            txt.alignment = TMPro.TextAlignmentOptions.Center;

            var view = go.AddComponent<Card3DView>();
            view.Initialize(data);
            return view;
        }
    }
}