using BankOrganizer.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BankOrganizer.Models
{
    /// <summary>
    /// Result object containing all bank entries and metadata
    /// </summary>
    public class BankEntryResult
    {
        public List<BankEntry> Entries { get; set; } = new List<BankEntry>();
        public int TotalSlots { get; set; }

        public BankEntryResult()
        {
        }

        public BankEntryResult(List<BankEntry> entries, int totalSlots)
        {
            Entries = entries;
            TotalSlots = totalSlots;
        }
    }

    /// <summary>
    /// Represents a grouped bank entry for items with the same ID
    /// </summary>
    public class BankEntry
    {
        private readonly List<ItemDataReference> _itemReferences = new();

        public int ItemId { get; private set; }
        public string ItemName { get; private set; } = "Unknown";
        public int MaxStackSize { get; private set; }

        /// <summary>
        /// Total quantity of this item across all bank slots
        /// </summary>
        public int TotalQuantity => _itemReferences.Sum(item => item.StackSize);

        /// <summary>
        /// Number of bank slots occupied by this item type
        /// </summary>
        public int SlotCount => _itemReferences.Count;

        /// <summary>
        /// Read-only collection of all ItemDataReferences that make up this entry
        /// </summary>
        public IReadOnlyList<ItemDataReference> ItemReferences => _itemReferences;

        public BankEntry(int itemId)
        {
            ItemId = itemId;
        }

        /// <summary>
        /// Build all bank entries from the current bank state
        /// </summary>
        public static BankEntryResult BuildBankEntries()
        {
            var entries = new List<BankEntry>();
            var entriesById = new Dictionary<int, BankEntry>();
            int totalSlots = 0;

            try
            {
                // Get all containers from the bank manager
                var allContainers = BankContainerManager.Instance.GetAllContainers();

                // Iterate through all containers and their slots
                foreach (var containerKvp in allContainers)
                {
                    var container = containerKvp.Value;
                    var slots = container.GetAllSlots();

                    foreach (var slotKvp in slots)
                    {
                        var itemRef = slotKvp.Value;

                        // Only process ItemDataReferences that have items
                        if (itemRef.IsItem && itemRef.Id > 0)
                        {
                            totalSlots++;

                            // Get or create a BankEntry for this item ID
                            if (!entriesById.TryGetValue(itemRef.Id, out BankEntry? entry))
                            {
                                entry = new BankEntry(itemRef.Id);
                                entriesById[itemRef.Id] = entry;
                                entries.Add(entry);
                            }

                            // Add the ItemDataReference to the entry
                            entry.AddItemReference(itemRef);
                        }
                    }
                }

                // Sort entries alphabetically by item name
                entries.Sort((a, b) => string.Compare(a.ItemName, b.ItemName, StringComparison.OrdinalIgnoreCase));

                return new BankEntryResult(entries, totalSlots);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Error building bank entries: {ex.Message}");
                return new BankEntryResult(entries, totalSlots);
            }
        }

        /// <summary>
        /// Add an ItemDataReference to this entry
        /// </summary>
        public void AddItemReference(ItemDataReference itemRef)
        {
            if (itemRef.Id != ItemId)
            {
                throw new ArgumentException($"ItemDataReference ID {itemRef.Id} does not match BankEntry ID {ItemId}");
            }

            if (!_itemReferences.Contains(itemRef))
            {
                _itemReferences.Add(itemRef);
                UpdateMetadata(itemRef);
            }
        }

        /// <summary>
        /// Remove an ItemDataReference from this entry
        /// </summary>
        public bool RemoveItemReference(ItemDataReference itemRef)
        {
            return _itemReferences.Remove(itemRef);
        }

        /// <summary>
        /// Clear all ItemDataReferences from this entry
        /// </summary>
        public void Clear()
        {
            _itemReferences.Clear();
        }

        /// <summary>
        /// Check if this entry contains any items
        /// </summary>
        public bool HasItems => _itemReferences.Count > 0 && TotalQuantity > 0;

        /// <summary>
        /// Update metadata (name, max stack size) from an ItemDataReference
        /// </summary>
        private void UpdateMetadata(ItemDataReference itemRef)
        {
            if (!string.IsNullOrEmpty(itemRef.ItemName) && itemRef.ItemName != "Empty")
            {
                ItemName = itemRef.ItemName;
            }

            if (itemRef.MaxStackSize > 0)
            {
                MaxStackSize = itemRef.MaxStackSize;
            }
        }

        /// <summary>
        /// Get a summary string of this entry
        /// </summary>
        public override string ToString()
        {
            return $"BankEntry: {ItemName} (ID: {ItemId}) - Total: {TotalQuantity}, Slots: {SlotCount}";
        }

        /// <summary>
        /// Get detailed information about stack distribution
        /// </summary>
        public string GetStackDistribution()
        {
            if (!HasItems) return "Empty";

            var stackSizes = new List<int>();
            foreach (var item in _itemReferences)
            {
                if (item.StackSize > 0)
                {
                    stackSizes.Add(item.StackSize);
                }
            }

            // Sort in descending order
            stackSizes.Sort((a, b) => b.CompareTo(a));

            if (stackSizes.Count == 1)
            {
                return $"{stackSizes[0]}";
            }

            return $"{string.Join(", ", stackSizes)} (Total: {TotalQuantity})";
        }
    }
}