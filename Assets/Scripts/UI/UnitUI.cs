using Assets.Scripts.Unit;
using UnityEngine;
using UnityEngine.UI;

public class UnitUI : MonoBehaviour
{
    private Unit unit;

    [Header("Stats Sliders References")]
    [SerializeField] private Transform statsSliders;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    
    [Header("Resource Indicator References")]
    [SerializeField] private Transform holyResourceIcon;
    [SerializeField] private Transform anyResourceIcon;
    [SerializeField] private Transform resourceIndicator;

    private void Awake()
    {
        unit = GetComponentInParent<Unit>();

        unit.DelegateManager.OnResourceGathered += Unit_OnResourceGathered;
    }

    public void SetUp()
    {
        // Show Stamina/Health only for local player
        statsSliders.gameObject.SetActive(true);
        healthSlider.gameObject.SetActive(true);
        staminaSlider.gameObject.SetActive(true);

        healthSlider.value = 1;
        staminaSlider.value = 1;

        unit.DelegateManager.OnHealthChanged += Unit_OnHealthChanged;
        unit.DelegateManager.OnStaminaChanged += Unit_OnStaminaChanged;
    }

    private void Unit_OnResourceGathered(bool isHolyResource)
    {
        // If already has any icon, don't enable its' background again
        if (!resourceIndicator.gameObject.activeSelf)
        {
            resourceIndicator.gameObject.SetActive(true);
            unit.DelegateManager.OnResourceUnload += Unit_OnResourceUnload;
        }

        if (isHolyResource)
        {
            holyResourceIcon.gameObject.SetActive(true);
            // In case Unit gathered non-holy resource before
            anyResourceIcon.gameObject.SetActive(false);

            // Player either going to return resource to base or die and disable any Icon in both cases
            unit.DelegateManager.OnResourceGathered -= Unit_OnResourceGathered;
            return;
        }

        anyResourceIcon.gameObject.SetActive(true);
    }

    private void Unit_OnResourceUnload()
    {
        // Unsubscribe until any resource gathered
        unit.DelegateManager.OnResourceUnload -= Unit_OnResourceUnload;

        resourceIndicator.gameObject.SetActive(false);
        holyResourceIcon.gameObject.SetActive(false);
        anyResourceIcon.gameObject.SetActive(false);
        unit.DelegateManager.OnResourceGathered += Unit_OnResourceGathered;
    }

    private void Unit_OnStaminaChanged(float stamina)
    {
        staminaSlider.value = stamina / unit.Config.StaminaMax;
    }

    private void Unit_OnHealthChanged(float health)
    {
        healthSlider.value = health / unit.Config.HealthMax;
    }
}
