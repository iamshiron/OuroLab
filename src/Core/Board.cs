namespace Shiron.OuroLab.Core;

public sealed class Board {
    private readonly Sphere[] _cells;

    public int Rows { get; }
    public int Columns { get; }
    public int Size => _cells.Length;

    public Sphere this[int index] {
        get => _cells[index];
        set => _cells[index] = value;
    }

    public Sphere this[int row, int column] {
        get => this[row * Columns + column];
        set => this[row * Columns + column] = value;
    }

    public Board(int rows, int columns) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);

        Rows = rows;
        Columns = columns;
        _cells = new Sphere[rows * columns];
    }

    public static Board FromArray(Sphere[] cells, int rows, int columns) {
        ArgumentNullException.ThrowIfNull(cells);

        if (cells.Length != rows * columns)
            throw new ArgumentException("Array length must equal rows * columns.", nameof(cells));

        return new Board(cells, rows, columns);
    }

    private Board(Sphere[] cells, int rows, int columns) {
        _cells = cells;
        Rows = rows;
        Columns = columns;
    }

    public int ToIndex(int row, int column) => row * Columns + column;

    public (int Row, int Column) ToPosition(int index) {
        var row = Math.DivRem(index, Columns, out var column);
        return (row, column);
    }

    public ReadOnlySpan<Sphere> AsSpan() => _cells.AsSpan();

    public Sphere[] ToArray() => [.. _cells];

    public void Clear() => Array.Clear(_cells);
}
