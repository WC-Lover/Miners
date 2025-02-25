using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class UnitUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Unit unit;
    public float unitMaxHealth;
    public float unitMaxStamina;

    private void Start()
    {
        // Initialize health from NetworkVariable
        if (!unit.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }
        healthSlider.value = 1;
        staminaSlider.value = 1;
        unit.health.OnValueChanged += Unit_OnHealthChanged;
        unit.OnStaminaChanged += Unit_OnStaminaChanged; ;
    }

    private void Unit_OnStaminaChanged(object sender, Unit.OnStaminaChangedEventArgs e)
    {
        staminaSlider.value = e.stamina / unitMaxStamina;
    }

    private void Unit_OnHealthChanged(float oldValue, float newValue)
    {
        healthSlider.value = newValue / unitMaxHealth;
    }
}
