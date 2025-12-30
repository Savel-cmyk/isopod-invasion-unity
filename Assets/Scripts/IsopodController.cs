using UnityEngine;
using UnityEngine.EventSystems;

public class IsopodController : MonoBehaviour
{
    // === Настройки в редакторе ===
    [Header("Movement Settings")]
    public float upSpeed = 0.5f; // Значение по умолчанию
    public Transform targetBox;
    public int assignedColumn = 0;

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    public int damagePerClick = 34;

    [Header("Sprites for Health States")]
    public Sprite highHealthSprite;
    public Sprite mediumHealthSprite;
    public Sprite lowHealthSprite;
    public Sprite deadSprite;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip killSound;

    // === Внутренние переменные ===
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private bool isAlive = true;
    private bool hasReachedTarget = false;
    private Vector3 spawnPosition;

    // Контроль движения - публичное поле для отладки
    [HideInInspector] public bool canMove = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // НЕ сохраняем позицию здесь!
        // initialPosition будет установлена позже

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<Physics2DRaycaster>() == null)
        {
            mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        // Теперь сохраняем позицию в Start(), после того как GameController установил её
        spawnPosition = transform.position;
        UpdateSprite();

        Debug.Log($"Isopod initialized at: {spawnPosition}");
    }

    void Update()
    {
        // ДЛЯ ОТЛАДКИ: показываем состояние
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log($"Isopod Debug - CanMove: {canMove}, Alive: {isAlive}, HasReached: {hasReachedTarget}, Target: {targetBox?.name}");
        }
    }

    void FixedUpdate()
    {
        // Двигаемся только если можем
        if (!canMove)
        {
            // Debug.Log("Can't move yet"); // Раскомментируйте для отладки
            return;
        }

        if (isAlive && !hasReachedTarget && targetBox != null)
        {
            // Движение с учетом Time.deltaTime
            float moveAmount = upSpeed * Time.fixedDeltaTime;
            transform.Translate(0, moveAmount, 0);

            // Debug.Log($"Moving up: {moveAmount}, Pos: {transform.position.y}, Target: {targetBox.position.y}");

            if (transform.position.y >= targetBox.position.y)
            {
                hasReachedTarget = true;
                Debug.Log($"Reached target box at {targetBox.position.y}");
            }
        }
        else if (targetBox == null)
        {
            Debug.LogWarning("Target box is null!");
        }
    }

    private void OnMouseDown()
    {
        if (!isAlive || !canMove) return;

        Debug.Log("Isopod clicked!");
        TakeDamage(damagePerClick);
        PlaySound(hitSound);
    }

    void TakeDamage(int damage)
    {
        if (!isAlive) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            UpdateSprite();
        }
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        float healthPercentage = (float)currentHealth / maxHealth * 100;

        if (healthPercentage > 66)
        {
            spriteRenderer.sprite = highHealthSprite;
        }
        else if (healthPercentage > 33)
        {
            spriteRenderer.sprite = mediumHealthSprite;
        }
        else if (healthPercentage > 0)
        {
            spriteRenderer.sprite = lowHealthSprite;
        }
        else
        {
            spriteRenderer.sprite = deadSprite;
        }

        spriteRenderer.sortingLayerName = "Insects";
        spriteRenderer.sortingOrder = 10;
    }

    void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        Debug.Log($"{gameObject.name} died!");

        UpdateSprite();
        PlaySound(killSound);

        Invoke("Respawn", 1f);
    }

    void Respawn()
    {
        // Сбрасываем все параметры
        currentHealth = maxHealth;
        isAlive = true;
        hasReachedTarget = false;

        // Возвращаемся на позицию спавна
        transform.position = spawnPosition;

        // Назначаем новую случайную коробку
        AssignNewRandomBox();

        // Устанавливаем начальный спрайт
        UpdateSprite();

        Debug.Log($"{gameObject.name} respawned at position: {spawnPosition}");
    }

    void AssignNewRandomBox()
    {
        // Находим GameController
        GameController gameController = FindObjectOfType<GameController>();
        if (gameController != null)
        {
            // Вызываем публичный метод для назначения случайной коробки
            gameController.AssignRandomBoxToIsopod(this, assignedColumn);
        }
        else
        {
            Debug.LogWarning("GameController not found for box assignment");
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Метод для включения/выключения движения
    public void SetGameRunning(bool running)
    {
        canMove = running;
        Debug.Log($"Isopod canMove set to: {running}");

        // Если игра началась, убедимся что targetBox назначен
        if (running && targetBox == null)
        {
            Debug.LogWarning("Isopod has no target box assigned!");

            // Попробуем найти коробку автоматически
            GameObject foundBox = GameObject.Find("Box_4_0");
            if (foundBox != null)
            {
                targetBox = foundBox.transform;
                Debug.Log($"Auto-assigned target: {foundBox.name}");
            }
        }
    }
}