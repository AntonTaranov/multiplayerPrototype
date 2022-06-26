using UnityEngine;
using System.Collections.Generic;

public class GameInput
{
    private const int MOUSE_ID = 1000;
    private bool isMouseRotationActivate = false;
    private TouchObserver mouseObserver = new TouchObserver();

    public enum TouchTarget
    {
        ButtonFire,
        ButtonJump,
        ButtonReload,
        ButtonWeapon1,
        ButtonWeapon2,
        ButtonWeapon3,
        Joystick,
        None
    }

    int screenWidth;
    int screenHeight;
    internal bool CheckMouseLock { get; private set; }
    internal bool HasInputData { get; private set; }
    internal bool HasJoystickData { get; private set; }
    internal float rotationHorizontal { get; private set; }
    internal float rotationVertical { get; private set; }

    internal Vector2 joystickValues { get; private set; }

    Dictionary<int, TouchObserver> activeTouches = new Dictionary<int, TouchObserver>();

    internal Vector2 reloadButtonPosition { get; private set; }
    internal Vector2 fireButtonPosition { get; private set; }
    internal Vector2 jumpButtonPosition { get; private set; }

    internal Vector2 weapon1ButtonPosition { get; private set; }
    internal Vector2 weapon2ButtonPosition { get; private set; }
    internal Vector2 weapon3ButtonPosition { get; private set; }

    internal Vector2 joystickPosition { get; private set; }

    internal bool jumpIsPressed { get; private set; }
    internal bool fireIsPressed { get; private set; }
    internal bool reloadIsPressed { get; private set; }
    internal bool keyboardJumpIsPressed { get; private set; }
    internal bool mousefireIsPressed { get; private set; }

    internal bool weapon1IsPressed { get; private set; }
    internal bool weapon2IsPressed { get; private set; }
    internal bool weapon3IsPressed { get; private set; }

    internal bool joystickIsPressed { get; private set; }

    int lastTouchId;

    float buttonRadius;

    public GameInput()
    {
        screenHeight = Screen.height;
        screenWidth = Screen.width;

        var buttonPositionRadius = screenHeight * 0.5f;
        var buttonOffset = screenWidth * 0.05f;

        reloadButtonPosition = new Vector2(screenWidth - buttonOffset, buttonPositionRadius);
        fireButtonPosition = new Vector2(screenWidth - buttonPositionRadius + buttonOffset, buttonPositionRadius - buttonOffset);
        jumpButtonPosition = new Vector2(screenWidth - buttonPositionRadius, buttonOffset);

        buttonRadius = screenHeight * 0.05f;

        weapon2ButtonPosition = new Vector2(screenWidth * 0.5f, buttonRadius * 1.5f);
        weapon1ButtonPosition = weapon2ButtonPosition + Vector2.left * buttonRadius * 2.5f;
        weapon3ButtonPosition = weapon2ButtonPosition + Vector2.right * buttonRadius * 2.5f;

        joystickPosition = new Vector2(screenWidth * 0.15f, screenWidth * 0.15f);
    }

    bool IsInsideRotationRectangle(float x, float y) => (x > screenWidth * 0.5f && y <= screenHeight * 0.6f);
    bool IsInsideJoystickRectangle(float x, float y) => (x < screenWidth * 0.5f && y <= screenHeight * 0.6f);

    void ProcessJoystickTouch(TouchObserver touch)
    {
        HasJoystickData = true;
        var deltaPosition = touch.lastPosition - joystickPosition;
        if (deltaPosition.magnitude < buttonRadius * 0.5f)
        {
            HasJoystickData = false;
        }
        else if (deltaPosition.magnitude > buttonRadius)
        {
            joystickValues = deltaPosition.normalized;
        }
        else
        {
            joystickValues = deltaPosition.normalized * (deltaPosition.magnitude / buttonRadius);
        }
    }

    void ProcessNextTouch(Touch touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            var button = HitButtonAt(touch.position);
            TouchObserver touchObserver = null;
            var insideRect = IsInsideRotationRectangle(touch.position.x, touch.position.y);            
            if (button != TouchTarget.None || insideRect)
            {                
                touchObserver = new TouchObserver();
                touchObserver.target = button;
                touchObserver.InitializePosition(touch.position);

                if (activeTouches.ContainsKey(touch.fingerId))
                {
                    if (button != TouchTarget.Joystick)
                    {
                        activeTouches[touch.fingerId] = touchObserver;
                    }
                }
                else
                {
                    if (button == TouchTarget.Joystick)//check for already touched
                    {
                        var alreadyTouched = false;
                        foreach (var activeTouch in activeTouches.Values)
                        {
                            if (activeTouch.target == button)
                            {
                                alreadyTouched = true;
                            }
                        }
                        if (!alreadyTouched)
                        {
                            activeTouches.Add(touch.fingerId, touchObserver);
                            ProcessJoystickTouch(touchObserver);
                        }
                    }
                    else
                    {
                        activeTouches.Add(touch.fingerId, touchObserver);
                    }                    
                }
                if (insideRect)
                {
                    lastTouchId = touch.fingerId;
                }
            }            
        }
        else if (activeTouches.ContainsKey(touch.fingerId))
        {
            if (touch.phase == TouchPhase.Moved)
            {
                var touchObserver = activeTouches[touch.fingerId];
                touchObserver.Update(touch);
                if (touchObserver.target == TouchTarget.Joystick)
                {
                    ProcessJoystickTouch(touchObserver);
                }
                else if (lastTouchId == touch.fingerId)
                {
                    rotationHorizontal = touchObserver.deltaPosition.x / screenWidth;
                    rotationVertical = touchObserver.deltaPosition.y / screenHeight;
                    HasInputData = true;
                }
            }
            else if (touch.phase != TouchPhase.Stationary)
            {
                var touchObserver = activeTouches[touch.fingerId];
                activeTouches.Remove(touch.fingerId);
                DeactivateTouchObserver(touchObserver);

            }
            else
            {
                var touchObserver = activeTouches[touch.fingerId];
                if (touchObserver.target == TouchTarget.Joystick)
                {
                    ProcessJoystickTouch(touchObserver);
                }
            }
        }
    }

    TouchTarget HitButtonAt(Vector2 position)
    {
        if (Vector2.Distance(position, jumpButtonPosition) < buttonRadius)
        {
            jumpIsPressed = true;
            return TouchTarget.ButtonJump;
        }
        else if (Vector2.Distance(position, fireButtonPosition) < buttonRadius)
        {
            fireIsPressed = true;
            return TouchTarget.ButtonFire;
        }
        else if (Vector2.Distance(position, reloadButtonPosition) < buttonRadius)
        {
            reloadIsPressed = true;
            return TouchTarget.ButtonReload;
        }
        else if (Vector2.Distance(position, weapon1ButtonPosition) < buttonRadius)
        {
            weapon1IsPressed = true;
            return TouchTarget.ButtonWeapon1;
        }
        else if (Vector2.Distance(position, weapon2ButtonPosition) < buttonRadius)
        {
            weapon2IsPressed = true;
            return TouchTarget.ButtonWeapon2;
        }
        else if (Vector2.Distance(position, weapon3ButtonPosition) < buttonRadius)
        {
            weapon3IsPressed = true;
            return TouchTarget.ButtonWeapon3;
        }
        else if (IsInsideJoystickRectangle(position.x,position.y))
        {
            joystickIsPressed = true;
            return TouchTarget.Joystick;
        }
        return TouchTarget.None;
    }

    void DeactivateTouchObserver(TouchObserver touchObserver)
    {
        switch (touchObserver.target)
        {
            case TouchTarget.ButtonFire:
                fireIsPressed = false;
                break;
            case TouchTarget.ButtonJump:
                jumpIsPressed = false;
                break;
            case TouchTarget.ButtonReload:
                reloadIsPressed = false;
                break;
            case TouchTarget.ButtonWeapon1:
                weapon1IsPressed = false;
                break;
            case TouchTarget.ButtonWeapon2:
                weapon2IsPressed = false;
                break;
            case TouchTarget.ButtonWeapon3:
                weapon3IsPressed = false;
                break;
            case TouchTarget.Joystick:
                joystickIsPressed = false;
                break;
        }
    }

    internal void Update()
    {
        /*
        if (Input.touchCount > 0)
        {
            foreach(var touch in Input.touches)
            {
                ProcessNextTouch(touch);
            }
        }        
        */
#if UNITY_EDITOR || PLATFORM_STANDALONE_WIN
        /*
        if (activeTouches.ContainsKey(MOUSE_ID))
        {
            if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
            {
                var touchObserver = activeTouches[MOUSE_ID];
                activeTouches.Remove(MOUSE_ID);
                DeactivateTouchObserver(touchObserver);
            }
            else
            {
                var mouseTouch = activeTouches[MOUSE_ID];
                mouseTouch.Update(Input.mousePosition);

                if (mouseTouch.target == TouchTarget.Joystick)
                {
                    ProcessJoystickTouch(mouseTouch);
                }
                else
                {
                    //rotation in scale from screen width
                    rotationHorizontal = mouseTouch.deltaPosition.x / screenWidth; 
                    rotationVertical = mouseTouch.deltaPosition.y / screenWidth;

                    HasInputData = true;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {            
            var touchPosition = Input.mousePosition;
            var insideRect = IsInsideRotationRectangle(touchPosition.x, touchPosition.y);
            var button = HitButtonAt(touchPosition);
            if (button != TouchTarget.None || insideRect) 
            {                
                var mouseObserver = new TouchObserver();
                activeTouches.Add(MOUSE_ID, mouseObserver);
                mouseObserver.InitializePosition(touchPosition);
                mouseObserver.target = button;
                if (button == TouchTarget.Joystick)
                {
                    ProcessJoystickTouch(mouseObserver);
                }
            }
        }
        */
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            HasJoystickData = true;
            joystickValues = Vector2.zero;
        }
        if(isMouseRotationActivate)
        {
            mouseObserver.Update(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
            rotationHorizontal = mouseObserver.deltaPosition.x / screenWidth;
            rotationVertical = mouseObserver.deltaPosition.y / screenWidth;
            HasInputData = true;
        }
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            ActivateMouseMoveAim(false);
        }
        if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            if(!isMouseRotationActivate)
            {
                CheckMouseLock = true;
            }
            else
            {
                if(Input.GetMouseButtonUp(0))
                {
                    mousefireIsPressed = true;
                }
            }
        }
        if (Input.GetButtonDown("Jump"))
        {
            keyboardJumpIsPressed = true;
        }
#endif 
    }

    internal void LateUpdate()
    {
        HasInputData = false;
        HasJoystickData = false;
        CheckMouseLock = false;
        mousefireIsPressed = false;
        keyboardJumpIsPressed = false;
    }

    internal void ActivateMouseMoveAim(bool value)
    {
#if UNITY_EDITOR || PLATFORM_STANDALONE_WIN
        if(value)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            mouseObserver.InitializePosition(new Vector2());
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
#endif
        isMouseRotationActivate = value;
    }
}
