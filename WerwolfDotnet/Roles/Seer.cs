using Microsoft.Extensions.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.Seer)]
public class Seer : RoleBase
{
    public IReadOnlyDictionary<Player, Role> WatchedPlayers => _watchedPlayers.AsReadOnly();
    private readonly Dictionary<Player, Role> _watchedPlayers = new();
    
    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        await ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.SeerSelection,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Except([self])]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer() is not { } selectedOne)
                return Task.FromResult<string[]?>(null);
            
            ctx.Logger.LogTrace("Seer {seer} saw role of {player}: {roleName}",
                self, selectedOne, selectedOne.Role!.Type);
            _watchedPlayers[selectedOne] = selectedOne.Role!.Type;
            return Task.FromResult<string[]?>([selectedOne.Name, selectedOne.Role!.Type.ToString()]);
        });
        
        await base.OnNightAsync(ctx, self, ct);
    }
}