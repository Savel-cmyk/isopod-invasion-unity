using System.Collections;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Sprites for Health States")]
    public Sprite fullHealthSprite;
    public Sprite mediumHealthSprite;
    public Sprite lowHealthSprite;
    public Sprite brokenSprite;

    [Header("Damage Settings")]
    public int damagePerSecond = 10;

    private SpriteRenderer spriteRenderer;
    private bool isBroken = false;
    private IsopodController attackingIsopod;
    private Vector3 originalScale;
    private Coroutine damageCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        UpdateBoxSprite();
    }

    public void StartTakingDamage(IsopodController isopod)
    {
        if (!isBroken && damageCoroutine == null)
        {
            attackingIsopod = isopod;
            damageCoroutine = StartCoroutine(DamageOverTime());
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
            yield return new WaitForSeconds(1f);
            if (!isBroken && attackingIsopod != null)
            {
                TakeDamage(damagePerSecond);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isBroken) return;

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

        transform.localScale = originalScale;
        spriteRenderer.sortingLayerName = "Boxes";
        spriteRenderer.sortingOrder = 1;
    }

    void BreakBox()
    {
        isBroken = true;
        UpdateBoxSprite();

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        if (attackingIsopod != null)
        {
            attackingIsopod.OnTargetBoxBroken();
        }
    }

    public bool IsBroken()
    {
        return isBroken;
    }
}