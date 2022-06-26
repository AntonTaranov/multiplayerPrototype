using System.Collections;
using UnityEngine.Animations;
using UnityEngine;

public class GunController : MonoBehaviour
{
    [SerializeField] Transform effectPositon;
    [SerializeField] bool hideEffectOnReload = false;

    [SerializeField] int MAX_AMMO;
    [SerializeField] float COOLDOWN_TIME;
    [SerializeField] float RELOAD_TIME;

        
    internal int maxAmmo { get => MAX_AMMO; }
    internal float cooldownTime { get => COOLDOWN_TIME; }
    internal float reloadTime { get => RELOAD_TIME; }
    internal Transform effectTransform { get => effectPositon; }

    Animation animationComponent;

    // Start is called before the first frame update
    void Start()
    {
        animationComponent = GetComponent<Animation>();
    }

    internal void Fire()
    {
        if (animationComponent != null)
        {
            animationComponent.Play("Shoot");
        }
    }

    internal void Reload()
    {
        if (animationComponent != null)
        {
            animationComponent.Play("Reload");
        }
        if (hideEffectOnReload && effectPositon)
        {
            effectPositon.gameObject.SetActive(false);
        }
    }

    internal void ReloadFinished()
    {
        if (hideEffectOnReload && effectPositon)
        {
            effectPositon.gameObject.SetActive(true);
        }
    }
}
