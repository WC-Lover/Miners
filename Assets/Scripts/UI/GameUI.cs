using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Transform playerHolyResourceDataTemplate;
    [SerializeField] private Transform playersHolyResourceDataContainer;

    void Start()
    {
        GameManager.Instance.OnPlayerHolyResourceDataListChanged += GameManager_OnPlayerHolyResourceDataListChanged;
    }

    private void GameManager_OnPlayerHolyResourceDataListChanged(object sender, GameManager.OnPlayerHolyResourceDataListChangedArgs e)
    {
        UpdatePlayersHolyResourceDataList(e.playerHolyResourceDataList);
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
