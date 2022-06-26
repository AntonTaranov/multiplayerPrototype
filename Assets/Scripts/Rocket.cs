using UnityEngine;

public class Rocket : MonoBehaviour
{
    const float LIFETIME = 5;
    const float START_SPEED = 25;

    [SerializeField] GameObject ExplosionPrefab;

    [SerializeField] private float ExplosionRadius;
    [SerializeField] private int maxDamage;

    PhotonView photonView;
    SynchronizationBuffer synchronization = new SynchronizationBuffer(2);

    Vector3 speed;
    float time = 0;
    private bool exploded = false;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        var rigidBody = GetComponent<Rigidbody>();
        if (!photonView.isMine)
        {            
            if (rigidBody != null)
            {
                rigidBody.isKinematic = true;
                rigidBody.detectCollisions = false;
            }
        }
        else
        {
            synchronization.ResetBuffer();
            synchronization.AddNewState(transform.position, Vector2.zero, 0, PhotonNetwork.time);
        }
    }
    
    internal void Launch(Quaternion rotation)
    {
        speed = (rotation * Vector3.forward) * START_SPEED;        
    }
    
    [PunRPC]
    void ShowExplosionRPC(Vector3 position)
    {
        var explosion = Instantiate(ExplosionPrefab);
        explosion.transform.position = position;
        var explosionComponent = explosion.GetComponent<Explosion>();
        explosionComponent.ExplosionRadius = ExplosionRadius;
    }
    
    void Update()
    {
        if (photonView.isMine)
        {
            var deltaTime = Time.deltaTime;
            time += deltaTime;
            if (time > LIFETIME)
            {
                CreateExplosion();
            }
            else
            {
                transform.position = transform.position + speed * deltaTime;
            }
        }      
        else
        {            
            synchronization.Update(Time.deltaTime);
            var position = synchronization.position;
            transform.position = position;            
        }
    }

    void CreateExplosion()
    {
        if (photonView.isMine && !exploded)
        {
            var explosion = Instantiate(ExplosionPrefab);
            explosion.transform.position = transform.position;
            var explosionComponent = explosion.GetComponent<Explosion>();
            explosionComponent.ExplosionRadius = ExplosionRadius;

            //create explosions for others
            photonView.RPC("ShowExplosionRPC", PhotonTargets.Others, transform.position);

            CalculateDamage(transform.position);            
            PhotonNetwork.Destroy(photonView);
            exploded = true;
        }
    }
        
    void CalculateDamage(Vector3 position)
    {
        Collider[] damagedObjects = Physics.OverlapSphere(position, ExplosionRadius);
        foreach (var item in damagedObjects)
        {
            var damageable = item.GetComponent<IDamageable>();
            if (damageable != null)
            {
                var closestPoint = item.ClosestPoint(position);
                int damage = (int)(maxDamage * Mathf.Lerp(1, 0, Vector3.Distance(closestPoint, position) / ExplosionRadius));
                damageable.AddDamage(damage);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        CreateExplosion();
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
