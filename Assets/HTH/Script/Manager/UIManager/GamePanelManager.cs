using System;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 게임 패널의 Hit / Stay 버튼 콜백을 담당합니다.
    /// </summary>
    public class GamePanelManager : MonoBehaviour
    {
        [Header("GamePanel 요소")]
        [SerializeField] private Button _hitButton;
        [SerializeField] private Button _stayButton;

        /// <summary>
        /// 게임 패널을 초기화합니다.
        /// Hit / Stay 버튼에 콜백을 등록합니다.
        /// </summary>
        public void Setup(Action onHit, Action onStand)
        {
            _hitButton.onClick.RemoveAllListeners();
            _hitButton.onClick.AddListener(() => onHit?.Invoke());

            _stayButton.onClick.RemoveAllListeners();
            _stayButton.onClick.AddListener(() => onStand?.Invoke());
        }

        /// <summary>
        /// Hit 버튼 활성화 여부를 설정합니다.
        /// 덱이 비었을 때 비활성화합니다.
        /// </summary>
        public void SetHitInteractable(bool interactable)
            => _hitButton.interactable = interactable;

        /// <summary>
        /// Stay 버튼 활성화 여부를 설정합니다.
        /// 필드가 비어있을 때 비활성화합니다.
        /// </summary>
        public void SetStayInteractable(bool interactable)
            => _stayButton.interactable = interactable;
    }
}