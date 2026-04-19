using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteTest : MonoBehaviour
{
    [Range(0f, 100f)]
    public float betAmount = 0f;
    public float maxBet = 100f;

    private Vignette _vignette;

    void Start()
    {
        Volume volume = Camera.main.GetComponentInChildren<Volume>();

        if (volume == null)
        {
            return;
        }

        volume.profile.TryGet(out _vignette);
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