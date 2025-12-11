using System.Text.Json;
using System.Text.Json.Serialization;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Exceptions;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for IConstraint polymorphic types.
/// </summary>
public sealed class ConstraintJsonConverter<TRoomType> : JsonConverter<IConstraint<TRoomType>> where TRoomType : Enum
{
    public override IConstraint<TRoomType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
        {
            throw new JsonException("Constraint must have a 'type' property.");
        }

        var type = typeElement.GetString();
        return type switch
        {
            "MinDistanceFromStart" => DeserializeMinDistanceFromStart(root, options),
            "MaxDistanceFromStart" => DeserializeMaxDistanceFromStart(root, options),
            "MustBeDeadEnd" => DeserializeMustBeDeadEnd(root, options),
            "NotOnCriticalPath" => DeserializeNotOnCriticalPath(root, options),
            "MaxPerFloor" => DeserializeMaxPerFloor(root, options),
            "MinConnectionCount" => DeserializeMinConnectionCount(root, options),
            "MaxConnectionCount" => DeserializeMaxConnectionCount(root, options),
            "MustBeAdjacentTo" => DeserializeMustBeAdjacentTo(root, options),
            "MustNotBeAdjacentTo" => DeserializeMustNotBeAdjacentTo(root, options),
            "MinDistanceFromRoomType" => DeserializeMinDistanceFromRoomType(root, options),
            "MaxDistanceFromRoomType" => DeserializeMaxDistanceFromRoomType(root, options),
            "MustComeBefore" => DeserializeMustComeBefore(root, options),
            "OnlyInZone" => DeserializeOnlyInZone(root, options),
            "CompositeConstraint" => DeserializeCompositeConstraint(root, options),
            _ => throw new JsonException($"Unknown constraint type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, IConstraint<TRoomType> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case MinDistanceFromStartConstraint<TRoomType> c:
                writer.WriteString("type", "MinDistanceFromStart");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WriteNumber("minDistance", c.MinDistance);
                break;

            case MaxDistanceFromStartConstraint<TRoomType> c:
                writer.WriteString("type", "MaxDistanceFromStart");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WriteNumber("maxDistance", c.MaxDistance);
                break;

            case MustBeDeadEndConstraint<TRoomType> c:
                writer.WriteString("type", "MustBeDeadEnd");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                break;

            case NotOnCriticalPathConstraint<TRoomType> c:
                writer.WriteString("type", "NotOnCriticalPath");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                break;

            case MaxPerFloorConstraint<TRoomType> c:
                writer.WriteString("type", "MaxPerFloor");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WriteNumber("maxCount", c.MaxCount);
                break;

            case MinConnectionCountConstraint<TRoomType> c:
                writer.WriteString("type", "MinConnectionCount");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WriteNumber("minConnections", c.MinConnections);
                break;

            case MaxConnectionCountConstraint<TRoomType> c:
                writer.WriteString("type", "MaxConnectionCount");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WriteNumber("maxConnections", c.MaxConnections);
                break;

            case MustBeAdjacentToConstraint<TRoomType> c:
                writer.WriteString("type", "MustBeAdjacentTo");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WritePropertyName("requiredAdjacentTypes");
                JsonSerializer.Serialize(writer, c.RequiredAdjacentTypes, options);
                break;

            case MustNotBeAdjacentToConstraint<TRoomType> c:
                writer.WriteString("type", "MustNotBeAdjacentTo");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WritePropertyName("forbiddenAdjacentTypes");
                JsonSerializer.Serialize(writer, c.ForbiddenAdjacentTypes, options);
                break;

            case MinDistanceFromRoomTypeConstraint<TRoomType> c:
                writer.WriteString("type", "MinDistanceFromRoomType");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WritePropertyName("referenceRoomTypes");
                JsonSerializer.Serialize(writer, c.ReferenceRoomTypes, options);
                writer.WriteNumber("minDistance", c.MinDistance);
                break;

            case MaxDistanceFromRoomTypeConstraint<TRoomType> c:
                writer.WriteString("type", "MaxDistanceFromRoomType");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WritePropertyName("referenceRoomTypes");
                JsonSerializer.Serialize(writer, c.ReferenceRoomTypes, options);
                writer.WriteNumber("maxDistance", c.MaxDistance);
                break;

            case MustComeBeforeConstraint<TRoomType> c:
                writer.WriteString("type", "MustComeBefore");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WritePropertyName("referenceRoomTypes");
                JsonSerializer.Serialize(writer, c.ReferenceRoomTypes, options);
                break;

            case OnlyInZoneConstraint<TRoomType> c:
                writer.WriteString("type", "OnlyInZone");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WriteString("zoneId", c.ZoneId);
                break;

            case CompositeConstraint<TRoomType> c:
                writer.WriteString("type", "CompositeConstraint");
                writer.WritePropertyName("targetRoomType");
                JsonSerializer.Serialize(writer, c.TargetRoomType, options);
                writer.WriteString("operator", c.Operator.ToString());
                writer.WritePropertyName("constraints");
                writer.WriteStartArray();
                foreach (var constraint in c.Constraints)
                {
                    JsonSerializer.Serialize(writer, constraint, options);
                }
                writer.WriteEndArray();
                break;

            default:
                throw new JsonException($"Unknown constraint type: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }

    private IConstraint<TRoomType> DeserializeMinDistanceFromStart(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var minDistance = root.GetProperty("minDistance").GetInt32();
        return new MinDistanceFromStartConstraint<TRoomType>(targetRoomType, minDistance);
    }

    private IConstraint<TRoomType> DeserializeMaxDistanceFromStart(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var maxDistance = root.GetProperty("maxDistance").GetInt32();
        return new MaxDistanceFromStartConstraint<TRoomType>(targetRoomType, maxDistance);
    }

    private IConstraint<TRoomType> DeserializeMustBeDeadEnd(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        return new MustBeDeadEndConstraint<TRoomType>(targetRoomType);
    }

    private IConstraint<TRoomType> DeserializeNotOnCriticalPath(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        return new NotOnCriticalPathConstraint<TRoomType>(targetRoomType);
    }

    private IConstraint<TRoomType> DeserializeMaxPerFloor(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var maxCount = root.GetProperty("maxCount").GetInt32();
        return new MaxPerFloorConstraint<TRoomType>(targetRoomType, maxCount);
    }

    private IConstraint<TRoomType> DeserializeMinConnectionCount(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var minConnections = root.GetProperty("minConnections").GetInt32();
        return new MinConnectionCountConstraint<TRoomType>(targetRoomType, minConnections);
    }

    private IConstraint<TRoomType> DeserializeMaxConnectionCount(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var maxConnections = root.GetProperty("maxConnections").GetInt32();
        return new MaxConnectionCountConstraint<TRoomType>(targetRoomType, maxConnections);
    }

    private IConstraint<TRoomType> DeserializeMustBeAdjacentTo(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var requiredAdjacentTypes = root.GetProperty("requiredAdjacentTypes").Deserialize<TRoomType[]>(options)
            ?? throw new JsonException("MustBeAdjacentTo constraint must have 'requiredAdjacentTypes' array.");
        return new MustBeAdjacentToConstraint<TRoomType>(targetRoomType, requiredAdjacentTypes);
    }

    private IConstraint<TRoomType> DeserializeMustNotBeAdjacentTo(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var forbiddenAdjacentTypes = root.GetProperty("forbiddenAdjacentTypes").Deserialize<TRoomType[]>(options)
            ?? throw new JsonException("MustNotBeAdjacentTo constraint must have 'forbiddenAdjacentTypes' array.");
        return new MustNotBeAdjacentToConstraint<TRoomType>(targetRoomType, forbiddenAdjacentTypes);
    }

    private IConstraint<TRoomType> DeserializeMinDistanceFromRoomType(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var referenceRoomTypes = root.GetProperty("referenceRoomTypes").Deserialize<TRoomType[]>(options)
            ?? throw new JsonException("MinDistanceFromRoomType constraint must have 'referenceRoomTypes' array.");
        var minDistance = root.GetProperty("minDistance").GetInt32();
        return new MinDistanceFromRoomTypeConstraint<TRoomType>(targetRoomType, minDistance, referenceRoomTypes);
    }

    private IConstraint<TRoomType> DeserializeMaxDistanceFromRoomType(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var referenceRoomTypes = root.GetProperty("referenceRoomTypes").Deserialize<TRoomType[]>(options)
            ?? throw new JsonException("MaxDistanceFromRoomType constraint must have 'referenceRoomTypes' array.");
        var maxDistance = root.GetProperty("maxDistance").GetInt32();
        return new MaxDistanceFromRoomTypeConstraint<TRoomType>(targetRoomType, maxDistance, referenceRoomTypes);
    }

    private IConstraint<TRoomType> DeserializeMustComeBefore(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var referenceRoomTypes = root.GetProperty("referenceRoomTypes").Deserialize<TRoomType[]>(options)
            ?? throw new JsonException("MustComeBefore constraint must have 'referenceRoomTypes' array.");
        return new MustComeBeforeConstraint<TRoomType>(targetRoomType, referenceRoomTypes);
    }

    private IConstraint<TRoomType> DeserializeOnlyInZone(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var zoneId = root.GetProperty("zoneId").GetString()
            ?? throw new JsonException("OnlyInZone constraint must have 'zoneId' string.");
        return new OnlyInZoneConstraint<TRoomType>(targetRoomType, zoneId);
    }

    private IConstraint<TRoomType> DeserializeCompositeConstraint(JsonElement root, JsonSerializerOptions options)
    {
        var targetRoomType = DeserializeEnum(root.GetProperty("targetRoomType"), options);
        var operatorStr = root.GetProperty("operator").GetString()
            ?? throw new JsonException("CompositeConstraint must have 'operator' string.");
        var operatorEnum = Enum.Parse<CompositionOperator>(operatorStr);
        var constraintsArray = root.GetProperty("constraints").Deserialize<IConstraint<TRoomType>[]>(options)
            ?? throw new JsonException("CompositeConstraint must have 'constraints' array.");

        return operatorEnum switch
        {
            CompositionOperator.And => CompositeConstraint<TRoomType>.And(constraintsArray),
            CompositionOperator.Or => CompositeConstraint<TRoomType>.Or(constraintsArray),
            CompositionOperator.Not => constraintsArray.Length == 1
                ? CompositeConstraint<TRoomType>.Not(constraintsArray[0])
                : throw new JsonException("NOT composition must have exactly one constraint."),
            _ => throw new JsonException($"Unknown composition operator: {operatorStr}")
        };
    }

    private TRoomType DeserializeEnum(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (Enum.TryParse(typeof(TRoomType), value, true, out var result) && result is TRoomType enumResult)
            {
                return enumResult;
            }
            throw new InvalidConfigurationException($"Invalid enum value '{value}' for type {typeof(TRoomType).Name}.");
        }
        throw new JsonException($"Expected string for enum, got {element.ValueKind}.");
    }
}
