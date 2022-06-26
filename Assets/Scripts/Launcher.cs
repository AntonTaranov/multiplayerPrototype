using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Launcher : Photon.PunBehaviour
{
    [SerializeField] UILabel statusText;
    [SerializeField] UIButton startButton;
    [SerializeField] UIButton cancelButton;
    [SerializeField] Transform loadingPanel;
    [SerializeField] UIInput nicknameInput;

    TypedLobby lobby = new TypedLobby("TestLobby", LobbyType.Default);

    bool connected = false;
    bool loading = false;
    float connectionTimeout;

    PlayerStorage storage;

    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.lobby = lobby;
        PhotonNetwork.autoJoinLobby = false;
        Application.targetFrameRate = 30;

        if (statusText != null)
        {
            statusText.text = "DISCONNECTED...";
        }

        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(false);
        }

        storage = new PlayerStorage();

        if (nicknameInput != null)
        {
            nicknameInput.value = storage.Nickname;
            nicknameInput.submitOnUnselect = true;
            EventDelegate.Set(nicknameInput.onSubmit, ChangeNickname);
        }
    }

    public void ChangeNickname()
    {
        if (storage != null)
        {
            storage.SetNewNickname(nicknameInput.value);
        }
    }

    private void Update()
    {
        if (!connected && connectionTimeout > 0)
        {
            connectionTimeout -= Time.deltaTime;
            if (connectionTimeout <= 0)
            {
                PhotonNetwork.ConnectUsingSettings("1");

                if (statusText != null)
                {
                    statusText.text = "CONNECTING...";
                }
            }
            else
            {
                int timeout = (int)connectionTimeout;
                if (statusText != null)
                {
                    statusText.text = "TIMEOUT = " + timeout;
                }
            }
        }
    }

    void onConnectionFail(DisconnectCause cause)
    {
        if (statusText != null)
        {
            statusText.text = cause.ToString();
        }

        connectionTimeout = 1;
        connected = false;
    }

    public override void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        base.OnFailedToConnectToPhoton(cause);
        onConnectionFail(cause);
    }

    public override void OnConnectionFail(DisconnectCause cause)
    {
        base.OnConnectionFail(cause);
        onConnectionFail(cause);        
    }
    
    public void OnCancelClick()
    {
        loading = false;
        CancelLoading();        
    }

    public void ConnectToRoom()
    {
        loading = true;

        PhotonNetwork.ConnectUsingSettings("1");

        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text = "CONNECTING...";
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        connected = true;

        if (statusText != null)
        {
            statusText.text = "CONNECTED";
        }

        PhotonNetwork.player.NickName = storage.Nickname;
        PhotonNetwork.JoinLobby(lobby);        
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        base.OnPhotonJoinRoomFailed(codeAndMsg);
        //create own room
        var roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.IsVisible = true;
        if (PhotonNetwork.CreateRoom(null, roomOptions, lobby))
        {
            Debug.Log("Creating room");
        }
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        PhotonNetwork.isMessageQueueRunning = false;
        if (loading)
        {
            LoadMainScene();
        }
        else
        {
            PhotonNetwork.Disconnect();
            PhotonNetwork.isMessageQueueRunning = true;
        }        
    }
    
    void CancelLoading()
    {        
        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(false);
        }

        if (statusText != null)
        {
            statusText.text = "DISCONNECTED";
        }

        if (PhotonNetwork.room != null)
        {
            PhotonNetwork.Disconnect();            
            PhotonNetwork.isMessageQueueRunning = true;
        }        
    }

    public override void OnDisconnectedFromPhoton()
    {
        base.OnDisconnectedFromPhoton();
    
        if (statusText != null)
        {
            statusText.text = "DISCONNECTED";
            connected = false;
            connectionTimeout = loading ? 1f : 0;
        }
    }
    
    void LoadMainScene()
    {
        if (statusText != null)
        {
            statusText.text = "LOADING LEVEL...";
        }
        SceneManager.LoadScene("main");
    } 
}
