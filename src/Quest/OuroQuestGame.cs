using System.Numerics;
using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Quest;

public sealed class OuroQuestGame : IGame {
    private readonly int _purpleCount;
    private readonly int _maxClicks;
    private readonly Random _random;
    private readonly OuroQuestValueConverter _valueConverter = new();

    private Board _board = null!;
    private bool[] _revealed = [];
    private int _revealedCount;
    private int _clicksConsumed;
    private int _revealedPurples;
    private bool _redCollected;
    private int _autoRevealedRedIndex = -1;
    private HashSet<int> _purpleIndices = [];
    private ulong[] _neighborMasks = [];
    private Dictionary<int, IReadOnlyDictionary<Sphere, double>>? _probabilityCache;

    public string Name => "Ouro Quest";
    public int Rows { get; }
    public int Columns { get; }
    public int MaxClicks => _maxClicks;
    public IValueConverter ValueConverter => _valueConverter;
    public int RevealedCount => _revealedCount;

    public bool IsSolved => _clicksConsumed >= _maxClicks;

    public string? GoalDescription => "Find 3 purple spheres to reveal the Red";
    public Sphere? GoalSphere => Sphere.Red;
    public bool GoalAchieved => _redCollected;

    public int TheoreticalMaxScore {
        get {
            var nonPurpleValues = new List<int>();
            for (var i = 0; i < Rows * Columns; i++) {
                if (!_purpleIndices.Contains(i))
                    nonPurpleValues.Add(_valueConverter.GetValue(_board[i]));
            }

            nonPurpleValues.Sort();
            var sum = (_purpleCount - 1) * _valueConverter.GetValue(Sphere.Purple);
            sum += _valueConverter.GetValue(Sphere.Red);

            var clicksRemaining = _maxClicks - 1;
            for (var i = nonPurpleValues.Count - 1; i >= Math.Max(0, nonPurpleValues.Count - clicksRemaining); i--)
                sum += nonPurpleValues[i];

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

    public OuroQuestGame(int rows = 5, int columns = 5, int purpleCount = 4, int maxClicks = 7, int? seed = null) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfLessThan(purpleCount, 2);

        if (rows * columns < purpleCount + 1)
            throw new ArgumentException("Board too small for the given purple count.", nameof(rows));

        Rows = rows;
        Columns = columns;
        _purpleCount = purpleCount;
        _maxClicks = maxClicks;
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        ComputeNeighborMasks();
        NewGame();
    }

    public void NewGame() {
        _board = GenerateBoard();
        _revealed = new bool[Rows * Columns];
        _revealedCount = 0;
        _clicksConsumed = 0;
        _revealedPurples = 0;
        _redCollected = false;
        _autoRevealedRedIndex = -1;
        _probabilityCache = null;
    }

    public IReadOnlyDictionary<Sphere, double> GetPossibleSpheres(int index) {
        ValidateIndex(index);

        if (_revealed[index])
            return new Dictionary<Sphere, double> { { _board[index], 1.0 } };

        if (_autoRevealedRedIndex >= 0 && index == _autoRevealedRedIndex)
            return new Dictionary<Sphere, double> { { Sphere.Red, 1.0 } };

        _probabilityCache ??= ComputeAllProbabilities();

        return _probabilityCache.GetValueOrDefault(index, new Dictionary<Sphere, double>());
    }

    public Sphere Reveal(int index) {
        ValidateIndex(index);

        if (index == _autoRevealedRedIndex) {
            _revealed[index] = true;
            _revealedCount++;
            _clicksConsumed++;
            _autoRevealedRedIndex = -1;
            _redCollected = true;
            _probabilityCache = null;
            return _board[index];
        }

        if (_revealed[index])
            return _board[index];

        var sphere = _board[index];
        _revealed[index] = true;
        _revealedCount++;
        _probabilityCache = null;

        if (sphere == Sphere.Purple) {
            _revealedPurples++;
            if (_revealedPurples >= _purpleCount - 1 && _autoRevealedRedIndex < 0 && !_redCollected)
                AutoShowRed();
        } else {
            _clicksConsumed++;
        }

        return _board[index];
    }

    public bool IsRevealed(int index) {
        ValidateIndex(index);
        return _revealed[index];
    }

    public bool IsVisible(int index) {
        ValidateIndex(index);
        return _revealed[index] || index == _autoRevealedRedIndex;
    }

    public Sphere GetRevealedSphere(int index) {
        ValidateIndex(index);
        if (index == _autoRevealedRedIndex)
            return _board[index];
        if (!_revealed[index])
            throw new InvalidOperationException($"Cell {index} is not revealed.");
        return _board[index];
    }

    public bool ConsumeClick(int index) => _board[index] != Sphere.Purple;

    public Sphere PeekSphere(int row, int col) => _board[row, col];

    public IGame Fork() => new OuroQuestGame(this);

    public void ApplyHypothetical(int index, Sphere sphere) {
        _revealed[index] = true;
        _board[index] = sphere;
        _revealedCount++;
        _probabilityCache = null;

        if (sphere == Sphere.Purple) {
            _revealedPurples++;
            if (_revealedPurples >= _purpleCount - 1 && _autoRevealedRedIndex < 0 && !_redCollected) {
                foreach (var idx in _purpleIndices) {
                    if (!_revealed[idx]) {
                        _board[idx] = Sphere.Red;
                        _autoRevealedRedIndex = idx;
                        break;
                    }
                }
            }
        } else if (sphere == Sphere.Red && index == _autoRevealedRedIndex) {
            _autoRevealedRedIndex = -1;
            _redCollected = true;
            _clicksConsumed++;
        } else {
            _clicksConsumed++;
        }
    }

    private OuroQuestGame(OuroQuestGame other) {
        Rows = other.Rows;
        Columns = other.Columns;
        _purpleCount = other._purpleCount;
        _maxClicks = other._maxClicks;
        _random = other._random;
        _valueConverter = other._valueConverter;
        _board = Board.FromArray(other._board.ToArray(), other.Rows, other.Columns);
        _revealed = (bool[]) other._revealed.Clone();
        _revealedCount = other._revealedCount;
        _clicksConsumed = other._clicksConsumed;
        _revealedPurples = other._revealedPurples;
        _redCollected = other._redCollected;
        _autoRevealedRedIndex = other._autoRevealedRedIndex;
        _purpleIndices = new HashSet<int>(other._purpleIndices);
        _neighborMasks = other._neighborMasks;
    }

    private void ComputeNeighborMasks() {
        var size = Rows * Columns;
        _neighborMasks = new ulong[size];
        for (var i = 0; i < size; i++) {
            var row = i / Columns;
            var col = i % Columns;
            ulong mask = 0;
            for (var dr = -1; dr <= 1; dr++) {
                for (var dc = -1; dc <= 1; dc++) {
                    if (dr == 0 && dc == 0) continue;
                    var nr = row + dr;
                    var nc = col + dc;
                    if (nr >= 0 && nr < Rows && nc >= 0 && nc < Columns)
                        mask |= 1UL << (nr * Columns + nc);
                }
            }

            _neighborMasks[i] = mask;
        }
    }

    private Board GenerateBoard() {
        var board = new Board(Rows, Columns);
        var size = Rows * Columns;

        var indices = Enumerable.Range(0, size).ToList();
        Shuffle(indices);

        _purpleIndices = new HashSet<int>(indices.Take(_purpleCount));
        foreach (var idx in _purpleIndices)
            board[idx] = Sphere.Purple;

        for (var i = 0; i < size; i++) {
            if (_purpleIndices.Contains(i)) continue;
            var (row, col) = board.ToPosition(i);
            var count = CountPurpleNeighbors(row, col, _purpleIndices);
            board[i] = CountToSphere(count);
        }

        return board;
    }

    private void AutoShowRed() {
        foreach (var idx in _purpleIndices) {
            if (!_revealed[idx]) {
                _board[idx] = Sphere.Red;
                _autoRevealedRedIndex = idx;
                return;
            }
        }
    }

    private Dictionary<int, IReadOnlyDictionary<Sphere, double>> ComputeAllProbabilities() {
        var size = Rows * Columns;
        var result = new Dictionary<int, IReadOnlyDictionary<Sphere, double>>();

        ulong knownPurpleMask = 0;
        for (var i = 0; i < size; i++) {
            if (!_revealed[i]) continue;
            if (_board[i] == Sphere.Purple) knownPurpleMask |= 1UL << i;
        }

        if (_autoRevealedRedIndex >= 0)
            knownPurpleMask |= 1UL << _autoRevealedRedIndex;

        var remainingPurples = _purpleCount - BitOperations.PopCount(knownPurpleMask);

        var targetCells = new List<int>();
        for (var i = 0; i < size; i++) {
            if (_revealed[i]) continue;
            if (_autoRevealedRedIndex >= 0 && i == _autoRevealedRedIndex) continue;
            targetCells.Add(i);
        }

        if (remainingPurples == 0) {
            foreach (var idx in targetCells) {
                var sphere = ClassifyCellMask(idx, knownPurpleMask);
                result[idx] = new Dictionary<Sphere, double> { { sphere, 1.0 } };
            }

            return result;
        }

        var constraints = new List<(ulong NeighborMask, int Expected)>();
        for (var i = 0; i < size; i++) {
            if (!_revealed[i]) continue;
            var expected = SphereToCount(_board[i]);
            if (expected < 0) continue;
            constraints.Add((_neighborMasks[i], expected));
        }

        ulong eliminatedMask = 0;
        foreach (var (neighborMask, expected) in constraints) {
            if (expected == 0)
                eliminatedMask |= neighborMask;
        }

        var candidateBits = new List<int>();
        for (var i = 0; i < size; i++) {
            if (_revealed[i]) continue;
            if (_autoRevealedRedIndex >= 0 && i == _autoRevealedRedIndex) continue;
            if ((eliminatedMask & (1UL << i)) != 0) continue;
            candidateBits.Add(i);
        }

        if (candidateBits.Count < remainingPurples)
            return result;

        var targetNeighborMasks = new ulong[targetCells.Count];
        for (var t = 0; t < targetCells.Count; t++)
            targetNeighborMasks[t] = _neighborMasks[targetCells[t]];

        var targetBitMasks = new ulong[targetCells.Count];
        for (var t = 0; t < targetCells.Count; t++)
            targetBitMasks[t] = 1UL << targetCells[t];

        var tallies = new Dictionary<Sphere, int>[targetCells.Count];
        var total = 0;

        ForEachCombination(candidateBits.Count, remainingPurples, comboIndices => {
            ulong purpleMask = knownPurpleMask;
            for (var j = 0; j < remainingPurples; j++)
                purpleMask |= 1UL << candidateBits[comboIndices[j]];

            for (var c = 0; c < constraints.Count; c++) {
                if (BitOperations.PopCount(constraints[c].NeighborMask & purpleMask) != constraints[c].Expected)
                    return;
            }

            for (var t = 0; t < targetCells.Count; t++) {
                var sphere = (purpleMask & targetBitMasks[t]) != 0
                    ? Sphere.Purple
                    : CountToSphere(BitOperations.PopCount(targetNeighborMasks[t] & purpleMask));

                tallies[t] ??= new Dictionary<Sphere, int>();
                tallies[t].TryGetValue(sphere, out var count);
                tallies[t][sphere] = count + 1;
            }

            total++;
        });

        if (total == 0) return result;

        foreach (var t in tallies) {
            if (t == null) continue;
            var idx = targetCells[tallies.IndexOf(t)];
            var dist = new Dictionary<Sphere, double>(t.Count);
            foreach (var (sphere, count) in t)
                dist[sphere] = (double) count / total;
            result[idx] = dist;
        }

        return result;
    }

    private static void ForEachCombination(int n, int k, Action<Span<int>> action) {
        if (k == 0) {
            action(Span<int>.Empty);
            return;
        }

        if (n < k) return;

        Span<int> indices = stackalloc int[k];
        for (var i = 0; i < k; i++) indices[i] = i;

        action(indices);

        while (true) {
            int i;
            for (i = k - 1; i >= 0; i--) {
                if (indices[i] != i + n - k) break;
            }

            if (i < 0) break;

            indices[i]++;
            for (var j = i + 1; j < k; j++)
                indices[j] = indices[j - 1] + 1;

            action(indices);
        }
    }

    private Sphere ClassifyCellMask(int index, ulong purpleMask) {
        if ((purpleMask & (1UL << index)) != 0)
            return Sphere.Purple;
        return CountToSphere(BitOperations.PopCount(_neighborMasks[index] & purpleMask));
    }

    private int CountPurpleNeighbors(int row, int col, HashSet<int> purpleSet) {
        var count = 0;
        for (var dr = -1; dr <= 1; dr++) {
            for (var dc = -1; dc <= 1; dc++) {
                if (dr == 0 && dc == 0) continue;
                var nr = row + dr;
                var nc = col + dc;
                if (nr >= 0 && nr < Rows && nc >= 0 && nc < Columns) {
                    if (purpleSet.Contains(nr * Columns + nc))
                        count++;
                }
            }
        }

        return count;
    }

    private static Sphere CountToSphere(int count) => count switch {
        0 => Sphere.Blue,
        1 => Sphere.Teal,
        2 => Sphere.Green,
        3 => Sphere.Yellow,
        _ => Sphere.Orange,
    };

    private static int SphereToCount(Sphere sphere) => sphere switch {
        Sphere.Blue => 0,
        Sphere.Teal => 1,
        Sphere.Green => 2,
        Sphere.Yellow => 3,
        Sphere.Orange => 4,
        _ => -1,
    };

    private void ValidateIndex(int index) {
        if (index < 0 || index >= Rows * Columns)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    private void Shuffle<T>(List<T> list) {
        for (var i = list.Count - 1; i > 0; i--) {
            var j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
