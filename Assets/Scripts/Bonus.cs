using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bonus : MonoBehaviour
{
    public enum Type
    {
        None,
        Health,
        Bullets
    }

    [SerializeField]
    internal Type type;

    internal BonusManager manager;

    internal void ConsumeMe(PhotonView owner)
    {
        if(owner.isMine && manager != null)
        {
            manager.ConsumeBonus(gameObject, owner.ownerId);
        }
        gameObject.SetActive(false);
    }

    
}
