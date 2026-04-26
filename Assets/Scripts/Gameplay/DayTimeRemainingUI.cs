using TMPro;
using UnityEngine;

public class DayTimeRemainingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private string format = "Time left: {0:00}:{1:00}";

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        DayTimer.TimeUpdated += HandleTimeUpdated;
    }

    private void OnDisable()
    {
        DayTimer.TimeUpdated -= HandleTimeUpdated;
    }

    private void HandleTimeUpdated(float remainingSeconds)
    {
        if (targetText == null)
            return;

        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
        targetText.text = string.Format(format, minutes, seconds);
    }
}
