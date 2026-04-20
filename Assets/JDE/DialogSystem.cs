using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class DialogSystem : MonoBehaviour
{
    [SerializeField]
    private Speaker speaker;
    [SerializeField]
    private DialogData[] dialogs;
    [SerializeField]
    private int currentDialogIndex = -1;
    [SerializeField]
    private bool isAutoStart = true;
    
    private bool isFirst = true;
    
    [Header("Typing Effect Settings")]
    [SerializeField] private bool useTypingEffect = true;
    [SerializeField] private float typingSpeed = 0.05f;
    
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        Setup();
    }

    private void Setup()
    {
        //대사 관련 UI모두 비활성화
        SetActiveObjects(speaker, false);
    }

    // 외부(게임매니저 등)에서 새로운 다이얼로그 배열을 주입하고 시작할 때 호출할 수 있는 메서드
    public void StartNewDialogs(DialogData[] newDialogs)
    {
        dialogs = newDialogs;
        currentDialogIndex = -1;
        isFirst = true;
    }

    public bool UpdateDialog()
    {
        if (isFirst)
        {
            Setup();
            currentDialogIndex = -1;
            
            if (isAutoStart) 
            {
                SetNextDialog();
            }
            isFirst = false;
            return false; // 첫 프레임에는 입력을 처리하지 않음 (즉시 넘어감 방지)
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // 타이핑 연출 중 클릭 시 타이핑 스킵 후 전체 텍스트 즉시 출력
                CompleteTyping();
            }
            else
            {
                // 대사가 남아있을 경우 다음 대사 진행
                if (dialogs != null && dialogs.Length > currentDialogIndex + 1)
                {
                    SetNextDialog();
                }
                // 대사가 더 이상 없을 경우 모든 오브젝트를 비활성화 후 true 반환
                else
                {
                    SetActiveObjects(speaker, false);
                    isFirst = true; // 다음 스테이지를 위해 초기화
                    return true;
                }
            }
        }

        return false;
    }

    private void SetNextDialog()
    {
        currentDialogIndex++;
        SetActiveObjects(speaker, true);
        speaker.objectArrow.SetActive(false); // 텍스트가 다 나오기 전까지 화살표 숨김

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        if (useTypingEffect)
        {
            typingCoroutine = StartCoroutine(TypeText(dialogs[currentDialogIndex].dialogue));
        }
        else
        {
            CompleteTyping();
        }
    }

    private void SetActiveObjects(Speaker speaker, bool visible)
    {
        if (speaker.textDialog != null)
            speaker.textDialog.gameObject.SetActive(visible);
        
        if (speaker.objectArrow != null)
            speaker.objectArrow.SetActive(false);
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        speaker.textDialog.text = "";

        foreach (char c in text.ToCharArray())
        {
            speaker.textDialog.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        CompleteTyping();
    }

    private void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogs != null && currentDialogIndex >= 0 && currentDialogIndex < dialogs.Length)
        {
            speaker.textDialog.text = dialogs[currentDialogIndex].dialogue;
        }

        if (speaker.objectArrow != null)
            speaker.objectArrow.SetActive(true);
            
        isTyping = false;
    }
}


[System.Serializable]
public struct Speaker
{
    public TextMeshProUGUI textDialog;
    public GameObject objectArrow; //대사 완료시 출력되는 커서 오브젝트
}

[System.Serializable]
public struct DialogData
{
    public Speaker speaker;
    public string dialogue;
}