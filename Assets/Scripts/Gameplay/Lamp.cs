using UnityEngine;
using System.Collections;
using System.Text;
using System;

public class Lamp : MonoBehaviour
{
    public enum State { Off, Ringing, Holding, GracePeriod, InConversation, Failure }
    public static event Action<string> CallFailedForPin;
    public static event Action<string, bool> CallResolvedForPin; // pinID, success

    [SerializeField] private string linkedPinID;
    [SerializeField] private Sprite offSprite, yellowSprite, greenSprite, redSprite;

    [Header("Тайминги")]
    [SerializeField] private float blinkSpeed = 0.5f;
    [SerializeField] private float gracePeriod = 5f; // Время на исправление
    [SerializeField] private float minTalkTime = 20f;
    [SerializeField] private float maxTalkTime = 60f;

    private State currentState = State.Off;
    private string targetSocketID;
    private float timeLeft = 10f;
    private SpriteRenderer sr;
    private Coroutine graceRoutine;

    public string LinkedPinID => linkedPinID;
    public State CurrentState => currentState;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    public void StartIncomingCall(string socketID)
    {
        targetSocketID = NormalizeSocketID(socketID);
        timeLeft = 10f;
        if (graceRoutine != null)
        {
            StopCoroutine(graceRoutine);
            graceRoutine = null;
        }
        StopAllCoroutines();
        StartCoroutine(RingingRoutine());
    }

    private IEnumerator RingingRoutine()
    {
        currentState = State.Ringing;
        while (timeLeft > 0)
        {
            if (currentState == State.Ringing)
            {
                timeLeft -= Time.deltaTime;
                sr.sprite = (Mathf.FloorToInt(Time.time / blinkSpeed) % 2 == 0) ? yellowSprite : offSprite;
            }
            else if (currentState == State.Holding)
            {
                sr.sprite = yellowSprite; // Просто горит при удержании
            }
            else if (currentState == State.GracePeriod)
            {
                // Красный удерживается в GracePeriodRoutine
            }
            yield return null;
        }

        if (currentState != State.InConversation)
            FinalFailure();
    }

    public void NotifyPickedUp()
    {
        if (currentState == State.Ringing)
            currentState = State.Holding;
    }

    public void NotifyDropped()
    {
        if (currentState == State.InConversation)
        {
            // Выдернули пин во время разговора: мгновенный штраф и красная лампа.
            FinalFailure();
            return;
        }

        if (currentState == State.Holding)
            currentState = State.Ringing;
    }

    public void NotifyPlugged(string socketID)
    {
        if (currentState == State.Failure)
            return;

        string normalizedSocketID = NormalizeSocketID(socketID);
        if (normalizedSocketID == targetSocketID)
        {
            if (graceRoutine != null)
            {
                StopCoroutine(graceRoutine);
                graceRoutine = null;
            }
            StartCoroutine(ConversationRoutine());
        }
        else
        {
            // Ошибка! Даем 5 секунд на исправление, лампа горит красным.
            if (graceRoutine != null)
            {
                StopCoroutine(graceRoutine);
                graceRoutine = null;
            }
            graceRoutine = StartCoroutine(GracePeriodRoutine());
        }
    }

    private IEnumerator GracePeriodRoutine()
    {
        currentState = State.GracePeriod;
        float graceTimer = gracePeriod;
        while (graceTimer > 0)
        {
            sr.sprite = redSprite;
            graceTimer -= Time.deltaTime;
            yield return null;
        }

        graceRoutine = null;
        FinalFailure();
    }

    private IEnumerator ConversationRoutine()
    {
        currentState = State.InConversation;
        graceRoutine = null;
        sr.sprite = greenSprite;
        float talkTime = UnityEngine.Random.Range(minTalkTime, maxTalkTime);
        yield return new WaitForSeconds(talkTime);

        CallResolvedForPin?.Invoke(linkedPinID, true);
        currentState = State.Off;
        sr.sprite = offSprite;
        Debug.Log($"[ЛИНИЯ {linkedPinID}] Разговор окончен.");
    }

    private void FinalFailure()
    {
        StopAllCoroutines();
        graceRoutine = null;
        currentState = State.Failure;
        sr.sprite = redSprite;
        GameSessionData.TotalPenalties++;
        CallResolvedForPin?.Invoke(linkedPinID, false);
        CallFailedForPin?.Invoke(linkedPinID);
        Debug.Log($"[ШТРАФ] Ошибка на линии {linkedPinID}!");
        // Можно добавить через 2-3 секунды сброс в State.Off, чтобы лампа снова могла принимать звонки
        Invoke("ResetLamp", 3f);
    }

    private void ResetLamp()
    {
        currentState = State.Off;
        sr.sprite = offSprite;
    }

    private string NormalizeSocketID(string raw)
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

        int digitEnd = 0;
        while (digitEnd < value.Length && char.IsDigit(value[digitEnd]))
            digitEnd++;

        if (digitEnd <= 0 || digitEnd >= value.Length)
            return string.Empty;

        char letterPart = value[digitEnd];
        if (!char.IsLetter(letterPart))
            return string.Empty;

        return value.Substring(0, digitEnd) + letterPart;
    }

    private bool IsAllDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
                return false;
        }

        return true;
    }
}