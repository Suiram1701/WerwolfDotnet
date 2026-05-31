namespace WerwolfDotnet.Logging;

/// <summary>
/// Different events which can be logged.
/// </summary>
public enum Event              // Comment is a list of parameters which should be passed using this event
{
    Joined = -5,               // player
    Left = -4,                 // player
    BecameGameMaster = -3,     // old game master, new one
    GameStarted = -2,          // No parameters
    GameStopped = -1,          // No parameters
    GameWon = 0,               // [won players]
    Voting,                    // action type, ..[KeyValuePair<Player, Player[]>]
    Killed,                    // Killer, ..[killed ones]
    Healed,                    // healer, healed one
    SawRole,                   // seer, selected one, role
    SeerApprenticeActive,      // player
    Protect,                   // done by, protected one
    SuccessfullyProtected,     // done by, protected one
    FallInLove,                // amor, lover0, lover1
    SleepOver,                 // mattress, player
    VictimMissed,              // killer, victim
    TurnedToWerwolf,           // done by, victim
}