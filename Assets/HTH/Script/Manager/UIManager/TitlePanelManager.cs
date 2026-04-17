using System;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 타이틀 패널의 버튼 콜백을 담당합니다.
    /// </summary>
    public class TitlePanelManager : MonoBehaviour
    {
        [Header("TitlePanel 요소")]
        [SerializeField] private Button _startButton;

        /// <summary>타이틀 패널을 초기화하고 시작 버튼에 콜백을 등록합니다.</summary>
        public void Setup(Action onStart)
        {
            _startButton.onClick.RemoveAllListeners();
            _startButton.onClick.AddListener(() => onStart?.Invoke());
        }
    }
}