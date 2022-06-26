using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    const int damageValue = 15;
    const float damageTimeOut = 1;

    Dictionary<IDamageable, float> damageTimeoutStorage = new Dictionary<IDamageable, float>();

    List<IDamageable> notAffectedObjects = new List<IDamageable>();
    List<IDamageable> objectsToRemove = new List<IDamageable>();

    void AddDamage(IDamageable damageable)
    {
        damageable.AddDamage(damageValue);

        if (notAffectedObjects.Contains(damageable))
        {
            notAffectedObjects.Remove(damageable);
        }

        if (damageTimeoutStorage.ContainsKey(damageable))
        {
            damageTimeoutStorage[damageable] = damageTimeOut;
        }
        else
        {
            damageTimeoutStorage.Add(damageable, damageTimeOut);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            if (damageTimeoutStorage.ContainsKey(damageable))
            {
                if (damageTimeoutStorage[damageable] <= 0)
                {
                    AddDamage(damageable);
                }
            }
            else
            {
                AddDamage(damageable);
            }            
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        var damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && !notAffectedObjects.Contains(damageable))
        {
            notAffectedObjects.Add(damageable);
        }
    }

    private void Update()
    {
        objectsToRemove.Clear();
        var keys = new List<IDamageable>(damageTimeoutStorage.Keys);
        foreach(var damageable in keys)
        {
            damageTimeoutStorage[damageable] -= Time.deltaTime;
            if (damageTimeoutStorage[damageable] <= 0)
            {
                if (notAffectedObjects.Contains(damageable))
                {
                    notAffectedObjects.Remove(damageable);
                    objectsToRemove.Add(damageable);
                }
                else
                {
                    AddDamage(damageable);
                }
            }
        }
        
        foreach(var damageable in objectsToRemove)
        {
            damageTimeoutStorage.Remove(damageable);
        }
    }
}
