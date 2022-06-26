using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlesManager : MonoBehaviour
{
    [SerializeField] GameObject Shot;
    [SerializeField] GameObject Hit;

    List<HitParticle> hitEffects = new List<HitParticle>();

    private void Start()
    {
        if (hitEffects.Count == 0)
        {
            for (int i = 0; i < 5; i++)
            {
                CreateNewHitEffect();
            }
        }
    }

    HitParticle CreateNewHitEffect()
    {
        var hitInstance = Instantiate(Hit, transform);
        var hitEffect = hitInstance.GetComponent<HitParticle>();
        hitEffects.Add(hitEffect);
        return hitEffect;
    }

    internal void CreateHit(Transform hitPosition)
    {
        HitParticle hitEffect = null;
        if (hitEffects.Count == 0)
        {
            hitEffect = CreateNewHitEffect();
        }
        else
        {
            foreach(var effect in hitEffects)
            {
                if(!effect.isUseMine)
                {
                    hitEffect = effect;
                    break;
                }
            }
        }
        if (hitEffect == null)
        {
            hitEffect = CreateNewHitEffect();
        }

        hitEffect.StartShowParticle(hitPosition.position, hitPosition.rotation, true);
    }

    internal void CreateShot(Transform shotSource)
    {
        if (shotSource != null)
        {
            var shotEffect = Instantiate<GameObject>(Shot, shotSource, false);
            shotEffect.transform.localPosition = new Vector3();

            Destroy(shotEffect, 0.1f);
        }
    }
}
