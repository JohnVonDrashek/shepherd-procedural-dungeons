# Feature: Room Template Weighting System

**ID**: FEATURE-003
**Status**: complete
**Created**: 2025-12-12T21:30:00Z
**Priority**: high
**Complexity**: medium

## Description

Implement a probability-based room template weighting system that allows developers to control the relative likelihood of different templates being selected for the same room type. Currently, template selection is purely random (uniform distribution), which limits game design flexibility. This feature enables rich gameplay mechanics like rare room variants, common vs. special rooms, difficulty-based template selection, and thematic variety.

**Why this matters**: Template weighting is a fundamental game design tool that enables:
- **Rare room mechanics**: Create special "legendary" rooms that appear infrequently (e.g., 1% chance) to create memorable moments
- **Difficulty progression**: Use larger, more complex templates for later-game rooms based on difficulty scaling
- **Thematic variety**: Balance common and uncommon room shapes to prevent repetitive layouts
- **Game balance**: Control spawn rates of powerful or challenging room variants
- **Player experience**: Create excitement when rare templates appear, similar to loot drop systems in roguelikes

**Problems solved**:
- No way to make certain templates appear more or less frequently than others
- Cannot create "rare" room variants that add excitement to exploration
- Limited control over template distribution across difficulty levels
- Uniform randomness can lead to repetitive or unbalanced room selections
- Missing game design tool for controlling dungeon variety and pacing

## Requirements

- [ ] Add `Weight` property to `RoomTemplate<TRoomType>` (default: 1.0 for backward compatibility)
- [ ] Implement weighted random selection algorithm in template selection logic
- [ ] Support both relative weights (e.g., 10 vs 1 = 10x more likely) and normalized weights
- [ ] Update `RoomTemplateBuilder` to support setting weights via fluent API
- [ ] Ensure deterministic behavior: same seed + same weights = same template selections
- [ ] Support zero-weight templates (completely disabled) with clear error messages
- [ ] Update template selection in `FloorGenerator` and `IncrementalSolver` to use weighted selection
- [ ] Support zone-specific template weighting (templates can have different weights in different zones)
- [ ] Add validation to prevent negative weights
- [ ] Document weighting system in wiki with examples

## Technical Details

**Implementation Approach**:

1. **RoomTemplate Weight Property**:
   - Add `public double Weight { get; init; } = 1.0;` to `RoomTemplate<TRoomType>`
   - Default value of 1.0 ensures backward compatibility (all existing templates behave as before)
   - Weight must be >= 0.0 (zero disables template, negative throws validation error)

2. **Weighted Random Selection Algorithm**:
   - Implement weighted random selection using cumulative distribution function (CDF)
   - Algorithm: Calculate sum of all weights, generate random value in [0, sum), find template where cumulative weight exceeds random value
   - Ensure deterministic: Use provided Random instance, maintain selection order consistency
   - Handle edge cases: All weights zero (throw exception), single template (return immediately), equal weights (uniform distribution)

3. **RoomTemplateBuilder Enhancement**:
   - Add `WithWeight(double weight)` method to fluent API
   - Validate weight >= 0.0 in `Build()` method
   - Example: `RoomTemplateBuilder<RoomType>.Rectangle(4, 4).WithId("rare-combat").WithWeight(0.1).Build()`

4. **Template Selection Updates**:
   - Update `IncrementalSolver.SelectTemplate()` to use weighted selection instead of uniform random
   - Update zone-aware template selection to respect weights within zone-specific templates
   - Ensure template selection remains deterministic (same seed = same selections)

5. **Zone-Specific Weighting**:
   - Zone-specific templates inherit weights from their base template definition
   - Allow zone configs to override template weights for zone-specific variety
   - Example: A template might be common (weight 10) globally but rare (weight 1) in a specific zone

6. **Validation and Error Handling**:
   - Validate that at least one template with weight > 0 exists for each required room type
   - Throw `InvalidConfigurationException` if all templates for a room type have zero weight
   - Provide clear error messages indicating which room type has no valid templates

**Architecture Considerations**:

- This feature touches template selection logic in `IncrementalSolver` and potentially `FloorGenerator`
- Must maintain backward compatibility: existing code without weights continues to work
- Weight calculation should be efficient (O(n) where n = number of templates, acceptable for typical template counts)
- Determinism is critical: weighted selection must produce identical results for same seed
- Consider future extensibility: conditional weights (e.g., weight based on difficulty, distance from start)

**API Design**:

```csharp
// RoomTemplate enhancement
public sealed class RoomTemplate<TRoomType> where TRoomType : Enum
{
    // ... existing properties ...
    
    /// <summary>
    /// Relative weight for template selection. Higher weights are more likely to be selected.
    /// Default is 1.0 (uniform distribution). Zero weight disables the template.
    /// </summary>
    public double Weight { get; init; } = 1.0;
}

// RoomTemplateBuilder enhancement
public sealed class RoomTemplateBuilder<TRoomType> where TRoomType : Enum
{
    // ... existing methods ...
    
    /// <summary>
    /// Sets the selection weight for this template. Higher weights are more likely to be selected.
    /// </summary>
    /// <param name="weight">Weight value (must be >= 0.0). Default is 1.0.</param>
    public RoomTemplateBuilder<TRoomType> WithWeight(double weight);
}

// Weighted selection helper (internal)
internal static class WeightedRandom
{
    public static T Select<T>(IReadOnlyList<T> items, IReadOnlyList<double> weights, Random rng)
        where T : class;
}
```

**Usage Examples**:

```csharp
// Common combat room (appears frequently)
var commonCombat = RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
    .WithId("common-combat")
    .ForRoomTypes(RoomType.Combat)
    .WithWeight(10.0)  // 10x more likely than default
    .Build();

// Rare legendary combat room (appears infrequently)
var legendaryCombat = RoomTemplateBuilder<RoomType>.Rectangle(8, 8)
    .WithId("legendary-combat")
    .ForRoomTypes(RoomType.Combat)
    .WithWeight(0.1)  // 10x less likely than default, 100x less than common
    .Build();

// Disabled template (won't be selected)
var disabledTemplate = RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
    .WithId("disabled")
    .ForRoomTypes(RoomType.Combat)
    .WithWeight(0.0)  // Never selected
    .Build();

// With common (10), default (1), and rare (0.1):
// Selection probabilities: common = 90.1%, default = 9.0%, rare = 0.9%
```

## Dependencies

- None

## Test Scenarios

1. **Uniform Weighting (Backward Compatibility)**: Templates with default weight (1.0) should produce uniform random selection, matching current behavior.

2. **Weighted Selection**: Templates with weights [10, 1, 0.1] should be selected with probabilities approximately [90.1%, 9.0%, 0.9%] over many trials.

3. **Zero Weight Exclusion**: Templates with weight 0.0 should never be selected, even if they're the only templates for a room type (should throw exception).

4. **Deterministic Selection**: Same seed + same weights should produce identical template selections across multiple generation runs.

5. **Single Template**: When only one template with weight > 0 exists, it should always be selected.

6. **All Zero Weights**: Configuration with all templates having weight 0.0 for a room type should throw `InvalidConfigurationException` with clear error message.

7. **Zone-Specific Weighting**: Zone-specific templates should respect their weights when selecting templates within that zone.

8. **Edge Cases**: Very large weights (e.g., 1e6), very small weights (e.g., 1e-6), and mixed weight distributions should all work correctly.

## Acceptance Criteria

- [ ] `RoomTemplate<TRoomType>` has `Weight` property with default value 1.0
- [ ] `RoomTemplateBuilder<TRoomType>` has `WithWeight(double)` method
- [ ] Weighted random selection algorithm correctly implements probability distribution
- [ ] Template selection in `IncrementalSolver` uses weighted selection
- [ ] All existing tests pass (backward compatibility maintained)
- [ ] New tests verify weighted selection produces expected probability distributions
- [ ] Determinism tests verify same seed produces same template selections
- [ ] Zero-weight templates are excluded from selection
- [ ] Configuration validation throws clear errors when all templates have zero weight
- [ ] Zone-specific template weighting works correctly
- [ ] Wiki documentation includes weighting examples and best practices
- [ ] Performance is acceptable (weighted selection adds minimal overhead)
