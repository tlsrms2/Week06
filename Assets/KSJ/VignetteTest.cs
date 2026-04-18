using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteTest : MonoBehaviour
{
    [Range(0f, 10000f)]
    public float betAmount = 0f;
    public float maxBet = 10000f;

    private Vignette _vignette;

    void Start()
    {
        Volume volume = Camera.main.GetComponentInChildren<Volume>();

        if (volume == null)
        {
            Debug.LogError("Volume을 찾을 수 없습니다!");
            return;
        }

        volume.profile.TryGet(out _vignette);

        if (_vignette == null)
            Debug.LogError("Vignette를 찾을 수 없습니다!");
        else
            Debug.Log("Vignette 연결 성공!");
    }

    void Update()
    {
        if (_vignette == null) return;

        if (Input.GetKey(KeyCode.UpArrow))
            betAmount = Mathf.Min(betAmount + 100f, maxBet);
        if (Input.GetKey(KeyCode.DownArrow))
            betAmount = Mathf.Max(betAmount - 100f, 0f);

        float intensity = betAmount / maxBet;
        _vignette.intensity.Override(intensity);
    }
}