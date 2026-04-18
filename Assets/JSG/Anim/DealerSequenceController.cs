using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class DealerSequenceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform deckTransform;
    [SerializeField] private Transform deckRestPoint;
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private Transform cardSpawnPoint;
    [SerializeField] private GameObject cardVisualPrefab;

    [Header("Deal Targets")]
    [SerializeField] private Transform[] playerCardPoints;
    [SerializeField] private Transform[] dealerCardPoints;

    [Header("Animator Triggers")]
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string drawTrigger = "Draw";

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip drawClip;

    [Header("Sequence Timing")]
    [Min(1)]
    [SerializeField] private int cardsPerSide = 2;
    [SerializeField] private float pickupAttachDelay = 0.2f;
    [SerializeField] private float delayBeforeFirstDeal = 0.15f;
    [SerializeField] private float delayBetweenDeals = 0.45f;
    [SerializeField] private float putDownDetachDelay = 2.4f;
    [SerializeField] private float cardMoveDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private KeyCode testKey = KeyCode.Space;

    [Header("Events")]
    [SerializeField] private UnityEvent onSequenceStarted;
    [SerializeField] private UnityEvent onPlayerCardDealt;
    [SerializeField] private UnityEvent onDealerCardDealt;
    [SerializeField] private UnityEvent onSequenceCompleted;

    private readonly List<GameObject> spawnedCards = new List<GameObject>();
    private Coroutine sequenceCoroutine;
    private Vector3 deckInitialLocalPosition;
    private Quaternion deckInitialLocalRotation;
    private Transform deckOriginalParent;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        CacheDeckRestPose();
    }

    private void Start()
    {
        SnapDeckToRestPoint();

        if (playOnStart)
        {
            PlaySequence();
        }
    }

    private void Update()
    {
        if (playOnStart || testKey == KeyCode.None)
        {
            return;
        }

        if (Input.GetKeyDown(testKey))
        {
            PlaySequence();
        }
    }

    public void PlaySequence()
    {
        if (isPlaying)
        {
            return;
        }

        if (!ValidateSetup())
        {
            return;
        }

        sequenceCoroutine = StartCoroutine(PlaySequenceRoutine());
    }

    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        isPlaying = false;
        SnapDeckToRestPoint();
        SetIdle();
    }

    public void ClearSpawnedCards()
    {
        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            if (spawnedCards[i] != null)
            {
                Destroy(spawnedCards[i]);
            }
        }

        spawnedCards.Clear();
    }

    private IEnumerator PlaySequenceRoutine()
    {
        isPlaying = true;
        onSequenceStarted?.Invoke();

        FireTrigger(drawTrigger);

        if (pickupAttachDelay > 0f)
        {
            yield return new WaitForSeconds(pickupAttachDelay);
        }

        AttachDeckToHand();

        if (delayBeforeFirstDeal > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFirstDeal);
        }

        for (int i = 0; i < cardsPerSide; i++)
        {
            yield return DealSingleCard(playerCardPoints, i, true);
            yield return DealSingleCard(dealerCardPoints, i, false);
        }

        int totalDeals = cardsPerSide * 2;
        float singleDealDuration = cardVisualPrefab != null ? cardMoveDuration + delayBetweenDeals : delayBetweenDeals;
        float sequenceElapsed = pickupAttachDelay + delayBeforeFirstDeal + (totalDeals * singleDealDuration);
        float waitUntilPutDown = putDownDetachDelay - sequenceElapsed;
        if (waitUntilPutDown > 0f)
        {
            yield return new WaitForSeconds(waitUntilPutDown);
        }

        DetachDeckToFloor();

        float remainingAnimationTime = GetClipDuration(drawClip) - putDownDetachDelay;
        if (remainingAnimationTime > 0f)
        {
            yield return new WaitForSeconds(remainingAnimationTime);
        }

        SetIdle();
        isPlaying = false;
        sequenceCoroutine = null;
        onSequenceCompleted?.Invoke();
    }

    private IEnumerator DealSingleCard(Transform[] targetPoints, int index, bool isPlayerCard)
    {
        Transform targetPoint = GetTargetPoint(targetPoints, index);

        if (targetPoint == null)
        {
            yield break;
        }

        if (cardVisualPrefab != null)
        {
            yield return MoveSpawnedCard(targetPoint);
        }

        if (isPlayerCard)
        {
            onPlayerCardDealt?.Invoke();
        }
        else
        {
            onDealerCardDealt?.Invoke();
        }

        if (delayBetweenDeals > 0f)
        {
            yield return new WaitForSeconds(delayBetweenDeals);
        }
    }

    private IEnumerator MoveSpawnedCard(Transform targetPoint)
    {
        Vector3 spawnPosition = cardSpawnPoint != null
            ? cardSpawnPoint.position
            : (rightHandAnchor != null ? rightHandAnchor.position : deckTransform.position);

        Quaternion spawnRotation = cardSpawnPoint != null
            ? cardSpawnPoint.rotation
            : (rightHandAnchor != null ? rightHandAnchor.rotation : deckTransform.rotation);

        GameObject spawnedCard = Instantiate(cardVisualPrefab, spawnPosition, spawnRotation);
        spawnedCards.Add(spawnedCard);

        float elapsed = 0f;
        Vector3 startPosition = spawnedCard.transform.position;
        Quaternion startRotation = spawnedCard.transform.rotation;

        while (elapsed < cardMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(cardMoveDuration, 0.0001f));
            spawnedCard.transform.position = Vector3.Lerp(startPosition, targetPoint.position, t);
            spawnedCard.transform.rotation = Quaternion.Slerp(startRotation, targetPoint.rotation, t);
            yield return null;
        }

        spawnedCard.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
        spawnedCard.transform.SetParent(targetPoint, true);
    }

    private void AttachDeckToHand()
    {
        if (deckTransform == null || rightHandAnchor == null)
        {
            return;
        }

        deckTransform.SetParent(rightHandAnchor, true);
        deckTransform.localPosition = Vector3.zero;
        deckTransform.localRotation = Quaternion.identity;
    }

    private void DetachDeckToFloor()
    {
        if (deckTransform == null)
        {
            return;
        }

        deckTransform.SetParent(deckOriginalParent, true);
        SnapDeckToRestPoint();
    }

    private void SnapDeckToRestPoint()
    {
        if (deckTransform == null)
        {
            return;
        }

        if (deckRestPoint != null)
        {
            deckTransform.SetParent(deckRestPoint.parent, true);
            deckTransform.SetPositionAndRotation(deckRestPoint.position, deckRestPoint.rotation);
            return;
        }

        if (deckOriginalParent != null)
        {
            deckTransform.SetParent(deckOriginalParent, true);
        }

        deckTransform.localPosition = deckInitialLocalPosition;
        deckTransform.localRotation = deckInitialLocalRotation;
    }

    private void CacheDeckRestPose()
    {
        if (deckTransform == null)
        {
            return;
        }

        deckOriginalParent = deckTransform.parent;
        deckInitialLocalPosition = deckTransform.localPosition;
        deckInitialLocalRotation = deckTransform.localRotation;
    }

    private void SetIdle()
    {
        if (!string.IsNullOrWhiteSpace(idleTrigger))
        {
            FireTrigger(idleTrigger);
        }
    }

    private void FireTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    private float GetClipDuration(AnimationClip clip)
    {
        return clip != null ? clip.length : 0f;
    }

    private Transform GetTargetPoint(Transform[] points, int index)
    {
        if (points == null || points.Length == 0)
        {
            return null;
        }

        if (index < points.Length && points[index] != null)
        {
            return points[index];
        }

        return points[points.Length - 1];
    }

    private bool ValidateSetup()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{nameof(DealerSequenceController)}: Animator reference is missing.", this);
            return false;
        }

        if (deckTransform == null)
        {
            Debug.LogWarning($"{nameof(DealerSequenceController)}: Deck Transform reference is missing.", this);
            return false;
        }

        if (rightHandAnchor == null)
        {
            Debug.LogWarning($"{nameof(DealerSequenceController)}: Right hand anchor reference is missing.", this);
            return false;
        }

        if ((playerCardPoints == null || playerCardPoints.Length == 0) &&
            (dealerCardPoints == null || dealerCardPoints.Length == 0))
        {
            Debug.LogWarning($"{nameof(DealerSequenceController)}: At least one deal target is required.", this);
            return false;
        }

        return true;
    }
}
