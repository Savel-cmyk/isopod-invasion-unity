using System.Collections;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Sprites for Health States")]
    public Sprite fullHealthSprite;    // 100-67%
    public Sprite mediumHealthSprite;  // 66-34%
    public Sprite lowHealthSprite;     // 33-1%
    public Sprite brokenSprite;        // 0% - сломанная коробка

    [Header("Damage Settings")]
    public int damagePerSecond = 10;   // Урон в секунду от жука

    // Ссылки на компоненты
    private SpriteRenderer spriteRenderer;
    private bool isBroken = false;

    // Ссылка на жука, который атакует эту коробку
    private IsopodController attackingIsopod;
    private Vector3 originalScale;

    private Coroutine damageCoroutine;

    public void StartTakingDamage(IsopodController isopod)
    {
        if (!isBroken && damageCoroutine == null)
        {
            attackingIsopod = isopod;

            // Запускаем корутину для урона раз в секунду
            damageCoroutine = StartCoroutine(DamageOverTime());

            Debug.Log($"Box {name} started taking {damagePerSecond} damage per second");
        }
    }

    public void StopTakingDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
        attackingIsopod = null;
    }

    IEnumerator DamageOverTime()
    {
        while (!isBroken && attackingIsopod != null)
        {
            // Ждем 1 секунду
            yield return new WaitForSeconds(1f);

            if (!isBroken && attackingIsopod != null)
            {
                // Наносим урон раз в секунду
                TakeDamage(damagePerSecond);
            }
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;

        // Сохраняем оригинальный размер
        originalScale = transform.localScale;

        UpdateBoxSprite();

        Debug.Log($"Box {name} created with {currentHealth} HP");
    }

    public void TakeDamage(float damage)
    {
        if (isBroken) return;

        // Преобразуем float в int, округляя вверх
        int intDamage = Mathf.CeilToInt(damage);
        currentHealth -= intDamage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            BreakBox();
        }
        else
        {
            UpdateBoxSprite();
        }
    }

    void UpdateBoxSprite()
    {
        if (spriteRenderer == null) return;

        float healthPercentage = (float)currentHealth / maxHealth * 100;

        if (healthPercentage > 66)
        {
            spriteRenderer.sprite = fullHealthSprite;
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
            spriteRenderer.sprite = brokenSprite;
        }

        // Принудительно устанавливаем одинаковый размер
        transform.localScale = originalScale;

        spriteRenderer.sortingLayerName = "Boxes";
        spriteRenderer.sortingOrder = 1;
    }

    void BreakBox()
    {
        isBroken = true;
        UpdateBoxSprite();

        // Отключаем коллайдер, чтобы жук не взаимодействовал с сломанной коробкой
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Debug.Log($"Box {name} is broken!");

        // Уведомляем жука, что коробка сломана
        if (attackingIsopod != null)
        {
            attackingIsopod.OnTargetBoxBroken();
        }
    }

    // Геттеры для проверки состояния
    public bool IsBroken()
    {
        return isBroken;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth * 100;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}