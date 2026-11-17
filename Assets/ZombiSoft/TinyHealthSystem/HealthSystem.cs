//==============================================================
// HealthSystem (Sin Regeneración Automática)
//==============================================================

using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance;

    [Header("Configuración de Vida")]
    public Image currentHealthBar;
    public Text healthText;
    public float hitPoint = 100f;
    public float maxHitPoint = 100f;

    //==============================================================
    // Awake & Start
    //==============================================================
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        UpdateGraphics();
    }

    // ELIMINAMOS EL UPDATE Y EL REGEN PARA QUE NO SE CURE SOLO

    //==============================================================
    // Health Logic
    //==============================================================
    private void UpdateHealthBar()
    {
        if (currentHealthBar != null)
        {
            float ratio = hitPoint / maxHitPoint;
            currentHealthBar.rectTransform.localPosition = new Vector3(currentHealthBar.rectTransform.rect.width * ratio - currentHealthBar.rectTransform.rect.width, 0, 0);
        }
        
        if (healthText != null)
        {
            healthText.text = hitPoint.ToString ("0") + "/" + maxHitPoint.ToString ("0");
        }
    }

    public void TakeDamage(float Damage)
    {
        hitPoint -= Damage;
        if (hitPoint < 1)
            hitPoint = 0;

        UpdateGraphics();
        StartCoroutine(PlayerHurts());
    }

    public void HealDamage(float Heal)
    {
        hitPoint += Heal;
        if (hitPoint > maxHitPoint) 
            hitPoint = maxHitPoint;

        UpdateGraphics();
    }

    public void SetMaxHealth(float max)
    {
        maxHitPoint += (int)(maxHitPoint * max / 100);
        UpdateGraphics();
    }

    //==============================================================
    // Update UI graphics
    //==============================================================
    private void UpdateGraphics()
    {
        UpdateHealthBar();
    }

    //==============================================================
    // Coroutine Player Hurts
    //==============================================================
    IEnumerator PlayerHurts()
    {
        if (PopupText.Instance != null)
        {
            PopupText.Instance.Popup("Ouch!", 1f, 1f); 
        }

        if (hitPoint < 1) 
        {
            yield return StartCoroutine(PlayerDied()); 
        }
        else
        {
            yield return null;
        }
    }

    //==============================================================
    // Hero is dead
    //==============================================================
    IEnumerator PlayerDied()
    {
        if (PopupText.Instance != null)
        {
            PopupText.Instance.Popup("You have died!", 1f, 1f); 
        }

        yield return null;
    }
}