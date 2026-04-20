using System;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// ICardView의 3D 카드 구현체.
    /// 연산자 카드는 드래그로 필드에 배치합니다.
    /// </summary>
    public class Card3DView : MonoBehaviour, ICardView
    {
        // ─── ICardView ────────────────────────────────────────────
        public CardDataSO Data { get; private set; }
        public event Action<ICardView> OnClicked;

        // ─── 드래그 이벤트 (FieldManager가 구독) ─────────────────
        /// <summary>드래그 중 매 프레임 발행 (월드 포지션)</summary>
        public event Action<Card3DView, Vector3> OnDragging;
        /// <summary>드래그 종료 시 발행 (월드 포지션)</summary>
        public event Action<Card3DView, Vector3> OnDropped;
        /// <summary>드래그 시작 시 발행</summary>
        public event Action<Card3DView> OnDragStarted;

        // ─── 컴포넌트 참조 ────────────────────────────────────────
        private MeshRenderer _meshRenderer;
        private BoxCollider _col;
        private Rigidbody _rigid;

        // ─── 상태 ─────────────────────────────────────────────────
        private bool _interactable = true;
        private bool _isDragging = false;
        private bool _isDragEnabled = false;  // 드래그 허용 여부 (조커 뜰 때만 true)

        // ─── 드래그 내부 ──────────────────────────────────────────
        private Camera _mainCam;
        private Plane _dragPlane;
        private Vector3 _dragOffset;

        // ─── 앞뒤면 각도 ──────────────────────────────────────────
        private static readonly Quaternion FaceUpRotation
            = Quaternion.Euler(-90f, 0f, 90f);
        private static readonly Quaternion FaceDownRotation
            = Quaternion.Euler(90f, 0f, 90f);

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _col = GetComponent<BoxCollider>();
            _rigid = GetComponent<Rigidbody>();
            _mainCam = Camera.main;
        }

        private void OnMouseDown()
        {
            if (!_interactable) return;

            if (_isDragEnabled && Data != null && Data.cardType == CardType.Operator)
            {
                // 드래그 시작: 카메라가 바라보는 평면
                _mainCam = Camera.main;
                if (_mainCam != null)
                {
                    _dragPlane = new Plane(-_mainCam.transform.forward, transform.position);
                    _isDragging = true;
                    
                    Vector3 pointerHit = GetDragWorldPosition();
                    // Z축 오프셋을 주면 UI 스케일에 따라 카메라 뒤로 넘어가버릴 수 있으므로 
                    // 잡았을 때 사라지는 버그를 막기 위해 오프셋을 제거합니다.
                    _dragOffset = transform.position - pointerHit;

                    OnDragStarted?.Invoke(this);
                }
            }
            else
            {
                OnClicked?.Invoke(this);
            }
        }

        private void OnMouseDrag()
        {
            if (!_interactable || !_isDragging) return;

            Vector3 worldPos = GetDragWorldPosition() + _dragOffset;
            transform.position = worldPos;
            OnDragging?.Invoke(this, worldPos);
        }

        private void OnMouseUp()
        {
            if (!_interactable || !_isDragging) return;

            _isDragging = false;
            Vector3 worldPos = GetDragWorldPosition() + _dragOffset;
            OnDropped?.Invoke(this, worldPos);
        }

        // ─── ICardView 구현 ───────────────────────────────────────

        public void Initialize(CardDataSO data)
        {
            Data = data;
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (_col != null) _col.enabled = interactable;
        }

        public void SetSelected(bool selected) { /* 드래그 방식으로 전환 — 미사용 */ }

        public void SetBlurLevel(float normalized)
        {
            if (_meshRenderer == null) return;
            var color = _meshRenderer.material.color;
            color.a = Mathf.Lerp(1f, 0.2f, normalized);
            _meshRenderer.material.color = color;
        }

        public void ClearClickListeners() => OnClicked = null;

        public void SetFaceUp()
            => transform.localRotation = FaceUpRotation;

        public void SetFaceDown()
            => transform.localRotation = FaceDownRotation;

        // ─── 드래그 공개 API ──────────────────────────────────────

        /// <summary>조커 카드가 손패에 왔을 때 드래그를 허용합니다.</summary>
        public void EnableDrag() => _isDragEnabled = true;

        /// <summary>배치 완료 또는 턴 종료 시 드래그를 비활성합니다.</summary>
        public void DisableDrag() => _isDragEnabled = false;

        // ─── 내부 헬퍼 ────────────────────────────────────────────

        private Vector3 GetDragWorldPosition()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (_dragPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return transform.position;
        }
    }
}