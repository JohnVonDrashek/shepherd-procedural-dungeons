using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Library of built-in dungeon themes.
/// </summary>
public static class ThemePresetLibrary<TRoomType> where TRoomType : Enum
{
    private static readonly Dictionary<string, DungeonTheme<TRoomType>> _themes = new();
    private static readonly object _lock = new();

    /// <summary>Gets a built-in theme by ID.</summary>
    public static DungeonTheme<TRoomType>? GetTheme(string themeId)
    {
        EnsureThemesInitialized();
        _themes.TryGetValue(themeId.ToLowerInvariant(), out var theme);
        return theme;
    }
    
    /// <summary>Gets all built-in themes.</summary>
    public static IReadOnlyList<DungeonTheme<TRoomType>> GetAllThemes()
    {
        EnsureThemesInitialized();
        return _themes.Values.ToList();
    }
    
    /// <summary>Gets themes matching the specified tags.</summary>
    public static IReadOnlyList<DungeonTheme<TRoomType>> GetThemesByTags(params string[] tags)
    {
        EnsureThemesInitialized();
        var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
        return _themes.Values
            .Where(t => t.Tags.Any(tag => tagSet.Contains(tag)))
            .ToList();
    }

    /// <summary>Castle theme - structured, grid-based layout with low branching.</summary>
    public static DungeonTheme<TRoomType> Castle
    {
        get
        {
            EnsureThemesInitialized();
            return _themes["castle"];
        }
    }

    /// <summary>Cave theme - organic, cellular automata layout with high branching.</summary>
    public static DungeonTheme<TRoomType> Cave
    {
        get
        {
            EnsureThemesInitialized();
            return _themes["cave"];
        }
    }

    /// <summary>Temple theme - structured, maze-like layout.</summary>
    public static DungeonTheme<TRoomType> Temple
    {
        get
        {
            EnsureThemesInitialized();
            return _themes["temple"];
        }
    }

    /// <summary>Laboratory theme - structured, grid-based layout.</summary>
    public static DungeonTheme<TRoomType> Laboratory
    {
        get
        {
            EnsureThemesInitialized();
            return _themes["laboratory"];
        }
    }

    /// <summary>Crypt theme - underground, maze-like layout.</summary>
    public static DungeonTheme<TRoomType> Crypt
    {
        get
        {
            EnsureThemesInitialized();
            return _themes["crypt"];
        }
    }

    /// <summary>Forest theme - organic, spanning tree layout.</summary>
    public static DungeonTheme<TRoomType> Forest
    {
        get
        {
            EnsureThemesInitialized();
            return _themes["forest"];
        }
    }

    private static void EnsureThemesInitialized()
    {
        if (_themes.Count > 0)
            return;

        lock (_lock)
        {
            if (_themes.Count > 0)
                return;

            InitializeThemes();
        }
    }

    private static void InitializeThemes()
    {
        // Get all room types from the enum
        var allRoomTypes = Enum.GetValues(typeof(TRoomType)).Cast<TRoomType>().ToArray();

        // Try to find common room type names, fallback to first few enum values
        TRoomType FindRoomTypeByName(string name, TRoomType fallback)
        {
            var enumNames = Enum.GetNames(typeof(TRoomType));
            var enumValues = Enum.GetValues(typeof(TRoomType)).Cast<TRoomType>().ToArray();
            
            for (int i = 0; i < enumNames.Length; i++)
            {
                if (string.Equals(enumNames[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return enumValues[i];
                }
            }
            return fallback;
        }

        var spawnType = FindRoomTypeByName("Spawn", allRoomTypes[0]);
        var bossType = allRoomTypes.Length > 1 
            ? FindRoomTypeByName("Boss", allRoomTypes[1]) 
            : allRoomTypes[0];
        var defaultType = allRoomTypes.Length > 2 
            ? FindRoomTypeByName("Combat", allRoomTypes.Length > 2 ? allRoomTypes[2] : allRoomTypes[0])
            : (allRoomTypes.Length > 1 ? allRoomTypes[1] : allRoomTypes[0]);

        // Create default templates that work for all room types
        var defaultTemplates = new List<RoomTemplate<TRoomType>>
        {
            RoomTemplateBuilder<TRoomType>.Rectangle(3, 3)
                .WithId("default")
                .ForRoomTypes(allRoomTypes)
                .WithDoorsOnAllExteriorEdges()
                .Build()
        };

        // Helper to create base config
        FloorConfig<TRoomType> CreateBaseConfig(
            int seed,
            int roomCount,
            GraphAlgorithm algorithm,
            float branchingFactor,
            HallwayMode hallwayMode,
            GridBasedGraphConfig? gridConfig = null,
            CellularAutomataGraphConfig? cellularConfig = null,
            MazeBasedGraphConfig? mazeConfig = null)
        {
            return new FloorConfig<TRoomType>
            {
                Seed = seed,
                RoomCount = roomCount,
                SpawnRoomType = spawnType,
                BossRoomType = bossType,
                DefaultRoomType = defaultType,
                Templates = defaultTemplates,
                GraphAlgorithm = algorithm,
                BranchingFactor = branchingFactor,
                HallwayMode = hallwayMode,
                GridBasedConfig = gridConfig,
                CellularAutomataConfig = cellularConfig,
                MazeBasedConfig = mazeConfig
            };
        }

        // Castle theme - structured, grid-based
        var castleConfig = CreateBaseConfig(
            seed: 12345,
            roomCount: 15,
            algorithm: GraphAlgorithm.GridBased,
            branchingFactor: 0.25f, // Low branching for structured feel
            hallwayMode: HallwayMode.AsNeeded, // Changed from None to allow flexible placement
            gridConfig: new GridBasedGraphConfig
            {
                GridWidth = 10,
                GridHeight = 2,
                ConnectivityPattern = ConnectivityPattern.FourWay
            }
        );

        _themes["castle"] = new DungeonTheme<TRoomType>
        {
            Id = "castle",
            Name = "Castle",
            Description = "Structured castle layout with grid-based rooms and low branching factor",
            BaseConfig = castleConfig,
            Tags = new HashSet<string> { "structured", "indoor", "medieval", "grid-based" }
        };

        // Cave theme - organic, cellular automata
        var caveConfig = CreateBaseConfig(
            seed: 12345,
            roomCount: 15,
            algorithm: GraphAlgorithm.CellularAutomata,
            branchingFactor: 0.5f, // Higher branching for organic feel
            hallwayMode: HallwayMode.AsNeeded,
            cellularConfig: new CellularAutomataGraphConfig
            {
                BirthThreshold = 4,
                SurvivalThreshold = 3,
                Iterations = 5
            }
        );

        _themes["cave"] = new DungeonTheme<TRoomType>
        {
            Id = "cave",
            Name = "Cave",
            Description = "Organic cave layout with cellular automata generation and high branching factor",
            BaseConfig = caveConfig,
            Tags = new HashSet<string> { "organic", "underground", "natural", "cave-like" }
        };

        // Temple theme - structured, maze-based
        var templeConfig = CreateBaseConfig(
            seed: 12345,
            roomCount: 15,
            algorithm: GraphAlgorithm.MazeBased,
            branchingFactor: 0.3f,
            hallwayMode: HallwayMode.AsNeeded,
            mazeConfig: new MazeBasedGraphConfig
            {
                MazeType = MazeType.Perfect,
                Algorithm = MazeAlgorithm.Prims
            }
        );

        _themes["temple"] = new DungeonTheme<TRoomType>
        {
            Id = "temple",
            Name = "Temple",
            Description = "Structured temple layout with maze-based generation",
            BaseConfig = templeConfig,
            Tags = new HashSet<string> { "structured", "indoor", "religious", "maze-like" }
        };

        // Laboratory theme - structured, grid-based
        var laboratoryConfig = CreateBaseConfig(
            seed: 12345,
            roomCount: 15,
            algorithm: GraphAlgorithm.GridBased,
            branchingFactor: 0.35f,
            hallwayMode: HallwayMode.AsNeeded, // Changed from None to allow flexible placement
            gridConfig: new GridBasedGraphConfig
            {
                GridWidth = 4,
                GridHeight = 4,
                ConnectivityPattern = ConnectivityPattern.EightWay
            }
        );

        _themes["laboratory"] = new DungeonTheme<TRoomType>
        {
            Id = "laboratory",
            Name = "Laboratory",
            Description = "Structured laboratory layout with grid-based rooms",
            BaseConfig = laboratoryConfig,
            Tags = new HashSet<string> { "structured", "indoor", "scientific", "grid-based" }
        };

        // Crypt theme - underground, maze-based
        var cryptConfig = CreateBaseConfig(
            seed: 12345,
            roomCount: 15,
            algorithm: GraphAlgorithm.MazeBased,
            branchingFactor: 0.4f,
            hallwayMode: HallwayMode.AsNeeded,
            mazeConfig: new MazeBasedGraphConfig
            {
                MazeType = MazeType.Imperfect,
                Algorithm = MazeAlgorithm.Kruskals
            }
        );

        _themes["crypt"] = new DungeonTheme<TRoomType>
        {
            Id = "crypt",
            Name = "Crypt",
            Description = "Underground crypt layout with maze-based generation",
            BaseConfig = cryptConfig,
            Tags = new HashSet<string> { "underground", "maze-like", "dark", "tomb-like" }
        };

        // Forest theme - organic, spanning tree
        var forestConfig = CreateBaseConfig(
            seed: 12345,
            roomCount: 15,
            algorithm: GraphAlgorithm.SpanningTree,
            branchingFactor: 0.4f,
            hallwayMode: HallwayMode.AsNeeded
        );

        _themes["forest"] = new DungeonTheme<TRoomType>
        {
            Id = "forest",
            Name = "Forest",
            Description = "Organic forest layout with spanning tree generation",
            BaseConfig = forestConfig,
            Tags = new HashSet<string> { "organic", "outdoor", "natural", "tree-like" }
        };
    }
}
