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

        await ctx.RequestPlayerActionAsync(new PhaseAction
        {
            Type = ActionType.AmorSelection,
            Minimum = 2,
            Maximum = 2,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.Alive)]     // In the first round everyone should be alive but just to be sure
        }, (action, _) =>
        {
            Player[] votes = action.PlayerVotes[self];
            ctx.PlayersFallInLove(votes[0], votes[1]);
            ctx.Logger.LogTrace(
                "Amor {amor} ({amorId}) made {player1} ({player1Id}) to fall in Love with {player2} ({player2Id})",
                self.Name, self.Id, votes[0].Name, votes[0].Id, votes[1].Name, votes[1].Id);
            
            Done = true;
            return Task.FromResult<string[]?>(null);
        }, ct);
        await base.OnNightAsync(ctx, self, ct);
    }
}