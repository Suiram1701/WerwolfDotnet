using Microsoft.Extensions.Logging;

namespace WerwolfDotnet.Roles;

public sealed class Seer : RoleBase
{
    public override Role Type => Role.Seer;

    internal override async Task OnNightAsync(GameContext ctx, Player self, CancellationToken ct)
    {
        await ctx.RequestPlayerActionAsync(new PhaseAction
        {
            Type = ActionType.SeerSelection,
            ExcludeParticipants = true,
            Participants = [self]
        }, (action, _) =>
        {
            if (action.GetMostVotedPlayer() is { } selectedOne)
            {
                ctx.Logger.LogTrace(
                    "Seer {seerName} ({seerId}) saw role of {playerName} ({playerId}): {roleName}",
                    self.Name, self.Id, selectedOne.Name, selectedOne.Id, selectedOne.Role!.Type);
                return Task.FromResult<string[]?>([selectedOne.Name, selectedOne.Role!.Type.ToString()]);
            }
            return Task.FromResult<string[]?>(null);
        }, ct);
        
        await base.OnNightAsync(ctx, self, ct);
    }
}