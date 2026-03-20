namespace WerwolfDotnet.Roles;

[Role(Role.Hunter)]
public sealed class Hunter : RoleBase
{
    internal override async Task OnDeathAsync(GameContext ctx, Player self, CauseOfDeath cause, CancellationToken ct)
    {
        await ctx.RequestPlayerActionAsync(new PhaseAction(ct)
        {
            Type = ActionType.HunterSelection,
            Minimum = ctx.GameOptions!.HunterMustKill ? 1 : 0,
            Maximum = 1,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.Alive)]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer() is { } selectedOne)
                selectedOne.Kill(CauseOfDeath.ShootByHunter, self);
            return Task.FromResult<string[]?>(null);
        });
        
        await base.OnDeathAsync(ctx, self, cause, ct);
    }
}