using Microsoft.Extensions.Logging;

namespace WerwolfDotnet.Roles;

[Role(Role.Urwolf, FixedAmount = 1)]
public sealed class Urwolf : Werwolf
{
    public bool Done { get; private set; }
    
    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        if (Done)
        {
            await base.OnNightAsync(ctx, self, ct);
            return;
        }

        Player? werwolfSelected = ctx.PreviousActions.First(action => action.Type == ActionType.WerwolfSelection).GetMostVotedPlayer();
        await ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.UrwolfSelection,
            Minimum = 0,
            Maximum = 1,
            Participants = [self],
            VotablePlayers = werwolfSelected is not null ? [werwolfSelected] : [],
        }, (action, _) =>
        {
            if (action.PlayerVotes[self].FirstOrDefault() is not { } player)
                return Task.FromResult<string[]?>(null);
            
            if (player.Role is VillageMattress { LastSleepover: not null })
            {
                ctx.Logger.LogTrace("Urwolf {self} couldn't change role of {player} to Werwolf (player not home)", self, player);
            }
            else
            {
                player.Revive(self);     // When not killed (e.g. saved by healer, ...) ignored
                player.Role = new Werwolf();
                ctx.Logger.LogTrace("Urwolf {self} changed the role of {player} to Werwolf", self, player);

                Player? visit = ctx.Players.SingleOrDefault(p => p.Role is VillageMattress m && player.Equals(m.LastSleepover));
                visit?.Kill(CauseOfDeath.WerwolfKill, self);     // Regularly handled in OnDeathAsync
            }
            
            Done = true;
            return Task.FromResult<string[]?>([player.Name]);
        });
        await base.OnNightAsync(ctx, self, ct);
    }
}