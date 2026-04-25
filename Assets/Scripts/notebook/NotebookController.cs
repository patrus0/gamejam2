using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class NotebookController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [System.Serializable]
    public class NotebookPage
    {
        [TextArea(3, 10)]
        public string[] texts;
    }

    [Header("Настройки движения")]
    public RectTransform panel;
    public Vector2 hiddenPos;
    public Vector2 visiblePos;
    public float speed = 10f;

    [Header("Текст и Страницы")]
    public TextMeshProUGUI[] noteTexts;
    public NotebookPage[] pages;

    [Header("Кнопки навигации")]
    public Button nextButton; // Перетащи сюда правую кнопку
    public Button backButton; // Перетащи сюда левую кнопку

    private int currentPage = 0;
    private Vector2 targetPos;
    private bool isFullyOpen = false;

    void Start()
    {
        targetPos = hiddenPos;
        panel.anchoredPosition = hiddenPos;
        SetTextForAll(string.Empty);

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
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    public void PrevPage()
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            SetTextForAll(string.Empty);
            UpdateButtons();
            return;
        }

        index = Mathf.Clamp(index, 0, pages.Length - 1);
        currentPage = index;
        UpdateButtons();
        SetTextForPage(pages[index]);
    }

    void UpdateButtons()
    {
        bool hasPages = pages != null && pages.Length > 0;

        // Кнопка НАЗАД активна, если мы не на первой странице
        backButton.gameObject.SetActive(hasPages && currentPage > 0 && isFullyOpen);

        // Кнопка ВПЕРЕД активна, если есть куда листать
        nextButton.gameObject.SetActive(hasPages && currentPage < pages.Length - 1 && isFullyOpen);
    }

    void SetTextForAll(string content)
    {
        if (noteTexts == null || noteTexts.Length == 0)
        {
            return;
        }

        for (int i = 0; i < noteTexts.Length; i++)
        {
            if (noteTexts[i] != null)
            {
                noteTexts[i].text = content;
            }
        }
    }

    void SetTextForPage(NotebookPage page)
    {
        if (page == null || page.texts == null)
        {
            SetTextForAll(string.Empty);
            return;
        }

        if (noteTexts == null || noteTexts.Length == 0)
        {
            return;
        }

        for (int i = 0; i < noteTexts.Length; i++)
        {
            if (noteTexts[i] == null)
            {
                continue;
            }

            if (i < page.texts.Length)
            {
                noteTexts[i].text = page.texts[i];
            }
            else
            {
                noteTexts[i].text = string.Empty;
            }
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
        SetTextForAll(string.Empty);
        UpdateButtons(); // Скроет кнопки, когда блокнот уезжает
    }
}

