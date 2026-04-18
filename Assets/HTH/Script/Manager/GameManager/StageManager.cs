using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 스테이지 로드 및 진행을 담당합니다.
    /// GameManager가 스테이지 전환 결과만 전달하면
    /// 인덱스 관리와 SO 로드는 이 클래스가 처리합니다.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        // ─── 프로퍼티 ─────────────────────────────────────────────
        public StageDataSO CurrentStage { get; private set; }

        // ─── 의존성 ───────────────────────────────────────────────
        private StageRegistrySO _stageRegistry;
        public int StageIndex { get; private set; }
        public int StageCount => _stageRegistry?.StageCount ?? 0;
        public bool IsLastStage => StageIndex >= StageCount - 1;

        /// <summary>
        /// StageRegistrySO를 주입합니다.
        /// GameManager.Awake에서 호출합니다.
        /// </summary>
        public void Initialize(StageRegistrySO registry)
        {
            _stageRegistry = registry;
        }

        // ─── API ──────────────────────────────────────────────────

        /// <summary>스테이지 인덱스를 0으로 초기화하고 첫 스테이지를 로드합니다.</summary>
        public bool ResetAndLoad()
        {
            StageIndex = 0;
            return LoadCurrent();
        }

        /// <summary>다음 스테이지로 이동하고 로드합니다.</summary>
        public bool LoadNext()
        {
            StageIndex++;
            return LoadCurrent();
        }

        /// <summary>현재 스테이지를 다시 로드합니다. (재도전)</summary>
        public bool ReloadCurrent() => LoadCurrent();

        /// <summary>현재 인덱스의 StageDataSO를 로드합니다.</summary>
        private bool LoadCurrent()
        {
            CurrentStage = _stageRegistry.GetStage(StageIndex);
            if (CurrentStage == null)
            {
                Debug.LogError("[StageManager] StageDataSO 로드 실패");
                return false;
            }
            Debug.Log($"[StageManager] Stage {CurrentStage.stageIndex} 로드 — " +
                      $"quota:{CurrentStage.bustValue:N0}");
            return true;
        }
    }
}
