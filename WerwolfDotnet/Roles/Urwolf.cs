using WerwolfDotnet.Logging;

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
            if (action.PlayerVotes[self].FirstOrDefault() is not { } selectedOne)
                return Task.FromResult<string[]?>(null);
            
            if (selectedOne.Role is VillageMattress { LastSleepover: not null })
            {
                ctx.Logger.Log(Event.VictimMissed, self, selectedOne);
            }
            else
            {
                selectedOne.Revive(self);     // When not killed (e.g. saved by healer, ...) ignored
                selectedOne.Role = new Werwolf();
                ctx.Logger.Log(Event.TurnedToWerwolf, source: self, targets: [selectedOne]);

                Player? visit = ctx.Players.SingleOrDefault(p => p.Role is VillageMattress m && selectedOne.Equals(m.LastSleepover));
                visit?.Kill(CauseOfDeath.WerwolfKill, self);     // Regularly handled in OnDeathAsync, would be a WerwolfKill.
            }
            
            Done = true;
            return Task.FromResult<string[]?>([selectedOne.Name]);
        });
        await base.OnNightAsync(ctx, self, ct);
    }
}