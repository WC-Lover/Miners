using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBuildingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider unitSpawnSlider;
    [SerializeField] private Slider buildingXPSlider;
    [SerializeField] private Building building;

    private void Start()
    {
        // Initialize health from NetworkVariable
        unitSpawnSlider.value = 1;
        buildingXPSlider.value = 0;
        building.OnUnitSpawnTimeChanged += Building_OnUnitSpawnTimeChanged;
        building.OnBuildingXPChanged += Building_OnBuildingXPChanged;
    }

    private void Building_OnBuildingXPChanged(object sender, Building.OnBuildingXPChangedEventArgs e)
    {
        buildingXPSlider.value = e.buildingXP / e.buildingXPMax;
    }

    private void Building_OnUnitSpawnTimeChanged(object sender, Building.OnUnitSpawnTimeChangedEventArgs e)
    {
        // Switch colors if local player claiming(CYAN) or enemy player claiming(RED)
        unitSpawnSlider.value = 1 - (e.unitSpawnTimer / e.unitSpawnTimerMax);
    }
}
