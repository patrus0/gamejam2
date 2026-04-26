using UnityEngine;
using UnityEngine.SceneManagement;

public partial class LevelManager : MonoBehaviour
{
    [Header("Настройки времени")]
    [SerializeField] private float dayDuration = 60f; // Длительность дня в секундах
    [SerializeField] private string statsSceneName = "StatsScene";

    private float timer;
    private bool isGameOver = false;

    // Синглтон для легкого доступа из других скриптов (например, из LampIndicator)
    public static LevelManager Instance;

    private void Awake()
    {
        Instance = this;
        GameSessionData.TotalPenalties = 0; // Сброс при старте уровня
    }

    private void Update()
    {
        if (isGameOver) return;

        if (timer < dayDuration)
        {
            timer += Time.deltaTime;
        }
        else
        {
            EndDay();
        }
    }

    public void AddPenalty()
    {
        GameSessionData.TotalPenalties++;
        Debug.Log($"Штраф! Всего: {GameSessionData.TotalPenalties}");
    }

    private void EndDay()
    {
        isGameOver = true;
        Debug.Log("День окончен. Переход к статистике...");
        SceneManager.LoadScene(statsSceneName);
    }
}