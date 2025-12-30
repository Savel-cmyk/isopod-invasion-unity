using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas mainMenuCanvas;
    public Button startButton;

    [Header("Game Elements")]
    public Transform boxesContainer;
    public GameObject isopodPrefab;
    public Transform isopodSpawnPoint;

    [Header("Box Settings")]
    public GameObject boxPrefab;
    public int rows = 5;
    public int columns = 4;
    public float spacingX = 2f;
    public float spacingY = 1.5f;

    private List<GameObject> allBoxes = new List<GameObject>();
    private GameObject currentIsopod;
    private bool gameIsRunning = false; // Флаг состояния игры
    private List<GameObject> allIsopods = new List<GameObject>();

    void Start()
    {
        // НЕ используем Time.timeScale!
        gameIsRunning = false; // Игра еще не началась

        if (boxesContainer != null)
            boxesContainer.gameObject.SetActive(false);

        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);

        startButton.onClick.AddListener(StartGame);

        Debug.Log("GameController: Game is in menu state");
    }

    void StartGame()
    {
        Debug.Log("=== GAME STARTING ===");

        gameIsRunning = true; // Теперь игра запущена

        // Скрываем меню
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.gameObject.SetActive(false);
            Debug.Log("Menu hidden");
        }

        // Создаем/активируем контейнер для коробок
        if (boxesContainer == null)
        {
            GameObject container = new GameObject("Boxes Container");
            boxesContainer = container.transform;
        }
        boxesContainer.gameObject.SetActive(true);

        // Создаем коробки
        CreateBoxes();

        // Спавним жука
        SpawnIsopods();

        Debug.Log("=== GAME STARTED ===");
    }

    void CreateBoxes()
    {
        Debug.Log("Creating boxes...");
        allBoxes.Clear();

        // Очищаем старые коробки
        if (boxesContainer != null)
        {
            int childCount = boxesContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(boxesContainer.GetChild(i).gameObject);
            }
        }

        // Находим центр сетки
        float totalWidth = (columns - 1) * spacingX;
        float totalHeight = (rows - 1) * spacingY;

        float startX = -totalWidth / 2f;
        float startY = totalHeight / 2f;

        Debug.Log($"Grid: {rows}x{columns}, Start: ({startX}, {startY})");

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Позиция коробки
                float x = startX + col * spacingX;
                float y = startY - row * spacingY;

                // Создаем коробку
                GameObject box = Instantiate(boxPrefab, boxesContainer);
                box.transform.position = new Vector3(x, y, 0);
                box.name = $"Box_{row}_{col}";

                // Тег (если создали)
                if (!string.IsNullOrEmpty(box.tag))
                {
                    try { box.tag = "Box"; } catch { }
                }

                // Настройка спрайта
                SpriteRenderer spriteRenderer = box.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = box.AddComponent<SpriteRenderer>();
                    Debug.LogWarning($"Added SpriteRenderer to {box.name}");
                }

                spriteRenderer.sortingLayerName = "Boxes";
                spriteRenderer.sortingOrder = 1;

                allBoxes.Add(box);
                Debug.Log($"Created: {box.name} at ({x:F1}, {y:F1})");
            }
        }

        Debug.Log($"Created {allBoxes.Count} boxes total");
    }

    void SpawnIsopods()
    {
        // Проверяем префаб
        if (isopodPrefab == null)
        {
            Debug.LogError("Isopod prefab is null!");
            return;
        }

        // Проверяем компонент на префабе
        IsopodController prefabController = isopodPrefab.GetComponent<IsopodController>();
        if (prefabController == null)
        {
            Debug.LogError("No IsopodController found on isopod prefab!");
            return;
        }

        // Очищаем старых жуков
        foreach (GameObject isopod in allIsopods)
        {
            if (isopod != null) Destroy(isopod);
        }
        allIsopods.Clear();

        Vector3[] spawnPositions = {
            new Vector3(-1.5f, -7.2f, 0),
            new Vector3(-0.5f, -7.2f, 0),
            new Vector3(0.5f, -7.2f, 0),
            new Vector3(1.5f, -7.2f, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject isopod = Instantiate(isopodPrefab);
            isopod.transform.position = spawnPositions[i];
            isopod.name = $"Isopod_{i}";

            IsopodController controller = isopod.GetComponent<IsopodController>();
            if (controller != null)
            {
                // === ПРОСТОЙ СПОСОБ ===
                // Сохраняем колонку
                controller.assignedColumn = i;

                // Позиция спавна уже сохранена в transform.position
                // Respawn() будет использовать текущую позицию

                // Назначаем коробку
                AssignRandomBoxToIsopod(controller, i);

                // Включаем движение
                controller.SetGameRunning(true);
            }
        }

        Debug.Log($"Total {allIsopods.Count} isopods spawned and ready");
    }

    // Новый метод для назначения случайной коробки в колонке
    public void AssignRandomBoxToIsopod(IsopodController controller, int column)
    {
        if (allBoxes.Count == 0)
        {
            Debug.LogError("No boxes created yet!");
            return;
        }

        // Собираем все коробки в указанной колонке
        List<GameObject> boxesInColumn = new List<GameObject>();

        for (int row = 0; row < rows; row++)
        {
            // Формула: индекс = строка * колонок + столбец
            int boxIndex = row * columns + column;
            if (boxIndex < allBoxes.Count)
            {
                boxesInColumn.Add(allBoxes[boxIndex]);
            }
        }

        // Выбираем случайную коробку из этой колонки
        if (boxesInColumn.Count > 0)
        {
            int randomIndex = Random.Range(0, boxesInColumn.Count);
            GameObject targetBox = boxesInColumn[randomIndex];
            controller.targetBox = targetBox.transform;

            Debug.Log($"Isopod at column {column} assigned to {targetBox.name} at {targetBox.transform.position}");
        }
        else
        {
            // Запасной вариант: назначаем любую коробку
            if (allBoxes.Count > 0)
            {
                int randomBoxIndex = Random.Range(0, allBoxes.Count);
                controller.targetBox = allBoxes[randomBoxIndex].transform;
                Debug.LogWarning($"No boxes in column {column}. Assigned random box: {allBoxes[randomBoxIndex].name}");
            }
        }
    }

    void SpawnIsopod()
    {
        if (isopodPrefab == null)
        {
            Debug.LogError("Isopod prefab is null!");
            return;
        }

        if (isopodSpawnPoint == null)
        {
            Debug.LogError("Isopod spawn point is null! Creating default...");
            GameObject spawn = new GameObject("DefaultSpawnPoint");
            spawn.transform.position = new Vector3(0, -4, 0);
            isopodSpawnPoint = spawn.transform;
        }

        // Уничтожаем предыдущего жука если есть
        if (currentIsopod != null)
        {
            Destroy(currentIsopod);
        }

        // Создаем жука
        currentIsopod = Instantiate(isopodPrefab);
        currentIsopod.transform.position = isopodSpawnPoint.position;
        currentIsopod.name = "Isopod";

        Debug.Log($"Isopod spawned at: {currentIsopod.transform.position}");

        // Получаем контроллер
        IsopodController controller = currentIsopod.GetComponent<IsopodController>();
        if (controller == null)
        {
            Debug.LogError("No IsopodController found on isopod prefab!");
            return;
        }

        // Назначаем целевую коробку
        if (allBoxes.Count > 16) // Box_4_0 имеет индекс 16 при 5x4
        {
            GameObject targetBox = allBoxes[16];
            if (targetBox != null)
            {
                controller.targetBox = targetBox.transform;
                Debug.Log($"Isopod target set to: {targetBox.name} at {targetBox.transform.position}");
            }
            else
            {
                Debug.LogWarning("Target box is null, trying to find by name...");
                targetBox = GameObject.Find("Box_4_0");
                if (targetBox != null)
                {
                    controller.targetBox = targetBox.transform;
                }
            }
        }
        else if (allBoxes.Count > 0)
        {
            // Используем первую коробку если Box_4_0 не существует
            controller.targetBox = allBoxes[0].transform;
            Debug.Log($"Using first box as target: {allBoxes[0].name}");
        }

        // Важно: сообщаем жуку что игра началась
        controller.SetGameRunning(true);
        controller.enabled = true; // Убедимся что компонент включен

        Debug.Log("Isopod ready to move!");
    }

    void Update()
    {
        // Для отладки: можно добавить горячие клавиши
        if (Input.GetKeyDown(KeyCode.Space) && !gameIsRunning)
        {
            StartGame();
        }
    }

    public void ReturnToMenu()
    {
        gameIsRunning = false;

        // Отключаем всех жуков
        IsopodController[] allControllers = FindObjectsOfType<IsopodController>();
        foreach (IsopodController controller in allControllers)
        {
            controller.SetGameRunning(false);
        }

        foreach (GameObject isopod in allIsopods)
        {
            if (isopod != null) Destroy(isopod);
        }
        allIsopods.Clear();

        // Очищаем коробки
        foreach (GameObject box in allBoxes)
        {
            if (box != null) Destroy(box);
        }
        allBoxes.Clear();

        // Очищаем жуков
        if (currentIsopod != null)
        {
            Destroy(currentIsopod);
        }

        // Показываем меню
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);

        if (boxesContainer != null)
            boxesContainer.gameObject.SetActive(false);

        Debug.Log("Returned to menu");
    }

    // Геттер для проверки состояния игры
    public bool IsGameRunning()
    {
        return gameIsRunning;
    }
}