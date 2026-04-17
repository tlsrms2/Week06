using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class CardFlip : MonoBehaviour
{
    public float flipSpeed = 0.5f;
    public float flipAngle = 180f;

    private bool _isFlipped = false;

    private void OnMouseEnter()
    {
        if (!_isFlipped)
        {
            _isFlipped = true;
            transform.DORotate(new Vector3(0, flipAngle, 0), flipSpeed, RotateMode.LocalAxisAdd)
                .OnComplete(() =>
                {
                    transform.DORotate(new Vector3(0, -flipAngle, 0), flipSpeed, RotateMode.LocalAxisAdd)
                        .OnComplete(() => _isFlipped = false);
                });
        }
    }

    public async UniTask FlipOnce()
    {
        await transform.DORotate(new Vector3(0, flipAngle, 0), flipSpeed, RotateMode.LocalAxisAdd)
            .AsyncWaitForCompletion();
    }
}