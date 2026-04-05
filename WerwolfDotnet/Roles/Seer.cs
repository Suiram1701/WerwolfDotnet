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
            ExcludeSelf = true,
            VotablePlayers = [..ctx.Players]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer() is not { } selectedOne)
                return Task.FromResult<string[]?>(null);
            
            ctx.Logger.LogTrace("Seer {seer} saw role of {player}: {roleName}",
                self, selectedOne, selectedOne.Role!.Type);

            Role playerRole = selectedOne.Role!.Type;
            _watchedPlayers[selectedOne] = ctx.GameOptions!.SeerSeesRole ? playerRole : (playerRole > 0 ? Role.Villager : Role.Werwolf);
            return Task.FromResult<string[]?>([selectedOne.Name, _watchedPlayers[selectedOne].ToString()]);
        });
        
        await base.OnNightAsync(ctx, self, ct);
    }
}