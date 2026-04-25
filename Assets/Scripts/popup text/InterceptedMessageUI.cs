using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InterceptedMessageUI : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private TMP_Text messageText;

    [Header("Behavior")]
    [SerializeField] private string fallbackMessage = "Signal intercepted, but message is unreadable.";
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool clearTextOnStart = true;
    [SerializeField, Min(0f)] private float firstMessageDelay = 1f;
    [SerializeField, Min(0.1f)] private float messageInterval = 5f;
    [SerializeField, Min(0f)] private float messageLifetime = 3f;
    [SerializeField] private bool avoidImmediateRepeat = true;

    [Header("Source file")]
    [Tooltip("Текстовый файл с фразами. Одна фраза = одна строка.")]
    [SerializeField] private TextAsset messageFile;

    private Coroutine messageLoopRoutine;
    private Coroutine clearTextRoutine;
    private readonly List<string> loadedMessages = new List<string>();
    private int lastMessageIndex = -1;

    private void Awake()
    {
        if (messageText == null)
        {
            Debug.LogWarning("InterceptedMessageUI is missing messageText reference.", this);
            enabled = false;
            return;
        }

        if (clearTextOnStart)
            messageText.text = string.Empty;

        ReloadMessagesFromFile();
    }

    private void OnEnable()
    {
        if (autoStart)
            StartMessageLoop();
    }

    private void OnDisable()
    {
        StopMessageLoop();
        if (clearTextRoutine != null)
        {
            StopCoroutine(clearTextRoutine);
            clearTextRoutine = null;
        }
    }

    /// <summary>
    /// Перечитывает фразы из TextAsset.
    /// Формат: одна фраза на строку.
    /// </summary>
    public void ReloadMessagesFromFile()
    {
        loadedMessages.Clear();
        lastMessageIndex = -1;

        if (messageFile == null || string.IsNullOrWhiteSpace(messageFile.text))
            return;

        string[] lines = messageFile.text.Split(new[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string candidate = lines[i].Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
                loadedMessages.Add(candidate);
        }
    }

    public void StartMessageLoop()
    {
        if (messageLoopRoutine != null)
            return;

        messageLoopRoutine = StartCoroutine(MessageLoop());
    }

    public void StopMessageLoop()
    {
        if (messageLoopRoutine == null)
            return;

        StopCoroutine(messageLoopRoutine);
        messageLoopRoutine = null;
    }

    public void ShowRandomMessageNow()
    {
        string randomMessage = GetRandomMessage();
        ShowMessage(randomMessage);
    }

    public void HidePanel(bool instant = false)
    {
        // Панель статична: метод оставлен для совместимости со старыми вызовами.
        if (clearTextRoutine != null)
        {
            StopCoroutine(clearTextRoutine);
            clearTextRoutine = null;
        }

        if (messageText != null)
            messageText.text = string.Empty;
    }

    public void ShowMessage(string message)
    {
        if (messageText == null) return;

        messageText.text = string.IsNullOrWhiteSpace(message) ? fallbackMessage : message;

        if (clearTextRoutine != null)
        {
            StopCoroutine(clearTextRoutine);
            clearTextRoutine = null;
        }

        if (messageLifetime > 0f)
        {
            clearTextRoutine = StartCoroutine(ClearTextAfterDelay(messageLifetime));
        }
    }

    private string GetRandomMessage()
    {
        if (loadedMessages.Count == 0)
            return fallbackMessage;

        int selectedIndex = Random.Range(0, loadedMessages.Count);
        if (avoidImmediateRepeat && loadedMessages.Count > 1 && selectedIndex == lastMessageIndex)
        {
            selectedIndex = (selectedIndex + 1) % loadedMessages.Count;
        }

        lastMessageIndex = selectedIndex;
        return loadedMessages[selectedIndex];
    }

    private IEnumerator MessageLoop()
    {
        if (firstMessageDelay > 0f)
            yield return new WaitForSeconds(firstMessageDelay);

        while (true)
        {
            ShowRandomMessageNow();
            if (messageInterval > 0f)
                yield return new WaitForSeconds(messageInterval);
            else
                yield return null;
        }
    }

    private IEnumerator ClearTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messageText != null)
            messageText.text = string.Empty;
        clearTextRoutine = null;
    }
}
