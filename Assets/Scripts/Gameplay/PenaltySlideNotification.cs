using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PenaltySlideNotification : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Vector2 hiddenAnchoredPosition = new Vector2(420f, -120f);
    [SerializeField] private Vector2 shownAnchoredPosition = new Vector2(-40f, -120f);
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float visibleDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip penaltyAppearClip;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        panelRect.anchoredPosition = hiddenAnchoredPosition;
    }

    private void OnEnable()
    {
        Lamp.CallFailedForPin += HandlePenalty;
    }

    private void OnDisable()
    {
        Lamp.CallFailedForPin -= HandlePenalty;
    }

    private void HandlePenalty(string _)
    {
        PlayPenaltyAppearSound();

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayNotificationRoutine());
    }

    private void PlayPenaltyAppearSound()
    {
        if (penaltyAppearClip == null)
            return;

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null)
            sfxSource.PlayOneShot(penaltyAppearClip);
    }

    private IEnumerator PlayNotificationRoutine()
    {
        yield return SlideTo(shownAnchoredPosition);
        yield return new WaitForSeconds(visibleDuration);
        yield return SlideTo(hiddenAnchoredPosition);
        playRoutine = null;
    }

    private IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = panelRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            panelRect.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        panelRect.anchoredPosition = target;
    }
}
