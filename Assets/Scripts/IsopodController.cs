using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class IsopodController : MonoBehaviour
{
    // === Настройки в редакторе ===
    [Header("Movement Settings")]
    public float upSpeed = 0.5f;
    public Transform targetBox;
    public int assignedColumn = 0;
    public Vector3 spawnPosition; // Добавлено public для доступа из GameController

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

    [Header("Box Damage Settings")]
    public int damageToBoxPerSecond = 10;

    [Header("Fall Settings")]
    public float fallSpeed = 3f; // Скорость падения

    // === Внутренние переменные ===
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private bool isAlive = true;
    private bool hasReachedTarget = false;
    private BoxController currentBoxController;

    // Новые переменные для падения
    private bool isFalling = false;
    private bool shouldDespawn = false;

    // Ссылка на GameController
    [HideInInspector] public GameController gameController;
    [HideInInspector] public bool canMove = false;

    // Добавьте это в начало класса (после других полей):
    public delegate void IsopodDespawnedHandler();
    public event IsopodDespawnedHandler OnIsopodDespawned;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<Physics2DRaycaster>() == null)
        {
            mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        UpdateSprite();
    }

    void Update()
    {
        // Если жук падает - обрабатываем падение
        if (isFalling)
        {
            HandleFalling();
        }
    }

    void FixedUpdate()
    {
        // Проверяем активна ли игра
        if (!canMove || isFalling || !IsGameActive()) return;

        if (isAlive && !hasReachedTarget && targetBox != null)
        {
            float moveAmount = upSpeed * Time.fixedDeltaTime;
            transform.Translate(0, moveAmount, 0);

            if (transform.position.y >= targetBox.position.y)
            {
                hasReachedTarget = true;
                OnReachedBox();
            }
        }
    }

    private bool IsGameActive()
    {
        if (gameController != null)
        {
            return gameController.IsGameActive();
        }
        return false;
    }

    private void OnMouseDown()
    {
        // Можно кликать только если игра активна
        if (!isAlive || !canMove || isFalling || !IsGameActive()) return;

        Debug.Log("Isopod clicked!");
        TakeDamage(damagePerClick);
        PlaySound(hitSound);
    }

    void HandleFalling()
    {
        // Двигаемся вниз
        transform.Translate(0, -fallSpeed * Time.deltaTime, 0);

        // Проверяем, упал ли жук достаточно низко для деспавна
        if (transform.position.y < spawnPosition.y - 10f) // На 10 единиц ниже точки спавна
        {
            Despawn();
        }
    }

    void Despawn()
    {
        Debug.Log($"{name} despawned (fell off screen)");

        // Останавливаем атаку если была
        if (currentBoxController != null)
        {
            currentBoxController.StopTakingDamage();
            currentBoxController = null;
        }

        // Вызываем событие деспавна
        OnIsopodDespawned?.Invoke();

        // Отключаем жука
        gameObject.SetActive(false);
    }

    void OnReachedBox()
    {
        Debug.Log($"{name} reached the box");

        if (targetBox != null)
        {
            currentBoxController = targetBox.GetComponent<BoxController>();
            if (currentBoxController != null)
            {
                currentBoxController.StartTakingDamage(this);
            }
        }
    }

    // Вызывается когда текущая коробка ломается
    public void OnTargetBoxBroken()
    {
        if (currentBoxController != null)
        {
            currentBoxController.StopTakingDamage();
            currentBoxController = null;
        }

        // Ищем новую ближайшую коробку
        if (gameController != null)
        {
            gameController.AssignNearestBoxToIsopod(this, assignedColumn);

            // Если AssignNearestBoxToIsopod не назначил новую коробку,
            // значит все коробки в колонке сломаны
            if (targetBox == null)
            {
                StartFalling();
            }
            else
            {
                // Есть новая коробка - сбрасываем состояние движения
                hasReachedTarget = false;
            }
        }
    }

    // Вызывается из GameController когда все коробки в колонке сломаны
    public void OnAllBoxesBroken()
    {
        Debug.Log($"{name}: All boxes in column {assignedColumn} are broken!");
        StartFalling();
    }

    void StartFalling()
    {
        isFalling = true;
        canMove = false;
        hasReachedTarget = true;

        // Останавливаем атаку если была
        if (currentBoxController != null)
        {
            currentBoxController.StopTakingDamage();
            currentBoxController = null;
        }

        // Сбрасываем target
        targetBox = null;

        Debug.Log($"{name} started falling");
    }

    void TakeDamage(int damage)
    {
        if (!isAlive) return;

        currentHealth -= damage;

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
        UpdateSprite();
        PlaySound(killSound);

        // Уведомляем GameController об убийстве
        if (gameController != null)
        {
            gameController.AddKill();
        }

        Invoke("Respawn", 1f);
    }

    void Respawn()
    {
        currentHealth = maxHealth;
        isAlive = true;
        hasReachedTarget = false;
        isFalling = false;
        transform.position = spawnPosition;

        // Останавливаем атаку
        if (currentBoxController != null)
        {
            currentBoxController.StopTakingDamage();
            currentBoxController = null;
        }

        UpdateSprite();

        // Просим GameController назначить новую коробку
        if (gameController != null)
        {
            gameController.AssignNearestBoxToIsopod(this, assignedColumn);
            canMove = true;
        }

        // Уведомляем GameController что жук вернулся
        // (если нужно отслеживать активных жуков)
        // gameController?.HandleIsopodRespawned();
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void ForceFall()
    {
        if (isFalling) return; // Уже падает

        Debug.Log($"{name} forced to fall");

        // Останавливаем атаку на коробку
        if (currentBoxController != null)
        {
            currentBoxController.StopTakingDamage();
            currentBoxController = null;
        }

        // Запускаем падение
        StartFalling();

        // Можно добавить эффект (например, изменение цвета)
        if (spriteRenderer != null)
        {
            // Временно меняем цвет на красный
            StartCoroutine(FlashRed());
        }
    }

    // Эффект мигания при падении
    IEnumerator FlashRed()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.3f);

        spriteRenderer.color = originalColor;

        yield return new WaitForSeconds(0.3f);

        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.3f);

        spriteRenderer.color = originalColor;
    }

    public void SetGameRunning(bool running)
    {
        canMove = running;
    }
}