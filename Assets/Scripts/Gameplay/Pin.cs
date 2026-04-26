using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Pin : MonoBehaviour
{
    [Header("Идентификация")]
    [SerializeField] private string pinID;

    [Header("Настройки движения")]
    [SerializeField] private float returnSpeed = 25f;
    private Vector2 startPosition;

    [Header("Спрайты штыря")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite draggedSprite;
    [SerializeField] private Sprite pluggedSprite;

    [Header("Звук")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip plugClip;
    [SerializeField] private AudioClip unplugClip;

    private SpriteRenderer sr;
    private Lamp myLamp; // Ссылка на связанную лампу
    private bool isDragging = false;
    private bool isPlugged = false;
    private Socket hoverSocket = null;
    private Socket currentSocket = null;

    public string PinID => pinID;

    private void Start()
    {
        startPosition = transform.position;
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = normalSprite;

        // Автоматический поиск своей лампы по ID
        Lamp[] allLamps = FindObjectsByType<Lamp>(FindObjectsSortMode.None);
        foreach (Lamp l in allLamps)
        {
            if (l.LinkedPinID == pinID) { myLamp = l; break; }
        }
    }

    private void Update()
    {
        // Механика возврата на место (резинка)
        if (!isDragging && !isPlugged)
        {
            transform.position = Vector2.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);
        }

        // Следование за мышью
        if (isDragging)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePos.x, mousePos.y, -1f);
            //transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void OnMouseDown()
    {

        if (isPlugged) Unplug();

        isDragging = true;
        sr.sprite = draggedSprite;

        // СОСТОЯНИЕ: Лампа замирает (горит желтым)
        if (myLamp != null) myLamp.NotifyPickedUp();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (hoverSocket != null && !hoverSocket.IsOccupied)
        {
            PlugInto(hoverSocket);
        }
        else
        {
            sr.sprite = normalSprite;
            // СОСТОЯНИЕ: Пин брошен, лампа продолжает мигать и считать время
            if (myLamp != null) myLamp.NotifyDropped();
        }
    }

    private void PlugInto(Socket socket)
    {
        isPlugged = true;
        currentSocket = socket;
        currentSocket.SetOccupied(true);
        
        transform.position = socket.transform.position + new Vector3(0, 0, -0.1f);
        sr.sprite = pluggedSprite;
        PlaySfx(plugClip);

        // СОСТОЯНИЕ: Проверка сокета лампой (Зеленый или Красный)
        if (myLamp != null) myLamp.NotifyPlugged(socket.SocketID);
    }

    public void Unplug()
    {
        if (!isPlugged) return;
        
        if (currentSocket != null) currentSocket.SetOccupied(false);
        if (myLamp != null) myLamp.NotifyDropped();
        isPlugged = false;
        currentSocket = null;
        sr.sprite = normalSprite;
        PlaySfx(unplugClip);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Socket s)) hoverSocket = s;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (hoverSocket != null && other.gameObject == hoverSocket.gameObject) hoverSocket = null;
    }
}