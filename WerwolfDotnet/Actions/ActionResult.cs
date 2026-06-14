namespace WerwolfDotnet.Actions;

/// <summary>
/// The result of an executed player action.
/// </summary>
/// <param name="Succeeded">Indicates whether the action succeeded.</param>
/// <param name="Results">Parameters that can be shown to the user. <see cref="ActionType"/> which parameters are expected for each Type.</param>
public record ActionResult(bool Succeeded, object[] Results)
{
    private static readonly ActionResult _failedInstance = new(false, []); 
    
    /// <summary>
    /// The action succeeded and a message is shown to the user.
    /// </summary>
    /// <param name="results">Parameters used for formating. <seealso cref="ActionType"/></param>
    /// <returns>A new instance containing the data</returns>
    public static ActionResult Success(params object[] results) => new(true, results);
    
    // /// <summary>
    // /// The action succeeded but .
    // /// </summary>
    // /// <returns>A new instance</returns>
    // public static ActionResult SuccessNoMessage() => new(true, null);

    /// <summary>
    /// The action failed. The user didn't choose correctly, timed out or was skipped.
    /// </summary>
    /// <remarks>
    /// Should only be used for not-intential cases.
    /// </remarks>
    /// <returns>A shared instance.</returns>
    public static ActionResult Failed() => _failedInstance;
}