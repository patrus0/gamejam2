using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PinWire : MonoBehaviour
{
    [SerializeField] private Transform startPoint;      // глобальная стартовая точка (сцены)
    [SerializeField] private Transform attachPoint;     // дочерняя точка на пине
    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        // Настройки материала для тайлинга по длине
        line.textureMode = LineTextureMode.Tile;
    }

    private void LateUpdate()
    {
        if (startPoint == null || attachPoint == null) return;

        // Устанавливаем позиции вершин
        line.SetPosition(0, startPoint.position);
        line.SetPosition(1, attachPoint.position);

        // Динамический тайлинг: длина линии / ширина текстуры
        float distance = Vector3.Distance(startPoint.position, attachPoint.position);
        float textureWidth = 0.05f; // ширина текстуры в юнитах (если ваш спрайт шириной 1 юнит – настройте)
        // Или получите из рендерера: 
        // textureWidth = line.material.mainTexture.width / line.material.mainTexture.texelSize.x;
        // Для простоты задайте вручную в инспекторе:
        line.material.mainTextureScale = new Vector2(distance / textureWidth, 1);
    }
}