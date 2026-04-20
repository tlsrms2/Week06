using UnityEngine;
using DG.Tweening;
using System.Collections;
using System;

namespace HTH
{
    /// <summary>
    /// 스테이지 시작, 승/패에 따른 눈알 프리팹 스폰 및 이동 연출을 관리합니다.
    /// </summary>
    public class EyeSpawnManager : MonoBehaviour
    {
        [Header("프리팹 및 위치 설정")]
        [Tooltip("생성할 눈알 프리팹")]
        [SerializeField] private GameObject _eyePrefab;
        
        [Tooltip("눈알이 최초 생성될 스폰 위치")]
        [SerializeField] private Transform _spawnPoint;
        
        [Tooltip("스테이지 시작 시 이동할 목표 위치")]
        [SerializeField] private Transform _stagePoint;
        
        [Tooltip("스테이지 패배 시 이동할 목표 위치")]
        [SerializeField] private Transform _losePoint;

        [Header("이동 및 대기 시간 설정")]
        [Tooltip("스폰 지점에서 스테이지 목표 위치로 이동하는 시간")]
        [SerializeField] private float _moveToStageDuration = 2f;
        
        [Tooltip("승리 시 다시 스폰 지점으로 되돌아가는 시간")]
        [SerializeField] private float _moveBackDuration = 2f;
        
        [Tooltip("패배 시 패배 목표 위치로 이동하는 시간")]
        [SerializeField] private float _moveToLoseDuration = 1.5f;
        
        [Tooltip("패배 지점 도착 후 파괴될 때까지의 대기 시간")]
        [SerializeField] private float _destroyWaitAfterLose = 2f;

        [Tooltip("눈알이 눌려 터지는 연출 시간")]
        [SerializeField] private float _squashDuration = 0.2f;

        [Header("파티클 설정")]
        [Tooltip("눈알이 파괴될 때 생성될 파티클 프리팹")]
        [SerializeField] private GameObject _breakParticlePrefab;

        [Header("딜러 설정")]
        [Tooltip("딜러 트랜스폼 (위치 조절용)")]
        [SerializeField] private Transform _dealerTransform;
        
        [Tooltip("패배 애니메이션 시작 시 딜러가 위치할 지점")]
        [SerializeField] private Vector3 _dealerLosePoint;

        [Tooltip("딜러 애니메이터")]
        [SerializeField] private Animator _dealerAnimator;
        
        [Tooltip("패배 시 재생할 애니메이션 파라미터(Trigger) 이름")]
        [SerializeField] private string _dealerLoseAnimTrigger = "Trigger7";

        private GameObject _currentEye;

        /// <summary>
        /// 스테이지 시작 시 호출. 스폰 포인트에 프리팹을 생성하고 목표 지점으로 이동시킵니다.
        /// </summary>
        public void SpawnAndMoveToStage()
        {
            if (_eyePrefab == null || _spawnPoint == null || _stagePoint == null) return;
            
            // 기존 눈알이 남아있다면 파괴
            if (_currentEye != null) 
            {
                _currentEye.transform.DOKill();
                Destroy(_currentEye);
            }

            _currentEye = Instantiate(_eyePrefab, _spawnPoint.position, _spawnPoint.rotation);
            
            _currentEye.transform.DOMove(_stagePoint.position, _moveToStageDuration).SetEase(Ease.InOutQuad);
        }

        /// <summary>
        /// 스테이지 승리 시 호출. 다시 스폰 지점으로 이동한 뒤 파괴됩니다.
        /// </summary>
        public void OnWin(Action onComplete = null)
        {
            if (_currentEye == null || _spawnPoint == null) 
            {
                onComplete?.Invoke();
                return;
            }

            _currentEye.transform.DOMove(_spawnPoint.position, _moveBackDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    if (_currentEye != null) Destroy(_currentEye);
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 스테이지 패배 시 호출. 정해진 지점으로 이동, 몇 초 대기 후 파괴되며 딜러 애니메이션을 재생합니다.
        /// </summary>
        public void OnLose(Action onComplete = null)
        {
            if (_currentEye == null || _losePoint == null)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(LoseRoutine(onComplete));
        }

        private IEnumerator LoseRoutine(Action onComplete)
        {
            // 1. 패배 지점으로 이동
            _currentEye.transform.DOMove(_losePoint.position, _moveToLoseDuration).SetEase(Ease.InOutQuad);
            yield return new WaitForSeconds(_moveToLoseDuration);

            // 딜러 위치 설정 및 애니메이션 재생 시작
            if (_dealerTransform != null && _dealerLosePoint != Vector3.zero)
            {
                _dealerTransform.position = _dealerLosePoint;
            }

            if (_dealerAnimator != null)
            {
                _dealerAnimator.SetTrigger(_dealerLoseAnimTrigger);
            }

            // 2. 지정된 시간만큼 먼저 대기 (딜러가 동작을 취할 시간 등)
            yield return new WaitForSeconds(_destroyWaitAfterLose);

            // 3. Lerp를 사용하여 눈알이 서서히 눌려 터지는 연출
            if (_currentEye != null)
            {
                float elapsed = 0f;
                Vector3 startScale = _currentEye.transform.localScale;
                Vector3 targetScale = new Vector3(startScale.x * 1.5f, 0.05f, startScale.z * 1.5f);

                while (elapsed < _squashDuration)
                {
                    if (_currentEye == null) break;

                    elapsed += Time.deltaTime;
                    float t = elapsed / _squashDuration;
                    _currentEye.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                    yield return null;
                }
            }

            if (_currentEye != null && _breakParticlePrefab != null)
            {
                Instantiate(_breakParticlePrefab, _currentEye.transform.position, Quaternion.identity);
            }

            // 4. 최종 파괴
            if (_currentEye != null)
            {
                _currentEye.transform.DOKill();
                Destroy(_currentEye);
            }

            // 5. 완료 콜백 (다음 스테이지/재시작 로직 진행)
            onComplete?.Invoke();
        }
    }
}
