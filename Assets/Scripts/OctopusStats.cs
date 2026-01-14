using UnityEngine;
using UnityEngine.UI;

public class OctopusStats : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill;
    [SerializeField] private Image inkFill;

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float maxInk = 100f;

    [SerializeField] private float healthRegen = 2f;
    [SerializeField] private float staminaRegen = 4f;
    [SerializeField] private float inkRegen = 4f;

    [SerializeField] private float healthRegenDelay = 1.5f;
    [SerializeField] private float staminaRegenDelay = 0.6f;
    [SerializeField] private float inkRegenDelay = 0.8f;

    public float Health { get; private set; }
    public float Stamina { get; private set; }
    public float Ink { get; private set; }

    public float Health01 => maxHealth <= 0f ? 0f : Health / maxHealth;
    public float Stamina01 => maxStamina <= 0f ? 0f : Stamina / maxStamina;
    public float Ink01 => maxInk <= 0f ? 0f : Ink / maxInk;

    private float healthRegenBlockedUntil;
    private float staminaRegenBlockedUntil;
    private float inkRegenBlockedUntil;

    private void Awake()
    {
        Health = maxHealth;
        Stamina = maxStamina;
        Ink = maxInk;

        UpdateUI();
    }

    private void Update()
    {
        Regenerate();
        UpdateUI();
    }

    private void Regenerate()
    {
        float dt = Time.deltaTime;

        if (Time.time >= healthRegenBlockedUntil && Health < maxHealth)
            Health = Mathf.Min(maxHealth, Health + healthRegen * dt);

        if (Time.time >= staminaRegenBlockedUntil && Stamina < maxStamina)
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * dt);

        if (Time.time >= inkRegenBlockedUntil && Ink < maxInk)
            Ink = Mathf.Min(maxInk, Ink + inkRegen * dt);
    }

    private void UpdateUI()
    {
        if (healthFill != null) healthFill.fillAmount = Health01;
        if (staminaFill != null) staminaFill.fillAmount = Stamina01;
        if (inkFill != null) inkFill.fillAmount = Ink01;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        Health = Mathf.Max(0f, Health - amount);
        healthRegenBlockedUntil = Time.time + healthRegenDelay;

        // if (Health <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        Health = Mathf.Min(maxHealth, Health + amount);
    }

    public bool UseStamina(float amount)
    {
        if (amount <= 0f) return true;

        if (Stamina < amount) return false;

        Stamina -= amount;
        staminaRegenBlockedUntil = Time.time + staminaRegenDelay;
        return true;
    }

    public bool UseInk(float amount)
    {
        if (amount <= 0f) return true;

        if (Ink < amount) return false;

        Ink -= amount;
        inkRegenBlockedUntil = Time.time + inkRegenDelay;
        return true;
    }

    public bool HasStamina(float amount) => Stamina >= amount;
    public bool HasInk(float amount) => Ink >= amount;

    public void RefillAll()
    {
        Health = maxHealth;
        Stamina = maxStamina;
        Ink = maxInk;
        UpdateUI();
    }
}