using UnityEngine;
using UnityEngine.UI; // Для работы с Image
using TMPro;         // Для работы с текстом (TextMeshPro)
using UnityEngine.SceneManagement; // Для перезагрузки уровня

public class StatsUIController : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private TextMeshProUGUI penaltyCountText; // Ссылка на текст штрафов
    [SerializeField] private Image resultImage;              // Ссылка на картинку (Прошел/Нет)

    [Header("Настройки результата")]
    [SerializeField] private int maxAllowedPenalties = 3;    // Лимит штрафов
    [SerializeField] private Sprite passSprite;              // Картинка "ПРОШЕЛ"
    [SerializeField] private Sprite failSprite;              // Картинка "ПРОВАЛ"

    private void Start()
    {
        // Когда сцена загрузилась, берем данные из нашего "хранилища"
        int total = GameSessionData.TotalPenalties;
        
        // Выводим текст
        if (penaltyCountText != null)
            penaltyCountText.text = "Штрафы: " + total.ToString();

        // Проверяем результат
        if (total <= maxAllowedPenalties)
        {
            resultImage.sprite = passSprite;
        }
        else
        {
            resultImage.sprite = failSprite;
        }
    }

    // Эти методы назначим на кнопки позже
    public void RestartLevel()
    {
        SceneManager.LoadScene("GameLevel"); // Замени на имя своей игровой сцены
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");  // Замени на имя сцены меню
    }
}