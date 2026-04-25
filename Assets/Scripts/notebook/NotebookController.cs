using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class NotebookController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки движения")]
    public RectTransform panel;
    public Vector2 hiddenPos;
    public Vector2 visiblePos;
    public float speed = 10f;

    [Header("Текст и Страницы")]
    public TextMeshProUGUI noteText;
    [TextArea(3, 10)]
    public string[] pages;
    public float typingSpeed = 0.03f;

    [Header("Кнопки навигации")]
    public Button nextButton; // Перетащи сюда правую кнопку
    public Button backButton; // Перетащи сюда левую кнопку

    private int currentPage = 0;
    private Vector2 targetPos;
    private bool isFullyOpen = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        targetPos = hiddenPos;
        panel.anchoredPosition = hiddenPos;
        noteText.text = "";

        // Скрываем кнопки изначально
        UpdateButtons();
    }

    void Update()
    {
        panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, targetPos, Time.deltaTime * speed);

        if (Vector2.Distance(panel.anchoredPosition, visiblePos) < 1f && !isFullyOpen && targetPos == visiblePos)
        {
            isFullyOpen = true;
            ShowPage(currentPage);
        }
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    void ShowPage(int index)
    {
        UpdateButtons();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(pages[index]));
    }

    void UpdateButtons()
    {
        // Кнопка НАЗАД активна, если мы не на первой странице
        backButton.gameObject.SetActive(currentPage > 0 && isFullyOpen);

        // Кнопка ВПЕРЕД активна, если есть куда листать
        nextButton.gameObject.SetActive(currentPage < pages.Length - 1 && isFullyOpen);
    }

    System.Collections.IEnumerator TypeText(string content)
    {
        noteText.text = "";
        foreach (char letter in content.ToCharArray())
        {
            noteText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = visiblePos;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = hiddenPos;
        isFullyOpen = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        noteText.text = "";
        UpdateButtons(); // Скроет кнопки, когда блокнот уезжает
    }
}

