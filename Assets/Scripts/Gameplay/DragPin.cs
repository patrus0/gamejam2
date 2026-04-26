using UnityEngine;
using UnityEngine.Events; // для события на будущее

public class DragPin : MonoBehaviour
{
    [Header("Настройки возврата")]
    [SerializeField] private float returnSpeed = 5f; // скорость возврата (пикселей/сек)
    private Vector2 startPosition; // начальная позиция

    [Header("Спрайты")]
    [SerializeField] private Sprite normalSprite;   // обычный
    [SerializeField] private Sprite draggedSprite;  // когда перетаскиваем (или вставлен)

    [Header("Гнездо")]
    [SerializeField] private Transform socketTransform; // позиция гнезда
    [SerializeField] private UnityEvent onPlugged;     // событие при вставке (на будущее)

    [Header("Звук")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip plugClip;
    [SerializeField] private AudioClip unplugClip;

    private SpriteRenderer spriteRenderer;
    private bool isDragging = false;
    private bool isPlugged = false;   // вставлен ли в гнездо
    private bool isOverSocket = false; // находится ли коллайдер штырька над триггером гнезда

    private void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = normalSprite;

        // Небольшая проверка
        if (socketTransform == null)
            Debug.LogWarning("Гнездо не назначено в инспекторе!");
    }

    private void Update()
    {
        if (isDragging)
        {
            transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        // Движение к начальной позиции, если не перетаскиваем и не вставлены
        if (!isDragging && !isPlugged)
        {
            Vector2 newPos = Vector2.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);
            transform.position = newPos;
            // Если достигли начальной позиции – можно остановить (не обязательно)
        }
    }

    private void OnMouseDown()
    {
        isDragging = true;
        spriteRenderer.sprite = draggedSprite; // меняем спрайт при хватании
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // Проверяем, отпущен ли над гнездом
        if (isOverSocket && socketTransform != null)
        {
            // Вставляем в гнездо
            isPlugged = true;
            transform.position = socketTransform.position;
            spriteRenderer.sprite = draggedSprite; // оставляем изменённый спрайт
            PlaySfx(plugClip);

            // Вызываем событие для будущих действий (например, зажечь лампочку)
            onPlugged?.Invoke();
        }
        else
        {
            // Возвращаем обычный спрайт (не вставлен)
            spriteRenderer.sprite = normalSprite;
        }
    }

    // Обнаружение входа в зону гнезда (триггер)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform == socketTransform)
            isOverSocket = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform == socketTransform)
            isOverSocket = false;
    }

    // (Опционально) Если нужно выдёргивать штырёк из гнезда по клику
    public void Unplug()
    {
        if (!isPlugged) return;
        isPlugged = false;
        spriteRenderer.sprite = normalSprite;
        // Можно немного отодвинуть от гнезда, чтобы не залипал
        transform.position = startPosition;
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
}