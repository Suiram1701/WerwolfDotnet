using System.Diagnostics.CodeAnalysis;
using WerwolfDotnet.Logging;
using WerwolfDotnet.Roles;

namespace WerwolfDotnet;

partial class GameContext
{
    /// <summary>
    /// The current round aka. the number of nights (beginning with 0)
    /// </summary>
    public int Round { get; private set; } = 0;
    
    /// <summary>
    /// The current mayor of the village.
    /// </summary>
    public Player? Mayor { get; private set; }

    /// <summary>
    /// Maps which player has fallen in love with whom (triggers <c>CauseOfDeath.DeathByHeathBreak</c>).
    /// </summary>
    public IReadOnlyDictionary<Player, Player> PlayersInLove => _playersInLove.AsReadOnly();
    private readonly Dictionary<Player, Player> _playersInLove = new(2);     // There is only a pair
    
    /// <summary>
    /// Contains all players that are at the time protected mapped to the player who protects them.
    /// </summary>
    public IReadOnlyDictionary<Player, Player> WerwolfProtectedPlayers => _werwolfProtectedPlayers.AsReadOnly();
    private readonly Dictionary<Player, Player> _werwolfProtectedPlayers = [];
    
    private async Task RunAsync(CancellationToken ct)
    {
        State = GameState.Night;
        OnGameStateChanged?.Invoke(this, State, new Dictionary<Player, (CauseOfDeath, Role)>(0), null);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await RunNightAsync(ct);
                await EvaluatePreviousStateAsync(nextState: GameState.Day, ct);

                await RunDayAsync(ct);
                await EvaluatePreviousStateAsync(nextState: GameState.Night, ct);
                Round++;
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
    
    private async Task RunNightAsync(CancellationToken ct)
    {
        foreach (IGrouping<Role, Player> roleGroup in _players
                     .GroupBy(p => p.Role!.Type, p => p)
                     .OrderBy(group => GameOptions!.NightExecutionOrder.IndexOf(group.Key)))
        {
            if (roleGroup.Key == Role.Werwolf)
            {
                await RequestPlayerActionAsync(new PhaseAction(ct)
                {
                    Type = ActionType.WerwolfSelection,
                    Participants = [.._players.Where(p => p.IsAlive && p.Role!.Type < 0)],
                    VotablePlayers = [.._players.Where(p => p.IsAlive && p.Role!.Type > 0)]
                }, (action, _) =>
                {
                    if (action.GetMostVotedPlayer() is not { } playerToDie)
                        return Task.FromResult<string[]?>([]);     // Empty parameters will indicate that no one died.

                    if (_werwolfProtectedPlayers.TryGetValue(playerToDie, out Player? savedBy))
                    {
                        Logger.Log(Event.SuccessfullyProtected, savedBy, playerToDie);
                        return Task.FromResult<string[]?>([playerToDie.Name]);     // Still tell them.
                    }
                        
                    playerToDie.Kill(CauseOfDeath.WerwolfKill, null);
                    return Task.FromResult<string[]?>([playerToDie.Name]);
                });
            }
            else
            {
                foreach (Player player in roleGroup.Where(p => p.IsAlive).Shuffle())
                    await player.Role!.OnNightAsync(this, player, ct);
            }
        }
    }

    private async Task RunDayAsync(CancellationToken ct)
    {
        if (Mayor is null)
        {
            await RequestPlayerActionAsync(new PhaseAction(ct)
            {
                Type = ActionType.MayorVoting,
                Minimum = 0,
                Maximum = 1,
                Participants = [.._players.Where(p => p.IsAlive)],
                VotablePlayers = [.._players.Where(p => p.IsAlive)]
            }, (action, _) =>
            {
                if (action.GetMostVotedPlayer() is { } newMayor)
                {
                    Mayor = newMayor;
                    OnGameMetadataChanged?.Invoke(this, GameMaster.Id, newMayor.Id);
                    return Task.FromResult<string[]?>([newMayor.Name]);
                }
                return Task.FromResult<string[]?>([]);
            });
        }

        Player[] accusedPlayers = [];
        await RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.WerwolfAccuses,
            Minimum = 0,
            Maximum = 1,
            Participants = [.._players.Where(p => p.IsAlive)],
            VotablePlayers = [.._players.Where(p => p.IsAlive)]
        }, (action, _) =>
        {
            /*
             * The players with the three highest amount of votes should be accused.
             * Players are separated into 'levels' of votes and then the three highest levels are accused.
             * For example in a game: A -> 1; B -> 2; C -> 2; D -> 3; E -> 4 = E, D, C, and B are accused because they have the three highest 'levels' and C and B share a level.
             */
            accusedPlayers = [..action.GetPlayersByVoteCount()
                .GroupBy(kvp => kvp.Value, kvp => kvp.Key)
                .OrderByDescending(group => group.Key)
                .Take(3)
                .SelectMany(group => group)];
            
            if (accusedPlayers.Length == 1)
                accusedPlayers[0].Kill(CauseOfDeath.WerwolfKilling, null);
            return Task.FromResult<string[]?>(null);
        });
        
        if (accusedPlayers.Length > 1)
        {
            await RequestPlayerActionAsync(new PhaseAction(ct)
            {
                Type = ActionType.WerwolfKilling,
                Minimum = 1,
                Maximum = 1,
                Participants = [.._players.Where(p => p.IsAlive)],     // At most 3 people can be accused
                VotablePlayers = accusedPlayers
            }, (action, _) =>
            {
                if (action.GetMostVotedPlayer(Mayor is not null ? [Mayor] : null) is { } playerToExecute)
                    playerToExecute.Kill(CauseOfDeath.WerwolfKilling, null);
                return Task.FromResult<string[]?>(null);
            });
        }

        foreach (IGrouping<Role, Player> roleGroup in _players
                     .GroupBy(p => p.Role!.Type, p => p)
                     .OrderBy(group => GameOptions!.NightExecutionOrder.IndexOf(group.Key)))
        {
            foreach (Player player in roleGroup.Shuffle())
                await player.Role!.OnDayAsync(this, player, ct);
        }
    }
    
    private async Task EvaluatePreviousStateAsync(GameState nextState, CancellationToken ct)
    {
        bool? bearGrowls = null;
        if (nextState == GameState.Day)     // Execute before dying players are evaluated 
        {
            foreach ((int i, _) in _players.Index().Where(kvp => kvp.Item is { Role.Type: Role.BearGuide, IsAlive: true }))
            {
                bearGrowls ??= false;
                
                Player previousOne = _players[i <= 0 ? _players.Count - 1 : i - 1];
                bearGrowls |= previousOne.IsAlive && previousOne.Role!.Type < 0;
                
                Player nextOne = _players[i >= _players.Count - 1 ? 0 : i + 1];
                bearGrowls |= nextOne.IsAlive && nextOne.Role!.Type < 0;
            }
        }
        
        Dictionary<Player, (CauseOfDeath, Role)> diedPlayers = new();
        bool newDeathPlayer;
        do
        {
            newDeathPlayer = false;
            foreach (Player player in _players.Where(p => p.Status == PlayerState.PendingDeath))
            {
                newDeathPlayer = true;
                CauseOfDeath cause = player.KillInternal();
                
                await player.Role!.OnDeathAsync(this, player, cause, ct);
                if (GameOptions!.MayorDecidesNextMayor && player.Equals(Mayor))     // Mayor dies, logic stays here 
                {
                    await RequestPlayerActionAsync(new PhaseAction(ct)
                    {
                        Type = ActionType.NextMayorDecision,
                        Minimum = 0,
                        Maximum = 1,
                        Participants = [player],
                        VotablePlayers = [.._players.Where(p => p.Status == PlayerState.Alive)]
                    }, (action, _) =>
                    {
                        Mayor = action.PlayerVotes[player].FirstOrDefault();
                        return Task.FromResult<string[]?>(null);
                    });
                }
                else if (player.Equals(Mayor))
                {
                    Mayor = null;
                }
                
                Role displayedRole = GameOptions!.RevealRoleForCauses.Contains(cause) ? player.Role!.Type : Role.None;
                cause = nextState == GameState.Night ? cause : CauseOfDeath.None;     // The displayed caused (censored when switching to day)

                diedPlayers[player] = (cause, displayedRole);
            }
        } while (newDeathPlayer);     // Loop multiple times over in case other players died during Death-Handler
        
        _werwolfProtectedPlayers.Clear();
        CheckPlayerWin();     // Throws when the game ends
        
        State = nextState;
        OnGameStateChanged?.Invoke(this, State, diedPlayers, bearGrowls);
    }

    internal void PlayersFallInLove(Player player1, Player player2)
    {
        _playersInLove[player1] = player2;
        _playersInLove[player2] = player1;
    }

    /// <summary>
    /// Protects a player from being killed by the werewolf's.
    /// </summary>
    /// <param name="player">The player to protect.</param>
    /// <param name="doneBy">The player who protected him.</param>
    /// <returns>Indicates whether the player is already protected or not.</returns>
    internal bool ProtectPlayer(Player player, Player doneBy) => _werwolfProtectedPlayers.TryAdd(player, doneBy);
    
    private void CheckPlayerWin()
    {
        if (_players.Count == _playersInLove.Count && _players.All(p => p.Status == PlayerState.Alive && _playersInLove.ContainsKey(p)))
            GameWon(Fraction.Lovers);
        
        int amountWerwolfs = _players.Count(p => p.Status == PlayerState.Alive && p.Role!.Type < 0);
        if (Mayor?.Role?.Type < 0)     // Apply the extra vote of the mayor
            amountWerwolfs++;
        if (amountWerwolfs == 0)     // Village win
            GameWon(Fraction.Village);
        
        int amountVillagers = _players.Count(p => p.Status == PlayerState.Alive && p.Role!.Type > 0);
        if (Mayor?.Role?.Type > 0)
            amountVillagers++;
        if (_players.Where(p => p.Status == PlayerState.Alive && p.Role!.Type < 0).All(p => p.Role!.Type == Role.WhiteWolf) && amountVillagers <= 1)
            GameWon(Fraction.WhiteWolf);
        
        if (amountWerwolfs >= amountVillagers)     // Werwolf win
            GameWon(Fraction.Werwolf);
    }
    
    [DoesNotReturn]
    private void GameWon(Fraction wonBy)
    {
        IEnumerable<Player> winners = wonBy switch
        {
            Fraction.Village => _players.Where(p => p.Role?.Type > 0 && p.Status == PlayerState.Alive),
            Fraction.Werwolf => _players.Where(p => p.Role?.Type < 0 && p.Status == PlayerState.Alive),
            Fraction.WhiteWolf => _players.Where(p => p.Role?.Type == Role.WhiteWolf),     // Have to live 
            Fraction.Lovers => _players.Where(p => _playersInLove.ContainsKey(p)),     // Have to live
            _ => _players.Where(p => p.IsAlive)
        };
        Logger.Log(Event.GameWon, args: [wonBy, ..winners]);
        
        State = GameState.GameWon;
        OnGameWon?.Invoke(this, wonBy);
            
        _gameLoopCts!.Cancel();
        _gameLoopCts.Token.ThrowIfCancellationRequested();
#pragma warning disable CS8763 // Method does not return because of ThrowIfCancellationRequested
    }
#pragma warning restore CS8763
}