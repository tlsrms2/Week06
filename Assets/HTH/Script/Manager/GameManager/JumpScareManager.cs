using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace HTH
{
    /// <summary>
    /// 플레이어의 시야 상태에 따라 딜러를 숨기거나 점프스케어를 실행합니다.
    /// </summary>
    public class JumpScareManager : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("현재 씬에 있는 실제 딜러 객체")]
        [SerializeField] private GameObject _realDealer;
        [Tooltip("딜러를 복제할 때 사용할 프리팹 (점프스케어용)")]
        [SerializeField] private GameObject _dealerPrefab;

        [Header("1단계: 숨기 (시야 20 이하)")]
        [Tooltip("딜러가 숨을 위치")]
        [SerializeField] private Transform _hidingPoint;
        [Tooltip("숨는 위치로 이동하는 시간")]
        [SerializeField] private float _hideMoveDuration = 5f;
        [SerializeField] private int _hideThreshold = 20;

        [Header("2단계: 점프스케어 1 (시야 0)")]
        [Tooltip("복제된 딜러가 나타날 시작 위치")]
        [SerializeField] private Transform _js1StartPoint;
        [Tooltip("위로 올라갈 높이")]
        [SerializeField] private float _js1UpAmount = 2f;
        [Tooltip("올라가는 시간")]
        [SerializeField] private float _js1Duration = 3f;

        private bool _isHiding = false;
        private bool _isScareTriggered = false;

        private void OnEnable()
        {
            if (VisionManager.Instance != null)
            {
                VisionManager.Instance.OnVisionChanged += CheckVisionForHiding;
                VisionManager.Instance.OnVisionDepleted += TriggerRandomJumpScare;
            }
        }

        private void OnDisable()
        {
            if (VisionManager.Instance != null)
            {
                VisionManager.Instance.OnVisionChanged -= CheckVisionForHiding;
                VisionManager.Instance.OnVisionDepleted -= TriggerRandomJumpScare;
            }
        }

        /// <summary>
        /// 시야가 20 이하로 떨어지면 딜러를 정해진 위치로 숨깁니다.
        /// </summary>
        private void CheckVisionForHiding(int currentVision, int maxVision)
        {
            if (!_isHiding && currentVision <= _hideThreshold && currentVision > 0)
            {
                _isHiding = true;
                StartCoroutine(MoveDealerToHidingPoint());
            }
        }

        private IEnumerator MoveDealerToHidingPoint()
        {
            if (_realDealer == null || _hidingPoint == null) yield break;

            Debug.Log("[JumpScare] 딜러가 숨을 위치로 이동을 시작합니다.");

            // 애니메이터가 위치를 고정시키고 있을 수 있으므로 처리
            Animator anim = _realDealer.GetComponent<Animator>();
            if (anim != null) anim.enabled = false; 

            _realDealer.transform.DOKill();
            _realDealer.transform.DOMove(_hidingPoint.position, _hideMoveDuration).SetEase(Ease.Linear);
            _realDealer.transform.DORotateQuaternion(_hidingPoint.rotation, _hideMoveDuration).SetEase(Ease.Linear);

            yield return new WaitForSeconds(_hideMoveDuration);
            
            if (anim != null) anim.enabled = true;
        }

        /// <summary>
        /// 시야가 0이 되었을 때 실행할 랜덤 점프스케어를 결정합니다.
        /// </summary>
        private void TriggerRandomJumpScare()
        {
            if (_isScareTriggered) return;
            _isScareTriggered = true;

            // 현재는 첫 번째 점프스케어만 구현됨 (추후 랜덤 확장 가능)
            int randomIndex = 1; 
            
            switch (randomIndex)
            {
                case 1:
                    StartCoroutine(JumpScareRoutine1());
                    break;
            }
        }

        /// <summary>
        /// 점프스케어 1: 정해진 위치에 딜러를 복제한 후 천천히 위로 올립니다.
        /// </summary>
        private IEnumerator JumpScareRoutine1()
        {
            if (_dealerPrefab == null || _js1StartPoint == null) yield break;

            Debug.Log("[JumpScare] 점프스케어 1단계 시작!");

            // 1. 딜러 복제
            GameObject clone = Instantiate(_dealerPrefab, _js1StartPoint.position, _js1StartPoint.rotation);
            
            // 2. 애니메이터 설정 (필요 시 특정 공포 애니메이션 재생)
            Animator anim = clone.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("Idle"); // 또는 공포용 상태 이름
            }

            // 3. 천천히 위로 이동
            Vector3 targetPos = _js1StartPoint.position + Vector3.up * _js1UpAmount;
            clone.transform.DOMove(targetPos, _js1Duration).SetEase(Ease.OutSine);

            yield return new WaitForSeconds(_js1Duration);
        }

        /// <summary>
        /// 게임 재시작 시 상태를 초기화합니다.
        /// </summary>
        public void ResetAll()
        {
            _isHiding = false;
            _isScareTriggered = false;
        }
    }
}
