using UnityEngine;
using UnityEngine.UI;

public class ResourceUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider weightSlider;
    [SerializeField] private Resource resource;
    public float resourceMaxWeight;

    private void Start()
    {
        // Initialize health from NetworkVariable
        weightSlider.value = 1;
        resource.weight.OnValueChanged += Resource_OnWeightChanged;
        Hide();
    }
    private void Resource_OnWeightChanged(float oldValue, float newValue)
    {
        if (!gameObject.activeSelf && newValue != resourceMaxWeight) Show();

        weightSlider.value = newValue / resourceMaxWeight;

        if (resource.weight.Value <= 0) Hide();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}

