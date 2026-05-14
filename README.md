# OuroLab

> **This is an experimentation / test bench project.** It is not meant to be polished, hosted, or deployed. The code is rough, APIs are unstable, and everything exists to serve as a playground for benchmarking statistical algorithms against board games.

---

> **Disclaimer:** This is a **fan project** and does **NOT** involve the Mudae team or the Mudae Discord bot in any way, shape, or form. This project is not endorsed by, affiliated with, or connected to Mudae or its creators.

---

## What is this?

OuroLab is a C# benchmarking framework for evaluating statistical algorithms that solve board games. It ships with one game — **OuroChest** — and six solvers of varying complexity, from a random baseline to a full expectimax game-tree search with alpha pruning.

The goal: run thousands of iterations in parallel, measure which solver finds the target most reliably, and see how different strategies trade off speed vs. accuracy.

## Table of Contents

- [OuroChest Game Rules](#ourochest-game-rules)
- [Solvers](#solvers)
- [Benchmark Results](#benchmark-results)
- [CLI Usage](#cli-usage)
- [Project Structure](#project-structure)
- [Building](#building)
- [Extending](#extending)

## OuroChest Game Rules

OuroChest is played on a **5x5 grid**. You get **5 clicks** to reveal cells and maximize your score.

### Board Generation

One cell (never the center) is secretly designated **Red**. Every other cell's color is determined by its geometric relationship to Red:

| Sphere  | Placement Rule                                                  | Points |
|---------|----------------------------------------------------------------|--------|
| Red     | Exactly 1 cell, randomly placed (never center)                 | 154    |
| Orange  | Up to 2 cells cardinally adjacent to Red                       | 94     |
| Yellow  | Up to 3 cells on a diagonal from Red                           | 59     |
| Green   | Up to 4 cells in the same row or column as Red (not assigned)  | 39     |
| Teal    | Remaining cells "in line" with Red (same row/col/diagonal)     | 26     |
| Blue    | All remaining cells (not in line with Red)                      | 16     |

### Gameplay

- You don't know which cell is Red.
- Each click reveals one cell and its sphere type.
- The sphere type gives you geometric clues about where Red might be.
- **Goal:** Find Red. **Secondary goal:** Maximize total score.
- The **theoretical max score** is **460** (Red + 2 Orange + 2 Yellow).

### Probability Model

When you query an unrevealed cell, the game returns a probability distribution over possible spheres. This is computed by enumerating all positions where Red could still be consistent with every revealed cell, then classifying the target cell for each candidate Red position. The result is a frequency-weighted probability distribution — e.g., if Red could be at 10 positions and the cell is Blue in 7 of them, `P(Blue) = 0.7`.

## Solvers

### Random

Picks cells uniformly at random. Serves as the baseline.

### Greedy EV

Each turn, picks the unrevealed cell with the highest **expected value** — the probability-weighted sum of all possible sphere values. No lookahead.

### Goal Hunter

Restricts candidates to cells where the **goal sphere (Red)** appears in the probability distribution. Among those, picks by highest weighted EV. Falls back to pure greedy EV if no such cells exist.

### Info Gain

Minimizes `sum(P(non-goal)^2)` — the expected number of remaining Red candidates after a reveal. Picks cells that most evenly partition the hypothesis space. After finding Red, switches to greedy EV.

### Expectimax

Full game-tree search with configurable depth (default: 3). At each decision point:
1. For every unrevealed cell, branch on all possible outcomes weighted by probability.
2. Recursively evaluate the best expected future score from each resulting state.
3. Pick the cell that maximizes expected total score.

Uses **alpha pruning** at max nodes (skips cells whose upper bound can't beat the current best) and orders candidates by weighted EV for early pruning.

### Cached Expectimax

Identical to Expectimax, but with a **static transposition table** (`ConcurrentDictionary`) that caches search results across benchmark iterations. Valid because the hypothetical search state depends only on which cells are revealed and their sphere types — not on the underlying board. This is a **benchmarking optimization only**, not suitable for standalone use.

## Benchmark Results

10,000 iterations (Expectimax: 100 iterations), 5x5 board, 5 clicks, 8 threads:

| Solver             | Goal Hit Rate | Avg Score | Avg Efficiency | Avg Time |
|--------------------|---------------|-----------|----------------|----------|
| Random             | 19.7%         | 189.9     | 41.3%          | 0.01ms   |
| Greedy EV          | 97.1%         | 351.2     | 76.3%          | 0.92ms   |
| Info Gain          | 98.6%         | 340.2     | 74.0%          | 0.89ms   |
| Goal Hunter        | 99.9%         | 352.8     | 76.7%          | 0.60ms   |
| Expectimax-3       | 100.0%        | 368.0     | 80.0%          | ~1.7s    |
| CachedExpectimax-3 | 100.0%        | ~368      | ~80%           | ~69ms    |

## CLI Usage

### benchmark

Run solver benchmarks with configurable iterations and parallelism:

```bash
# Interactive (prompts for game and solver)
ourolab benchmark

# Specific game and solver
ourolab benchmark --game ouro-chest --solver greedy-ev

# Multiple games/solvers
ourolab benchmark -g ouro-chest -s greedy-ev,goal-hunter,expectimax

# With options
ourolab benchmark -g ouro-chest -s expectimax -n 100 -t 4
```

| Option            | Default | Description                        |
|-------------------|---------|------------------------------------|
| `-g, --game`      | prompt  | Game(s) to benchmark (comma-sep)   |
| `-s, --solver`    | prompt  | Solver(s) to benchmark (comma-sep) |
| `-n, --iterations`| 100     | Number of iterations               |
| `-t, --threads`   | 8       | Max degree of parallelism          |

Output is a Spectre.Console table with: Game, Solver, Iterations, Avg Score, Avg Efficiency, Best Score, Best Efficiency, Goal Hit Rate, Avg Time, Total Time.

### generate

Debug command that renders a generated board with colored sphere names and point values:

```bash
ourolab generate --game ouro-chest --seed 42
```

| Option      | Default | Description                    |
|-------------|---------|--------------------------------|
| `-g, --game`| prompt  | Game to generate a board for   |
| `-s, --seed`| random  | Seed for reproducible boards   |

## Project Structure

```
src/
├── Core/           # Shared abstractions: IGame, ISolver, Board, Benchmark engine
├── Chest/          # OuroChest game: board generation, probability model, value converter
├── Solvers/        # All solver implementations
└── Cli/            # CLI entry point, command definitions, game/solver registry
```

**Dependency graph:**

```
Core (leaf — no dependencies)
├── Chest      (game implementation)
├── Solvers    (algorithm implementations)
└── Cli        (orchestrator) → Spectre.Console.Cli
```

### Key Interfaces

- **`IGame`** — Game contract: board dimensions, reveal mechanics, probability queries, goal tracking, state cloning (`Fork`/`ApplyHypothetical`)
- **`ISolver`** — Solver contract: given a game, produce a `SolverResult`
- **`IValueConverter`** — Maps sphere types to numeric values
- **`Benchmark`** — Runs `Parallel.For` with configurable thread count, collects aggregate statistics

## Building

Requires .NET 10.0 SDK.

```bash
dotnet build
dotnet run --project src/Cli
```

## Extending

### Adding a new game

1. Create a new project under `src/` (or add to an existing one).
2. Implement `IGame` — provide board generation, `GetPossibleSpheres` (returns probability distributions), goal logic, `Fork()`, and `ApplyHypothetical()`.
3. Register in `Program.cs`:

```csharp
Registry.RegisterGame("my-game", () => new MyGame(), "solver-a", "solver-b");
```

### Adding a new solver

1. Create a class implementing `ISolver` in `src/Solvers/`.
2. Register in `Program.cs`:

```csharp
Registry.RegisterSolver("my-solver", () => new MySolver());
```

3. Add the solver name to the compatible solvers list for any games it supports.
