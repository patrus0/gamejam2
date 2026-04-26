using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class MessageSocketScoreController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InterceptedMessageUI messageUI;
    [Tooltip("Формат строк: Фамилия Имя = a5")]
    [SerializeField] private TextAsset nameToSocketMapFile;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text debugExpectedSocketText;

    [Header("Scoring")]
    [SerializeField] private int pointsForCorrect = 1;
    [SerializeField] private int pointsForWrong = 1;
    [SerializeField] private bool requireActiveMessage = true;
    [SerializeField] private bool consumeMessageAfterAttempt = true;

    [Header("Parsing")]
    [SerializeField] private bool caseInsensitiveNames = true;
    [SerializeField] private bool logWarnings = true;

    private readonly Dictionary<string, string> expectedSocketByName = new Dictionary<string, string>();

    private string activeName = string.Empty;
    private string expectedSocketForActiveName = string.Empty;
    private int score;

    public int Score => score;

    private void Awake()
    {
        if (messageUI == null)
            messageUI = FindFirstObjectByType<InterceptedMessageUI>();

        ReloadNameMap();
        RefreshScoreText();
        RefreshExpectedSocketDebugText();
    }

    private void OnEnable()
    {
        if (messageUI != null)
        {
            messageUI.MessageShown += HandleMessageShown;
            messageUI.MessageCleared += HandleMessageCleared;
        }

        Socket.PinPlugged += HandlePinPlugged;
    }

    private void OnDisable()
    {
        if (messageUI != null)
        {
            messageUI.MessageShown -= HandleMessageShown;
            messageUI.MessageCleared -= HandleMessageCleared;
        }

        Socket.PinPlugged -= HandlePinPlugged;
    }

    public void ReloadNameMap()
    {
        expectedSocketByName.Clear();

        if (nameToSocketMapFile == null || string.IsNullOrWhiteSpace(nameToSocketMapFile.text))
        {
            if (logWarnings)
                Debug.LogWarning("MessageSocketScoreController: mapping file is empty or missing.", this);
            return;
        }

        string[] lines = nameToSocketMapFile.text.Split(new[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                continue;

            if (!TryParseMappingLine(line, out string parsedName, out string parsedSocket))
            {
                if (logWarnings)
                    Debug.LogWarning($"MessageSocketScoreController: cannot parse mapping line {i + 1}: '{line}'", this);
                continue;
            }

            string key = NormalizeName(parsedName);
            string normalizedSocket = NormalizeCoordinate(parsedSocket);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(normalizedSocket))
                continue;

            expectedSocketByName[key] = normalizedSocket;
        }
    }

    private void HandleMessageShown(string shownMessage)
    {
        activeName = NormalizeName(shownMessage);
        if (!string.IsNullOrWhiteSpace(activeName) && expectedSocketByName.TryGetValue(activeName, out string expected))
        {
            expectedSocketForActiveName = expected;
        }
        else
        {
            expectedSocketForActiveName = string.Empty;
            if (logWarnings)
                Debug.LogWarning($"MessageSocketScoreController: no coordinate mapping for '{shownMessage}'.", this);
        }

        RefreshExpectedSocketDebugText();
    }

    private void HandleMessageCleared()
    {
        activeName = string.Empty;
        expectedSocketForActiveName = string.Empty;
        RefreshExpectedSocketDebugText();
    }

    private void HandlePinPlugged(Pin pin, Socket socket)
    {
        if (socket == null)
            return;

        if (requireActiveMessage && string.IsNullOrWhiteSpace(activeName))
            return;

        if (string.IsNullOrWhiteSpace(expectedSocketForActiveName))
            return;

        string pluggedSocket = NormalizeCoordinate(socket.SocketID);
        if (string.IsNullOrWhiteSpace(pluggedSocket))
            return;

        bool isCorrect = pluggedSocket == expectedSocketForActiveName;
        if (isCorrect)
            score += pointsForCorrect;
        else
            score -= pointsForWrong;

        RefreshScoreText();

        if (consumeMessageAfterAttempt && messageUI != null)
            messageUI.HidePanel(true);
    }

    private void RefreshScoreText()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void RefreshExpectedSocketDebugText()
    {
        if (debugExpectedSocketText == null)
            return;

        if (string.IsNullOrWhiteSpace(activeName) || string.IsNullOrWhiteSpace(expectedSocketForActiveName))
        {
            debugExpectedSocketText.text = string.Empty;
            return;
        }

        debugExpectedSocketText.text = $"Target: {expectedSocketForActiveName}";
    }

    private bool TryParseMappingLine(string line, out string name, out string socketCoordinate)
    {
        string[] separators = { "=", "->", ":", ";" };
        for (int i = 0; i < separators.Length; i++)
        {
            string sep = separators[i];
            int index = line.IndexOf(sep, System.StringComparison.Ordinal);
            if (index <= 0) continue;

            name = line.Substring(0, index).Trim();
            socketCoordinate = line.Substring(index + sep.Length).Trim();
            return true;
        }

        name = string.Empty;
        socketCoordinate = string.Empty;
        return false;
    }

    private string NormalizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string trimmed = raw.Trim();
        var sb = new StringBuilder(trimmed.Length);
        bool previousWasWhitespace = false;

        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            bool isWs = char.IsWhiteSpace(c);
            if (isWs)
            {
                if (!previousWasWhitespace)
                    sb.Append(' ');
                previousWasWhitespace = true;
            }
            else
            {
                sb.Append(c);
                previousWasWhitespace = false;
            }
        }

        string normalized = sb.ToString().Trim();
        if (caseInsensitiveNames)
            normalized = normalized.ToLowerInvariant();
        return normalized;
    }

    private string NormalizeCoordinate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var cleaned = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = char.ToLowerInvariant(raw[i]);
            if (char.IsLetterOrDigit(c))
                cleaned.Append(c);
        }

        if (cleaned.Length < 2)
            return string.Empty;

        string value = cleaned.ToString();

        // letter+number => number+letter (a5 -> 5a)
        if (char.IsLetter(value[0]))
        {
            char letter = value[0];
            string digits = value.Substring(1);
            if (IsAllDigits(digits))
                return digits + letter;
            return string.Empty;
        }

        // number+letter (6f -> 6f)
        int digitEnd = 0;
        while (digitEnd < value.Length && char.IsDigit(value[digitEnd]))
            digitEnd++;

        if (digitEnd <= 0 || digitEnd >= value.Length)
            return string.Empty;

        string numberPart = value.Substring(0, digitEnd);
        char letterPart = value[digitEnd];
        if (!char.IsLetter(letterPart))
            return string.Empty;

        return numberPart + letterPart;
    }

    private bool IsAllDigits(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        for (int i = 0; i < s.Length; i++)
        {
            if (!char.IsDigit(s[i]))
                return false;
        }

        return true;
    }
}
