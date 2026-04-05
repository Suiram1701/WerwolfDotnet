using Microsoft.Extensions.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.WhiteWolf, FixedAmount = 1)]
public sealed class WhiteWolf : Werwolf
{
    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        if (ctx.Round % 2 != 0)     // Only done every two nights
        {
            await base.OnNightAsync(ctx, self, ct);
            return;
        }
        
        Player? werwolfSelected = ctx.PreviousActions.First(action => action.Type == ActionType.WerwolfSelection).GetMostVotedPlayer();
        await ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.WhiteWolfSelection,
            Minimum = 0,
            Maximum = 1,
            Participants = [self],
            ExcludeSelf = true,
            VotablePlayers = [..ctx.Players.Where(p => p.IsAlive && !p.Equals(werwolfSelected))]
        }, (action, _) =>
        {
            if (action.PlayerVotes[self].FirstOrDefault() is not { } selectedOne)
                return Task.FromResult<string[]?>(null);
            
            if (ctx.WerwolfProtectedPlayers.TryGetValue(selectedOne, out Player? savedBy))
            {
                ctx.Logger.LogTrace("{self} was successfully protected by {savedBy}", selectedOne, savedBy);
                return Task.FromResult<string[]?>(null);
            }
                        
            selectedOne.Kill(CauseOfDeath.WhiteWolfKill, self);
            return Task.FromResult<string[]?>(null);
        });
        await base.OnNightAsync(ctx, self, ct);
    }
}