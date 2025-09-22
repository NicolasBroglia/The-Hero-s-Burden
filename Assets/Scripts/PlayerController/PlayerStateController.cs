using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    public void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;

        ExitState(CurrentState);
        CurrentState = newState;
        EnterState(CurrentState);
    }

    private void EnterState(PlayerState state)
    {
        // Logic on entering a state
        switch (state)
        {
            case PlayerState.Attacking:
                // e.g., reduce movement speed
                break;
            case PlayerState.Dashing:
                break;
        }
    }

    private void ExitState(PlayerState state)
    {
        // Logic on exiting a state
        switch (state)
        {
            case PlayerState.Attacking:
                // reset movement speed
                break;
        }
    }

    public bool CanMove() => CurrentState != PlayerState.Attacking && CurrentState != PlayerState.Dashing;
    public bool CanAttack() => CurrentState != PlayerState.Dashing;
}
