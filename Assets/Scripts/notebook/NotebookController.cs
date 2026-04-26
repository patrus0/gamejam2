using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("Источник данных")]
    [SerializeField] private bool useCallManagerCustomers = true;
    [SerializeField] private CallManager callManager;
    [SerializeField] private string notebookLineFormat = "{0} -> {1}";

    [Header("Кнопки навигации")]
    public Button nextButton; // Перетащи сюда правую кнопку
    public Button backButton; // Перетащи сюда левую кнопку

    [Header("Звук")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pageFlipClip;
    [SerializeField] private AudioClip notebookOpenClip;

    private int currentPage = 0;
    private Vector2 targetPos;
    private bool isFullyOpen = false;

    void Start()
    {
        if (useCallManagerCustomers)
            RebuildPagesFromCallManager();

        targetPos = hiddenPos;
        panel.anchoredPosition = hiddenPos;
        SetTextForAll(string.Empty);

        // Скрываем кнопки изначально
        UpdateButtons();
    }

    public void RebuildPagesFromCallManager()
    {
        if (callManager == null)
            callManager = FindFirstObjectByType<CallManager>();
        if (callManager == null || callManager.Customers == null)
            return;

        int linesPerPage = Mathf.Max(1, noteTexts != null && noteTexts.Length > 0 ? noteTexts.Length : 1);
        List<string> lines = new List<string>();

        for (int i = 0; i < callManager.Customers.Count; i++)
        {
            CustomerData customer = callManager.Customers[i];
            string fullName = string.IsNullOrWhiteSpace(customer.fullName) ? "Unknown" : customer.fullName;
            string socket = string.IsNullOrWhiteSpace(customer.targetSocket) ? "-" : customer.targetSocket;
            lines.Add(string.Format(notebookLineFormat, fullName, socket));
        }

        int pageCount = Mathf.Max(1, Mathf.CeilToInt(lines.Count / (float)linesPerPage));
        NotebookPage[] generated = new NotebookPage[pageCount];

        int lineIndex = 0;
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var page = new NotebookPage();
            page.texts = new string[linesPerPage];
            for (int lineInPage = 0; lineInPage < linesPerPage; lineInPage++)
            {
                page.texts[lineInPage] = lineIndex < lines.Count ? lines[lineIndex] : string.Empty;
                lineIndex++;
            }
            generated[pageIndex] = page;
        }

        pages = generated;
        currentPage = 0;
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
            PlayPageFlipSound();
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
            PlayPageFlipSound();
        }
    }

    void PlayPageFlipSound()
    {
        if (pageFlipClip == null)
            return;

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null)
            sfxSource.PlayOneShot(pageFlipClip);
    }

    void PlayNotebookOpenSound()
    {
        if (notebookOpenClip == null)
            return;

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null)
            sfxSource.PlayOneShot(notebookOpenClip);
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
        bool wasHidden = targetPos != visiblePos;
        targetPos = visiblePos;

        if (wasHidden)
            PlayNotebookOpenSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = hiddenPos;
        isFullyOpen = false;
        SetTextForAll(string.Empty);
        UpdateButtons(); // Скроет кнопки, когда блокнот уезжает
    }
}

