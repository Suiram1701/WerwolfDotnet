namespace WerwolfDotnet.Roles;

public sealed class Hunter : RoleBase
{
    public override Role Type => Role.Hunter;
    
    internal override async Task OnDeathAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        await ctx.RequestPlayerActionAsync(new PhaseAction
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
        }, ct);
        
        await base.OnDeathAsync(ctx, self, ct);
    }
}