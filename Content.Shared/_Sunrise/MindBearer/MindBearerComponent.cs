using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.MindBrearer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindBearerComponent : Component
{
    [DataField("usesLeft"), AutoNetworkedField]
    public int UsesLeft = 1;

    [DataField, AutoNetworkedField]
    public int DownloadTime = 15;

    [DataField, AutoNetworkedField]
    public int UploadTime = 10;

    [DataField]
    public ItemSlot Slot = new();
}
