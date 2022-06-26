using UnityEngine;
using System.Collections.Generic;

public class TargetsManager : MonoBehaviour
{
    const int respawnTimeOut = 5;        

    [SerializeField] ParticlesManager effects;
        
    PhotonView photonView;
    
    bool multiplayer = false;
    bool allTargetsDestroyed;
        
    float capsuleBirthCooldown = -1;
    Queue<int> deadCapsulesStorage = new Queue<int>();

    void Start()
    {            
        photonView = GetComponent<PhotonView>();
    }

    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (photonView.isMine)
        {
            photonView.RPC("SynchronizeState", newPlayer, deadCapsulesStorage.ToArray(),
                        capsuleBirthCooldown);
        }
    }

    void Update()
    {
        if (capsuleBirthCooldown >= 0)
        {
            capsuleBirthCooldown -= Time.deltaTime;
            if (capsuleBirthCooldown < 0)
            {
                CreateNewCapsule();
                if (deadCapsulesStorage.Count > 0)
                {
                    capsuleBirthCooldown = respawnTimeOut;
                }
                if (photonView.isMine)
                {
                    photonView.RPC("SynchronizeState", PhotonTargets.Others, deadCapsulesStorage.ToArray(),
                        capsuleBirthCooldown);
                }
            }
        }
    }

    void CreateNewCapsule()
    {        
        if (photonView.isMine && deadCapsulesStorage.Count > 0)
        {
            var capsuleId = deadCapsulesStorage.Dequeue();
            var capsule = PhotonView.Find(capsuleId);
            if (capsule != null)
            {
                var capsuleComponent = capsule.GetComponent<TestTarget>();
                if (capsuleComponent != null)
                {
                    var position = new Vector3(Random.Range(-40.0f, 40.0f), 3, Random.Range(-40.0f, 40.0f));
                    capsuleComponent.CreateAt(position);                    
                }
            }            
        }
    }

    [PunRPC]
    void SynchronizeState(int[] deadCapsules, float capsuleBirthCooldown)
    {
        deadCapsulesStorage.Clear();
        foreach(var capsuleId in deadCapsules)
        {
            deadCapsulesStorage.Enqueue(capsuleId);
            HideCapsuleWithIdIfVisible(capsuleId);
        }
        this.capsuleBirthCooldown = capsuleBirthCooldown;
    }

    void HideCapsuleWithIdIfVisible(int capsuleId)
    {
        var capsule = PhotonView.Find(capsuleId);
        if (capsule != null && capsule.gameObject.activeInHierarchy)
        {
            capsule.gameObject.SetActive(false);
        }
    }

    internal void OnTargetDeath(PhotonView deadCapsule)
    {
        if (photonView.isMine)
        {            
            if (capsuleBirthCooldown < 0)
            {
                capsuleBirthCooldown = respawnTimeOut;
            }
            deadCapsulesStorage.Enqueue(deadCapsule.viewID);
            
            photonView.RPC("SynchronizeState", PhotonTargets.Others, deadCapsulesStorage.ToArray(),
                capsuleBirthCooldown);
        }
    }
}
