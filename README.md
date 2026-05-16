# OuroLab

> **This is an experimentation / test bench project.** It is not meant to be polished, hosted, or deployed. The code is rough, APIs are unstable, and everything exists to serve as a playground for benchmarking statistical algorithms against board games.

---

> **Disclaimer:** This is a **fan project** and does **NOT** involve the Mudae team or the Mudae Discord bot in any way, shape, or form. This project is not endorsed by, affiliated with, or connected to Mudae or its creators.
>
> **Note:** All games (OuroChest, OuroQuest) were originally created by the Mudae bot team.

---

## What is this?

OuroLab is a C# benchmarking framework for evaluating statistical algorithms that solve board games. It ships with two games — **OuroChest** and **OuroQuest** — and eight solvers of varying complexity, from a random baseline to a full expectimax game-tree search with alpha pruning.

The goal: run thousands of iterations in parallel, measure which solver finds the target most reliably, and see how different strategies trade off speed vs. accuracy.

## Table of Contents

- [OuroChest Game Rules](#ourochest-game-rules)
- [OuroQuest Game Rules](#ouroquest-game-rules)
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

## OuroQuest Game Rules

OuroQuest is a **Minesweeper-inspired** puzzle played on a **5x5 grid** with **4 hidden Purple spheres** ("mines") and **7 clicks**.

### Board Generation

4 cells are randomly designated **Purple**. Every other cell's color is determined by its count of Purple neighbors (8-directional adjacency):

| Sphere  | Rule                                    | Points |
|---------|-----------------------------------------|--------|
| Purple  | The hidden "mines" (4 placed randomly) | 11     |
| Blue    | 0 Purple neighbors                      | 12     |
| Teal    | 1 Purple neighbor                       | 26     |
| Green   | 2 Purple neighbors                      | 41     |
| Yellow  | 3 Purple neighbors                      | 61     |
| Orange  | 4+ Purple neighbors                     | 96     |
| Red     | Auto-revealed after finding 3 Purples   | 156    |

### Gameplay

- **Goal:** Find 3 Purple spheres to auto-reveal Red, then click Red to collect it.
- Revealing a **Purple** sphere **does not consume a click** — it's free.
- Revealing any other sphere (including Red) **costs one click**.
- The game ends when all clicks are consumed.
- The neighbor counts give Minesweeper-like clues about where Purples might be.

### Probability Model

The game enumerates all valid Purple placement combinations consistent with revealed constraints using bitmask-based neighbor masks. Each valid combination contributes to a frequency-weighted probability distribution for every unrevealed cell.

## Solvers

### Random

Picks cells uniformly at random. Serves as the baseline.

### Greedy EV

Each turn, picks the unrevealed cell with the highest **expected value** — the probability-weighted sum of all possible sphere values. No lookahead.

### Greedy EV Lookahead (depth 1 / depth 2)

Extends Greedy EV with a limited game-tree search. At each step, branches on all possible outcomes for each candidate cell, evaluates the resulting state with pure Greedy EV, and picks the cell with the highest expected future score. Depth 1 looks one step ahead; depth 2 looks two steps ahead.

### Goal Hunter

Restricts candidates to cells where the **goal sphere (Red)** appears in the probability distribution. Among those, picks by highest weighted EV. Falls back to pure greedy EV if no such cells exist.

### Info Gain

Minimizes `sum(P(non-goal)^2)` — the expected number of remaining Red candidates after a reveal. Picks cells that most evenly partition the hypothesis space. After finding Red, switches to greedy EV.

### Cached Expectimax

Full game-tree search (depth 3) with a **static transposition table** (`ConcurrentDictionary`) that caches search results across benchmark iterations. Uses **alpha pruning** at max nodes and orders candidates by weighted EV for early pruning. The cache is valid because the hypothetical search state depends only on which cells are revealed and their sphere types — not on the underlying board. This is a **benchmarking optimization only**, not suitable for standalone use.

## Benchmark Results

### OuroChest

10,000 iterations (lookahead solvers: 1,000; Cached Expectimax: 100), 5x5 board, 5 clicks, 8 threads:

| Solver             | Goal Hit Rate | Avg Score | Avg Efficiency | Avg Time   |
|--------------------|---------------|-----------|----------------|------------|
| Random             | 19.9%         | 190.0     | 41.3%          | 0.01ms     |
| Greedy EV          | 98.3%         | 352.5     | 76.6%          | 0.88ms     |
| Info Gain          | 100.0%        | 355.3     | 77.2%          | 0.73ms     |
| Goal Hunter        | 99.9%         | 352.9     | 76.7%          | 0.72ms     |
| Greedy EV-1        | 99.2%         | 358.6     | 78.0%          | 11.92ms    |
| Greedy EV-2        | 99.7%         | 361.1     | 78.5%          | 356.45ms   |
| CachedExpectimax-3 | 100.0%        | 359.0     | 78.0%          | 40.38ms    |

### OuroQuest

10,000 iterations (lookahead solvers: 1,000 / 100; Cached Expectimax: 100), 5x5 board, 7 clicks, 4 Purples, 8 threads:

| Solver             | Goal Hit Rate | Avg Score | Avg Efficiency | Avg Time    |
|--------------------|---------------|-----------|----------------|-------------|
| Random             | 1.6%          | 197.6     | 45.3%          | 0.01ms      |
| Greedy EV          | 3.7%          | 273.6     | 62.2%          | 12.10ms     |
| Info Gain          | 25.5%         | 264.4     | 60.3%          | 11.90ms     |
| Goal Hunter        | 4.2%          | 274.3     | 62.3%          | 11.92ms     |
| Greedy EV-1        | 65.7%         | 359.2     | 82.0%          | 249.98ms    |
| Greedy EV-2        | 87.0%         | 395.4     | 90.2%          | 6,557.81ms  |
| CachedExpectimax-3 | 21.0%         | 297.8     | 67.9%          | 453.59ms    |

## CLI Usage

### benchmark

Run solver benchmarks with configurable iterations and parallelism:

```bash
# Interactive (prompts for game and solver)
ourolab benchmark

# Specific game and solver
ourolab benchmark --game ouro-chest --solver greedy-ev

# Multiple games/solvers
ourolab benchmark -g ouro-chest ouro-quest -s greedy-ev goal-hunter greedy-ev-1

# With options
ourolab benchmark -g ouro-chest -s cached-expectimax -n 100 -t 4
```

| Option            | Default | Description                        |
|-------------------|---------|------------------------------------|
| `-g, --game`      | prompt  | Game(s) to benchmark (space-sep)   |
| `-s, --solver`    | prompt  | Solver(s) to benchmark (space-sep) |
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

### play

Interactive play mode — manually select cells to reveal by row/column coordinates:

```bash
ourolab play --game ouro-chest
```

| Option      | Default | Description                    |
|-------------|---------|--------------------------------|
| `-g, --game`| prompt  | Game to play                   |

### test

Interactive grid editor to set up a custom board, then run selected solvers on it once each with a pass/okay/fail verdict:

```bash
ourolab test --game ouro-chest --solver greedy-ev goal-hunter
```

| Option      | Default | Description                       |
|-------------|---------|-----------------------------------|
| `-g, --game`| prompt  | Game to test on                   |
| `-s, --solver`| prompt | Solver(s) to test (space-sep)    |

## Project Structure

```
src/
├── Core/           # Shared abstractions: IGame, ISolver, Board, Benchmark engine
├── Chest/          # OuroChest game: board generation, probability model, value converter
├── Quest/          # OuroQuest game: Minesweeper-like board, bitmask probability engine
├── Solvers/        # All solver implementations
└── Cli/            # CLI entry point, command definitions, game/solver registry
```

**Dependency graph:**

```
Core (leaf — no dependencies)
├── Chest      (game implementation)
├── Quest      (game implementation)
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

```bash
Registry.RegisterGame("my-game", () => new MyGame(), "solver-a", "solver-b");
```

### Adding a new solver

1. Create a class implementing `ISolver` in `src/Solvers/`.
2. Register in `Program.cs`:

```bash
Registry.RegisterSolver("my-solver", () => new MySolver());
```

3. Add the solver name to the compatible solvers list for any games it supports.
