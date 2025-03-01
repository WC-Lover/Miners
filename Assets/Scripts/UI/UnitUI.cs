using System;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using static Unit;

public class UnitUI : MonoBehaviour
{
    [Header("Stats Sliders References")]
    [SerializeField] private Transform statsSliders;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    
    [Header("Resource Indicator References")]
    [SerializeField] private Transform holyResourceIcon;
    [SerializeField] private Transform anyResourceIcon;
    [SerializeField] private Transform resourceIndicator;
    
    [Space]
    [SerializeField] private Unit unit;

    private bool subscribedToUnload = false;

    private void Awake()
    {
        // Show resource gathered indicatior on all clients
        unit.OnResourceGathered += Unit_OnResourceGathered;
    }

    public void SetUp()
    {
        // Show Stamina/Health only for local player
        statsSliders.gameObject.SetActive(true);
        healthSlider.gameObject.SetActive(true);
        staminaSlider.gameObject.SetActive(true);

        healthSlider.value = 1;
        staminaSlider.value = 1;

        unit.OnHealthChanged += Unit_OnHealthChanged;
        unit.OnStaminaChanged += Unit_OnStaminaChanged;
        unit.OnUnitDespawn += Unit_OnUnitDespawn;
    }

    private void Unit_OnResourceGathered(bool isHolyResource)
    {
        // If already has any icon, don't enable its' background again
        if (!resourceIndicator.gameObject.activeSelf) resourceIndicator.gameObject.SetActive(true);

        if (!subscribedToUnload)
        {
            subscribedToUnload = true;
            unit.OnUnloadedResources += Unit_OnUnloadedResources;
        }

        if (isHolyResource)
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

    private void Unit_OnUnloadedResources()
    {
        // Unsubscribe until any resource gathered
        unit.OnUnloadedResources -= Unit_OnUnloadedResources;
        subscribedToUnload = false;

        resourceIndicator.gameObject.SetActive(false);
        holyResourceIcon.gameObject.SetActive(false);
        anyResourceIcon.gameObject.SetActive(false);
        unit.OnResourceGathered += Unit_OnResourceGathered;
    }

    private void Unit_OnUnitDespawn()
    {
        // Unsubscribe from all events
        unit.OnResourceGathered -= Unit_OnResourceGathered;
        unit.OnHealthChanged -= Unit_OnHealthChanged;
        unit.OnStaminaChanged -= Unit_OnStaminaChanged;
        unit.OnUnloadedResources -= Unit_OnUnloadedResources;
        unit.OnUnitDespawn -= Unit_OnUnitDespawn;
    }

    private void Unit_OnStaminaChanged(float stamina, float staminaMax)
    {
        staminaSlider.value = stamina / staminaMax;
    }

    private void Unit_OnHealthChanged(float health, float healthMax)
    {
        healthSlider.value = health / healthMax;
    }
}
