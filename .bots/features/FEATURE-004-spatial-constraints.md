# Feature: Spatial Constraints System

**ID**: FEATURE-004
**Status**: complete
**Created**: 2025-12-13T01:15:00Z
**Priority**: high
**Complexity**: complex

## Description

Implement a spatial constraints system that allows room placement rules based on actual 2D spatial positions rather than just graph topology. This enables powerful dungeon design patterns like clustering rooms in specific regions, placing rooms in quadrants, enforcing spatial proximity/distance rules, and creating thematic spatial zones that complement the existing graph-based constraint system.

**Why this matters**: Spatial constraints unlock entirely new dungeon design capabilities that graph-based constraints cannot express:

- **Spatial clustering**: Place all shops in a "bazaar" area, all combat rooms in a "gauntlet" region, or all treasure rooms in a "vault" corner
- **Quadrant-based placement**: Ensure boss rooms are in the center, spawn rooms in a corner, or special rooms in specific quadrants
- **Spatial proximity rules**: Require certain room types to be within N cells of each other in 2D space (beyond graph distance)
- **Thematic spatial zones**: Create visual/thematic regions (e.g., "northern fortress", "southern crypts") that affect room placement
- **Layout control**: Design dungeons with specific spatial patterns (circular, linear, grid-based clusters) that complement graph topology
- **Game design flexibility**: Enable roguelike mechanics that depend on spatial relationships (e.g., "treasure rooms near boss", "shops form a market district")

**Problems solved**:
- Graph constraints can't express spatial relationships (e.g., "all shops in top-right quadrant")
- No way to create spatial clusters or thematic regions based on 2D positions
- Cannot enforce spatial proximity rules independent of graph distance
- Limited control over overall dungeon layout shape and organization
- Missing tool for creating visually/thematically organized dungeons

**What makes this adventurous**:
- Cross-cutting feature that touches constraint system, spatial solver, and room placement logic
- Enables entirely new dungeon design patterns not possible with graph constraints alone
- Requires architectural changes to constraint evaluation (spatial awareness)
- Complements existing constraint system rather than replacing it
- Opens up new possibilities for roguelike game design

## Requirements

- [x] Create `ISpatialConstraint<TRoomType>` interface that extends `IConstraint<TRoomType>` with spatial evaluation
- [x] Implement spatial constraint evaluation during spatial placement phase (not just type assignment)
- [x] Add built-in spatial constraints:
  - [x] `MustBeInQuadrantConstraint` - Room must be in specific quadrant (top-left, top-right, bottom-left, bottom-right, center)
  - [x] `MinSpatialDistanceFromRoomTypeConstraint` - Room must be at least N cells away from rooms of specified type(s)
  - [x] `MaxSpatialDistanceFromRoomTypeConstraint` - Room must be within N cells of rooms of specified type(s)
  - [x] `MustFormSpatialClusterConstraint` - Rooms of this type must form a spatial cluster (within N cells of each other)
  - [x] `MustBeInRegionConstraint` - Room must be within a defined rectangular region (min/max X/Y bounds)
  - [x] `MinSpatialDistanceFromStartConstraint` - Room must be at least N cells from spawn position
  - [x] `MaxSpatialDistanceFromStartConstraint` - Room must be within N cells of spawn position
- [x] Integrate spatial constraint evaluation into `IncrementalSolver` placement logic
- [x] Support constraint composition: spatial constraints can be combined with graph constraints using `CompositeConstraint`
- [x] Ensure spatial constraints work with existing graph constraints (both must pass)
- [x] Add validation to prevent impossible spatial constraints (e.g., conflicting quadrant requirements)
- [x] Maintain determinism: spatial constraint evaluation must be deterministic given same seed
- [x] Document spatial constraints in wiki with examples and best practices
- [x] Performance: Spatial constraint evaluation should be efficient (consider spatial indexing for distance queries)

## Technical Details

**Implementation Approach**:

1. **ISpatialConstraint Interface**:
   ```csharp
   public interface ISpatialConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
   {
       /// <summary>
       /// Checks if a room placement is valid spatially.
       /// Called during spatial placement phase, not type assignment phase.
       /// </summary>
       /// <param name="proposedPosition">The proposed anchor position for the room.</param>
       /// <param name="roomTemplate">The template being placed.</param>
       /// <param name="placedRooms">All rooms already placed in the dungeon.</param>
       /// <param name="graph">The floor graph for context.</param>
       /// <param name="assignments">Room type assignments.</param>
       /// <returns>True if this spatial position is valid for the target room type.</returns>
       bool IsValidSpatially(
           Cell proposedPosition,
           RoomTemplate<TRoomType> roomTemplate,
           IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
           FloorGraph graph,
           IReadOnlyDictionary<int, TRoomType> assignments);
   }
   ```

2. **Constraint Evaluation Phases**:
   - **Phase 1 (Type Assignment)**: Graph-based constraints evaluate during `RoomTypeAssigner` (existing behavior)
   - **Phase 2 (Spatial Placement)**: Spatial constraints evaluate during `IncrementalSolver` placement attempts
   - Both phases must pass: A room must satisfy both graph constraints AND spatial constraints

3. **IncrementalSolver Integration**:
   - When attempting to place a room, check all spatial constraints for that room type
   - If any spatial constraint fails, try next placement candidate
   - Spatial constraints are evaluated after checking for cell overlap but before finalizing placement
   - Consider spatial constraints in `TryPlaceAdjacent` and `PlaceNearby` methods

4. **Built-in Spatial Constraints**:

   **MustBeInQuadrantConstraint**:
   ```csharp
   public class MustBeInQuadrantConstraint<TRoomType> : ISpatialConstraint<TRoomType>
   {
       public Quadrant AllowedQuadrants { get; } // Flags enum
       // Quadrant determined by room's center or anchor position relative to dungeon bounds
   }
   ```

   **MinSpatialDistanceFromRoomTypeConstraint**:
   ```csharp
   public class MinSpatialDistanceFromRoomTypeConstraint<TRoomType> : ISpatialConstraint<TRoomType>
   {
       public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }
       public int MinDistance { get; } // In cells (Manhattan or Euclidean)
       // Calculates minimum distance from any cell of this room to any cell of reference rooms
   }
   ```

   **MustFormSpatialClusterConstraint**:
   ```csharp
   public class MustFormSpatialClusterConstraint<TRoomType> : ISpatialConstraint<TRoomType>
   {
       public int ClusterRadius { get; } // Max distance between rooms in cluster
       public int MinClusterSize { get; } // Minimum rooms in cluster
       // Validates that all rooms of this type form a connected spatial cluster
   }
   ```

5. **Spatial Indexing (Performance Optimization)**:
   - Consider spatial indexing (e.g., grid-based or quadtree) for efficient distance queries
   - Cache room positions and bounding boxes for fast spatial lookups
   - Optimize for common queries: "find nearest room of type X", "check if position is in quadrant"

6. **Constraint Composition**:
   - Spatial constraints can be combined with graph constraints using `CompositeConstraint`
   - Example: `CompositeConstraint.And(graphConstraint, spatialConstraint)`
   - Both constraint types are evaluated in their respective phases

**Architecture Considerations**:

- **Two-Phase Constraint Evaluation**: Graph constraints in type assignment, spatial constraints in placement
- **Backward Compatibility**: Existing graph constraints continue to work unchanged
- **Determinism**: Spatial constraint evaluation must be deterministic (same seed = same results)
- **Performance**: Spatial queries may be expensive; optimize with indexing and caching
- **Error Handling**: Clear error messages when spatial constraints cannot be satisfied
- **Extensibility**: Interface allows custom spatial constraints for advanced use cases

**API Design**:

```csharp
// Spatial constraint interface
public interface ISpatialConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    bool IsValidSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments);
}

// Example: Quadrant constraint
public enum Quadrant
{
    TopLeft = 1,
    TopRight = 2,
    BottomLeft = 4,
    BottomRight = 8,
    Center = 16
}

public class MustBeInQuadrantConstraint<TRoomType> : ISpatialConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public Quadrant AllowedQuadrants { get; }
    
    public MustBeInQuadrantConstraint(TRoomType roomType, Quadrant allowedQuadrants);
}

// Example: Spatial distance constraint
public class MinSpatialDistanceFromRoomTypeConstraint<TRoomType> : ISpatialConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }
    public int MinDistance { get; }
    
    public MinSpatialDistanceFromRoomTypeConstraint(
        TRoomType targetRoomType,
        TRoomType referenceRoomType,
        int minDistance);
    
    public MinSpatialDistanceFromRoomTypeConstraint(
        TRoomType targetRoomType,
        int minDistance,
        params TRoomType[] referenceRoomTypes);
}
```

**Usage Examples**:

```csharp
// All shops must be in top-right quadrant (bazaar area)
var shopQuadrantConstraint = new MustBeInQuadrantConstraint<RoomType>(
    RoomType.Shop,
    Quadrant.TopRight
);

// Treasure rooms must be at least 10 cells from spawn (spatial distance, not graph distance)
var treasureDistanceConstraint = new MinSpatialDistanceFromStartConstraint<RoomType>(
    RoomType.Treasure,
    minDistance: 10
);

// Boss room must be in center quadrant
var bossCenterConstraint = new MustBeInQuadrantConstraint<RoomType>(
    RoomType.Boss,
    Quadrant.Center
);

// Combat rooms must form a spatial cluster (all within 5 cells of each other)
var combatClusterConstraint = new MustFormSpatialClusterConstraint<RoomType>(
    RoomType.Combat,
    clusterRadius: 5,
    minClusterSize: 3
);

// Secret rooms must be within 3 cells of boss room (spatial proximity)
var secretProximityConstraint = new MaxSpatialDistanceFromRoomTypeConstraint<RoomType>(
    RoomType.Secret,
    RoomType.Boss,
    maxDistance: 3
);

// Combine graph and spatial constraints
var complexConstraint = CompositeConstraint<RoomType>.And(
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Special, 5), // Graph constraint
    new MustBeInQuadrantConstraint<RoomType>(RoomType.Special, Quadrant.BottomLeft) // Spatial constraint
);
```

## Dependencies

- None (builds on existing constraint system and spatial solver)

## Test Scenarios

1. **Quadrant Constraint**: Room placed in correct quadrant when constraint is specified; placement fails if no valid positions in quadrant.

2. **Spatial Distance from Room Type**: Room placed at correct minimum/maximum distance from reference room types; distance calculated correctly (Manhattan or Euclidean).

3. **Spatial Cluster Formation**: Multiple rooms of same type form a spatial cluster when `MustFormSpatialClusterConstraint` is used; cluster validation works correctly.

4. **Spatial Distance from Start**: Room placed at correct distance from spawn position (spatial, not graph distance).

5. **Region Constraint**: Room placed within specified rectangular region bounds; placement fails if region is too small or conflicts with other constraints.

6. **Constraint Composition**: Spatial constraints work correctly when combined with graph constraints using `CompositeConstraint`.

7. **Determinism**: Same seed + same spatial constraints = identical spatial placements.

8. **Performance**: Spatial constraint evaluation is efficient even with many placed rooms (consider spatial indexing).

9. **Impossible Constraints**: Clear error messages when spatial constraints cannot be satisfied (e.g., quadrant too small, distance too large).

10. **Backward Compatibility**: Existing graph constraints continue to work unchanged; spatial constraints are optional.

## Acceptance Criteria

- [x] `ISpatialConstraint<TRoomType>` interface defined and implemented
- [x] All 7 built-in spatial constraints implemented and tested
- [x] Spatial constraint evaluation integrated into `IncrementalSolver` placement logic
- [x] Spatial constraints work correctly with existing graph constraints (both phases pass)
- [x] Spatial constraints can be composed with graph constraints using `CompositeConstraint`
- [x] All existing tests pass (backward compatibility maintained)
- [x] New tests verify each spatial constraint type works correctly
- [x] Determinism tests verify same seed produces same spatial placements
- [x] Performance tests verify spatial constraint evaluation is efficient
- [x] Clear error messages when spatial constraints cannot be satisfied
- [x] Wiki documentation includes spatial constraint examples and best practices
- [x] Spatial indexing/caching implemented for performance (if needed)
- [x] API documentation complete for all spatial constraint types
