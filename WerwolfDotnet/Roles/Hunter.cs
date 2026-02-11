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
            ExcludeParticipants = true,
            Participants = [self]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer() is { } selectedOne)
                selectedOne.Kill(CauseOfDeath.ShootByHunter, self);
            return Task.FromResult<string[]?>(null);
        }, ct);
        
        await base.OnDeathAsync(ctx, self, ct);
    }
}