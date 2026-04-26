using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class MessageSocketScoreController : MonoBehaviour
{
    private static readonly List<MessageSocketScoreController> ActiveControllers = new List<MessageSocketScoreController>();

    [Header("References")]
    [SerializeField] private InterceptedMessageUI messageUI;
    [Tooltip("Формат строк: Фамилия Имя = a5")]
    [SerializeField] private TextAsset nameToSocketMapFile;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text debugExpectedSocketText;
    [SerializeField] private TMP_Text debugExpectedPinText;

    [Header("Scoring")]
    [SerializeField] private bool enableLegacyScore = false;
    [SerializeField] private int pointsForCorrect = 1;
    [SerializeField] private int pointsForWrong = 1;
    [SerializeField] private int pointsForTimeout = 1;
    [SerializeField] private bool requireActiveMessage = true;
    [SerializeField] private bool clearBlockAfterEvaluatedAnswer = true;
    [SerializeField] private bool subtractPointsOnTimeout = true;
    [SerializeField] private bool requireMatchingPin = true;
    [SerializeField] private bool enforceUniquePinsAcrossBlocks = true;

    [Header("Parsing")]
    [SerializeField] private bool caseInsensitiveNames = true;
    [SerializeField] private bool logWarnings = true;
    [Tooltip("Если пусто, список пинов будет взят автоматически из объектов Pin в сцене.")]
    [SerializeField] private List<string> allowedPinIDs = new List<string>();

    private readonly Dictionary<string, string> expectedSocketByName = new Dictionary<string, string>();

    private string activeName = string.Empty;
    private string expectedSocketForActiveName = string.Empty;
    private string expectedPinForActiveName = string.Empty;
    private bool suppressNextClearPenalty;
    private int score;

    public int Score => score;

    private void Awake()
    {
        if (messageUI == null)
            messageUI = FindFirstObjectByType<InterceptedMessageUI>();

        EnsureAllowedPinsLoaded();
        ReloadNameMap();
        RefreshScoreText();
        RefreshDebugTargetsText();
    }

    private void OnEnable()
    {
        if (!ActiveControllers.Contains(this))
            ActiveControllers.Add(this);

        if (messageUI != null)
        {
            messageUI.MessageShown += HandleMessageShown;
            messageUI.MessageCleared += HandleMessageCleared;
        }

        Socket.PinPlugged += HandlePinPlugged;
        Lamp.CallFailedForPin += HandleLampFailure;
    }

    private void OnDisable()
    {
        ActiveControllers.Remove(this);

        if (messageUI != null)
        {
            messageUI.MessageShown -= HandleMessageShown;
            messageUI.MessageCleared -= HandleMessageCleared;
        }

        Socket.PinPlugged -= HandlePinPlugged;
        Lamp.CallFailedForPin -= HandleLampFailure;
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

        if (LooksLikeCodeFile(nameToSocketMapFile.text))
        {
            if (logWarnings)
                Debug.LogWarning("MessageSocketScoreController: nameToSocketMapFile looks like a C# file. Assign a plain text mapping file (e.g. name_to_socket_map.txt).", this);
            return;
        }

        int parsedCount = 0;
        string[] lines = nameToSocketMapFile.text.Split(new[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//") || IsLikelyCodeLine(line))
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
            parsedCount++;
        }

        if (parsedCount == 0 && logWarnings)
        {
            Debug.LogWarning("MessageSocketScoreController: no valid mappings were parsed. Check nameToSocketMapFile format: 'Фамилия Имя = a5'.", this);
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

        AssignRandomPinForActiveName();
        RefreshDebugTargetsText();
    }

    private void HandleMessageCleared()
    {
        bool hadActiveTask = !string.IsNullOrWhiteSpace(activeName)
            || !string.IsNullOrWhiteSpace(expectedSocketForActiveName)
            || !string.IsNullOrWhiteSpace(expectedPinForActiveName);

        if (suppressNextClearPenalty)
        {
            suppressNextClearPenalty = false;
        }
        else if (enableLegacyScore && subtractPointsOnTimeout && hadActiveTask)
        {
            score -= pointsForTimeout;
            RefreshScoreText();
        }

        activeName = string.Empty;
        expectedSocketForActiveName = string.Empty;
        expectedPinForActiveName = string.Empty;
        RefreshDebugTargetsText();
    }

    private void HandlePinPlugged(Pin pin, Socket socket)
    {
        if (pin == null || socket == null)
            return;

        if (requireActiveMessage && string.IsNullOrWhiteSpace(activeName))
            return;

        if (string.IsNullOrWhiteSpace(expectedSocketForActiveName))
            return;

        string pluggedSocket = NormalizeCoordinate(socket.SocketID);
        if (string.IsNullOrWhiteSpace(pluggedSocket))
            return;

        string normalizedPinID = NormalizeName(pin.PinID);
        bool pinMatches = !requireMatchingPin
            || string.IsNullOrWhiteSpace(expectedPinForActiveName)
            || normalizedPinID == expectedPinForActiveName;

        // Если это "чужой" пин, текущий блок попытку игнорирует и не штрафует.
        if (!pinMatches)
            return;

        bool socketMatches = pluggedSocket == expectedSocketForActiveName;
        bool isCorrect = socketMatches;
        if (enableLegacyScore)
        {
            if (isCorrect)
                score += pointsForCorrect;
            else
                score -= pointsForWrong;

            RefreshScoreText();
        }

        if (clearBlockAfterEvaluatedAnswer && messageUI != null)
        {
            suppressNextClearPenalty = true;
            messageUI.HidePanel(true);
        }
    }

    private void HandleLampFailure(string failedPinID)
    {
        if (string.IsNullOrWhiteSpace(activeName))
            return;
        if (string.IsNullOrWhiteSpace(expectedPinForActiveName))
            return;

        string normalizedFailedPin = NormalizeName(failedPinID);
        if (normalizedFailedPin != expectedPinForActiveName)
            return;

        // Штраф уже начислен лампой. Здесь просто завершаем и очищаем этот блок.
        if (messageUI != null)
        {
            suppressNextClearPenalty = true;
            messageUI.HidePanel(true);
        }
        else
        {
            activeName = string.Empty;
            expectedSocketForActiveName = string.Empty;
            expectedPinForActiveName = string.Empty;
            RefreshDebugTargetsText();
        }
    }

    private void RefreshScoreText()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void RefreshDebugTargetsText()
    {
        if (debugExpectedSocketText == null)
        {
            // do nothing
        }
        else if (string.IsNullOrWhiteSpace(activeName) || string.IsNullOrWhiteSpace(expectedSocketForActiveName))
        {
            debugExpectedSocketText.text = string.Empty;
        }
        else
        {
            debugExpectedSocketText.text = $"Target socket: {expectedSocketForActiveName}";
        }

        if (debugExpectedPinText == null)
            return;

        if (string.IsNullOrWhiteSpace(activeName) || string.IsNullOrWhiteSpace(expectedPinForActiveName))
            debugExpectedPinText.text = string.Empty;
        else
            debugExpectedPinText.text = $"Target pin: {expectedPinForActiveName}";
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
            char c = NormalizeConfusableLetter(trimmed[i]);
            if (c == 'ё') c = 'е';
            if (c == 'Ё') c = 'Е';

            bool isWs = char.IsWhiteSpace(c);
            if (isWs)
            {
                if (!previousWasWhitespace)
                    sb.Append(' ');
                previousWasWhitespace = true;
            }
            else
            {
                // Оставляем только буквы/цифры и дефис в составе имени.
                if (char.IsLetterOrDigit(c) || c == '-')
                {
                    sb.Append(c);
                    previousWasWhitespace = false;
                }
                // Любые другие символы (скрытые, пунктуация, форматирование) игнорируем.
                previousWasWhitespace = false;
            }
        }

        string normalized = sb.ToString().Trim();
        if (caseInsensitiveNames)
            normalized = normalized.ToLowerInvariant();
        return normalized;
    }

    private char NormalizeConfusableLetter(char c)
    {
        // Латиница -> кириллица для визуально одинаковых символов.
        switch (c)
        {
            case 'A': return 'А';
            case 'a': return 'а';
            case 'B': return 'В';
            case 'E': return 'Е';
            case 'e': return 'е';
            case 'K': return 'К';
            case 'k': return 'к';
            case 'M': return 'М';
            case 'm': return 'м';
            case 'H': return 'Н';
            case 'h': return 'н';
            case 'O': return 'О';
            case 'o': return 'о';
            case 'P': return 'Р';
            case 'p': return 'р';
            case 'C': return 'С';
            case 'c': return 'с';
            case 'T': return 'Т';
            case 't': return 'т';
            case 'X': return 'Х';
            case 'x': return 'х';
            case 'Y': return 'У';
            case 'y': return 'у';
            default: return c;
        }
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

    private void EnsureAllowedPinsLoaded()
    {
        if (allowedPinIDs.Count > 0)
            return;

        Pin[] pins = FindObjectsByType<Pin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var uniquePins = new HashSet<string>();
        for (int i = 0; i < pins.Length; i++)
        {
            string normalized = NormalizeName(pins[i].PinID);
            if (string.IsNullOrWhiteSpace(normalized))
                continue;
            if (uniquePins.Add(normalized))
                allowedPinIDs.Add(normalized);
        }

        if (allowedPinIDs.Count == 0 && logWarnings)
            Debug.LogWarning("MessageSocketScoreController: no pin IDs found for random assignment.", this);
    }

    private void AssignRandomPinForActiveName()
    {
        expectedPinForActiveName = string.Empty;

        if (!requireMatchingPin)
            return;

        EnsureAllowedPinsLoaded();
        if (allowedPinIDs.Count == 0)
            return;

        List<string> candidatePins = BuildAvailablePinPool();
        if (candidatePins.Count == 0)
            return;

        int randomIndex = UnityEngine.Random.Range(0, candidatePins.Count);
        expectedPinForActiveName = candidatePins[randomIndex];
    }

    private List<string> BuildAvailablePinPool()
    {
        if (!enforceUniquePinsAcrossBlocks)
            return new List<string>(allowedPinIDs);

        var reservedPins = new HashSet<string>();
        for (int i = 0; i < ActiveControllers.Count; i++)
        {
            MessageSocketScoreController controller = ActiveControllers[i];
            if (controller == null || controller == this)
                continue;
            if (!controller.isActiveAndEnabled)
                continue;
            if (!controller.requireMatchingPin)
                continue;
            if (string.IsNullOrWhiteSpace(controller.activeName))
                continue;
            if (string.IsNullOrWhiteSpace(controller.expectedPinForActiveName))
                continue;

            reservedPins.Add(controller.expectedPinForActiveName);
        }

        var available = new List<string>(allowedPinIDs.Count);
        for (int i = 0; i < allowedPinIDs.Count; i++)
        {
            string candidate = allowedPinIDs[i];
            if (!reservedPins.Contains(candidate))
                available.Add(candidate);
        }

        // Если пинов меньше, чем блоков, разрешаем повторно использовать, чтобы блок не завис.
        if (available.Count == 0)
            available.AddRange(allowedPinIDs);

        return available;
    }

    private bool IsLikelyCodeLine(string line)
    {
        if (line == "{" || line == "}" || line == ";" || line == "};")
            return true;

        string lower = line.ToLowerInvariant();
        return lower.StartsWith("using ")
            || lower.StartsWith("namespace ")
            || lower.StartsWith("class ")
            || lower.StartsWith("public ")
            || lower.StartsWith("private ")
            || lower.StartsWith("protected ")
            || lower.StartsWith("internal ")
            || lower.Contains("=>")
            || lower.Contains("monobehaviour");
    }

    private bool LooksLikeCodeFile(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        string lower = content.ToLowerInvariant();
        return lower.Contains("using unityengine")
            || lower.Contains("class messagesocketscorecontroller")
            || lower.Contains("monobehaviour");
    }
}
