using UnityEngine;
using System.Collections;

public class TouchObserver
{
    internal Vector2 deltaPosition { get; private set; }
    internal Vector2 lastPosition { get; private set; }

    internal GameInput.TouchTarget target = GameInput.TouchTarget.None;

    internal void Update(Touch touch)
    {
        deltaPosition = touch.deltaPosition;
        lastPosition = touch.position;
    }

    internal void Update(Vector2 newPosition, bool lockedMouse = true)
    {
        deltaPosition = newPosition - lastPosition;
        lastPosition = lockedMouse ? Vector2.zero : newPosition;
    }

    internal void InitializePosition(Vector2 position)
    {
        lastPosition = position;
    }
}
