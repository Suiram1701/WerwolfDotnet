using WerwolfDotnet.Actions;
using WerwolfDotnet.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.Seer)]
public class Seer : RoleBase
{
    public IReadOnlyDictionary<Player, Role> WatchedPlayers => _watchedPlayers.AsReadOnly();
    private readonly Dictionary<Player, Role> _watchedPlayers = new();

    protected virtual ActionType ActionType => ActionType.SeerSelection;
    
    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        await ctx.RequestPlayerActionAsync(new PlayerAction(ct)
        {
            Type = ActionType,
            Participants = [self],
            ExcludeSelf = true,
            VotablePlayers = [..ctx.Players]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer() is not { } selectedOne)
                return ActionResult.Failed();
            
            Role playerRole = selectedOne.Role!.Type;
            ctx.Logger.Log(Event.SawRole, self, selectedOne, playerRole);
            _watchedPlayers[selectedOne] = ctx.GameOptions!.SeerSeesRole ? playerRole : (playerRole > 0 ? Role.Villager : Role.Werwolf);
            
            return ActionResult.Success(selectedOne, _watchedPlayers[selectedOne]);
        });
        
        await base.OnNightAsync(ctx, self, ct);
    }
}