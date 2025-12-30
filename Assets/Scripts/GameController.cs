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
    private List<GameObject> allIsopods = new List<GameObject>();

    void Start()
    {
        if (boxesContainer != null)
            boxesContainer.gameObject.SetActive(false);

        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);

        startButton.onClick.AddListener(StartGame);
    }

    void StartGame()
    {
        Debug.Log("=== GAME STARTING ===");

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

        // Спавним жуков
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

                // Тег для поиска
                try { box.tag = "Box"; } catch { }

                // Настройка спрайта
                SpriteRenderer spriteRenderer = box.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = box.AddComponent<SpriteRenderer>();
                }

                spriteRenderer.sortingLayerName = "Boxes";
                spriteRenderer.sortingOrder = 1;

                allBoxes.Add(box);
            }
        }

        Debug.Log($"Created {allBoxes.Count} boxes total");
    }

    void SpawnIsopods()
    {
        if (isopodPrefab == null)
        {
            Debug.LogError("Isopod prefab is null!");
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
                controller.assignedColumn = i;
                controller.spawnPosition = spawnPositions[i];

                // Назначаем ближайшую коробку
                AssignNearestBoxToIsopod(controller, i);

                controller.SetGameRunning(true);

                // Сохраняем ссылку на GameController
                controller.gameController = this;
            }

            allIsopods.Add(isopod);
        }

        Debug.Log($"Spawned {allIsopods.Count} isopods");
    }

    // Новый метод: назначение БЛИЖАЙШЕЙ коробки в колонке
    public void AssignNearestBoxToIsopod(IsopodController controller, int column)
    {
        if (allBoxes.Count == 0) return;

        GameObject nearestBox = null;
        float nearestDistance = float.MaxValue;
        Vector3 isopodPosition = controller.transform.position;

        // Ищем ближайшую НЕ сломанную коробку в колонке
        for (int row = 0; row < rows; row++)
        {
            int boxIndex = row * columns + column;
            if (boxIndex < allBoxes.Count)
            {
                GameObject box = allBoxes[boxIndex];
                BoxController boxController = box.GetComponent<BoxController>();

                // Проверяем, не сломана ли коробка
                if (boxController != null && !boxController.IsBroken())
                {
                    float distance = Vector3.Distance(isopodPosition, box.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestBox = box;
                    }
                }
            }
        }

        if (nearestBox != null)
        {
            controller.targetBox = nearestBox.transform;
            Debug.Log($"Isopod_{column} assigned to nearest box: {nearestBox.name}");
        }
        else
        {
            // Все коробки в колонке сломаны
            Debug.Log($"All boxes in column {column} are broken. Isopod will fall.");
            controller.OnAllBoxesBroken();
        }
    }

    // Метод для проверки, все ли коробки в колонке сломаны
    public bool AreAllBoxesBrokenInColumn(int column)
    {
        for (int row = 0; row < rows; row++)
        {
            int boxIndex = row * columns + column;
            if (boxIndex < allBoxes.Count)
            {
                BoxController boxController = allBoxes[boxIndex].GetComponent<BoxController>();
                if (boxController != null && !boxController.IsBroken())
                {
                    return false; // Нашли хотя бы одну целую коробку
                }
            }
        }
        return true; // Все коробки сломаны
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
    }

    public void ReturnToMenu()
    {
        // Очищаем всех жуков
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

        // Показываем меню
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);

        if (boxesContainer != null)
            boxesContainer.gameObject.SetActive(false);
    }
}