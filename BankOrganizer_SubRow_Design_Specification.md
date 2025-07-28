# BankOrganizer Sub-Row Layout Design Specification

## Overview

This document provides a comprehensive design specification for implementing enhanced sub-row layouts in the BankOrganizer mod. The enhancement will display individual item stacks horizontally beneath each main bank entry row, providing users with detailed visibility into their bank inventory distribution.

## Current Implementation Analysis

### Existing UI Structure
- **UIBankList**: Manages the scrollable list of bank entries
- **VerticalLayoutGroup**: Arranges bank entries vertically
- **Single Row Display**: Each bank entry shows only item name and total quantity
- **Fixed Height**: All rows use 25px height with fixed layout elements

### Current Data Flow
- **BankEntry**: Groups items by ID, contains list of ItemDataReference objects
- **ItemDataReference**: Represents individual stacks with sprite, quantity, and metadata
- **BankContainerManager**: Manages bank containers and synchronizes data from game

## Enhanced Row Structure Design

### Main Row (Top Section)
```
MainRowContainer (40px height)
├── ItemIconContainer (32x32px)
│   └── ItemIcon (Image) - Primary item sprite
├── ItemInfoContainer (Flexible width)
│   ├── ItemNameText (Text) - Item name
│   └── TotalQuantityText (Text) - "Total: X (Y stacks)"
└── ExpandCollapseButton (24x24px)
    └── ExpandIcon (Image) - Arrow indicator
```

**Visual Specifications:**
- Background: Semi-transparent gray (0.3f, 0.3f, 0.3f, 0.5f)
- Border: 1px white outline on hover
- Padding: 5px horizontal, 2px vertical
- Font: LegacyRuntime.ttf, 14px, white text

### Sub-Row (Bottom Section)
```
SubRowContainer (Dynamic height: 36-108px)
├── SubRowBackground (Image) - Darker background
└── StacksContent (GameObject)
    └── StacksLayoutGroup (GridLayoutGroup)
        ├── StackElement1 (64x32px)
        │   ├── StackBackground (Image)
        │   ├── StackIcon (Image) - 24x24px
        │   └── StackQuantityText (Text) - "x64"
        ├── StackElement2 (64x32px)
        └── ... (Additional stacks)
```

**Stack Element Specifications:**
- Dimensions: 64x32px fixed size
- Spacing: 4px between elements
- Icon: 24x24px sprite from ItemDataReference
- Quantity: "x{number}" format, 10px font, right-aligned
- Background: Dark gray (0.2f, 0.2f, 0.2f, 0.6f)
- Border: Light gray outline (0.4f, 0.4f, 0.4f, 0.8f)

## UI Component Hierarchy

### Modified UIBankList Structure
```
ScrollView (_scrollView)
└── Viewport
    └── Content (_itemListContent) - VerticalLayoutGroup
        ├── SummaryHeader (existing)
        └── Enhanced Bank Entry (NEW)
            ├── MainRowContainer
            │   ├── MainRowBackground (Image)
            │   └── MainRowContent (HorizontalLayoutGroup)
            │       ├── ItemIconContainer
            │       ├── ItemInfoContainer
            │       └── ExpandCollapseButton
            └── SubRowContainer
                ├── SubRowBackground (Image)
                └── StacksContent
                    └── StacksLayoutGroup (GridLayoutGroup)
```

### Layout Component Configuration

**VerticalLayoutGroup (Modified)**:
- `childControlHeight`: false (changed from true)
- `childForceExpandHeight`: false
- `spacing`: 2f
- `padding`: (5,5,5,5)

**MainRowContainer**:
- `LayoutElement`: preferredHeight = 40px, minHeight = 40px
- `HorizontalLayoutGroup`: spacing = 5px, padding = (5,5,2,2)

**SubRowContainer**:
- `LayoutElement`: preferredHeight = dynamic
- `ContentSizeFitter`: verticalFit = PreferredSize
- Initially hidden (`SetActive(false)`)

**StacksLayoutGroup (GridLayoutGroup)**:
- `cellSize`: (64, 32)
- `spacing`: (4, 4)
- `constraint`: FixedColumnCount
- `constraintCount`: Calculated as `(containerWidth - padding) / (cellWidth + spacing)`

## Data Integration Approach

### BankEntry Data Access
```csharp
// Access individual stacks from existing BankEntry
foreach (ItemDataReference stackRef in bankEntry.ItemReferences)
{
    int stackQuantity = stackRef.StackSize;
    UnityEngine.Sprite stackSprite = stackRef.Sprite;
    string itemName = stackRef.ItemName;
    int maxStackSize = stackRef.MaxStackSize;
}
```

### Sprite Handling Strategy
1. **Primary Source**: Use `ItemDataReference.Sprite` property
2. **Null Checking**: Handle IL2CPP object lifecycle with `sprite.WasCollected` checks
3. **Fallback**: Use default sprite or text placeholder if sprite unavailable
4. **Caching**: Cache sprite references to avoid repeated lookups

### Data Refresh Strategy
- **Event-Driven**: Monitor `ItemDataReference.OnChange` events
- **Incremental Updates**: Only refresh affected stack elements
- **State Preservation**: Maintain expansion state during refreshes

## State Management

### Expansion State
- **Default**: Collapsed (sub-row hidden)
- **Toggle**: Click on main row or expand button
- **Persistence**: Maintain state during data refreshes
- **Animation**: Optional smooth expand/collapse transition

### Dynamic Height Calculation
```csharp
int stacksPerRow = (containerWidth - padding) / (stackWidth + spacing);
int numberOfRows = Math.Ceiling(stackCount / stacksPerRow);
int subRowHeight = (numberOfRows * 32) + ((numberOfRows - 1) * 4) + 8; // padding
int totalHeight = 40 + (isExpanded ? subRowHeight : 0);
```

## IL2CPP Considerations

### Sprite Handling Safety
```csharp
private bool TrySetSprite(Image imageComponent, UnityEngine.Sprite sprite)
{
    if (sprite == null || sprite.WasCollected)
    {
        imageComponent.sprite = null;
        return false;
    }
    
    try
    {
        imageComponent.sprite = sprite;
        return true;
    }
    catch (System.Exception ex)
    {
        MelonLogger.Warning($"Failed to set sprite: {ex.Message}");
        imageComponent.sprite = null;
        return false;
    }
}
```

### Memory Management
- **Reference Cleanup**: Explicitly null references when destroying UI elements
- **Event Unsubscription**: Clear event subscriptions to prevent memory leaks
- **GameObject Destruction**: Use `GameObject.Destroy()` for proper cleanup

### Performance Optimization
- **Batch Operations**: Create all UI elements in single frame
- **Layout Batching**: Disable layout groups during bulk updates
- **Lazy Loading**: Only create sub-row elements when first expanded

## Implementation Methods

### Core Method Modifications

**Enhanced CreateItemListEntry**:
```csharp
private void CreateEnhancedItemListEntry(BankEntry entry)
{
    GameObject entryContainer = CreateMainRowContainer(entry);
    GameObject subRowContainer = CreateSubRowContainer(entry, entryContainer);
    PopulateStackElements(entry.ItemReferences, subRowContainer);
    SetupExpandCollapseLogic(entryContainer, subRowContainer);
}
```

**Stack Population**:
```csharp
private void PopulateStackElements(IReadOnlyList<ItemDataReference> stacks, GameObject subRowContainer)
{
    foreach (ItemDataReference stack in stacks)
    {
        if (ValidateStackElement(stack))
        {
            CreateStackElement(stack, subRowContainer);
        }
    }
}
```

### Error Handling Strategy
- **Graceful Degradation**: Fall back to current display if sub-row creation fails
- **Validation**: Check for null references and invalid data
- **Logging**: Log errors without crashing UI

## Visual Design Specifications

### Color Scheme
- **Main Row Background**: (0.3f, 0.3f, 0.3f, 0.5f)
- **Sub-Row Background**: (0.15f, 0.15f, 0.15f, 0.8f)
- **Stack Background**: (0.2f, 0.2f, 0.2f, 0.6f)
- **Stack Border**: (0.4f, 0.4f, 0.4f, 0.8f)
- **Text Color**: White (#FFFFFF)
- **Hover Highlight**: (0.4f, 0.4f, 0.4f, 0.7f)

### Typography
- **Main Text**: LegacyRuntime.ttf, 14px
- **Stack Quantity**: LegacyRuntime.ttf, 10px
- **Alignment**: Left for names, right for quantities

### Spacing and Sizing
- **Main Row Height**: 40px
- **Stack Element Size**: 64x32px
- **Icon Size**: 24x24px (main), 24x24px (stacks)
- **Spacing**: 4px between stack elements
- **Padding**: 5px horizontal, 2px vertical

## Performance Considerations

### Optimization Strategies
1. **Object Pooling**: Reuse stack element GameObjects when possible
2. **Lazy Creation**: Only create sub-row UI when first expanded
3. **Batch Updates**: Group multiple changes into single UI refresh
4. **Memory Monitoring**: Track and limit maximum number of expanded rows

### Scalability Limits
- **Maximum Stacks per Row**: Based on container width
- **Maximum Sub-Row Height**: 108px (3 rows maximum)
- **Performance Threshold**: Monitor frame rate with large inventories

## Testing Considerations

### Test Scenarios
1. **Single Stack Items**: Items with only one stack
2. **Multiple Stack Items**: Items with 2-10 stacks
3. **Many Stack Items**: Items with 10+ stacks
4. **Mixed Inventory**: Combination of single and multiple stack items
5. **Empty Stacks**: Handling of zero-quantity stacks
6. **Sprite Loading**: Missing or corrupted sprite handling

### Edge Cases
- **Very Long Item Names**: Text overflow handling
- **Large Stack Quantities**: Number formatting (1000+ items)
- **Rapid Inventory Changes**: UI update performance
- **Memory Pressure**: Behavior under low memory conditions

## Future Enhancement Opportunities

### Potential Improvements
1. **Stack Sorting**: Sort stacks by quantity (largest first)
2. **Stack Merging**: Visual indication of mergeable stacks
3. **Tooltips**: Hover information for individual stacks
4. **Drag and Drop**: Move stacks between containers
5. **Search and Filter**: Find specific items or stack sizes
6. **Export Functionality**: Save inventory data to file

### Accessibility Features
- **Keyboard Navigation**: Tab through expandable rows
- **Screen Reader Support**: Proper ARIA labels
- **High Contrast Mode**: Alternative color scheme
- **Font Scaling**: Respect system font size settings

## Conclusion

This design specification provides a comprehensive foundation for implementing the enhanced sub-row layout in the BankOrganizer mod. The design maintains compatibility with the existing codebase while adding significant functionality for detailed inventory management. The IL2CPP considerations ensure robust operation in the Unity environment, and the performance optimizations support scalability for large inventories.

The implementation should follow the specified component hierarchy and data integration patterns to ensure maintainable and efficient code. Error handling and graceful degradation strategies will provide a stable user experience even when encountering edge cases or system limitations.