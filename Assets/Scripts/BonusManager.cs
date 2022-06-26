using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusManager : MonoBehaviour
{
    const int respawnTimeOut = 25;

    [SerializeField]
    GameObject healthBonusPrefab;
    [SerializeField]
    GameObject bulletsBonusPrefab;

    [SerializeField] Transform[] spawnPoints;

    Dictionary<int, GameObject> aliveBonuses = new Dictionary<int, GameObject>();
    float respawnCountDown = respawnTimeOut;
    PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (photonView.isMine)
        {
            SendSynchronizeRPC(newPlayer);
        }
    }

    void SpawnNewBonus()
    {
        respawnCountDown = respawnTimeOut;
        if (photonView.isMine)
        {
            if (aliveBonuses.Count < 3)
            {
                var freeSpawns = new List<int>();
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    if (!aliveBonuses.ContainsKey(i))
                    {
                        freeSpawns.Add(i);
                    }
                }
                var randomIndex = freeSpawns[Random.Range(0, freeSpawns.Count)];
                InstantiateNewBonus(randomIndex, Random.value > 0.5 ? Bonus.Type.Health : Bonus.Type.Bullets);
                SendSynchronizeRPC();
            }
        }
    }

    void SendSynchronizeRPC(PhotonPlayer targetPlayer = null)
    {
        var state = new int[spawnPoints.Length];
        for (var i = 0; i < state.Length; i++)
        {
            if (aliveBonuses.ContainsKey(i))
            {
                var bonus = aliveBonuses[i];
                var bonusComponent = bonus.GetComponent<Bonus>();
                if (bonusComponent.type == Bonus.Type.Health)
                {
                    state[i] = 1;
                }
                else if (bonusComponent.type == Bonus.Type.Bullets)
                {
                    state[i] = 2;
                }
                else
                {
                    state[i] = 0;
                }
            }
            else
            {
                state[i] = 0;
            }
        }
        if (targetPlayer != null)
        {
            photonView.RPC("SynchronizeBonuses", targetPlayer, state, (int)respawnCountDown);
        }
        else
        {
            photonView.RPC("SynchronizeBonuses", PhotonTargets.Others, state, (int)respawnCountDown);
        }
    }

    void InstantiateNewBonus(int key, Bonus.Type type)
    {
        var bonus = Instantiate(type == Bonus.Type.Bullets ? bulletsBonusPrefab : healthBonusPrefab, spawnPoints[key], false);
        var bonusComponent = bonus.GetComponent<Bonus>();
        if (bonusComponent != null)
        {
            bonusComponent.manager = this;
        }
        aliveBonuses.Add(key, bonus);
    }

    void DestroyAliveBonus(int key)
    {
        var bonus = aliveBonuses[key];
        Destroy(bonus);
        aliveBonuses.Remove(key);
    }

    int GetBonusKey(GameObject value)
    {
        if (aliveBonuses.ContainsValue(value))
        {
            foreach(var pair in aliveBonuses)
            {
                if (pair.Value == value)
                {
                    return pair.Key;
                }
            }
        }
        return -1;
    }

    [PunRPC]
    void AddRewardToPlayerRPC(int bonusType)//1-health,2-bullets
    {
        var bonus = Bonus.Type.None;
        if (bonusType == 1)
        {
            bonus = Bonus.Type.Health;
        }
        else if (bonusType == 2)
        {
            bonus = Bonus.Type.Bullets;
        }
        var playerGameObject = (GameObject)PhotonNetwork.player.TagObject;
        if(playerGameObject !=null)
        {
            var playerComponent = playerGameObject.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.AddBonus(bonus);
            }
        }
    }

    [PunRPC]
    void ConsumeBonusRPC(int key, int ownerId)
    {
        if (photonView.isMine && aliveBonuses.ContainsKey(key))
        {
            var bonus = aliveBonuses[key];

            var player = PhotonPlayer.Find(ownerId);
            var bonusComponent = bonus.GetComponent<Bonus>();
            if (bonusComponent != null)
            {
                int bonusType = 0;
                if (bonusComponent.type == Bonus.Type.Health)
                {
                    bonusType = 1;
                }
                else if (bonusComponent.type == Bonus.Type.Bullets)
                {
                    bonusType = 2;
                }

                photonView.RPC("AddRewardToPlayerRPC", player, bonusType);
            }

            DestroyAliveBonus(key);
            SendSynchronizeRPC();
        }
    }

    internal void ConsumeBonus(GameObject bonus, int ownerId)
    {
        var bonusKey = GetBonusKey(bonus);
        if (bonusKey >= 0)
        {
            photonView.RPC("ConsumeBonusRPC", PhotonTargets.AllViaServer, bonusKey, ownerId);
        }
    }

    [PunRPC]
    void SynchronizeBonuses(int[] bonuses, int respawnCountDown)
    {
        this.respawnCountDown = respawnCountDown;
        for (var i = 0; i < bonuses.Length; i++)
        {
            var bonusState = bonuses[i];
            if (bonusState == 0)//empty
            {
                if (aliveBonuses.ContainsKey(i))
                {
                    DestroyAliveBonus(i);
                }
            }
            else if (bonusState == 1) //health
            {
                if (aliveBonuses.ContainsKey(i))
                {
                    var bonus = aliveBonuses[i];
                    var bonusComponent = bonus.GetComponent<Bonus>();
                    if (bonusComponent == null || bonusComponent.type != Bonus.Type.Health)
                    {
                        DestroyAliveBonus(i);
                        InstantiateNewBonus(i, Bonus.Type.Health);
                    }
                }
                else
                {
                    InstantiateNewBonus(i, Bonus.Type.Health);
                }
            }
            else if (bonusState == 2) //bullets
            {
                if (aliveBonuses.ContainsKey(i))
                {
                    var bonus = aliveBonuses[i];
                    var bonusComponent = bonus.GetComponent<Bonus>();
                    if (bonusComponent == null || bonusComponent.type != Bonus.Type.Bullets)
                    {
                        DestroyAliveBonus(i);
                        InstantiateNewBonus(i, Bonus.Type.Bullets);
                    }
                }
                else
                {
                    InstantiateNewBonus(i, Bonus.Type.Bullets);
                }
            }
        }
    }

    void Update()
    {
        if (respawnCountDown > 0)
        {
            respawnCountDown -= Time.deltaTime;
            if (respawnCountDown <= 0)
            {
                SpawnNewBonus();
            }
        }
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
    }
}