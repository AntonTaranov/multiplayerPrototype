using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    enum State
    {
        Idle,
        PlayerActive,
        PlayerDead,
        MoveToIdle
    }

    State currentState = State.Idle;
       
    [SerializeField] Transform cameraIdlePosition;

    Animator cameraAnimator;

    float animationProgress = 0;
    const float animationSpeed = 0.3f;
    Vector3 animationStartPosition;
    Quaternion animationStartRotation;

    internal void OnPlayerBirth()
    {
        if (cameraAnimator != null)
        {
            cameraAnimator.enabled = false;
        }
        currentState = State.PlayerActive;
    }

    internal void OnPlayerDied()
    {
        if (cameraAnimator != null)
        {
            cameraAnimator.enabled = true;
        }
        currentState = State.PlayerDead;
    }

    void Awake()
    {
        cameraAnimator = GetComponent<Animator>();
    }

    internal void OnRoundFinished()
    {
        currentState = State.MoveToIdle;
        transform.SetParent(null);
        animationStartPosition = transform.position;
        animationStartRotation = transform.rotation;
        animationProgress = 0;
    }
        
    void Update()
    {
        if (currentState == State.MoveToIdle)
        {
            animationProgress = Time.deltaTime * animationSpeed + animationProgress;
            transform.position = Vector3.Lerp(animationStartPosition,
                cameraIdlePosition.position, animationProgress);
            transform.rotation = Quaternion.Lerp(animationStartRotation,
                cameraIdlePosition.rotation, animationProgress);
            if (animationProgress >= 1)
            {
                currentState = State.Idle;
            }
        }
    }
}
