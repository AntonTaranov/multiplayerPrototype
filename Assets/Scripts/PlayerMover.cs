using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    public enum Direction
    {
        idle, forward, backward, left, right
    }

    internal Direction moveDirection { get; private set; } = Direction.idle;
    
    const float MAX_SPEED = 5f;
    const float MAX_ANGLE_VERICAL = 89;
    const float MIN_ANGLE_VERTICAL = -65f;
    const float ROTATION_SENSIVITY = 300;

    GameInput gameInput;
    Vector2 rotations;

    [SerializeField]
    GameObject body;
    [SerializeField]
    Transform verticalRotationPivot;

    internal Vector2 GetRotations() => rotations;

    internal void SetRotations(Vector2 rotations)
    {
        var currentEulers = transform.localEulerAngles;
        currentEulers.y = rotations.x;
        transform.localEulerAngles = currentEulers;
        
        var aimEulers = verticalRotationPivot.transform.localEulerAngles;
        aimEulers.x = rotations.y;
        verticalRotationPivot.transform.localEulerAngles = aimEulers;

        this.rotations = rotations;
    }

    internal void SetControls(GameInput input)
    {
        this.gameInput = input;       
    }

    internal void SetBodyVisible(bool value)
    {
        if (body!= null)
        {
            body.SetActive(value);
        }
    }

    internal void UpdateWithDelta(float horizontalRotation, float verticalRotation)
    {
        var currentEulers = transform.localEulerAngles;
        currentEulers.y += horizontalRotation * ROTATION_SENSIVITY;
        transform.localEulerAngles = currentEulers;

        rotations.x = currentEulers.y;

        if (verticalRotationPivot != null)
        {
            var aimEulers = verticalRotationPivot.transform.localEulerAngles;
            aimEulers.x -= verticalRotation * ROTATION_SENSIVITY;
            if (aimEulers.x > 180)
            {
                aimEulers.x -= 360;
            }
            if (aimEulers.x > MAX_ANGLE_VERICAL)
            {
                aimEulers.x = MAX_ANGLE_VERICAL;
            }
            else if (aimEulers.x < MIN_ANGLE_VERTICAL)
            {
                aimEulers.x = MIN_ANGLE_VERTICAL;
            }
            verticalRotationPivot.transform.localEulerAngles = aimEulers;
            rotations.y = aimEulers.x;
        }
    }

    void UpdateDirectionFromMove(Vector3 move)
    {
        if (move.z > 0)
        {
            moveDirection = Direction.forward;
            if (move.x > 0)
            {
                if (move.x > move.z)
                {
                    moveDirection = Direction.right;
                }
            }
            else
            {
                if (move.x < -move.z)
                {
                    moveDirection = Direction.left;
                }
            }
        }
        else
        {
            moveDirection = Direction.backward;
            if (move.x > 0)
            {
                if (move.x > -move.z)
                {
                    moveDirection = Direction.right;
                }
            }
            else
            {
                if (move.x < move.z)
                {
                    moveDirection = Direction.left;
                }
            }
        }
    }

    void Update()
    {
        if (gameInput != null)
        {
            if (gameInput.HasInputData)
            {
                UpdateWithDelta(gameInput.rotationHorizontal, gameInput.rotationVertical);
            }
            moveDirection = Direction.idle;
            if (gameInput.HasJoystickData)
            {
                Vector3 move = new Vector3(gameInput.joystickValues.x + Input.GetAxis("Horizontal"),
                    0,
                    gameInput.joystickValues.y + Input.GetAxis("Vertical")) * Time.deltaTime;


                var moveTransform = transform.TransformVector(move * MAX_SPEED);
                transform.Translate(moveTransform, Space.World);

                UpdateDirectionFromMove(move);
            }
        }
    }
}
