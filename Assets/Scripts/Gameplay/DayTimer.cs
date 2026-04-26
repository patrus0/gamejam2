using UnityEngine;
using TMPro; // Используем TextMeshPro
using UnityEngine.SceneManagement;
using System;

public class DayTimer : MonoBehaviour
{
    public static event Action<float> TimeUpdated;

    [Header("Настройки времени")]
    [SerializeField] private float dayDuration = 360f; // Длительность дня в секундах
    [SerializeField] private string statsSceneName = "StatsScene";
    
    [Header("UI элементы")]
    [SerializeField] private TextMeshProUGUI timerText;

    private float currentTime;
    private bool isEnded = false;
    public float TimeRemaining => Mathf.Max(0f, currentTime);

    private void Start()
    {
        currentTime = dayDuration;
        UpdateUI();
    }

    private void Update()
    {
        if (isEnded) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateUI();
        }
        else
        {
            EndDay();
        }
    }

    void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        TimeUpdated?.Invoke(TimeRemaining);
    }

    void EndDay()
    {
        isEnded = true;
        SceneManager.LoadScene(statsSceneName);
    }
}