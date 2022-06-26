using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class TestTarget : MonoBehaviour, IDamageable
{
    const int MAX_HEALTH = 75;

    internal int health { get; private set; } = MAX_HEALTH;

    ParticlesManager effects;
    
    SynchronizationBuffer synchronization = new SynchronizationBuffer(5);
    PhotonView photonView;
    NavMeshAgent agent;
        
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        agent = GetComponent<NavMeshAgent>();
        effects = FindObjectOfType<ParticlesManager>();

        if (photonView.isMine)
        {
            CreateRandomDestination();
        }
        else
        {
            agent.enabled = false;
        }
    }

    [PunRPC]
    void CreateRPC(Vector3 position, PhotonMessageInfo info)
    {
        gameObject.SetActive(true);
        synchronization.ResetBuffer();
        synchronization.AddNewState(position, Vector2.zero, 0, info.timestamp);
        health = MAX_HEALTH;

        transform.position = position;
        transform.rotation = Quaternion.identity;
    }

    internal void CreateAt(Vector3 position)
    {
        health = MAX_HEALTH;
        transform.position = position;
        transform.rotation = Quaternion.identity;        
        gameObject.SetActive(true);

        photonView.RPC("CreateRPC", PhotonTargets.Others, position);
    }

    void CreateRandomDestination()
    {
        if (agent != null)
        {
            var randomX = -50 + Random.value * 100;
            var randomY = -50 + Random.value * 100;
            agent.SetDestination(new Vector3(randomX, 0, randomY));
        }
    }

    void Update()
    {
        if (photonView.isMine)
        {
            if (agent != null)
            {
                if (agent.enabled == false)
                {
                    agent.enabled = true;
                    CreateRandomDestination();
                }
                if (agent.remainingDistance < 3)
                {
                    CreateRandomDestination();
                }
            }
        }
        else
        {
            synchronization.Update(Time.deltaTime);
            var position = synchronization.position;
            transform.position = position;
        }
    }

    void Die()
    {
        if (photonView.isMine)
        {
            var manager = FindObjectOfType<TargetsManager>();
            if (manager != null)
            {
                manager.OnTargetDeath(photonView);
            }            
        }
        gameObject.SetActive(false);        
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
                else if (photonView.isMine)
                {
                    CreateRandomDestination();
                }
            }
        }
    }
       
    public void AddDamage(int value)
    {
        effects.CreateHit(this.transform);
        health -= value;
        photonView.RPC("AddDamageRPC", PhotonTargets.Others, value, health > 0);

        if (health <= 0)
        {
            if (photonView.isMine)
            {
                SendMessageToKiller(PhotonNetwork.player);
            }
            Die();
        }         
        else if(photonView.isMine)
        {
            CreateRandomDestination();
        }
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            stream.SendNext(transform.position);            
        }
        else
        {
            synchronization.AddNewState((Vector3)stream.ReceiveNext(),
                Vector2.zero, 0, info.timestamp);
        }
    }
}
