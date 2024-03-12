using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInfoUI : MonoBehaviour
{
    [Header("Top UI")]
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    [Header("ReadyState")]
    [SerializeField] private TextMeshProUGUI hostReadyText;
    [SerializeField] private TextMeshProUGUI clientReadyText;
    
    [Header("KickButton")]
    [SerializeField] private Button kickButton;

    private const string NOT_READY_TEXT = "NOT READY";
    private const string READY_TEXT = "READY";

    private void Awake()
    {
        kickButton.onClick.AddListener(() =>
        {
            try
            {
                PlayerData playerDataToKick = GameMultiplayerManager.Instance.GetClientPlayerData();

                GameMultiplayerManager.Instance.KickPlayer(playerDataToKick.clientId);
                GameLobbyManager.Instance.KickPlayer(playerDataToKick.lobbyPlayerId.ToString());
            }
            catch (NoClientException e)
            {
                Debug.Log(e);
            }
        });
    }

    private void Start()
    {
        ShowKickButtonOnServer();
        
        GameMultiplayerManager.Instance.OnPlayerReadyCharacterSelectChanged
            += GameMultiplayerManager_OnPlayerReadyCharacterSelectChanged;
        
        GameMultiplayerManager.Instance.OnPlayerReadyReset += GameMultiplayerManager_OnPlayerReadyReset;
        
        lobbyNameText.text = GameLobbyManager.Instance.GetLobbyName();
        lobbyCodeText.text = GameLobbyManager.Instance.GetLobbyCode();

        hostReadyText.text = NOT_READY_TEXT;
        clientReadyText.text = NOT_READY_TEXT;
    }

    private void ShowKickButtonOnServer()
    {
        if (NetworkManager.Singleton.LocalClientId == NetworkManager.ServerClientId)
        {
            kickButton.gameObject.SetActive(true);
        }
        else
        {
            kickButton.gameObject.SetActive(false);
        }
        
    }

    private void GameMultiplayerManager_OnPlayerReadyReset(object sender, EventArgs e)
    {
        hostReadyText.text = NOT_READY_TEXT;
        clientReadyText.text = NOT_READY_TEXT;
    }

    private void GameMultiplayerManager_OnPlayerReadyCharacterSelectChanged
        (object sender, GameMultiplayerManager.OnPlayerReadyCharacterSelectChangedEventArgs e)
    {
        if (e.isHost)
        {
            SetHostReadyText(e.isReady);
        }
        else
        {
            SetClientReadyText(e.isReady);
        }
    }

    private void SetHostReadyText(bool isReady)
    {
        if (isReady)
        {
            hostReadyText.text = READY_TEXT;
        }
        else
        {
            hostReadyText.text = NOT_READY_TEXT;
        }
    }
    
    private void SetClientReadyText(bool isReady)
    {
        if (isReady)
        {
            clientReadyText.text = READY_TEXT;
        }
        else
        {
            clientReadyText.text = NOT_READY_TEXT;
        }
    }

    private void OnDestroy()
    {
        GameMultiplayerManager.Instance.OnPlayerReadyCharacterSelectChanged
            -= GameMultiplayerManager_OnPlayerReadyCharacterSelectChanged;
        
        GameMultiplayerManager.Instance.OnPlayerReadyReset -= GameMultiplayerManager_OnPlayerReadyReset;
    }
}