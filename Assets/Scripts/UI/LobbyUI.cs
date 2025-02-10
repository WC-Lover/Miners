using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    private Color currentReadyButtonColor;
    private Color greenReadyButtonColor;
    [SerializeField] private Color redReadyButtonColor;

    [SerializeField] private Transform lobbyPlayerTemplate;
    [SerializeField] private Transform lobbyPlayersContainer;

    private void Awake()
    {
        greenReadyButtonColor = readyButton.GetComponent<Image>().color;

        mainMenuButton.onClick.AddListener(() =>
        {
            GameLobbyManager.Instance.LeaveLobby();
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        readyButton.onClick.AddListener(() =>
        {
            LobbyPlayerSelectReady.Instance.ChangePlayerReady();
            // Change color of the ReadyButton
            currentReadyButtonColor = readyButton.GetComponent<Image>().color;
            readyButton.GetComponent<Image>().color = currentReadyButtonColor == greenReadyButtonColor ? redReadyButtonColor : greenReadyButtonColor; 
        });
    }

    void Start()
    {
        GameMultiplayerManager.Instance.OnPlayerDataNetworkListChanged += GameMultiplayer_OnPlayerDataNetworkListChanged;
    }

    private void GameMultiplayer_OnPlayerDataNetworkListChanged(object sender, GameMultiplayerManager.OnPlayerDataNetworkListChangedArgs e)
    {
        UpdateLobbyPlayersList(e.playerDataList);
    }

    private void UpdateLobbyPlayersList(List<PlayerData> playerDataList)
    {
        foreach (Transform child in lobbyPlayersContainer)
        {
            Destroy(child.gameObject);
        }

        bool allClientsReady = true;
        for (int i = 0; i < playerDataList.Count; i++)
        {
            Transform lobbyPlayerTransform = Instantiate(lobbyPlayerTemplate, lobbyPlayersContainer);
            lobbyPlayerTransform.gameObject.SetActive(true);
            lobbyPlayerTransform.GetComponent<LobbyPlayer>().SetLobbyPlayerData(playerDataList[i]);
            if (!playerDataList[i].playerReady) allClientsReady = false;
        }

        if (allClientsReady && NetworkManager.Singleton.IsServer)
        {
            GameLobbyManager.Instance.DeleteLobby();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }

    private void OnDestroy()
    {
        GameMultiplayerManager.Instance.OnPlayerDataNetworkListChanged -= GameMultiplayer_OnPlayerDataNetworkListChanged;
    }
}
