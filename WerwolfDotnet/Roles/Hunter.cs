using WerwolfDotnet.Actions;

namespace WerwolfDotnet.Roles;

[Role(Role.Hunter)]
public sealed class Hunter : RoleBase
{
    internal override async Task OnDeathAsync(GameContext ctx, Player self, CauseOfDeath cause, CancellationToken ct)
    {
        await ctx.RequestPlayerActionAsync(new PlayerAction(ct)
        {
            Type = ActionType.HunterSelection,
            Minimum = ctx.GameOptions!.HunterMustKill ? 1 : 0,
            Maximum = 1,
            Participants = [self],
            VotablePlayers = [..ctx.Players.Where(p => p.Status == PlayerState.Alive)]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer() is not { } selectedOne)
                return ctx.GameOptions!.HunterMustKill ? ActionResult.Failed() : ActionResult.Success();
            
            selectedOne.Kill(CauseOfDeath.ShootByHunter, self);
            return ActionResult.Success(selectedOne);
        });
        
        await base.OnDeathAsync(ctx, self, cause, ct);
    }
}