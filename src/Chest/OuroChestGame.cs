using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Chest;

public sealed class OuroChestGame : IGame {
    private readonly int _maxClicks;
    private readonly Random _random;
    private readonly OuroChestValueConverter _valueConverter = new();

    private Board _board = null!;
    private bool[] _revealed = [];
    private int _revealedCount;
    private int _clicksConsumed;

    public string Name => "Ouro Chest";
    public int Rows { get; }
    public int Columns { get; }
    public IValueConverter ValueConverter => _valueConverter;
    public bool IsSolved => _clicksConsumed >= _maxClicks;
    public int RevealedCount => _revealedCount;
    public string? GoalDescription => "Find Red";
    public bool GoalAchieved => GoalDescription is not null && _revealed.Any(r => r) && WasSphereRevealed(Sphere.Red);

    public int TheoreticalMaxScore {
        get {
            var size = Rows * Columns;
            var values = new int[size];
            for (var i = 0; i < size; i++)
                values[i] = _valueConverter.GetValue(_board[i]);

            Array.Sort(values);
            var sum = 0;
            for (var i = size - 1; i >= Math.Max(0, size - _maxClicks); i--)
                sum += values[i];

            return sum;
        }
    }

    public int Score {
        get {
            var score = 0;
            for (var i = 0; i < _revealed.Length; i++) {
                if (_revealed[i])
                    score += _valueConverter.GetValue(_board[i]);
            }

            return score;
        }
    }

    public OuroChestGame(int rows = 5, int columns = 5, int maxClicks = 5, int? seed = null) {
        Rows = rows;
        Columns = columns;
        _maxClicks = maxClicks;
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        NewGame();
    }

    public void NewGame() {
        _board = GenerateBoard();
        _revealed = new bool[Rows * Columns];
        _revealedCount = 0;
        _clicksConsumed = 0;
    }

    public IReadOnlySet<Sphere> GetPossibleSpheres(int index) {
        ValidateIndex(index);

        if (_revealed[index])
            return new HashSet<Sphere> { _board[index] };

        var possible = new HashSet<Sphere>();
        var size = Rows * Columns;
        var center = Rows / 2 * Columns + Columns / 2;

        for (var rp = 0; rp < size; rp++) {
            if (rp == center) continue;
            if (!IsRedPositionConsistent(rp)) continue;

            var (row, col) = _board.ToPosition(index);
            var (redRow, redCol) = _board.ToPosition(rp);

            if (index == rp) {
                possible.Add(Sphere.Red);
            } else if (IsCardinallyAdjacent(row, col, redRow, redCol)) {
                possible.Add(Sphere.Orange);
                possible.Add(Sphere.Green);
                possible.Add(Sphere.Teal);
            } else if (IsDiagonal(row, col, redRow, redCol)) {
                possible.Add(Sphere.Yellow);
                possible.Add(Sphere.Teal);
            } else if (IsInSameRowOrColumn(row, col, redRow, redCol)) {
                possible.Add(Sphere.Green);
                possible.Add(Sphere.Teal);
            } else {
                possible.Add(Sphere.Blue);
            }
        }

        return possible;
    }

    public Sphere Reveal(int index) {
        ValidateIndex(index);
        if (_revealed[index])
            return _board[index];

        _revealed[index] = true;
        _revealedCount++;
        if (ConsumeClick(index))
            _clicksConsumed++;
        return _board[index];
    }

    public bool IsRevealed(int index) {
        ValidateIndex(index);
        return _revealed[index];
    }

    public Sphere GetRevealedSphere(int index) {
        ValidateIndex(index);
        if (!_revealed[index])
            throw new InvalidOperationException($"Cell {index} is not revealed.");
        return _board[index];
    }

    public bool ConsumeClick(int index) => true;

    private Board GenerateBoard() {
        var board = new Board(Rows, Columns);
        var size = Rows * Columns;
        var center = Rows / 2 * Columns + Columns / 2;

        var candidates = new List<int>();
        for (var i = 0; i < size; i++) {
            if (i != center) candidates.Add(i);
        }

        var redIndex = candidates[_random.Next(candidates.Count)];
        var (redRow, redCol) = board.ToPosition(redIndex);
        board[redIndex] = Sphere.Red;

        var adjacent = GetCardinallyAdjacent(redRow, redCol);
        Shuffle(adjacent);
        var orangeCount = Math.Min(2, adjacent.Count);
        for (var i = 0; i < orangeCount; i++)
            board[adjacent[i].Row, adjacent[i].Col] = Sphere.Orange;

        var diagonal = GetDiagonalCells(redRow, redCol);
        Shuffle(diagonal);
        var yellowCount = Math.Min(3, diagonal.Count);
        for (var i = 0; i < yellowCount; i++)
            board[diagonal[i].Row, diagonal[i].Col] = Sphere.Yellow;

        var rowCol = GetSameRowOrColumnCells(redRow, redCol);
        var available = rowCol
            .Where(p => board[p.Row, p.Col] == Sphere.Purple)
            .ToList();
        Shuffle(available);
        var greenCount = Math.Min(4, available.Count);
        for (var i = 0; i < greenCount; i++)
            board[available[i].Row, available[i].Col] = Sphere.Green;

        for (var i = 0; i < size; i++) {
            if (board[i] != Sphere.Purple) continue;
            var (row, col) = board.ToPosition(i);
            board[i] = IsInLine(row, col, redRow, redCol) ? Sphere.Teal : Sphere.Blue;
        }

        return board;
    }

    private bool IsRedPositionConsistent(int redPos) {
        if (_revealed[redPos] && _board[redPos] != Sphere.Red)
            return false;

        var (redRow, redCol) = _board.ToPosition(redPos);

        for (var i = 0; i < _revealed.Length; i++) {
            if (!_revealed[i]) continue;

            var (row, col) = _board.ToPosition(i);
            var sphere = _board[i];

            switch (sphere) {
                case Sphere.Red when i != redPos: return false;
                case Sphere.Orange when !IsCardinallyAdjacent(row, col, redRow, redCol): return false;
                case Sphere.Yellow when !IsDiagonal(row, col, redRow, redCol): return false;
                case Sphere.Green when !IsInSameRowOrColumn(row, col, redRow, redCol): return false;
                case Sphere.Teal when !IsInLine(row, col, redRow, redCol): return false;
                case Sphere.Blue when IsInLine(row, col, redRow, redCol): return false;
            }
        }

        return true;
    }

    private void ValidateIndex(int index) {
        if (index < 0 || index >= Rows * Columns)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    private bool WasSphereRevealed(Sphere sphere) {
        for (var i = 0; i < _revealed.Length; i++) {
            if (_revealed[i] && _board[i] == sphere)
                return true;
        }

        return false;
    }

    private List<(int Row, int Col)> GetCardinallyAdjacent(int row, int col) {
        var result = new List<(int, int)>(4);
        if (row > 0) result.Add((row - 1, col));
        if (row < Rows - 1) result.Add((row + 1, col));
        if (col > 0) result.Add((row, col - 1));
        if (col < Columns - 1) result.Add((row, col + 1));
        return result;
    }

    private List<(int Row, int Col)> GetDiagonalCells(int row, int col) {
        var result = new List<(int, int)>();
        for (var d = 1; ; d++) {
            var found = false;
            if (row - d >= 0 && col - d >= 0) { result.Add((row - d, col - d)); found = true; }
            if (row - d >= 0 && col + d < Columns) { result.Add((row - d, col + d)); found = true; }
            if (row + d < Rows && col - d >= 0) { result.Add((row + d, col - d)); found = true; }
            if (row + d < Rows && col + d < Columns) { result.Add((row + d, col + d)); found = true; }
            if (!found) break;
        }

        return result;
    }

    private List<(int Row, int Col)> GetSameRowOrColumnCells(int row, int col) {
        var result = new List<(int, int)>();
        for (var c = 0; c < Columns; c++)
            if (c != col) result.Add((row, c));
        for (var r = 0; r < Rows; r++)
            if (r != row) result.Add((r, col));
        return result;
    }

    private static bool IsCardinallyAdjacent(int r1, int c1, int r2, int c2)
        => (r1 == r2 && Math.Abs(c1 - c2) == 1) || (c1 == c2 && Math.Abs(r1 - r2) == 1);

    private static bool IsDiagonal(int r1, int c1, int r2, int c2)
        => r1 != r2 && c1 != c2 && Math.Abs(r1 - r2) == Math.Abs(c1 - c2);

    private static bool IsInSameRowOrColumn(int r1, int c1, int r2, int c2)
        => r1 == r2 || c1 == c2;

    private static bool IsInLine(int r1, int c1, int r2, int c2)
        => r1 == r2 || c1 == c2 || Math.Abs(r1 - r2) == Math.Abs(c1 - c2);

    private void Shuffle<T>(List<T> list) {
        for (var i = list.Count - 1; i > 0; i--) {
            var j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
