using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class CardDealManager : MonoBehaviour
{
    public CardFlip[] cards; // 인스펙터에서 카드 5개 순서대로 드래그

    private async void Start()
    {
        await FlipCardsInOrder();
    }

    private async UniTask FlipCardsInOrder()
    {
        foreach (CardFlip card in cards)
        {
            await card.FlipOnce();
        }
    }
}