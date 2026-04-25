using UnityEngine;
using UnityEngine.EventSystems;

public class HoverSlide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform panel; // Объект, который двигаем
    public Vector2 hiddenPos;   // Координаты за экраном (например, 0, -200)
    public Vector2 visiblePos;  // Координаты при наведении (например, 0, 0)
    public float speed = 10f;

    private Vector2 targetPos;

    void Start()
    {
        targetPos = hiddenPos;
        panel.anchoredPosition = hiddenPos;
    }

    void Update()
    {
        // Плавное движение к цели
        panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, targetPos, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = visiblePos;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = hiddenPos;
    }
}

