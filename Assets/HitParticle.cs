using UnityEngine;

public class HitParticle : MonoBehaviour {
	private float liveTime = -1f;
	public float maxliveTime = 0.3f;
	public bool isUseMine =false;
	Transform myTransform;
	public ParticleSystem myParticleSystem;

    public const float DefaultHeightFlyOutEffect = 1.75f;

	void Start()
	{
		myTransform=transform;
		myTransform.position=new Vector3(-10000f, -10000f, -10000f);
		myParticleSystem.enableEmission=false;
		gameObject.SetActive (false);
	}
	
	public void StartShowParticle(Vector3 pos, Quaternion rot, bool _isUseMine)
	{
		gameObject.SetActive (true);

		isUseMine=_isUseMine;
		liveTime=maxliveTime;        
		myTransform.position=pos;
		myTransform.rotation=rot;
		myParticleSystem.enableEmission=true;
	}

    public void StartShowParticle(Vector3 pos, Quaternion rot, bool _isUseMine, Vector3 flyOutPos)
    {
        StartShowParticle(pos, rot, _isUseMine);
        if (myTransform.childCount > 0)
        {
			myParticleSystem.transform.position=flyOutPos;
        }
    }
	
	void Update()
	{
		if (liveTime<0) return;
		liveTime -= Time.deltaTime;
		if (liveTime < 0)
		{
			myTransform.position=new Vector3(-10000f, -10000f, -10000f);
			myParticleSystem.enableEmission=false;
			isUseMine=false;

			gameObject.SetActive (false);
		}
	}
}
