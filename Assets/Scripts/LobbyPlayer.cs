using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] private GameObject readyGameObject;
    [SerializeField] private Button kickButton;
    [SerializeField] private TextMeshProUGUI playerNameText;
    private PlayerData playerData;

    private void Awake()
    {
        kickButton.onClick.AddListener(() =>
        {
            GameLobbyManager.Instance.KickPlayer(playerData.playerId.ToString());
            GameMultiplayerManager.Instance.KickPlayer(playerData.clientId);
        });
    }

    void Start()
    {
        kickButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
    }

    public void SetLobbyPlayerData(PlayerData playerData)
    {
        playerNameText.text = playerData.playerName.ToString();
        readyGameObject.SetActive(playerData.playerReady);

        this.playerData = playerData;
    }
}
