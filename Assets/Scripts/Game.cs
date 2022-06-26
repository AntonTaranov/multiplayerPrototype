//using System.Collections;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour, IScoreCounter
{
    private const bool DEBUG_SINGLE_PLAYER_START = false;

    [SerializeField] GameObject playerPrefab;

    [SerializeField] Camera GUICamera;
    
    [SerializeField] UIWidget jumpWidget;
    [SerializeField] UIWidget fireWidget;
    [SerializeField] UIWidget reloadWidget;
    [SerializeField] UIWidget weapon1;
    [SerializeField] UIWidget weapon2;
    [SerializeField] UIWidget weapon3;
    [SerializeField] UIWidget joystickWidget;
    [SerializeField] UIWidget joystickKnob;
    
    [Header("UI Labels")]
    [SerializeField] UILabel HealthText;
    [SerializeField] UILabel AmmoText;
    [SerializeField] UILabel ReloadingSign;
    [SerializeField] UILabel KillsText;
    [SerializeField] UILabel DeathsText;
    [SerializeField] UILabel TimerLabel;

    [Header("UI parts")]
    [SerializeField] UIPanel pauseMenu;
    [SerializeField] GameObject HUD;
    [SerializeField] GameObject waitingLabel;
    [SerializeField] UITable results;
    [SerializeField] GameObject repeatButton;

    [Header("UI prefabs")]
    [SerializeField] GameObject resultTextPrefab;

    [Header("Others")]
    [SerializeField] ParticlesManager effects;
    
    CameraController playerCamera;
    Player currentPlayer;
    PlayerMover playerMover;
    int killsCounter = 0;
    int deathCounter = 0;
    int playersWantRepeat;

    GameInput gameInput;
    bool paused = false;
    bool disconnecting = false;

    GameRound round = new GameRound();

    public void Disconnect()
    {
        if (disconnecting) return;

        if (PhotonNetwork.connectedAndReady)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            OnDisconnectedFromPhoton();
        }
        disconnecting = true;
    }    

    void OnDisconnectedFromPhoton()
    {        
        SceneManager.LoadScene("launcher");
    }

    void RestartRound()
    {
        PauseGame = false;
        playerCamera = FindObjectOfType<CameraController>();
        currentPlayer.PlaceCamera(playerCamera);
        if (HUD != null && !HUD.activeSelf)
        {
            HUD.SetActive(true);
        }
        if (waitingLabel != null && waitingLabel.activeSelf)
        {
            waitingLabel.SetActive(false);
        }
        currentPlayer.RebirthInGame();
    }

    void CreatePlayer()
    {
        GameObject player = null;
        if (PhotonNetwork.room != null)
        {
            player = PhotonNetwork.Instantiate("JoyPlayerPrefab", Vector3.zero, Quaternion.identity, 0);
            PhotonNetwork.player.TagObject = player;
        }
        else
        {
            player = Instantiate(playerPrefab);
        }
        playerMover = player.GetComponent<PlayerMover>();

        playerCamera = FindObjectOfType<CameraController>();

        if (playerMover != null)
        {
            playerMover.SetControls(gameInput);
                        
            currentPlayer = player.GetComponent<Player>();

            currentPlayer.PlaceCamera(playerCamera);
            currentPlayer.scoreCounter = this;
        }      
        
        if (HUD != null && !HUD.activeSelf)
        {
            HUD.SetActive(true);
        }
        if (waitingLabel != null && waitingLabel.activeSelf)
        {
            waitingLabel.SetActive(false);
        }
        PauseGame = false;
    }

    void Start()
    {
        PhotonNetwork.isMessageQueueRunning = true;

        Input.multiTouchEnabled = true;

        gameInput = new GameInput();

        if (GUICamera != null)
        {
            jumpWidget.transform.position = GUICamera.ScreenToWorldPoint(gameInput.jumpButtonPosition);
            fireWidget.transform.position = GUICamera.ScreenToWorldPoint(gameInput.fireButtonPosition);
            reloadWidget.transform.position = GUICamera.ScreenToWorldPoint(gameInput.reloadButtonPosition);
            weapon1.transform.position = GUICamera.ScreenToWorldPoint(gameInput.weapon1ButtonPosition);
            weapon2.transform.position = GUICamera.ScreenToWorldPoint(gameInput.weapon2ButtonPosition);
            weapon3.transform.position = GUICamera.ScreenToWorldPoint(gameInput.weapon3ButtonPosition);
            joystickWidget.transform.position = GUICamera.ScreenToWorldPoint(gameInput.joystickPosition);

            pauseMenu.gameObject.SetActive(false);
            repeatButton.SetActive(false);
        }

        if (HUD != null && HUD.activeSelf)
        {
            HUD.SetActive(false);
        }
        if (results != null && results.gameObject.activeSelf)
        {
            results.gameObject.SetActive(false);
        }

        //initialize player stats
        if (round.currentState == GameRound.State.WaitForPlayers)
        {
            ClearScores();
        }

        var room = PhotonNetwork.room;
        if (room != null)
        {
            round.OnPlayerConnected(room.PlayerCount);
            if (PhotonNetwork.isMasterClient)
            {
                if (round.currentState == GameRound.State.ReadyToStart
                    || round.currentState == GameRound.State.WaitForPlayers && DEBUG_SINGLE_PLAYER_START)
                {
                    StartRoundInRoom();
                    CreatePlayer();                    
                }
            }
            else
            {
                if (round.currentState == GameRound.State.ReadyToStart)
                {
                    if(room.CustomProperties.ContainsKey("startTime"))
                    {
                        var timestamp = (int)room.CustomProperties["startTime"];
                        round.OnGameStarted((uint)timestamp);
                    }
                    CreatePlayer();
                }
            }
        }
        
    }

    void StartRoundInRoom()
    {
        var room = PhotonNetwork.room;
        room.IsOpen = false;
        var customProperties = new Hashtable();    
        customProperties.Add("startTime", PhotonNetwork.ServerTimestamp);
        room.SetCustomProperties(customProperties);
        round.OnGameStarted((uint)PhotonNetwork.ServerTimestamp);
    }


    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("player entered " + newPlayer.NickName);        
        round.OnPlayerConnected(PhotonNetwork.room.PlayerCount);
        if (PhotonNetwork.isMasterClient && round.currentState == GameRound.State.ReadyToStart)
        {
            StartRoundInRoom();
            CreatePlayer();
        }
    }

    void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        var room = PhotonNetwork.room;
        if (!room.IsOpen)
        {
            room.IsOpen = true;
        }
    }

    void OnPhotonCustomRoomPropertiesChanged(Hashtable properties)
    {
        if (properties.ContainsKey("startTime") && round.currentState == GameRound.State.ReadyToStart)
        {
            var timestamp = (int)properties["startTime"];
            round.OnGameStarted((uint)timestamp);
        }
    }
        
    internal void AnotherPlayerWantsRepeat()
    {
        playersWantRepeat++;
        round.OnPlayerConnected(playersWantRepeat);

        if (round.currentState == GameRound.State.ReadyToStart)
        {
            StartRoundInRoom();
            RestartRound();
        }
    }

    void ClearScores()
    {
        var properties = new Hashtable();
        killsCounter = deathCounter = 0;
        properties.Add("kills", killsCounter);
        properties.Add("deaths", deathCounter);
        PhotonNetwork.player.SetCustomProperties(properties);
        KillsText.text = "Kills: " + killsCounter;
        DeathsText.text = "Deaths: " + deathCounter;
    }

    public void RepeatClicked()
    {
        repeatButton.SetActive(false);
        results.transform.DestroyChildren();
        results.gameObject.SetActive(false);
        waitingLabel.gameObject.SetActive(true);

        round.OnPlayerWantRepeat();
        playersWantRepeat++;
        currentPlayer.ReadyToRepeat();
        round.OnPlayerConnected(playersWantRepeat);
        ClearScores();
        
        if (round.currentState == GameRound.State.ReadyToStart)
        {
            StartRoundInRoom();
            RestartRound();
        }
    }

    private bool PauseGame
    {
        set
        {
            paused = value;
            gameInput.ActivateMouseMoveAim(!paused);
        }
    }

    public void PauseClicked()
    {
        if (!paused || !pauseMenu.gameObject.activeSelf)
        {
            PauseGame = true;
            pauseMenu.gameObject.SetActive(true);
        }
    }

    public void ResumeClicked()
    {
        if (paused)
        {
            PauseGame = false;
            pauseMenu.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (round.IsRunning((float)PhotonNetwork.time))
        {
            TimerLabel.text = "Time:" + round.GetSecondsLeft();
        }
        else if (round.currentState == GameRound.State.Finished)
        {
            TimerLabel.text = "Time is out";
            PauseGame = true;
            if (HUD != null && HUD.activeSelf)
            {
                HUD.SetActive(false);
                ShowScores();
            }
            if (playerCamera != null)
            {
                playerCamera.OnRoundFinished();
                playerCamera = null;
                playerMover.SetBodyVisible(true);
            }
            if (repeatButton != null && !repeatButton.activeSelf)
            {
                playersWantRepeat = 0;
                repeatButton.SetActive(true);
            }            
        }
        else
        {
            TimerLabel.text = "Time:" + GameRound.ROUND_TIME;
        }

        if (paused) return;

        gameInput.Update();
        
        if (gameInput.HasJoystickData)
        {
            joystickKnob.transform.localPosition = gameInput.joystickValues * 25;
        }
        else
        {
            joystickKnob.transform.localPosition = Vector3.zero;
        }

        if (gameInput.CheckMouseLock)
        {
            gameInput.ActivateMouseMoveAim(true);
        }

        if (currentPlayer != null)
        {        
            if (gameInput.jumpIsPressed || gameInput.keyboardJumpIsPressed)
            {
                currentPlayer.Jump();
            }
            if (gameInput.fireIsPressed || gameInput.mousefireIsPressed)
            {
                currentPlayer.Fire();
            }
            if (gameInput.reloadIsPressed)
            {
                currentPlayer.Reload();
            }
            
            if (gameInput.weapon1IsPressed)
            {
                currentPlayer.ChangeWeapon(1);
            }
            if (gameInput.weapon2IsPressed)
            {
                currentPlayer.ChangeWeapon(2);
            }
            if (gameInput.weapon3IsPressed)
            {
                currentPlayer.ChangeWeapon(3);
            }

            HealthText.text = "Health: " + currentPlayer.health;
            AmmoText.text = "Ammo: " + currentPlayer.ammo;       

            if (ReloadingSign != null)
            {
                if (currentPlayer.reloading && !ReloadingSign.gameObject.activeSelf)
                {
                    ReloadingSign.gameObject.SetActive(true);
                }
                if (!currentPlayer.reloading && ReloadingSign.gameObject.activeSelf)
                {
                    ReloadingSign.gameObject.SetActive(false);
                }
            }
        }
    }

    void LateUpdate()
    {
        gameInput.LateUpdate();    
    }

    void ShowScores()
    {
        if (results != null && !results.gameObject.activeSelf)
        {
            results.gameObject.SetActive(true);
        }
        bool win = true;
        int myScore = (int)PhotonNetwork.player.CustomProperties["kills"];
        foreach(var player in PhotonNetwork.playerList)
        {            
            var playerResult = NGUITools.AddChild(results.gameObject, resultTextPrefab);
            playerResult.GetComponent<UILabel>().text = player.NickName + " => Kills:" +
                player.CustomProperties["kills"] + " " + "Deaths:" + player.CustomProperties["deaths"];
            if ((int)player.CustomProperties["kills"] > myScore)
            {
                win = false;
            }
        }
        var resultLabel = NGUITools.AddChild(results.gameObject, resultTextPrefab,0);
        resultLabel.GetComponent<UILabel>().text = win ? "You win!!!" : "You lose...";
        results.Reposition();
    }

    public void IncreaseKillsCounter()
    {
        killsCounter++;
        KillsText.text = "Kills: " + killsCounter;

        var properties = new Hashtable();
        properties.Add("kills", killsCounter);
        PhotonNetwork.player.SetCustomProperties(properties);
    }

    public void IncreaseDeathCounter()
    {
        deathCounter++;
        DeathsText.text = "Deaths: " + deathCounter;

        var properties = new Hashtable();
        properties.Add("deaths", deathCounter);
        PhotonNetwork.player.SetCustomProperties(properties);
    }
}
