using System.Collections;
using System.Collections.Generic;
using TMPro; // Для TextMeshPro
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas mainMenuCanvas;
    public Canvas mainGameCanvas; // Канвас для игры
    public Canvas gameOverCanvas; // Канвас для конца игры
    public Canvas leaderboardCanvas;
    public Button startButton;
    public Button leaderboardButton;
    public Button returnToMenuButton; // Кнопка на экране конца игры
    public Button returnFromLeaderboardButton;
    public TextMeshProUGUI killCounterText; // Счётчик во время игры
    public TextMeshProUGUI finalScoreText; // Финальный счёт на экране конца игры
    public Button endGameButton; // Кнопка завершения игры

    [Header("Backgrounds")]
    public Image menuBackground;    // Фон меню
    public Image gameBackground;    // Фон игры

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

    [Header("Leaderboard UI")]
    [SerializeField] private Transform leaderboardEntriesContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private TMP_FontAsset leaderboardFont;

    private List<GameObject> allBoxes = new List<GameObject>();
    private List<GameObject> allIsopods = new List<GameObject>();
    private int killCount = 0;
    private bool isGameActive = false;
    private int activeIsopodsCount = 0; // Новый счётчик активных жуков

    void Start()
    {
        // Скрываем все игровые элементы
        if (boxesContainer != null)
            boxesContainer.gameObject.SetActive(false);

        if (mainGameCanvas != null)
            mainGameCanvas.gameObject.SetActive(false);

        if (gameOverCanvas != null)
            gameOverCanvas.gameObject.SetActive(false);

        if (leaderboardCanvas != null) // скрываем таблицу
            leaderboardCanvas.gameObject.SetActive(false);

        // Показываем только меню
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);

        // Настраиваем кнопки
        startButton.onClick.AddListener(StartGame);
        returnToMenuButton.onClick.AddListener(ReturnToMainMenu);

        // Инициализируем счётчик
        UpdateKillCounter();

        // Настраиваем все кнопки
        startButton.onClick.AddListener(StartGame);
        leaderboardButton.onClick.AddListener(ShowLeaderboard);
        returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        endGameButton.onClick.AddListener(ForceEndGame); // Новая кнопка

        // Делаем кнопку завершения неактивной
        if (endGameButton != null)
            endGameButton.gameObject.SetActive(false);

        // Настраиваем фоны
        SetupBackgrounds();

        // Подписываемся на событие загрузки данных
        if (FirebaseLeaderboardManager.Instance != null)
        {
            FirebaseLeaderboardManager.Instance.OnLeaderboardLoaded += PopulateLeaderboardUI;
        }
    }

    private void PopulateLeaderboardUI(List<LeaderboardEntry> entries)
    {
        if (leaderboardEntriesContainer == null || leaderboardEntryPrefab == null)
        {
            Debug.LogError("Leaderboard UI references not set!");
            return;
        }

        Debug.Log($"Populating leaderboard with {entries.Count} entries");

        // Очищаем старые строки
        foreach (Transform child in leaderboardEntriesContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаём новые строки
        for (int i = 0; i < entries.Count; i++)
        {
            GameObject entryGO = Instantiate(leaderboardEntryPrefab, leaderboardEntriesContainer);
            TMP_Text entryText = entryGO.GetComponent<TMP_Text>();

            if (entryText == null)
            {
                Debug.LogError("Entry prefab has no TMP_Text component!");
                continue;
            }

            // Применяем кастомный шрифт
            if (leaderboardFont != null)
            {
                entryText.font = leaderboardFont;
            }

            // Форматируем строку
            string place = (i + 1).ToString();
            string name = entries[i].playerName;
            string score = entries[i].score.ToString();
            entryText.text = $"{place}. {name} - {score}";
        }

        // Если записей нет
        if (entries.Count == 0)
        {
            GameObject emptyGO = Instantiate(leaderboardEntryPrefab, leaderboardEntriesContainer);
            TMP_Text emptyText = emptyGO.GetComponent<TMP_Text>();
            emptyText.text = "No records yet!";
            emptyText.alignment = TextAlignmentOptions.Center;

            if (leaderboardFont != null)
                emptyText.font = leaderboardFont;
        }
    }

    void SetupBackgrounds()
    {
        // Показываем фон меню
        if (menuBackground != null)
            menuBackground.gameObject.SetActive(true);

        // Скрываем фон игры
        if (gameBackground != null)
            gameBackground.gameObject.SetActive(false);

        // Можно добавить отдельный фон для таблицы
        // if (leaderboardBackground != null)
        //    leaderboardBackground.gameObject.SetActive(false);
    }

    public void ShowLeaderboard()
    {
        Debug.Log("Showing leaderboard...");

        // Скрываем меню
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(false);

        // Показываем таблицу лидеров
        if (leaderboardCanvas != null)
        {
            leaderboardCanvas.gameObject.SetActive(true);

            // Загружаем данные
            if (FirebaseLeaderboardManager.Instance != null)
            {
                FirebaseLeaderboardManager.Instance.LoadTop10Leaderboard();
            }
        }
    }

    private void OnDestroy()
    {
        if (FirebaseLeaderboardManager.Instance != null)
        {
            FirebaseLeaderboardManager.Instance.OnLeaderboardLoaded -= PopulateLeaderboardUI;
        }
    }

    void StartGame()
    {
        Debug.Log("=== GAME STARTING ===");

        // Активируем кнопку завершения игры
        if (endGameButton != null)
            endGameButton.gameObject.SetActive(true);

        // Переключаем фоны
        if (menuBackground != null)
            menuBackground.gameObject.SetActive(false);

        if (gameBackground != null)
            gameBackground.gameObject.SetActive(true);

        // Скрываем меню
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(false);

        // Показываем игровой канвас
        if (mainGameCanvas != null)
            mainGameCanvas.gameObject.SetActive(true);

        if (killCounterText != null)
            killCounterText.gameObject.SetActive(true);

        // Скрываем экран конца игры
        if (gameOverCanvas != null)
            gameOverCanvas.gameObject.SetActive(false);

        // Сбрасываем счётчик и таймер
        killCount = 0;
        isGameActive = true;
        UpdateKillCounter();

        // Создаем коробки
        if (boxesContainer == null)
        {
            GameObject container = new GameObject("Boxes Container");
            boxesContainer = container.transform;
        }
        boxesContainer.gameObject.SetActive(true);

        CreateBoxes();

        // Спавним жуков
        SpawnIsopods();

        Debug.Log("=== GAME STARTED ===");
    }

    public void ForceEndGame()
    {
        if (!isGameActive) return;

        Debug.Log("Game force ended by player");

        // Запускаем анимацию падения всех жуков
        MakeAllIsopodsFall();

        // Ждём немного и показываем результаты
        Invoke("ShowGameOverScreen", 2f);
    }

    void ShowGameOverScreen()
    {
        // Вызываем стандартный конец игры
        EndGame();
    }

    public void ReturnToMainMenuFromLeaderboard()
    {
        Debug.Log("Returning to main menu from leaderboard...");

        // Скрываем таблицу
        if (leaderboardCanvas != null)
            leaderboardCanvas.gameObject.SetActive(false);

        // Показываем меню
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);
    }

    void MakeAllIsopodsFall()
    {
        foreach (GameObject isopod in allIsopods)
        {
            if (isopod != null && isopod.activeInHierarchy)
            {
                IsopodController controller = isopod.GetComponent<IsopodController>();
                if (controller != null)
                {
                    // Заставляем жука упасть (включая отмену респавна)
                    controller.ForceFall();
                }
            }
        }

        // Также обрабатываем неактивных жуков, которые ждут респавна
        StartCoroutine(CleanupAllIsopods());
    }

    IEnumerator CleanupAllIsopods()
    {
        // Ждём немного чтобы все Invoke/корутины завершились
        yield return new WaitForSeconds(1.5f);

        // Принудительно деактивируем всех жуков
        foreach (GameObject isopod in allIsopods)
        {
            if (isopod != null)
            {
                isopod.SetActive(false);
            }
        }
    }

    // Обновите EndGame() чтобы отменить все Invoke
    void EndGame()
    {
        if (!isGameActive) return;

        isGameActive = false;
        Debug.Log("=== GAME ENDED ===");

        // Отменяем все запланированные вызовы
        CancelInvoke();

        // Отменяем все корутины на этом объекте
        StopAllCoroutines();
        Debug.Log($"Final score: {killCount}");

        // Деактивируем кнопку завершения
        if (endGameButton != null)
            endGameButton.gameObject.SetActive(false);

        // Скрываем игровой канвас
        if (killCounterText != null)
            killCounterText.gameObject.SetActive(false);

        // Показываем экран конца игры
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(true);

            // Обновляем финальный счёт
            if (finalScoreText != null)
            {
                finalScoreText.text = $"FINAL SCORE: {killCount}";
            }
        }

        // Останавливаем всех жуков
        foreach (GameObject isopod in allIsopods)
        {
            IsopodController controller = isopod.GetComponent<IsopodController>();
            if (controller != null)
            {
                controller.SetGameRunning(false);
            }
        }

        // Очищаем игровые объекты (опционально)
        // ClearGameObjects();

        FirebaseLeaderboardManager.Instance.SubmitScore(killCount, "PlayerName");
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu");

        // Скрываем экран конца игры
        if (gameOverCanvas != null)
            gameOverCanvas.gameObject.SetActive(false);

        // Очищаем игровые объекты
        ClearGameObjects();

        // Показываем главное меню
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);

        // Переключаем фоны обратно
        if (menuBackground != null)
            menuBackground.gameObject.SetActive(true);

        if (gameBackground != null)
            gameBackground.gameObject.SetActive(false);
    }

    void ClearGameObjects()
    {
        // Отписываемся от событий
        foreach (GameObject isopod in allIsopods)
        {
            IsopodController controller = isopod?.GetComponent<IsopodController>();
            if (controller != null)
            {
                controller.OnIsopodDespawned -= HandleIsopodDespawned;
            }

            if (isopod != null) Destroy(isopod);
        }
        allIsopods.Clear();
        activeIsopodsCount = 0;

        // Очищаем коробки
        foreach (GameObject box in allBoxes)
        {
            if (box != null) Destroy(box);
        }
        allBoxes.Clear();

        if (boxesContainer != null)
            boxesContainer.gameObject.SetActive(false);
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
                controller.gameController = this; // Передаем ссылку

                // Подписываемся на событие деспавна жука
                controller.OnIsopodDespawned += HandleIsopodDespawned;

                AssignNearestBoxToIsopod(controller, i);
                controller.SetGameRunning(true);
            }

            allIsopods.Add(isopod);
        }

        // Устанавливаем начальное количество активных жуков
        activeIsopodsCount = allIsopods.Count;

        Debug.Log($"Spawned {allIsopods.Count} isopods");
    }

    // Новый метод: обработка деспавна жука
    void HandleIsopodDespawned()
    {
        if (!isGameActive) return;

        activeIsopodsCount--;
        Debug.Log($"Isopod despawned. Active isopods: {activeIsopodsCount}");

        // Если все жуки деспавнились - заканчиваем игру
        if (activeIsopodsCount <= 0)
        {
            EndGame();
        }
    }

    // Метод для увеличения счётчика убийств (будет вызываться из IsopodController)
    public void AddKill()
    {
        killCount++;
        UpdateKillCounter();
        Debug.Log($"Kill count: {killCount}");
    }

    void UpdateKillCounter()
    {
        if (killCounterText != null)
        {
            killCounterText.text = killCount.ToString();
        }
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    public void AssignNearestBoxToIsopod(IsopodController controller, int column)
    {
        if (allBoxes.Count == 0) return;

        GameObject nearestBox = null;
        float nearestDistance = float.MaxValue;
        Vector3 isopodPosition = controller.transform.position;

        for (int row = 0; row < rows; row++)
        {
            int boxIndex = row * columns + column;
            if (boxIndex < allBoxes.Count)
            {
                GameObject box = allBoxes[boxIndex];
                BoxController boxController = box.GetComponent<BoxController>();

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
        }
        else
        {
            controller.OnAllBoxesBroken();
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