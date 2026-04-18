using System;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// ICardView의 3D 카드 구현체.
    /// CardCreateManager.CreateFrom3DPrefab에서 AddComponent로 추가됩니다.
    /// 프리팹에 미리 부착하지 않습니다.
    ///
    /// 앞뒤면 전환
    /// └── Y축 회전으로 처리
    ///     뒷면 : rotation.y = 0
    ///     앞면 : rotation.y = 180
    /// </summary>
    public class Card3DView : MonoBehaviour, ICardView
    {
        // ─── ICardView ────────────────────────────────────────────
        public CardDataSO Data { get; private set; }
        public event Action<ICardView> OnClicked;

        // ─── 컴포넌트 참조 ────────────────────────────────────────
        private MeshRenderer _meshRenderer;
        private BoxCollider _col;
        private Rigidbody _rigid;

        // ─── 상태 ─────────────────────────────────────────────────
        private bool _interactable = true;

        // ─── 앞뒤면 각도 ──────────────────────────────────────────
        private static readonly Quaternion FaceUpRotation
            = Quaternion.Euler(0f, 180f, 0f);
        private static readonly Quaternion FaceDownRotation
            = Quaternion.Euler(0f, 0f, 0f);

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            // AddComponent 후 프리팹 컴포넌트 참조
            _meshRenderer = GetComponent<MeshRenderer>();
            _col = GetComponent<BoxCollider>();
            _rigid = GetComponent<Rigidbody>();
        }

        private void OnMouseDown()
        {
            if (!_interactable) return;
            OnClicked?.Invoke(this);
        }

        // ─── ICardView 구현 ───────────────────────────────────────

        /// <summary>카드 데이터를 초기화합니다.</summary>
        public void Initialize(CardDataSO data)
        {
            Data = data;
        }

        /// <summary>클릭 가능 여부를 설정합니다.</summary>
        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (_col != null) _col.enabled = interactable;
        }

        /// <summary>선택 상태를 시각적으로 표시합니다.</summary>
        public void SetSelected(bool selected)
        {
            var pos = transform.localPosition;
            pos.y = selected ? 0.05f : 0f;
            transform.localPosition = pos;
        }

        /// <summary>시야 감소 시 블러를 적용합니다.</summary>
        public void SetBlurLevel(float normalized)
        {
            if (_meshRenderer == null) return;
            var color = _meshRenderer.material.color;
            color.a = Mathf.Lerp(1f, 0.2f, normalized);
            _meshRenderer.material.color = color;
        }

        /// <summary>OnClicked 이벤트 구독을 해제합니다.</summary>
        public void ClearClickListeners()
            => OnClicked = null;

        /// <summary>앞면을 공개합니다. Y축 180도 회전.</summary>
        public void SetFaceUp()
            => transform.localRotation = FaceUpRotation;

        /// <summary>뒷면을 공개합니다. Y축 0도 회전.</summary>
        public void SetFaceDown()
            => transform.localRotation = FaceDownRotation;
    }
}