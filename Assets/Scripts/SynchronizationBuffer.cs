using System.Collections.Generic;
using UnityEngine;

public class SynchronizationBuffer
{
    const float accelerationRate = 1.25f;
    const float deaccelerationRate = 0.75f;
    
    Queue<StateInTime> receivedData = new Queue<StateInTime>();
    List<StateInTime> bufferFrame = new List<StateInTime>();

    double time = -1;

    StateInTime activeState;

    internal Vector3 position { get; private set; }
    internal int animationIndex { get; private set; }
    
    Vector2 rotations;
    internal Vector2 GetRotations() => rotations;

    private readonly int bufferSize;
       
    public SynchronizationBuffer(int size)
    {
        bufferSize = size;
    }

    internal void ResetBuffer()
    {
        bufferFrame.Clear();
        receivedData.Clear();
        rotations = Vector2.zero;
        time = -1;
    }

    internal void SetRotations(Vector2 rotations)
    {
        this.rotations = rotations;
    }

    internal void AddNewState(Vector3 position, Vector2 rotations, int animationIndex, double time)
    {
        receivedData.Enqueue(new StateInTime(position, rotations, animationIndex, time));
        UpdateBufferFrame();
    }

    void UpdateBufferFrame()
    {
        if (bufferFrame.Count < bufferSize)
        {
            bufferFrame.Add(receivedData.Dequeue());
        }
        else if (bufferFrame[1].time < time)
        {
            while (bufferFrame[1].time < time && receivedData.Count > 0)
            {
                bufferFrame = bufferFrame.GetRange(1, bufferSize - 1);
                bufferFrame.Add(receivedData.Dequeue());
            }
        }
    }

    internal void Update(float timeDelta)
    {
        if (time < 0)
        {
            //initialize time
            if (bufferFrame.Count > 0)
            {
                var state = bufferFrame[0];
                time = state.time;
                activeState = state;
                position = activeState.position;
                animationIndex = activeState.animationIndex;
                return;
            }
        }
        else
        {
            var nextTime = time + timeDelta;
            if (bufferFrame.Count >= bufferSize)
            {
                var findNextState = false;
                StateInTime nextState = null;
                int step = 0;
                while (!findNextState && step < bufferSize - 1)
                {
                    nextState = bufferFrame[step + 1];
                    step++;
                    if (nextState.time > nextTime)
                    {
                        findNextState = true;
                    }
                    else if(step < bufferSize - 1)
                    {
                        activeState = nextState;
                    }
                }
                if (!findNextState)
                {
                    time += timeDelta;
                    UpdateBufferFrame();
                    return;
                }

                var statesTimeInterval = nextState.time - activeState.time;
                var statesDeltaTime = nextTime - activeState.time;

                int halfBuffer = (int)Mathf.Ceil(bufferSize * 0.5f);
                if (step < halfBuffer)
                {
                    statesDeltaTime *= accelerationRate;
                }
                else if (step > halfBuffer)
                {
                    statesDeltaTime *= deaccelerationRate;
                }

                time = activeState.time + statesDeltaTime;

                if (time > nextState.time)
                {
                    time = nextState.time;
                }

                var progress = (float)(statesDeltaTime / statesTimeInterval);
                this.position = Vector3.Lerp(activeState.position, nextState.position,progress);
                                
                rotations.x = activeState.rotations.x;
                rotations.y = activeState.rotations.y;    
                if (rotations.x != nextState.rotations.x)
                {
                    rotations.x = Mathf.LerpAngle(activeState.rotations.x, nextState.rotations.x, progress);
                }
                if (rotations.y != nextState.rotations.y)
                {
                    rotations.y = Mathf.LerpAngle(activeState.rotations.y, nextState.rotations.y, progress);
                }
                
                animationIndex = progress > 0 ? nextState.animationIndex : activeState.animationIndex;

                UpdateBufferFrame();
            }
        }
    }

    class StateInTime
    {
        internal Vector3 position { get; private set; }
        internal Vector2 rotations { get; private set; }        
        internal int animationIndex { get; private set; }
        internal double time { get; private set; }

        public StateInTime(Vector3 position, Vector2 rotations,
                            int animationIndex, double time)
        {
            this.position = position;
            this.rotations = rotations;
            
            this.animationIndex = animationIndex;
            this.time = time;
        }
    }
}