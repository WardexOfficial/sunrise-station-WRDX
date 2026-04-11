using Content.Shared.Whitelist;

using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.MindBearer;

/// <summary>
/// Данный компонент используется исключительно в целях пометки.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindBearerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int UsesLeft = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan UseTime = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public EntityWhitelist? AllowTargets = new();
}
