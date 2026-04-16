// 추후 구현 예정 — ICardView 구현체
using HTH;
using UnityEngine;

public class Card3DView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _frontRenderer;  // 앞면
    [SerializeField] private SpriteRenderer _backRenderer;   // 뒷면
    [SerializeField] private GameObject _middleLayer;    // 가운데 프리미티브

    public void Initialize(CardDataSO data)
    {
        // 앞면 텍스처 적용
        if (data.HasFrontTexture)
            _frontRenderer.sprite = Sprite.Create(
                data.frontTexture,
                new Rect(0, 0, data.frontTexture.width, data.frontTexture.height),
                Vector2.one * 0.5f);

        // 뒷면 텍스처 적용
        if (data.HasBackTexture)
            _backRenderer.sprite = Sprite.Create(
                data.backTexture,
                new Rect(0, 0, data.backTexture.width, data.backTexture.height),
                Vector2.one * 0.5f);

        // 가운데 프리미티브 생성
        if (data.HasMiddlePrimitive)
            Instantiate(data.middlePrimitivePrefab, transform);
    }
    // ...
}