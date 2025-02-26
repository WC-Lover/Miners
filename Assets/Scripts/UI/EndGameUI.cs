using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
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
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        GameManager.Instance.OnGameFinished += GameConnectionManager_OnGameFinished;

        gameObject.SetActive(false);
    }

    private void GameConnectionManager_OnGameFinished(object sender, GameManager.OnGameFinishedEventArgs e)
    {
        gameObject.SetActive(true);
        var counter = 1;
        foreach (var phrd in e.playerHolyResourceDataList.OrderByDescending(phrd => phrd.holyResourceGathered))
        {
            if (counter > 3) return;

            if (counter == 1)
            {
                firstPlaceName.text = phrd.playerName.ToString();
                firstPlacePercentageText.text = Mathf.RoundToInt(phrd.holyResourceGathered / 200f) + "%";
            }
            if (counter == 2)
            {
                secondPlaceName.text = phrd.playerName.ToString();
                secondPlacePercentageText.text = Mathf.RoundToInt(phrd.holyResourceGathered / 200f) + "%";
            }
            if (counter == 3)
            {
                thirdPlaceName.text = phrd.playerName.ToString();
                thirdPlacePercentageText.text = Mathf.RoundToInt(phrd.holyResourceGathered / 200f) + "%";
            }
            counter++;
        }
    }

}
