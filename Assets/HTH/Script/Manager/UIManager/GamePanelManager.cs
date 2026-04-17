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

        /// <summary>
        /// Hit / Stay 버튼을 활성화합니다.
        /// 재도전 또는 다음 스테이지 시작 시 호출합니다.
        /// </summary>
        public void EnableButtons()
        {
            _hitButton.interactable = true;
            _stayButton.interactable = true;
        }

        /// <summary>
        /// Hit / Stay 버튼을 비활성화합니다.
        /// 버스트 또는 Stand 후 입력을 차단할 때 호출합니다.
        /// </summary>
        public void DisableButtons()
        {
            _hitButton.interactable = false;
            _stayButton.interactable = false;
        }
    }
}