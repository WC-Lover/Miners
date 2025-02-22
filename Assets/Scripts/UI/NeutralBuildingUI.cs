using UnityEngine;
using UnityEngine.UI;

public class NeutralBuildingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider claimingSlider;
    [SerializeField] private Building building;
    private float claimPercentageMax = 100;

    private void Start()
    {
        // Initialize health from NetworkVariable
        claimingSlider.value = 0;
        building.OnClaimingPercentageChanged += Building_OnClaimingPercentageChanged;
        Hide();
    }

    private void Building_OnClaimingPercentageChanged(object sender, Building.OnClaimingPercentageChangedEventArgs e)
    {
        if (!gameObject.activeSelf) Show();
        // Switch colors if local player claiming(CYAN) or enemy player claiming(RED)
        claimingSlider.value = e.claimingPercentage / claimPercentageMax;
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
