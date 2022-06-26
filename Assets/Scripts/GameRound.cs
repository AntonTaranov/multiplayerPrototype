
public class GameRound
{
    internal const int MAX_PLAYERS = 2;
    internal const float ROUND_TIME = 60;

    float startTime = 0;
    float roundTime;

    internal enum State
    {
        WaitForPlayers,
        ReadyToStart,
        Running,
        Finished
    }

    internal State currentState { get; private set; } = State.WaitForPlayers;

    internal void OnPlayerWantRepeat()
    {
        currentState = State.WaitForPlayers;
    }

    internal void OnPlayerConnected(int numPlayersInRoom)
    {
        if (currentState == State.WaitForPlayers && numPlayersInRoom == MAX_PLAYERS)
        {
            currentState = State.ReadyToStart;
        }
    }

    internal void OnGameStarted(uint time)
    {
        if (currentState == State.ReadyToStart)
        {
            startTime = time / 1000.0f;
            currentState = State.Running;
        }
    }

    internal bool IsRunning(float time)
    {
        roundTime = time - startTime;
        if (roundTime >= ROUND_TIME && currentState == State.Running)
        {
            currentState = State.Finished;
        }
        return currentState == State.Running;
    }

    internal int GetSecondsLeft()
    {
        return (int)(ROUND_TIME - roundTime);
    }
   
}
