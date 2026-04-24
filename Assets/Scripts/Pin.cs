using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D))]
public class Pin : MonoBehaviour
{
    [Header("Идентификация")]
    [SerializeField] private string pinID;

    [Header("Возврат на резинке")]
    [SerializeField] private float returnSpeed = 5f;
    private Vector2 startPosition;

    [Header("Спрайты")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite draggedSprite;
    [SerializeField] private Sprite pluggedSprite;

    [Header("События")]
    [SerializeField] private UnityEvent<Pin, Socket> onPluggedCorrect;
    [SerializeField] private UnityEvent<Pin, Socket> onWrongAttempt;

    private SpriteRenderer spriteRenderer;
    private bool isDragging = false;
    private bool isPlugged = false;
    private Socket currentSocket = null;
    private Socket hoverSocket = null;

    public string PinID => pinID;

    private void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = normalSprite;
    }

    private void Update()
    {
        // Возврат к начальной позиции (резинка)
        if (!isDragging && !isPlugged)
        {
            transform.position = Vector2.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);
        }
        // Перетаскивание мышкой
        if (isDragging)
        {
            transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void OnMouseDown()
    {
        if (isPlugged)
        {
            // Выдёргиваем из гнезда и сразу начинаем тащить
            Unplug();
            isDragging = true;
            spriteRenderer.sprite = draggedSprite;
        }
        else
        {
            isDragging = true;
            spriteRenderer.sprite = draggedSprite;
        }
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        if (hoverSocket != null && !hoverSocket.IsOccupied)
        {
            if (hoverSocket.AttemptPlug(this))
            {
                // Вставка успешна – всё обработается в PlugInto
                isDragging = false;
            }
            else
            {
                onWrongAttempt?.Invoke(this, hoverSocket);
                isDragging = false;
                spriteRenderer.sprite = normalSprite;
            }
        }
        else
        {
            isDragging = false;
            spriteRenderer.sprite = normalSprite;
        }
    }

    public void PlugInto(Socket socket)
    {
        isPlugged = true;
        currentSocket = socket;
        Vector3 targetPos = (socket.PlugPosition != null ? socket.PlugPosition.position : socket.transform.position) + new Vector3(0, 0, -0.1f);
        transform.position = targetPos;
        spriteRenderer.sprite = pluggedSprite;
        isDragging = false;
        onPluggedCorrect?.Invoke(this, socket);
    }

    public void Unplug()
    {
        if (!isPlugged) return;
        if (currentSocket != null) currentSocket.UnplugPin(this);
        isPlugged = false;
        currentSocket = null;
        spriteRenderer.sprite = normalSprite;
        // Позиция не сбрасывается – штырь сам начнёт возвращаться к startPosition через Update
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Socket socket = other.GetComponent<Socket>();
        if (socket != null && !socket.IsOccupied)
            hoverSocket = socket;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (hoverSocket != null && other.GetComponent<Socket>() == hoverSocket)
            hoverSocket = null;
    }
}