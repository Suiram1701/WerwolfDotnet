using Microsoft.Extensions.Logging;

namespace WerwolfDotnet.Roles;

public sealed class Amor : RoleBase
{
    public override Role Type => Role.Amor;
    
    public bool Done { get; private set; }

    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        if (Done)
        {
            await base.OnNightAsync(ctx, self, ct);
            return;
        }

        await ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.AmorSelection,
            Minimum = 2,
            Maximum = 2,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.Alive)]     // In the first round everyone should be alive but just to be sure
        }, (action, _) =>
        {
            Player[] votes = action.PlayerVotes[self];
            if (votes.Length != 2)
                return Task.FromResult<string[]?>(null);
            
            ctx.PlayersFallInLove(votes[0], votes[1]);
            ctx.Logger.LogTrace("Amor {amor} made {player1} to fall in Love with {player2})", self, votes[0], votes[1]);
            
            Done = true;
            return Task.FromResult<string[]?>(null);
        });
        await base.OnNightAsync(ctx, self, ct);
    }
}