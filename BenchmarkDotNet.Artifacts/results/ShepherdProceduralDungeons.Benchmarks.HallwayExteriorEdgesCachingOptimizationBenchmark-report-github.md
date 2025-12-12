```

BenchmarkDotNet v0.13.12, macOS 15.7.2 (24G325) [Darwin 24.6.0]
Apple M3 Pro, 1 CPU, 11 logical and 11 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 10.0.0 (10.0.25.52411), Arm64 RyuJIT AdvSIMD


```
| Method                                      | hallwayCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|-------------------------------------------- |------------- |-----:|------:|------:|--------:|------------:|
| **HallwayGenerationManyExteriorEdges**          | **?**            |   **NA** |    **NA** |     **?** |       **?** |           **?** |
| HallwayGenerationMultipleConnectionsPerRoom | ?            |   NA |    NA |     ? |       ? |           ? |
|                                             |              |      |       |       |         |             |
| **HallwayGeneration**                           | **5**            |   **NA** |    **NA** |     **?** |       **?** |           **?** |
| HallwayGenerationMemoryAllocations          | 5            |   NA |    NA |     ? |       ? |           ? |
|                                             |              |      |       |       |         |             |
| **HallwayGeneration**                           | **10**           |   **NA** |    **NA** |     **?** |       **?** |           **?** |
| HallwayGenerationMemoryAllocations          | 10           |   NA |    NA |     ? |       ? |           ? |
|                                             |              |      |       |       |         |             |
| **HallwayGeneration**                           | **20**           |   **NA** |    **NA** |     **?** |       **?** |           **?** |
| HallwayGenerationMemoryAllocations          | 20           |   NA |    NA |     ? |       ? |           ? |
| EndToEndGenerationManyHallways              | 20           |   NA |    NA |     ? |       ? |           ? |
|                                             |              |      |       |       |         |             |
| **HallwayGeneration**                           | **50**           |   **NA** |    **NA** |     **?** |       **?** |           **?** |
| HallwayGenerationMemoryAllocations          | 50           |   NA |    NA |     ? |       ? |           ? |
| EndToEndGenerationManyHallways              | 50           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGenerationManyExteriorEdges: DefaultJob
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGenerationMultipleConnectionsPerRoom: DefaultJob
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGeneration: DefaultJob [hallwayCount=5]
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGenerationMemoryAllocations: DefaultJob [hallwayCount=5]
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGeneration: DefaultJob [hallwayCount=10]
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGenerationMemoryAllocations: DefaultJob [hallwayCount=10]
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGeneration: DefaultJob [hallwayCount=20]
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGenerationMemoryAllocations: DefaultJob [hallwayCount=20]
  HallwayExteriorEdgesCachingOptimizationBenchmark.EndToEndGenerationManyHallways: DefaultJob [hallwayCount=20]
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGeneration: DefaultJob [hallwayCount=50]
  HallwayExteriorEdgesCachingOptimizationBenchmark.HallwayGenerationMemoryAllocations: DefaultJob [hallwayCount=50]
  HallwayExteriorEdgesCachingOptimizationBenchmark.EndToEndGenerationManyHallways: DefaultJob [hallwayCount=50]
