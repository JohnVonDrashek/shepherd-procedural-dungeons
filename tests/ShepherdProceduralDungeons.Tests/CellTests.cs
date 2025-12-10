using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

public class CellTests
{
    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var cell = new Cell(5, 10);

        // Act
        var result = cell.ToString();

        // Assert
        Assert.Equal("Cell(5, 10)", result);
    }

    [Fact]
    public void ToString_DoesNotCauseRecursionWhenAccessingDirectionalProperties()
    {
        // Arrange
        var cell = new Cell(5, 10);

        // Act & Assert - This test verifies that accessing directional properties
        // and calling ToString() on them doesn't cause infinite recursion
        var north = cell.North;
        var south = cell.South;
        var east = cell.East;
        var west = cell.West;

        // These should all complete without stack overflow
        var northStr = north.ToString();
        var southStr = south.ToString();
        var eastStr = east.ToString();
        var westStr = west.ToString();

        // Verify they have the expected format
        Assert.Equal("Cell(5, 9)", northStr);
        Assert.Equal("Cell(5, 11)", southStr);
        Assert.Equal("Cell(6, 10)", eastStr);
        Assert.Equal("Cell(4, 10)", westStr);
    }

    [Fact]
    public void ToString_WorksForNegativeCoordinates()
    {
        // Arrange
        var cell = new Cell(-5, -10);

        // Act
        var result = cell.ToString();

        // Assert
        Assert.Equal("Cell(-5, -10)", result);
    }

    [Fact]
    public void ToString_WorksForZeroCoordinates()
    {
        // Arrange
        var cell = new Cell(0, 0);

        // Act
        var result = cell.ToString();

        // Assert
        Assert.Equal("Cell(0, 0)", result);
    }
}

