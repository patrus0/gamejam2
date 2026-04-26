using UnityEngine;
using System.Collections;

public class Lamp : MonoBehaviour
{
    public enum State { Off, Ringing, Holding, InConversation, Failure }

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
        targetSocketID = socketID;
        timeLeft = 10f;
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
            yield return null;
        }

        if (currentState != State.InConversation) FinalFailure();
    }

    public void NotifyPickedUp()
    {
        if (currentState == State.Ringing) currentState = State.Holding;
        if (graceRoutine != null) { StopCoroutine(graceRoutine); graceRoutine = null; }
    }

    public void NotifyDropped()
    {
        if (currentState == State.Holding) currentState = State.Ringing;
    }

    public void NotifyPlugged(string socketID)
    {
        if (currentState == State.Failure) return;

        if (socketID == targetSocketID)
        {
            if (graceRoutine != null) StopCoroutine(graceRoutine);
            StartCoroutine(ConversationRoutine());
        }
        else
        {
            // Ошибка! Даем 5 секунд
            graceRoutine = StartCoroutine(GracePeriodRoutine());
        }
    }

    private IEnumerator GracePeriodRoutine()
    {
        sr.sprite = redSprite; // Горит красным, пока не вытащат
        float graceTimer = gracePeriod;
        while (graceTimer > 0)
        {
            graceTimer -= Time.deltaTime;
            yield return null;
        }
        FinalFailure();
    }

    private IEnumerator ConversationRoutine()
    {
        currentState = State.InConversation;
        sr.sprite = greenSprite;
        float talkTime = Random.Range(minTalkTime, maxTalkTime);
        yield return new WaitForSeconds(talkTime);
        
        currentState = State.Off;
        sr.sprite = offSprite;
        Debug.Log($"[ЛИНИЯ {linkedPinID}] Разговор окончен.");
    }

    private void FinalFailure()
    {
        StopAllCoroutines();
        currentState = State.Failure;
        sr.sprite = redSprite;
        GameSessionData.TotalPenalties++;
        Debug.Log($"[ШТРАФ] Ошибка на линии {linkedPinID}!");
        // Можно добавить через 2-3 секунды сброс в State.Off, чтобы лампа снова могла принимать звонки
        Invoke("ResetLamp", 3f);
    }

    void ResetLamp() { currentState = State.Off; sr.sprite = offSprite; }
}