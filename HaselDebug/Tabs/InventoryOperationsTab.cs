using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

// Yes, this does not list ALL inventory operations. It's a complicated system with many packets.

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InventoryOperationsTab : DebugTab, IDisposable
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISigScanner _sigScanner;
    private readonly DebugRenderer _debugRenderer;
    private readonly ItemService _itemService;

    private readonly List<IInventoryAction> _actions = [];

    private Hook<RemoveOperationByIdDelegate>? _removeOperationByIdHook;
    private Hook<PacketDispatcher.Delegates.HandleUpdateInventorySlotPacket>? _handleUpdateInventorySlotPacketHook;
    private int _typeBase;
    private bool _enabled;

    private interface IInventoryAction : IDisposable
    {
        Type Type { get; }
        void* Pointer { get; }
        DateTime Time { get; }
    }

    private class InventoryAction<T> : IInventoryAction where T : unmanaged
    {
        public Type Type { get; } = typeof(T);
        public void* Pointer { get; private set; }
        public DateTime Time { get; init; }

        public InventoryAction(T data)
        {
            Pointer = IMemorySpace.GetDefaultSpace()->Malloc<T>();
            *(T*)Pointer = data;
            Time = DateTime.Now;
        }

        public void Dispose()
        {
            if (Pointer != null)
            {
                IMemorySpace.Free((T*)Pointer);
                Pointer = null;
            }
        }
    }

    public delegate void RemoveOperationByIdDelegate(InventoryManager* thisPtr, uint contextId);

    public delegate bool SendPacketDelegate(NetworkModuleProxy* thisPtr, void* packet, uint a3, uint a4);

    [AutoPostConstruct]
    private void Initialize()
    {
        _typeBase = *(int*)(_sigScanner.ScanText("81 F9 ?? ?? ?? ?? 0F 85 ?? ?? ?? ?? 8B 4B") + 2) - 7;
    }

    public void Dispose()
    {
        _removeOperationByIdHook?.Dispose();
        _handleUpdateInventorySlotPacketHook?.Dispose();

        foreach (var action in _actions)
            action.Dispose();

        _actions.Clear();
    }

    private void RemoveOperationByIdDetour(InventoryManager* thisPtr, uint contextId)
    {
        foreach (var operation in thisPtr->PendingOperations)
        {
            if (!operation.IsEmpty && operation.ContextId == contextId)
            {
                _actions.Insert(0, new InventoryAction<InventoryManager.InventoryOperation>(operation));
            }
        }

        _removeOperationByIdHook!.Original(thisPtr, contextId);
    }

    private void HandleUpdateInventorySlotPacketDetour(uint targetId, UpdateInventorySlotPacket* packet)
    {
        _actions.Insert(0, new InventoryAction<UpdateInventorySlotPacket>(*packet));
        _handleUpdateInventorySlotPacketHook!.Original(targetId, packet);
    }

    public override void Draw()
    {
        _removeOperationByIdHook ??= _gameInteropProvider.HookFromSignature<RemoveOperationByIdDelegate>(
            "E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 4C 8B 45",
            RemoveOperationByIdDetour);

        _handleUpdateInventorySlotPacketHook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleUpdateInventorySlotPacket>(
            PacketDispatcher.Addresses.HandleUpdateInventorySlotPacket.Value,
            HandleUpdateInventorySlotPacketDetour);

        if (_removeOperationByIdHook == null || _handleUpdateInventorySlotPacketHook == null)
        {
            ImGui.Text("Hook not created"u8);
            return;
        }

        if (ImGui.Checkbox("Enabled"u8, ref _enabled))
        {
            if (_enabled)
            {
                if (!_removeOperationByIdHook.IsEnabled)
                    _removeOperationByIdHook.Enable();

                if (!_handleUpdateInventorySlotPacketHook.IsEnabled)
                    _handleUpdateInventorySlotPacketHook.Enable();
            }
            else
            {
                if (_removeOperationByIdHook.IsEnabled)
                    _removeOperationByIdHook.Disable();

                if (_handleUpdateInventorySlotPacketHook.IsEnabled)
                    _handleUpdateInventorySlotPacketHook.Disable();
            }
        }

        ImGui.SameLine();

        using (ImRaii.Disabled(_actions.Count == 0))
        {
            if (ImGui.Button("Clear"u8))
            {
                foreach (var action in _actions)
                    action.Dispose();

                _actions.Clear();
            }
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text($"TypeBase: {_typeBase}");

        using var table = ImRaii.Table("InventoryOperationTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings, new Vector2(-1));
        if (!table)
            return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var action in _actions)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(action.Time.ToLongTimeString());

            ImGui.TableNextColumn();
            var nodeOptions = new NodeOptions();

            if (action.Type == typeof(InventoryManager.InventoryOperation))
            {
                var data = (InventoryManager.InventoryOperation*)action.Pointer;
                nodeOptions = nodeOptions with { Title = $"[InventoryOperation] {(InventoryOperationType)(data->Type - _typeBase)}" };
            }
            else if (action.Type == typeof(UpdateInventorySlotPacket))
            {
                var data = (UpdateInventorySlotPacket*)action.Pointer;
                nodeOptions = nodeOptions with { Title = $"[UpdateInventorySlotPacket] {(InventoryType)data->InventoryType}#{data->InventorySlot}, Item: {_itemService.GetItemName(data->ItemId)} ({data->ItemId}), Quantity: {data->Quantity}, Condition: {data->Condition}" };
            }

            _debugRenderer.DrawPointerType(action.Pointer, action.Type, nodeOptions);
        }
    }
}

// FYI: This is based on Sapphires enum: https://github.com/SapphireServer/Sapphire/blob/a203ffef937df84c50c7fec130f2acd39efdd31e/src/common/Common.h#L84
// I only confirmed a couple of them.
public enum InventoryOperationType
{
    None = 0,
    CreateStorage = 1,
    DeleteStorage = 2,
    CompactStorage = 3,
    ResyncStorage = 4,
    CreateItem = 5,
    UpdateItem = 6,
    DeleteItem = 7, // confirmed
    MoveItem = 8, // confirmed
    SwapItem = 9, // confirmed
    SplitItem = 10, // confirmed
    SplitTomerge = 11,
    MergeItem = 12, // confirmed
    RepairItem = 13,
    NpcRepairItem = 14,
    RepairMannequin = 15,
    NpcRepairMannequin = 16,
    RepairInventory = 17,
    NpcRepairInventory = 18,
    EquipMannequin = 19,
    BringOutLegacyItem = 20,


    GiveToRetainer = 23, // confirmed
    TakeFromRetainer = 24, // confirmed
    SetRetainerGil = 25, // confirmed
    TradeCommand = 26, // confirmed for putting an item on the market
    MoveTrade = 27,
    SetGilTrade = 28,
    SetTradeStack = 29,
    UpdatePartnerBox = 30,
    CreateMateria = 31,
    AttachMateria = 32,
    RemoveMateria = 33,
    AskattachMateria = 34,
    DebugAddItem = 35,
    DebugSetItem = 36,
    DebugSetStack = 37,
    DebugSetRefine = 38,
    DebugSetDurability = 39,
    AliasItem = 40,
    UnaliasItem = 41,
    Movealias = 42,
    SwapAlias = 43,
    TakeFromFcChest = 44,
    FateReward = 45,
    QuestReward = 46,
    LeveReward = 47,
    SpecialShopTrade = 48,
    CraftLeveTrade = 49,
    QuestTrade = 50,
    Gathering = 51,
    Craft = 52,
    Fishing = 53,
    GcSupply = 54,
    CabinetTake = 55,
    CabinetGive = 56,
    ShopBuyback = 57,
    Telepo = 58,
    VentureStart = 59,
    VentureEnd = 60,
    GardeningHarvest = 61,
    SalvageResult = 62,
    TreasurePublic = 63,
    TreasureGuildLead = 64,
    TreasureRaid = 65,
    TreasureMonster = 66,
    TreasureHunt = 67,
    TreasureDebugDropTable = 68,
    TreasureDebugDropPack = 69,
    TreasureDebugDropTreasure = 70,
    TreasureDebugDropPackTreasure = 71,
    RandomItem = 72,
    SpecialShopBuyItem = 73,
    EpicWeapon020Trade = 74,
    EpicWeapon030Treasuremap = 75,
    MateriaSlot = 76,
    AchievementReward = 77,
}
