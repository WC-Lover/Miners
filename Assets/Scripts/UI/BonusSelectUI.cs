using System;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BonusSelectUI : MonoBehaviour
{

    [SerializeField] private Building building;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLeft = 0.5f;

    [Header("Temporary bonus")]
    private bool tempBonusChosen = false;
    [SerializeField] private Button damageTempBonusButton;
    [SerializeField] private Button speedTempBonusButton;
    [SerializeField] private Button gatherTempBonusButton;
    [SerializeField] private Button staminaTempBonusButton;

    [Header("Permanent bonus")]
    private bool permBonusChosen = false;
    [SerializeField] private Button damagePermBonusButton;
    [SerializeField] private Button speedPermBonusButton;
    [SerializeField] private Button gatherPermBonusButton;
    [SerializeField] private Button staminaPermBonusButton;

    private Bonus tempBonus = Bonus.Speed;
    private Bonus permBonus = Bonus.Speed;

    public enum Bonus
    {
        Damage,
        Speed, 
        Gather,
        Stamina,
        None
    }

    private void Awake()
    {
        // TEMPORARY BONUS SELECTION
        damageTempBonusButton.onClick.AddListener(() =>
        {
            MakeAllTempButtonsInteractable();
            damageTempBonusButton.interactable = false;
            tempBonus = Bonus.Damage;
        });

        speedTempBonusButton.onClick.AddListener(() =>
        {
            MakeAllTempButtonsInteractable();
            speedTempBonusButton.interactable = false;
            tempBonus = Bonus.Speed;
        });

        gatherTempBonusButton.onClick.AddListener(() =>
        {
            MakeAllTempButtonsInteractable();
            gatherTempBonusButton.interactable = false;
            tempBonus = Bonus.Gather;
        });

        staminaTempBonusButton.onClick.AddListener(() =>
        {
            MakeAllTempButtonsInteractable();
            staminaTempBonusButton.interactable = false;
            tempBonus = Bonus.Stamina;
        });

        // PERMANENT BONUS SELECTION
        damagePermBonusButton.onClick.AddListener(() =>
        {
            MakeAllPermButtonsInteractable();
            damagePermBonusButton.interactable = false;
            permBonus = Bonus.Damage;
        });

        speedPermBonusButton.onClick.AddListener(() =>
        {
            MakeAllPermButtonsInteractable();
            speedPermBonusButton.interactable = false;
            permBonus = Bonus.Speed;
        });

        gatherPermBonusButton.onClick.AddListener(() =>
        {
            MakeAllPermButtonsInteractable();
            gatherPermBonusButton.interactable = false;
            permBonus = Bonus.Gather;
        });

        staminaPermBonusButton.onClick.AddListener(() =>
        {
            MakeAllPermButtonsInteractable();
            staminaPermBonusButton.interactable = false;
            permBonus = Bonus.Stamina;
        });

        GameManager.Instance.bonusSelectTimer.OnValueChanged += GameConnectionManager_OnBonusSelectTimerChanged;
    }

    private void MakeAllTempButtonsInteractable()
    {
        damageTempBonusButton.interactable = true;
        speedTempBonusButton.interactable = true;
        gatherTempBonusButton.interactable = true;
        staminaTempBonusButton.interactable = true;
        tempBonusChosen = true;
    }

    private void MakeAllPermButtonsInteractable()
    {
        damagePermBonusButton.interactable = true;
        speedPermBonusButton.interactable = true;
        gatherPermBonusButton.interactable = true;
        staminaPermBonusButton.interactable = true;
        permBonusChosen = true;
    }

    private void GameConnectionManager_OnBonusSelectTimerChanged(float previousValue, float newValue)
    {
        timerText.text = $"Time Left: {Mathf.Ceil(newValue)}";
        if (Mathf.Ceil(newValue) <= 0)
        {
            // SET BONUS
            building.SetBonusAttributes(tempBonus, permBonus);
            // ALLOW UNITS SPAWN
            building.AllowUnitSpawn();
            GameManager.Instance.bonusSelectTimer.OnValueChanged -= GameConnectionManager_OnBonusSelectTimerChanged;

            PlayerMouseInput.Instance.GameHasStarted();

            if (GameManager.Instance.IsServer) ResourceSpawner.Instance.GameHasStarted();

            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.IsServer && timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            GameManager.Instance.bonusSelectTimer.Value = timeLeft;
        }
    }
}
