using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class UnitUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Transform holyResourceIcon;
    [SerializeField] private Transform anyResourceIcon;
    [SerializeField] private Transform resourceIndicator;
    [SerializeField] private Unit unit;
    private float unitMaxHealth;
    private float unitMaxStamina;
    private bool subscribedToUnload = false;

    private void Unit_OnUnloadedResources(object sender, System.EventArgs e)
    {
        // Unsubscribe until any resource gathered
        unit.OnUnloadedResources -= Unit_OnUnloadedResources;
        subscribedToUnload = false;

        resourceIndicator.gameObject.SetActive(false);
        holyResourceIcon.gameObject.SetActive(false);
        anyResourceIcon.gameObject.SetActive(false);
        unit.OnResourceGathered += Unit_OnResourceGathered;
    }

    private void Unit_OnResourceGathered(object sender, Unit.OnResourceGatheredEventArgs e)
    {
        // If already has any icon, don't enable its' background again
        if (!resourceIndicator.gameObject.activeSelf) resourceIndicator.gameObject.SetActive(true);

        if (!subscribedToUnload)
        {
            subscribedToUnload = true;
            unit.OnUnloadedResources += Unit_OnUnloadedResources;
        }

        if (e.holyResource)
        {
            holyResourceIcon.gameObject.SetActive(true);
            // In case Unit used to show non-holy resource Icon
            anyResourceIcon.gameObject.SetActive(false);

            // Player either going to return resource to base or die and disable any Icon in both cases
            unit.OnResourceGathered -= Unit_OnResourceGathered;
            return;
        }

        if (!anyResourceIcon.gameObject.activeSelf)
        {
            anyResourceIcon.gameObject.SetActive(true);
        }
    }

    private void Unit_OnStaminaChanged(object sender, Unit.OnStaminaChangedEventArgs e)
    {
        staminaSlider.value = e.stamina / unitMaxStamina;
    }

    private void Unit_OnHealthChanged(float oldValue, float newValue)
    {
        healthSlider.value = newValue / unitMaxHealth;
    }

    private void Unit_OnUnitDespawn(object sender, EventArgs e)
    {
        // Unsubscribe from all events
        unit.health.OnValueChanged -= Unit_OnHealthChanged;
        unit.OnStaminaChanged -= Unit_OnStaminaChanged;
        unit.OnResourceGathered -= Unit_OnResourceGathered;
        unit.OnUnitDespawn -= Unit_OnUnitDespawn;
        unit.OnUnloadedResources -= Unit_OnUnloadedResources;
    }

    public void SetUp(float health, float staminaMax)
    {
        if (!unit.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        unitMaxHealth = health;
        unitMaxStamina = staminaMax;

        healthSlider.value = 1;
        staminaSlider.value = 1;

        unit.health.OnValueChanged += Unit_OnHealthChanged;
        unit.OnStaminaChanged += Unit_OnStaminaChanged;
        unit.OnResourceGathered += Unit_OnResourceGathered;
        unit.OnUnitDespawn += Unit_OnUnitDespawn;
    }
}
