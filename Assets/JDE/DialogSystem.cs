using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 딜러가 스테이지 중간에 룰을 설명하는 대사 UI를 담당.
/// GameManager에서 ShowDialog(index)로 호출하고,
/// Update에서 UpdateDialog()를 매 프레임 호출하면 됩니다.
/// </summary>
public class DialogSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogDatabase database;
    [SerializeField] private TextMeshProUGUI textDialog;
    [SerializeField] private GameObject arrowObject; // 대사 완료 시 표시되는 커서

    [Header("Typing Effect")]
    [SerializeField] private bool useTypingEffect = true;
    [SerializeField] private float typingSpeed = 0.05f;

    // ── 상태 ──────────────────────────────────────
    private string[] currentLines;
    private int currentIndex = -1;

    private bool isActive   = false;
    private bool isTyping   = false;
    private bool isFirstFrame = false;

    private Coroutine typingCoroutine;

    // ── 공개 프로퍼티 ─────────────────────────────
    public bool IsActive => isActive;

    // ─────────────────────────────────────────────
    private void Awake() => HideUI();

    // ─────────────────────────────────────────────
    // 외부 호출 API
    // ─────────────────────────────────────────────

    /// <summary>
    /// 지정한 인덱스의 대사 시퀀스를 시작합니다.
    /// GameManager 등에서 호출하세요.
    /// </summary>
    public void ShowDialog(int sequenceIndex)
    {
        if (!database.TryGetSequence(sequenceIndex, out DialogSequence seq)) return;

        currentLines = seq.lines;
        currentIndex = -1;
        isActive     = true;
        isFirstFrame = true;

        ShowNextLine();
    }

    /// <summary>
    /// GameManager의 Update에서 매 프레임 호출.
    /// 대사가 완전히 끝나면 true를 반환합니다.
    /// </summary>
    public bool UpdateDialog()
    {
        if (!isActive) return false;

        // ShowDialog() 호출과 같은 프레임의 클릭 무시
        if (isFirstFrame) { isFirstFrame = false; return false; }

        if (!Input.GetMouseButtonDown(0)) return false;

        if (isTyping)
        {
            // 타이핑 중 클릭 → 전체 텍스트 즉시 표시
            CompleteTyping();
        }
        else if (currentIndex + 1 < currentLines.Length)
        {
            // 다음 대사
            ShowNextLine();
        }
        else
        {
            // 모든 대사 종료
            HideUI();
            isActive = false;
            return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────
    // 내부 로직
    // ─────────────────────────────────────────────

    private void ShowNextLine()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.S2);
        currentIndex++;
        SetArrow(false);
        textDialog.gameObject.SetActive(true);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (useTypingEffect)
            typingCoroutine = StartCoroutine(TypeText(currentLines[currentIndex]));
        else
            InstantShow(currentLines[currentIndex]);
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        textDialog.text = "";

        foreach (char c in text)
        {
            textDialog.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        CompleteTyping();
    }

    private void CompleteTyping()
    {
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        InstantShow(currentLines[currentIndex]);
    }

    private void InstantShow(string text)
    {
        textDialog.text = text;
        SetArrow(true);
        isTyping = false;
    }

    private void HideUI()
    {
        if (textDialog != null) textDialog.gameObject.SetActive(false);
        SetArrow(false);
    }

    private void SetArrow(bool visible)
    {
        if (arrowObject != null) arrowObject.SetActive(visible);
    }
}