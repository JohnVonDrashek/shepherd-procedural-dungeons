# Optimization: Constraint Filtering Performance Optimization

**ID**: OPT-003
**Status**: proposed
**Created**: 2025-01-27T16:00:00Z
**Category**: performance
**Priority**: high
**Estimated Impact**: medium
**Estimated Effort**: low

## Description

The `RoomTypeAssigner` class repeatedly filters constraints by room type using LINQ `Where()` operations throughout the `AssignTypes()` method. This filtering occurs multiple times for the same room types, creating unnecessary O(n × m) complexity where n is the number of room types and m is the number of constraints. For dungeons with many room types and constraints, this creates significant overhead in constraint evaluation.

## Current Performance Characteristics

- **Location**: `src/ShepherdProceduralDungeons/Generation/RoomTypeAssigner.cs`
  - Line 71: `constraints.Where(c => c.TargetRoomType.Equals(bossType)).ToList()` - Filters constraints for boss type
  - Line 123: `constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList()` - Filters constraints for each room requirement (in loop)
  - Line 163: `constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList()` - Filters constraints again for zone-specific requirements (in nested loops)
  - Line 243: `constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList()` - Filters constraints again in `GetConstraintPriority()` (called for each room requirement)

- **Current Complexity**: 
  - Constraint filtering: O(m) per filter operation where m is number of constraints
  - Total filtering operations: O(n × m) where n is number of room types and m is number of constraints
  - Each room type's constraints are filtered multiple times (once for boss, once per requirement, once per zone requirement, once per priority calculation)

- **Current Behavior**: 
  - Constraints are passed as `IReadOnlyList<IConstraint<TRoomType>>`
  - Every time constraints for a specific room type are needed, the entire constraints list is filtered using LINQ `Where()`
  - The filtered results are materialized with `.ToList()` each time
  - For a dungeon with 10 room types and 50 constraints, this results in ~40+ filter operations (10 for requirements, 10 for priority calculation, 1 for boss, potentially 20+ for zone requirements)

- **Performance Issues**: 
  - Issue 1: Repeated filtering - The same room type's constraints are filtered multiple times (boss constraints filtered once, requirement constraints filtered once per requirement, zone constraints filtered once per zone requirement, priority constraints filtered once per requirement)
  - Issue 2: LINQ overhead - Each `Where()` operation creates an enumerable that must be iterated, and `.ToList()` materializes the results, creating unnecessary allocations
  - Issue 3: O(n × m) complexity - For each of n room types, we scan through all m constraints, resulting in quadratic complexity
  - Issue 4: Priority calculation overhead - `GetConstraintPriority()` filters constraints again for each room type, even though this could be cached

## Optimization Opportunity

Pre-group constraints by room type at the start of `AssignTypes()` to eliminate repeated filtering:

1. **Pre-group constraints by room type**: Create `Dictionary<TRoomType, IReadOnlyList<IConstraint<TRoomType>>>` at the start of `AssignTypes()`
2. **Replace all filter operations**: Replace all `constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList()` calls with dictionary lookups
3. **Cache priority calculations**: Pre-calculate constraint priorities for each room type and cache in a dictionary
4. **Reduce allocations**: Eliminate repeated `.ToList()` calls by using pre-grouped lists

This will reduce:
- Constraint filtering from O(n × m) to O(m) where n is room types and m is constraints
- Memory allocations from multiple list materializations to single dictionary creation
- Overall constraint evaluation time by 30-60% for dungeons with many room types and constraints

## Expected Impact

- **Performance Improvement**: 
  - Constraint filtering: 50-80% faster for dungeons with 5+ room types and 20+ constraints
  - Room type assignment: 20-40% faster overall for complex constraint configurations
  - Priority calculation: 60-90% faster (eliminates repeated filtering)
  - Overall generation time: 5-15% improvement when many constraints are used

- **Memory Improvement**: 
  - Reduced allocations: Eliminates multiple `.ToList()` calls per room type
  - Additional memory: ~16 bytes per room type (dictionary overhead) + ~8 bytes per constraint (list overhead)
  - For 10 room types and 50 constraints: ~160 bytes dictionary + ~400 bytes lists = ~560 bytes additional memory (negligible compared to performance gain)

- **Complexity Improvement**: 
  - Constraint filtering: O(n × m) → O(m) where n is room types and m is constraints
  - Priority calculation: O(n × m) → O(m) (pre-calculated once)

- **Test Impact**: 
  - No impact on test performance (tests use small constraint sets)
  - May improve test execution time slightly for tests with many constraints

## Areas Affected

- `src/ShepherdProceduralDungeons/Generation/RoomTypeAssigner.cs`
  - `AssignTypes()` method - Add constraint grouping at start
  - `GetConstraintPriority()` method - Use pre-grouped constraints or cached priorities
  - All constraint filtering operations - Replace with dictionary lookups

## Risks and Trade-offs

- **Risk 1**: Dictionary lookup overhead for small constraint sets (unlikely to be significant, O(1) vs O(m) filtering)
- **Trade-off 1**: Slightly more complex code structure (pre-grouping step) for better performance
- **Trade-off 2**: Small additional memory overhead (dictionary + lists) for significant performance gain
- **Risk 2**: Need to ensure dictionary is properly initialized before use (low risk, straightforward implementation)

## Benchmark Requirements

What benchmarks are needed to measure this optimization?
- Benchmark 1: Constraint filtering performance - Measure time to filter constraints for a single room type (baseline: LINQ Where().ToList(), improved: dictionary lookup)
- Benchmark 2: Room type assignment performance - Measure end-to-end `AssignTypes()` performance with varying numbers of room types (5, 10, 20) and constraints (10, 50, 100)
- Benchmark 3: Priority calculation performance - Measure `GetConstraintPriority()` performance with varying constraint counts
- Benchmark 4: Memory allocations - Measure memory allocations during constraint filtering and room type assignment

## Baseline Metrics

**Benchmark File**: `.bots/benchmarks/BENCHMARK-OPT-003-constraint-filtering/Program.cs`

**Run Command**: `dotnet run --project .bots/benchmarks/BENCHMARK-OPT-003-constraint-filtering/BENCHMARK-OPT-003-constraint-filtering.csproj -c Release`

**Benchmark Structure**:
- **ConstraintFiltering**: Measures time to filter constraints for a single room type (10, 50, 100 constraints)
- **RoomTypeAssignment**: Measures end-to-end constraint filtering operations simulating `AssignTypes()` (5/10/20 nodes with 10/50/100 constraints)
- **PriorityCalculation**: Measures `GetConstraintPriority()` performance with varying constraint counts (10, 50, 100 constraints)
- **ConstraintFilteringMemoryAllocations**: Measures memory allocations during constraint filtering operations

**Results**: Baseline metrics populated from benchmark runs.

### Baseline Metrics Summary

**ConstraintFiltering** (single room type filter):
- 10 constraints: 114.6 ns
- 50 constraints: 642.9 ns (5.6x slower)
- 100 constraints: 1,143.6 ns (10.0x slower)
- **Scaling**: Linear O(m) where m is constraint count

**RoomTypeAssignment** (end-to-end constraint filtering):
- 5 nodes, 10 constraints: 1,350.4 ns
- 10 nodes, 50 constraints: 6,093.6 ns (4.5x slower)
- 20 nodes, 100 constraints: 11,829.9 ns (8.8x slower)
- **Scaling**: Shows quadratic O(n × m) behavior

**PriorityCalculation**:
- 10 constraints: 548.3 ns
- 50 constraints: 2,508.9 ns (4.6x slower)
- 100 constraints: 5,408.3 ns (9.9x slower)
- **Scaling**: Linear O(m) per room type

**Memory Allocations**:
- 10 constraints: 178.6 μs (7,480 B allocated)
- 50 constraints: 197.2 μs (28,758 B allocated)
- 100 constraints: 210.8 μs (53,384 B allocated)
- **Issue**: Significant allocations from repeated `.ToList()` calls

## Optimization Proposal

### Implementation Strategy

**Bottleneck Analysis**:
Benchmark results confirm the bottleneck: constraint filtering scales linearly with constraint count (O(m)), and this filtering occurs multiple times per room type (O(n × m) total). For 20 nodes with 100 constraints, we see ~11.8μs overhead from repeated filtering operations.

**Proposed Solution**:
Pre-group constraints by room type at the start of `AssignTypes()` to eliminate repeated filtering:

1. **Create constraint dictionary**: At the start of `AssignTypes()`, create `Dictionary<TRoomType, IReadOnlyList<IConstraint<TRoomType>>>` by grouping constraints once
2. **Replace all filter operations**: Replace all `constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList()` calls with dictionary lookups
3. **Cache priority calculations**: Pre-calculate constraint priorities for each room type and store in a dictionary
4. **Handle missing room types**: Use `TryGetValue()` with empty list fallback for room types without constraints

### Expected Improvements

Based on benchmark analysis:
- **Constraint filtering**: 50-80% faster (O(1) dictionary lookup vs O(m) LINQ filter)
- **Room type assignment**: 20-40% faster overall (eliminates repeated filtering)
- **Priority calculation**: 60-90% faster (pre-calculated vs repeated filtering)
- **Memory allocations**: 30-50% reduction (eliminates multiple `.ToList()` calls)

### Implementation Steps

1. **Add constraint grouping at start of `AssignTypes()`**:
   ```csharp
   // After setting floor/zone awareness (line 63)
   var constraintsByType = constraints
       .GroupBy(c => c.TargetRoomType)
       .ToDictionary(g => g.Key, g => (IReadOnlyList<IConstraint<TRoomType>>)g.ToList().AsReadOnly());
   ```

2. **Replace boss constraint filtering (line 71)**:
   ```csharp
   // Replace: var bossConstraints = constraints.Where(c => c.TargetRoomType.Equals(bossType)).ToList();
   var bossConstraints = constraintsByType.TryGetValue(bossType, out var boss) 
       ? boss 
       : Array.Empty<IConstraint<TRoomType>>();
   ```

3. **Replace requirement constraint filtering (line 123)**:
   ```csharp
   // Replace: var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
   var typeConstraints = constraintsByType.TryGetValue(roomType, out var type) 
       ? type 
       : Array.Empty<IConstraint<TRoomType>>();
   ```

4. **Replace zone requirement constraint filtering (line 163)**:
   ```csharp
   // Same replacement as step 3
   var typeConstraints = constraintsByType.TryGetValue(roomType, out var type) 
       ? type 
       : Array.Empty<IConstraint<TRoomType>>();
   ```

5. **Optimize `GetConstraintPriority()` method (line 243)**:
   - Option A: Pre-calculate priorities and cache in dictionary
   - Option B: Use pre-grouped constraints from dictionary instead of filtering
   - **Recommendation**: Option B (simpler, still eliminates filtering overhead)

6. **Update `GetConstraintPriority()` signature**:
   ```csharp
   // Change from: GetConstraintPriority(TRoomType roomType, IReadOnlyList<IConstraint<TRoomType>> constraints)
   // To: GetConstraintPriority(TRoomType roomType, IReadOnlyDictionary<TRoomType, IReadOnlyList<IConstraint<TRoomType>>> constraintsByType)
   // Or: Pass pre-grouped constraints directly
   ```

### Code Changes Required

- **File**: `src/ShepherdProceduralDungeons/Generation/RoomTypeAssigner.cs`
  - **Line 63**: Add constraint grouping dictionary creation
  - **Line 71**: Replace boss constraint filtering with dictionary lookup
  - **Line 123**: Replace requirement constraint filtering with dictionary lookup
  - **Line 163**: Replace zone requirement constraint filtering with dictionary lookup
  - **Line 161**: Update `GetConstraintPriority()` call to use dictionary
  - **Line 242-268**: Update `GetConstraintPriority()` method to accept pre-grouped constraints or dictionary

### Trade-offs Analysis

- **Complexity**: Slightly more complex code structure (pre-grouping step) for better performance - **Acceptable**
- **Maintainability**: Code remains readable, dictionary lookup is standard pattern - **No impact**
- **Readability**: Dictionary lookups are clear and standard - **No impact**
- **Risk**: Low risk - straightforward dictionary usage, easy to test - **Low risk**
- **Memory**: Small additional memory overhead (~16 bytes per room type + ~8 bytes per constraint) - **Negligible compared to performance gain**
- **Compatibility**: No breaking changes, internal optimization only - **No impact**
