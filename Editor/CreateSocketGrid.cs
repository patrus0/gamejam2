using UnityEngine;
using UnityEditor;

public class CreateSocketGrid : EditorWindow
{
    [MenuItem("Tools/Create Socket Grid")]
    static void CreateGrid()
    {
        // Настройки сетки
        int width = 6;      // количество столбцов (X)
        int height = 6;     // количество строк (Y)
        float startX = 0.5f;
        float endX = 5.5f;      // startX + (width-1)*stepX
        float startY = 2.5f;
        float endY = -2.5f;

        // Шаг между сокетами
        float stepX = (endX - startX) / (width - 1);
        float stepY = (endY - startY) / (height - 1);

        // Путь к префабу сокета (измените на свой)
        GameObject socketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Socket.prefab");
        if (socketPrefab == null)
        {
            Debug.LogError("Префаб сокета не найден. Укажите правильный путь в AssetDatabase.LoadAssetAtPath");
            return;
        }

        // Создаём родительский объект для всех сокетов
        GameObject parent = new GameObject("SocketsGrid");
        parent.transform.position = Vector3.zero;

        // Буквы для ID (A, B, C, D, E, F)
        char[] rowLetters = { 'A', 'B', 'C', 'D', 'E', 'F' };

        for (int y = 0; y < height; y++)      // строки (Y)
        {
            for (int x = 0; x < width; x++)   // столбцы (X)
            {
                // Позиция сокета
                float posX = startX + x * stepX;
                float posY = startY + y * stepY;
                Vector3 position = new Vector3(posX, posY, 0f);

                // Создаём экземпляр префаба
                GameObject socket = (GameObject)PrefabUtility.InstantiatePrefab(socketPrefab);
                socket.transform.SetParent(parent.transform);
                socket.transform.position = position;

                // Имя объекта: Socket_A1, Socket_B2 и т.д.
                char rowLetter = rowLetters[y];
                int columnNumber = x + 1; // 1..6
                string socketName = $"Socket_{rowLetter}{columnNumber}";
                socket.name = socketName;

                // Настраиваем компонент Socket (ID = "A1", "B2" и т.д.)
                Socket socketComponent = socket.GetComponent<Socket>();
                if (socketComponent != null)
                {
                    socketComponent.socketID = $"{rowLetter}{columnNumber}";
                    EditorUtility.SetDirty(socketComponent);
                }
                else
                {
                    Debug.LogWarning($"На объекте {socketName} нет компонента Socket!");
                }
            }
        }

        // Сохраняем созданные объекты (для работы в редакторе)
        EditorUtility.SetDirty(parent);
        Debug.Log($"Создана сетка {width}x{height} сокетов.");
    }
}