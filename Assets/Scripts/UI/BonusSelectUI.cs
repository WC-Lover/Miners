using System;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BonusSelectUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private Toggle damageBonusToggle;
    [SerializeField] private Toggle damageToggle;
    [SerializeField] private Toggle speedBonusToggle;
    [SerializeField] private Toggle speedToggle;
    [SerializeField] private Toggle gatherBonusToggle;
    [SerializeField] private Toggle gatherToggle;
    [SerializeField] private Toggle healthBonusToggle;
    [SerializeField] private Toggle healthToggle;

    private void Awake()
    {
        damageBonusToggle.onValueChanged.AddListener(delegate
        {
            BonusToggleValueChanged(damageBonusToggle.GetComponent<Toggle>());
        });
        speedBonusToggle.onValueChanged.AddListener(delegate
        {
            BonusToggleValueChanged(speedBonusToggle.GetComponent<Toggle>());
        });
        gatherBonusToggle.onValueChanged.AddListener(delegate
        {
            BonusToggleValueChanged(gatherBonusToggle.GetComponent<Toggle>());
        });
        healthBonusToggle.onValueChanged.AddListener(delegate
        {
            BonusToggleValueChanged(healthBonusToggle.GetComponent<Toggle>());
        });

        damageToggle.onValueChanged.AddListener(delegate
        {
            ToggleValueChanged(damageToggle.GetComponent<Toggle>());
        });
        speedToggle.onValueChanged.AddListener(delegate
        {
            ToggleValueChanged(speedToggle.GetComponent<Toggle>());
        });
        gatherToggle.onValueChanged.AddListener(delegate
        {
            ToggleValueChanged(gatherToggle.GetComponent<Toggle>());
        });
        healthToggle.onValueChanged.AddListener(delegate
        {
            ToggleValueChanged(healthToggle.GetComponent<Toggle>());
        });
    }

    private void ToggleValueChanged(Toggle toggle)
    {
        if (!toggle.isOn) return;

        if (toggle == damageToggle)
        {
            speedToggle.isOn = false;
            gatherToggle.isOn = false;
            healthToggle.isOn = false;
        }
        else if (toggle == speedToggle)
        {
            damageToggle.isOn = false;
            gatherToggle.isOn = false;
            healthToggle.isOn = false;
        }
        else if (toggle == gatherToggle)
        {
            damageToggle.isOn = false;
            speedToggle.isOn = false;
            healthToggle.isOn = false;
        }
        else if (toggle == healthToggle)
        {
            damageToggle.isOn = false;
            speedToggle.isOn = false;
            gatherToggle.isOn = false;
        }
    }

    private void BonusToggleValueChanged(Toggle toggle)
    {
        if (!toggle.isOn) return;

        if (toggle == damageBonusToggle)
        {
            speedBonusToggle.isOn = false;
            gatherBonusToggle.isOn = false;
            healthBonusToggle.isOn = false;
        }
        else if (toggle == speedBonusToggle)
        {
            damageBonusToggle.isOn = false;
            gatherBonusToggle.isOn = false;
            healthBonusToggle.isOn = false;
        }
        else if (toggle == gatherBonusToggle)
        {
            damageBonusToggle.isOn = false;
            speedBonusToggle.isOn = false;
            healthBonusToggle.isOn = false;
        }
        else if (toggle == healthBonusToggle)
        {
            damageBonusToggle.isOn = false;
            speedBonusToggle.isOn = false;
            gatherBonusToggle.isOn = false;
        }
    }

    void Start()
    {

    }

}
