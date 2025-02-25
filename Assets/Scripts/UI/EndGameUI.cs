using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance;

    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI firstPlaceName;
    [SerializeField] private TextMeshProUGUI secondPlaceName;
    [SerializeField] private TextMeshProUGUI thirdPlaceName;

    [SerializeField] private TextMeshProUGUI firstPlacePercentageText;
    [SerializeField] private TextMeshProUGUI secondPlacePercentageText;
    [SerializeField] private TextMeshProUGUI thirdPlacePercentageText;
    void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        GameManager.Instance.OnGameFinished += GameConnectionManager_OnGameFinished;

        gameObject.SetActive(false);
    }

    private void GameConnectionManager_OnGameFinished(object sender, GameManager.OnGameFinishedEventArgs e)
    {
        gameObject.SetActive(true);
        var counter = 1;
        foreach (var keyValue in e.playersHolyResourceGatheredDict.OrderByDescending(keyValue => keyValue.Value))
        {
            if (counter > 3) return;

            if (counter == 1)
            {
                firstPlaceName.text = keyValue.Key;
                firstPlacePercentageText.text = Mathf.RoundToInt(keyValue.Value / 200f) + "%";
            }
            if (counter == 2)
            {
                secondPlaceName.text = keyValue.Key;
                secondPlacePercentageText.text = Mathf.RoundToInt(keyValue.Value / 200f) + "%";
            }
            if (counter == 3)
            {
                thirdPlaceName.text = keyValue.Key;
                thirdPlacePercentageText.text = Mathf.RoundToInt(keyValue.Value / 200f) + "%";
            }
            counter++;
        }
    }

}
