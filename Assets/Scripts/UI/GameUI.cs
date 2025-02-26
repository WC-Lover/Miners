using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Transform playerHolyResourceDataTemplate;
    [SerializeField] private Transform playersHolyResourceDataContainer;
    private float lastUpdate;
    private float updateDelay = 0.5f;

    void Awake()
    {
        GameManager.Instance.OnGameFinished += GameManager_OnGameFinished;
        GameManager.Instance.OnPlayerHolyResourceDataListChanged += GameManager_OnPlayerHolyResourceDataListChanged;

        lastUpdate = Time.time;
    }

    private void GameManager_OnGameFinished(object sender, GameManager.OnGameFinishedEventArgs e)
    {
        GameManager.Instance.OnGameFinished -= GameManager_OnGameFinished;
        GameManager.Instance.OnPlayerHolyResourceDataListChanged -= GameManager_OnPlayerHolyResourceDataListChanged;

        gameObject.SetActive(false);
    }

    private void GameManager_OnPlayerHolyResourceDataListChanged(object sender, GameManager.OnPlayerHolyResourceDataListChangedArgs e)
    {
        if (Time.time - lastUpdate < updateDelay) return;
        
        UpdatePlayersHolyResourceDataList(e.playerHolyResourceDataList);
        lastUpdate = Time.time;
    }

    private void UpdatePlayersHolyResourceDataList(List<PlayerHolyResourceData> playerHolyResourceDataList)
    {
        foreach (Transform child in playersHolyResourceDataContainer)
        {
            if (child == playerHolyResourceDataTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (PlayerHolyResourceData phrd in playerHolyResourceDataList.OrderBy(phrd => phrd.holyResourceGathered))
            //(int i = 0; i < playerHolyResourceDataList.Count; i++)
        {
            Transform playerHolyResourceDataTransform = Instantiate(playerHolyResourceDataTemplate, playersHolyResourceDataContainer);
            playerHolyResourceDataTransform.gameObject.SetActive(true);
            playerHolyResourceDataTransform.GetComponent<PlayerHolyResourceDataUI>().SetPlayerHolyResourceData(phrd);
        }
    }

}
