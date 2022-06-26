using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] Transform DamageArea;

    [SerializeField] float ExplosionSpeed;

    float radius = 0.1f;

    bool explosionComplete = false;

    internal float ExplosionRadius;

    void Update()
    {
        if (explosionComplete)
        {
            DamageArea.gameObject.SetActive(false);
            Destroy(gameObject, 3);
            explosionComplete = false;
            return;
        }

        if (DamageArea.gameObject.activeSelf)
        {
            radius += Time.deltaTime * ExplosionSpeed;

            if (radius > ExplosionRadius)
            {
                radius = ExplosionRadius;
                explosionComplete = true;
            }

            DamageArea.localScale = new Vector3(radius, radius, radius) * 2;
        }
    }
}
