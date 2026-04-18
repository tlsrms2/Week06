using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationTriggerController : MonoBehaviour
{
    private Animator animator;

    [Header("각 애니메이션별 이동 위치 설정 (1번~6번)")]
    [Tooltip("인스펙터에서 각 번호에 맞는 X, Y, Z 좌표를 설정해주세요.")]
    public Vector3[] targetPositions = new Vector3[6];

    void Start()
    {
        // 해당 게임 오브젝트에 부착된 Animator 컴포넌트를 가져옵니다.
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 키보드 상단의 숫자 1~6 입력 감지
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayAnimationAndMove(0, "Anim1", 1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayAnimationAndMove(1, "Anim2", 2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayAnimationAndMove(2, "Anim3", 3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayAnimationAndMove(3, "Anim4", 4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            PlayAnimationAndMove(4, "Anim5", 5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            PlayAnimationAndMove(5, "Anim6", 6);
        }
    }

    // 애니메이션 실행과 위치 이동을 동시에 처리하는 함수
    private void PlayAnimationAndMove(int positionIndex, string triggerName, int animNumber)
    {
        // 1. 애니메이션 트리거 실행
        animator.SetTrigger(triggerName);
        
        // 2. 인스펙터에서 설정한 위치로 캐릭터 이동
        // 배열의 길이를 체크하여 에러를 방지합니다.
        if (positionIndex < targetPositions.Length)
        {
            transform.position = targetPositions[positionIndex];
        }

        Debug.Log($"{animNumber}번 애니메이션 실행 및 위치 이동 완료");
    }
}