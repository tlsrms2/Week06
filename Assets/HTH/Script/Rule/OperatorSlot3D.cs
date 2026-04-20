using System;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 3D 씬에 배치되는 연산자 슬롯입니다.
    /// 카드 사이에 배치되어 연산자 카드를 배치할 수 있습니다.
    /// BoxCollider로 클릭을 감지합니다.
    /// </summary>
    public class OperatorSlot3D : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        public int SlotIndex { get; private set; }
        public bool IsOccupied { get; private set; }

        // ─── 이벤트 ──────────────────────────────────────────────
        public event Action<int> OnSlotClicked;

        // ─── 컴포넌트 ────────────────────────────────────────────
        private MeshRenderer _meshRenderer;
        private BoxCollider _col;

        // ─── 색상 ─────────────────────────────────────────────────
        private static readonly Color ColorDefault = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        private static readonly Color ColorHighlight = new Color(1f, 1f, 0f, 0.8f);
        private static readonly Color ColorOccupied = new Color(0.2f, 0.8f, 0.2f, 1f);

        // ─── 초기화 ───────────────────────────────────────────────

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _col = GetComponent<BoxCollider>();
        }

        public void Initialize(int slotIndex)
        {
            SlotIndex = slotIndex;
            IsOccupied = false;
            SetColor(ColorDefault);
        }

        private void OnMouseDown()
        {
            if (IsOccupied) return;
            OnSlotClicked?.Invoke(SlotIndex);
        }

        // ─── 상태 변경 ────────────────────────────────────────────

        public void SetHighlight(bool highlight)
        {
            if (IsOccupied) return;
            SetColor(highlight ? ColorHighlight : ColorDefault);
        }

        public void SetOccupied(OperatorType op)
        {
            IsOccupied = true;
            SetColor(ColorOccupied);
            // 추후 연산자 텍스트 표시 추가 가능
        }

        private void SetColor(Color color)
        {
            if (_meshRenderer != null)
                _meshRenderer.material.color = color;
        }
    }
}