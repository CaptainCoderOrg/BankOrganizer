using HarmonyLib;
using Il2Cpp;
using Il2CppPantheonPersist;
using MelonLoader;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using Console = System.Console;
using UnityEngine.EventSystems;

namespace BankOrganizer.Hooks;

[HarmonyPatch(typeof(UIItemSlot),
    nameof(UIItemSlot.SetItem),
    typeof(IEntity), typeof(Item), typeof(int)
    )]
public class UIItemSlotSetItem
{
    // public unsafe override void OnInitialize(IEntity entity, Item item)
    public static void Postfix(UIItemSlot __instance, IEntity entity, Item item, int newSlotIndex)
    {
        if (__instance == null) { return; }
        if (__instance.SlotType != SlotType.Bank) { return; }
        if (__instance.ParentGuid == default) { return; } // Is a root bank slo
        BankContainerManager.Instance.SyncItemSlot(__instance);
    }
}

[HarmonyPatch(typeof(UIItemSlot),
    nameof(UIItemSlot.Clear),
    typeof(bool)
    )]
public class UIItemSlotClear
{
    public static void Postfix(UIItemSlot __instance, bool clearSlotIndex)
    {
        if (__instance == null) { return; }
        if (__instance.SlotType != SlotType.Bank) { return; }
        if (__instance.ParentGuid == default) { return; } // Is a root bank slo
        BankContainerManager.Instance.SyncItemSlot(__instance);
    }
}

[HarmonyPatch(typeof(UIBagManager),
    nameof(UIBagManager.CreateBagWindowIfItDoesNotExist),
    typeof(Item)
    )]
public class UIBagManagerCreateBagWindowIfItDoesNotExist
{
    // public unsafe override void OnInitialize(IEntity entity, Item item)
    public static void Postfix(UIBagManager __instance, Item item, ref UIBag __result)
    {
        if (item == null || item.WasCollected || item.SlotType != SlotType.Bank) { return; }

        if (__instance.bagWindows.TryGetValue(item.ItemInstanceGuid, out UIBag bagWindow))
        {
            BankContainerManager.Instance.SyncBankContainerData(item.ItemInstanceGuid.ToString(), item.SlotIndex, bagWindow);
        }
        else
        {
            MelonLogger.Error($"-- Unable to sync bag window data");
        }
    }
}



public class ItemDataReference
{
    private int _id;
    public int BankIndex { get; set; }
    public int ItemSlotIndex { get; set; }
    public UIItemSlot ItemSlot { get; set;  }
    public int Id
    {
        get => _id;
        set
        {
            if (_id == value) return;
            _id = value;
            _isDirty = true;
        }
    }
    private string? _itemName;
    public string? ItemName
    {
        get => _itemName;
        set
        {
            if (_itemName == value) return;
            _itemName = value;
            _isDirty = true;
        }
    }
    private int _stackSize;
    public int StackSize
    {
        get => _stackSize;
        set
        {
            if (_stackSize == value) return;
            _stackSize = value;
            _isDirty = true;
        }
    }
    private int _maxStackSize;
    public int MaxStackSize
    {
        get => _maxStackSize;
        set
        {
            if (_maxStackSize == value) return;
            _maxStackSize = value;
            _isDirty = true;
        }
    }
    private bool _isItem;
    public bool IsItem
    {
        get => _isItem;
        set
        {
            if (_isItem == value) return;
            _isItem = value;
            _isDirty = true;
        }
    }

    private UnityEngine.Sprite _sprite;
    public UnityEngine.Sprite? Sprite
    {
        get => _sprite == null || _sprite.WasCollected ? null : _sprite;
        set => _sprite = value;
    }
    public string? SlotKey { get; set; }
    private bool _isDirty = false;

    public event Action<ItemDataReference>? OnChange;

    public void MoveItemToInventory()
    {
        if (ItemSlot == null || ItemSlot.WasCollected)
        {
            MelonLogger.Error($"Item Slot could not be found");
            return;
        }
        
        try
        {
            // Create a PointerEventData for right-click simulation
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                MelonLogger.Warning("Cannot move item to inventory: No EventSystem found");
                return;
            }

            var pointerEventData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
            
            // Configure for right-click
            pointerEventData.button = UnityEngine.EventSystems.PointerEventData.InputButton.Right;
            pointerEventData.clickCount = 1;
            pointerEventData.clickTime = UnityEngine.Time.unscaledTime;
            pointerEventData.eligibleForClick = true;
            
            // Set position (can be approximate since we're simulating)
            pointerEventData.position = new UnityEngine.Vector2(0, 0);
            pointerEventData.pressPosition = new UnityEngine.Vector2(0, 0);
            
            // Set the target GameObject
            pointerEventData.pointerClick = ItemSlot.gameObject;
            pointerEventData.pointerPress = ItemSlot.gameObject;
            pointerEventData.rawPointerPress = ItemSlot.gameObject;

            // Call the PointerClicked method to simulate right-click
            ItemSlot.PointerClicked(pointerEventData);
            
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Failed to move item to inventory for {this}: {ex.Message}");
            MelonLogger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    public void OnClickBankStack()
    {
        MoveItemToInventory();
    }

    public override string? ToString()
    {
        return $"ItemDataReference: ({Id}) {ItemName} ({StackSize} / {MaxStackSize} @ {BankIndex}-{ItemSlotIndex})";
    }

    public void Clear()
    {
        IsItem = false;
        ItemName = "Empty";
        StackSize = 0;
        MaxStackSize = 0;
        Id = -1;
        Sprite = null;
        ItemSlot = null;
        if (_isDirty)
        {
            OnChange?.Invoke(this);
            _isDirty = false;
        }
    }

    public void Sync(UIItemSlot slot)
    {
        if (slot.Item == null)
        {
            Clear();
            return;
        }
        Sprite = slot.sprite;
        IsItem = true;
        ItemName = slot.Item.Template.ItemName;
        StackSize = slot.Item.StackSize;
        Id = slot.Item.Template.ItemId;
        MaxStackSize = slot.Item.Template.MaxStackSize;
        ItemSlot = slot;
        if (_isDirty)
        {
            OnChange?.Invoke(this);
            _isDirty = false;
        }
    }
}

public class BankContainerManager
{
    private static BankContainerManager? _instance;
    private static readonly object _lock = new object();

    private readonly Dictionary<string, BankContainer> _containers = new();

    // Event for when any bank data changes
    public event Action? OnBankDataChanged;

    private BankContainerManager() { }

    public static BankContainerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new BankContainerManager();
                }
            }
            return _instance;
        }
    }

    public BankContainer GetOrCreateContainer(string guid)
    {
        if (!_containers.TryGetValue(guid, out BankContainer? container))
        {
            container = new BankContainer(guid);
            // Subscribe to container changes
            container.OnContainerChanged += OnContainerChanged;
            _containers[guid] = container;
        }
        return container;
    }

    private void OnContainerChanged(BankContainer container)
    {
        // Trigger the bank data changed event
        OnBankDataChanged?.Invoke();
    }

    public void SyncItemSlot(UIItemSlot slot)
    {
        string guid = slot.ParentGuid.ToString();
        BankContainer container = GetOrCreateContainer(guid);
        ItemDataReference idr = container.GetItemReference(slot.SlotIndex);
        idr.Sync(slot);
    }

    public void SyncBankContainerData(string guid, int bankIndex, UIBag bagWindow)
    {
        BankContainer container = GetOrCreateContainer(guid);
        container.BankIndex = bankIndex;
        container.SyncBankContainerData(bagWindow.slots);
    }

    public void ReportItemChanged(ItemDataReference idr)
    {
        // Trigger the bank data changed event
        OnBankDataChanged?.Invoke();
    }

    public void ClearAll()
    {
        _containers.Clear();
    }

    public IReadOnlyDictionary<string, BankContainer> GetAllContainers()
    {
        return _containers;
    }
}

public class BankContainer
{
    private readonly Dictionary<int, ItemDataReference> _slots = new();
    public string GUID { get; private set; }
    public int BankIndex { get; set; }

    // Event for when this container's data changes
    public event Action<BankContainer>? OnContainerChanged;

    internal BankContainer(string guid) => GUID = guid;

    public ItemDataReference GetItemReference(int slotIndex)
    {
        if (!_slots.TryGetValue(slotIndex, out ItemDataReference? idr))
        {
            idr = new ItemDataReference();
            idr.BankIndex = BankIndex;
            idr.ItemSlotIndex = slotIndex;
            _slots[slotIndex] = idr;
            // Subscribe to item changes
            idr.OnChange += OnItemChanged;
        }
        return idr;
    }

    private void OnItemChanged(ItemDataReference idr)
    {
        // Trigger container changed event
        OnContainerChanged?.Invoke(this);
    }

    internal void SyncBankContainerData(Il2CppSystem.Collections.Generic.Dictionary<int, UIItemSlot> slots)
    {
        foreach ((int ix, UIItemSlot slot) in slots)
        {
            ItemDataReference idr = GetItemReference(ix);
            idr.SlotKey = $"{slot.ParentGuid.ToString()}-{ix}";
            idr.Sync(slot);
        }
    }

    public IReadOnlyDictionary<int, ItemDataReference> GetAllSlots()
    {
        return _slots;
    }
}