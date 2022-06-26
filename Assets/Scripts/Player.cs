using UnityEngine;
using UnityEngine.Animations;

public class Player : MonoBehaviour,IDamageable
{
    const float JUMP_HEIGHT = 5f;
    const int MAX_HEALTH = 100;
    
    private bool isGrounded = false;
    private bool isAnimationsAvaliable = false; 
    
    Rigidbody rigidBody;
    PlayerMover playerMover;
    internal int ammo { get => guns.ammo; }
    internal bool reloading { get => guns.reloading; }
    internal int health { get; private set; } = MAX_HEALTH;    
    internal IScoreCounter scoreCounter;

    [SerializeField]
    GameObject bulletPrefab;
    [SerializeField]
    GameObject nickTextField;
    [SerializeField]
    Animation animationComponent;

    int animationIndex = 0;
    Guns guns;
    CameraController activeCamera;

    ParticlesManager effects;

    PhotonView photonView;
    TextMesh statusText = null;

    float deathTimeout = -1;

    string[] animations = { "Idle", "Walk", "Walk_Back", "Walk_Left", "Walk_Right", "Jump" };
    string lastPlayedAnimation = "";
    SynchronizationBuffer synchronization = new SynchronizationBuffer(5);

    void Awake()
    {
        guns = GetComponent<Guns>();
    }

    // Start is called before the first frame update
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody != null && !photonView.isMine)
        {
            Destroy(rigidBody);
            rigidBody = null;
        }
        playerMover = GetComponent<PlayerMover>();
        effects = FindObjectOfType<ParticlesManager>();
        if (nickTextField != null)
        {
            if (photonView.isMine)
            {
                nickTextField.SetActive(false);
            }
            else
            {
                var constraint = nickTextField.GetComponent<LookAtConstraint>();
                var constraintSource = new ConstraintSource();
                statusText = nickTextField.GetComponent<TextMesh>();
                constraintSource.sourceTransform = Camera.main.transform;
                constraintSource.weight = 1;
                if (constraint != null)
                {
                    if (constraint.sourceCount > 0)
                    {
                        constraint.SetSource(0, constraintSource);
                    }
                    else
                    {
                        constraint.AddSource(constraintSource);
                    }
                }
                constraint.constraintActive = true;
            }
        }
        isAnimationsAvaliable = animationComponent != null;
        if (isAnimationsAvaliable && !photonView.isMine)
        {
            animationComponent["Jump"].speed = 0.9f;
        }
        Birth();
    }

    void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = photonView.owner.NickName + "\n" 
                + (health > 0 ? health.ToString() : "deadman");
        }
    }

    internal void ChangeWeapon(int gunType)
    {
        if (guns.SetGunType(gunType))
        {
            photonView.RPC("ChangeWeaponType", PhotonTargets.Others, gunType);
            guns.PlaceCamera(activeCamera.transform);
        }
    }

    [PunRPC]
    void ChangeWeaponType(int gunType)
    {
        guns.SetGunType(gunType);
    }

    internal void PlaceCamera(CameraController targetCamera)
    {
        activeCamera = targetCamera;
        if (guns == null)
        {
            guns = GetComponent<Guns>();
        }
        guns.PlaceCamera(activeCamera.transform);        
    }

    [PunRPC]
    private void StartReloadRPC()
    {
        guns.Reload();
    }

    internal void SendReloadRPC()
    {
        photonView.RPC("StartReloadRPC", PhotonTargets.All);
    }

    internal void Reload()
    {
        guns.Reload();
    }

    [PunRPC]
    void ShowShootAnimation()
    {
        guns.ShowShootAnimation();
        effects.CreateShot(guns.effectTransform);
    }

    internal void Fire()
    {
        var success = guns.Fire(this);
        if (success)
        {
            effects.CreateShot(guns.effectTransform);
            photonView.RPC("ShowShootAnimation", PhotonTargets.All);
        }
    }    

    internal void LaunchRocket(Transform aimData)
    {
        //var bullet = Instantiate<GameObject>(rocketPrefab);

        var direction = aimData.TransformDirection(Vector3.forward);
        var position = aimData.position + direction * 1;
        var rotation = aimData.rotation;

        var rocket = PhotonNetwork.Instantiate("Rocket", position, rotation, 0);
        var rocketComponent = rocket.GetComponent<Rocket>();
        rocketComponent.Launch(rotation);        
    }

    internal void Jump()
    {
        if (isGrounded && rigidBody != null)
        {
            rigidBody.AddForce(Vector3.up * JUMP_HEIGHT, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    [PunRPC]
    void WantRepeatRPC()
    {
        var game = FindObjectOfType<Game>();
        if (game != null)
        {
            game.AnotherPlayerWantsRepeat();
        }
    }

    internal void ReadyToRepeat()
    {
        photonView.RPC("WantRepeatRPC", PhotonTargets.Others);        
    }
    
    private void OnCollisionEnter(Collision collision)
    {        
        if (collision.gameObject.tag == "Ground")
        {
            isGrounded = true;            
        }        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Bonus")
        {
            var bonusComponent = other.gameObject.GetComponent<Bonus>();
            if (bonusComponent != null)
            {
                bonusComponent.ConsumeMe(photonView);
            }
        }
    }

    internal void AddBonus(Bonus.Type bonus)
    {
        switch (bonus)
        {
            case Bonus.Type.Health:
                health = Mathf.Clamp(health + (int)(MAX_HEALTH * 0.5f), 0, MAX_HEALTH);
                if (photonView.isMine)
                {
                    photonView.RPC("SyncMyStateRPC", PhotonTargets.Others, health,
                        guns.currentGunType, playerMover.GetRotations());
                }
                break;
            case Bonus.Type.Bullets:
                guns.ApplyBulletsBonus();
                break;
        }
    }

    [PunRPC]
    void BirthRPC(Vector3 position, PhotonMessageInfo info)
    {
        gameObject.SetActive(true);
        synchronization.ResetBuffer();
        synchronization.AddNewState(position, Vector2.zero, 0, info.timestamp);
        health = MAX_HEALTH;
        UpdateStatusText();

        transform.position = position;
        transform.rotation = Quaternion.identity;
        playerMover.SetRotations(Vector2.zero);
    }

    Vector3 Birth()
    {
        playerMover.SetBodyVisible(!photonView.isMine);
        guns.SetGunVisible(true);
        if (rigidBody != null)
        {
            rigidBody.detectCollisions = true;
            rigidBody.isKinematic = !photonView.isMine;
        }
        if (activeCamera != null)
        {
            PlaceCamera(activeCamera);
            activeCamera.OnPlayerBirth();
        }

        var randomPosition = new Vector3(Random.Range(-25, 25), 1, Random.Range(-25, 25));
        health = MAX_HEALTH;
        UpdateStatusText();

        if (photonView.isMine)
        {
            transform.position = randomPosition;
            transform.localRotation = Quaternion.identity;
            playerMover.SetRotations(Vector2.zero);
        }

        return randomPosition;
    }

    [PunRPC]
    void NewKillRPC()
    {
        if (scoreCounter != null)
        {
            scoreCounter.IncreaseKillsCounter();
        }
    }

    internal void ScoreNewKill(PhotonPlayer player)
    {
        photonView.RPC("NewKillRPC", player);
    }

    [PunRPC]
    void SyncMyStateRPC(int healthValue, int weaponIndex, Vector2 rotations)
    {
        health = healthValue;
        UpdateStatusText();
        guns.SetGunType(weaponIndex);        
        synchronization.SetRotations(rotations);
    }

    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (photonView.isMine)
        {
            photonView.RPC("SyncMyStateRPC", newPlayer, health,
                guns.currentGunType, playerMover.GetRotations());
        }
    }

    void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        info.sender.TagObject = this.gameObject;        
    }
       
    void Die()
    {
        health = 0;
        if (scoreCounter != null)
        {
            scoreCounter.IncreaseDeathCounter();
        }
        if (photonView.isMine)
        {
            deathTimeout = 2.4f;
            playerMover.SetBodyVisible(false);
            guns.SetGunVisible(false);
            if (rigidBody != null)
            {
                rigidBody.detectCollisions = false;
                rigidBody.isKinematic = true;
            }
        }
        else
        {
            gameObject.SetActive(false);            
        }

        if (activeCamera != null)
        {
            activeCamera.transform.SetParent(transform, true);
            activeCamera.OnPlayerDied();            
        }
    }

    public void AddDamage(int value)
    {
        health -= value;

        if (health <= 0)
        {
            Die();
        }
        UpdateStatusText();

        effects.CreateHit(this.transform);

        photonView.RPC("AddDamageRPC", PhotonTargets.Others, value, health > 0);        
    }

    void SendMessageToKiller(PhotonPlayer killer)
    {
        if (photonView.isMine)
        {
            var killerGameObject = killer.TagObject as GameObject;
            if (killerGameObject != null)
            {
                var killerPlayerComponent = killerGameObject.GetComponent<Player>();
                if (killerPlayerComponent != null)
                {
                    killerPlayerComponent.ScoreNewKill(killer);
                }
            }
        }
    }

    [PunRPC]
    void AddDamageRPC(int value, bool alive, PhotonMessageInfo info)
    {
        effects.CreateHit(this.transform);
        if (health > 0)
        {
            if (!alive)
            {                
                Die();
                SendMessageToKiller(info.sender);
            }
            else
            {
                health -= value;
                if (health <= 0)
                {
                    Die();
                    SendMessageToKiller(info.sender);
                }
            }
            UpdateStatusText();
        }
    } 

    internal void RebirthInGame()
    {
        var position = Birth();
        photonView.RPC("BirthRPC", PhotonTargets.Others, position);
    }

    void Update()
    {        
        if (photonView.isMine && deathTimeout > 0)
        {
            deathTimeout -= Time.deltaTime;
            if (deathTimeout <= 0)
            {
                RebirthInGame();
            }
        }
        if (photonView.isMine && playerMover != null)
        {
            animationIndex = 0;
            if (isGrounded)
            {
                switch (playerMover.moveDirection)
                {
                    case PlayerMover.Direction.forward:
                        animationIndex = 1;
                        break;
                    case PlayerMover.Direction.backward:
                        animationIndex = 2;
                        break;
                    case PlayerMover.Direction.left:
                        animationIndex = 3;
                        break;
                    case PlayerMover.Direction.right:
                        animationIndex = 4;
                        break;
                }
            }
            else
            {
                animationIndex = 5;
            }
        }
        else
        {
            synchronization.Update(Time.deltaTime);
            transform.position = synchronization.position;
            playerMover.SetRotations(synchronization.GetRotations());
           
            if (isAnimationsAvaliable)
            {
                var animation = animations[synchronization.animationIndex];

                if (!animationComponent.IsPlaying(animation))
                {
                    if (lastPlayedAnimation == animation)
                    {
                        //only one jump animation                    
                        if (animation != "Jump")
                        {
                            animationComponent.Play(animation);
                        }
                    }
                    else
                    {
                        animationComponent.CrossFade(animation);
                    }
                    lastPlayedAnimation = animation;
                }
            }
        }
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {            
            stream.SendNext(transform.position);
            stream.SendNext(playerMover != null ? playerMover.GetRotations() : Vector2.zero);
            stream.SendNext(animationIndex);            
        }
        else
        {            
            synchronization.AddNewState((Vector3)stream.ReceiveNext(),
                (Vector2)stream.ReceiveNext(),
                (int)stream.ReceiveNext(), info.timestamp);            
        }
    }
}
