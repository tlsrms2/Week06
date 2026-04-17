using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// ICardView의 UI Text 기반 임시 구현체.
    /// 3D 카드 오브젝트가 준비되면 Card3DView로 교체합니다.
    /// GameUIManager.CreateCardView()를 통해 생성됩니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public class CardUIView : MonoBehaviour, ICardView
    {
        // ─── ICardView ────────────────────────────────────────────
        public CardDataSO Data { get; private set; }
        public event Action<ICardView> OnClicked;

        // ─── 컴포넌트 참조 ────────────────────────────────────────
        private Image _image;
        private Button _button;
        private TextMeshProUGUI _label;

        private Color _defaultColor;

        // ─── 생명주기 ─────────────────────────────────────────────
        private void Awake()
        {
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();

            _button.onClick.AddListener(() => OnClicked?.Invoke(this));
        }

        // ─── ICardView 구현 ───────────────────────────────────────

        /// <summary>
        /// 카드 데이터를 받아 UI를 초기화합니다.
        /// 숫자 카드는 흰색, 연산자 카드는 금색 배경으로 표시합니다.
        /// </summary>
        public void Initialize(CardDataSO data)
        {
            Data = data;

            if (_label == null)
                _label = GetComponentInChildren<TextMeshProUGUI>();
            // 앞면 텍스처가 있으면 텍스트 숨김
            // 없으면 displayLabel 텍스트로 임시 표시
            if (data.HasFrontTexture)
            {
                // TODO: 추후 SpriteRenderer / RawImage로 텍스처 표시
                // 현재는 텍스처가 있어도 텍스트로 대체 표시
                if (_label != null) _label.gameObject.SetActive(false);
            }
            else
            {
                // 텍스처 없음 → displayLabel 텍스트로 임시 표시
                if (_label != null)
                {
                    _label.gameObject.SetActive(true);
                    _label.text = string.IsNullOrEmpty(data.displayLabel)
                        ? (data.cardType == CardType.Number
                            ? data.numberValue.ToString()
                            : "?")
                        : data.displayLabel;
                    _label.color = UIColor.CardText;
                }
                else
                {
                    Debug.LogWarning($"[CardUIView] Label이 없습니다 — {data.displayLabel}");
                }
            }

            _defaultColor = data.cardType == CardType.Number
                ? UIColor.CardNumBg
                : UIColor.CardOpBg;
            _image.color = _defaultColor;
        }

        /// <summary>클릭 가능 여부를 설정합니다.</summary>
        public void SetInteractable(bool interactable)
            => _button.interactable = interactable;

        /// <summary>
        /// 선택 상태를 시각적으로 표시합니다.
        /// 선택 시 하이라이트, 해제 시 기본 색상으로 복귀합니다.
        /// </summary>
        public void SetSelected(bool selected)
            => _image.color = selected ? UIColor.Selected : _defaultColor;

        /// <summary>
        /// 시야 감소 시 판독 난이도를 적용합니다.
        /// 현재 구현: 알파값으로 뭉개짐 표현.
        /// Card3DView에서는 Blur 셰이더로 교체합니다.
        /// </summary>
        public void SetBlurLevel(float normalized)
        {
            float alpha = Mathf.Lerp(1f, 0.2f, normalized);
            var col = _image.color;
            _image.color = new Color(col.r, col.g, col.b, alpha);

            if (_label == null) return;
            var labelCol = _label.color;
            _label.color = new Color(labelCol.r, labelCol.g, labelCol.b, alpha);
        }
        public void ClearClickListeners()
        {
            // delegate 전체 초기화
            OnClicked = null;
            // 버튼 리스너도 초기화 후 재등록
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => OnClicked?.Invoke(this));
        }
    }
}