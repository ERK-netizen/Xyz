---
name: game-balance
description: >
  Balances a deterministic grand-strategy simulation by measurement: target bands per metric,
  multi-seed headless sweeps, one-knob sensitivity runs, economy faucet/sink ledgers, combat
  sanity curves and snowball checks, with tunables kept in content data. Use for "this nation is
  too strong", runaway economies, wars that never end or always end on day 3, or before locking
  numbers for a release. Not for frame rate (`performance-optimization`) or AI decision code
  (`ai-behavior-trees-utility-ai`, `game-ai`).
---

# Game Balance (deterministic strategy simulation)

Project-local skill for Tarih Strateji. It is **not** part of the upstream
`awesome-gamedev-agent-skills` catalog (that repository has no balance skill); it was written
for this repository's constraints: `Fixed64` arithmetic, a deterministic PRNG, a single
`SimulationKernel` and a headless .NET test runner.

## When to use

- A country, unit type, building or technology dominates (it is picked in almost every AI run,
  or it wins almost every war it is in).
- An economy runs away (treasury, stockpile or population grows without bound) or collapses.
- Wars end too fast, never end, or casualties are unrealistic for the era.
- Before freezing content numbers for a milestone, and after any change to a formula.

**When not to use:** a slow frame or hitch → `performance-optimization`; the AI picks a bad
action although the numbers are fine → `ai-behavior-trees-utility-ai`; a save or replay that
diverges → that is a determinism bug, not balance: fix it first (golden-hash tests).

## Principles

1. **Balance with bands, not points.** Every metric gets a target range with a reason
   ("1914–1918 total war: 3–6 years for a great-power war to end"). A single number invites
   over-fitting to one seed.
2. **Many seeds, same build.** A conclusion from one seed is an anecdote. Use at least 32 seeds
   for a decision and report median, 10th and 90th percentile.
3. **One knob at a time.** Change one tunable, rerun the same seed set, compare distributions.
   Two simultaneous changes make the result unexplainable.
4. **Numbers live in content, not code.** Rates, costs and multipliers are `Fixed64` values in
   content definitions (`Core.Content`), so a balance pass is a data diff that tests can pin.
5. **Same rules for the AI.** Never balance by giving the AI hidden bonuses in Normal mode; it
   hides broken numbers and breaks the fog-of-war rule (CLAUDE.md §2.6).
6. **Determinism first.** If the same seed gives two results, stop: balance data from a
   non-deterministic run is noise.

## Workflow

1. **Name the symptom as a metric** — e.g. "Country X controls > 40 % of regions by day 365".
2. **Write the band** for it in `Docs/` (or the feature note) with its historical or design
   reason. Add a lower and an upper bound.
3. **Instrument**: record the metric per day in the headless simulation (a read-only observer
   over `IGameReadModel` or a per-day snapshot), never by adding state to rules code.
4. **Sweep**: run N seeds × D days (default 32 × 365) in the headless test project, collect
   percentiles. Keep the seed list fixed in the test so runs are comparable.
5. **Locate the cause** with a sensitivity run: vary one tunable by −20 %, −10 %, +10 %, +20 %
   and plot the metric. The steepest curve is the lever.
6. **Change data**, rerun the same sweep, and check that *other* metrics stayed in their bands
   (balance changes leak).
7. **Gate it**: add or update a band test so CI fails when a later change pushes the metric
   out of range. Keep these tests separate from exact golden-hash determinism tests.

## Patterns

### 1. Metric band table (document next to the feature)

| Metric | Band (P10–P90) | Reason |
|--------|----------------|--------|
| Days until first great-power war ends | 700–2200 | Era: attritional wars of 2–6 years |
| Treasury after 365 days / starting treasury | 0.6–2.5 | Neither bankrupt nor runaway |
| Share of regions held by the largest country, day 365 | ≤ 0.25 | No early snowball |
| Mobilised share of working-age cohort in total war | 0.08–0.20 | Historical mobilisation range |

### 2. Daily vs. yearly rates

Rates compound per tick, and a tick is a day. Convert explicitly and test the conversion:
`daily = yearly^(1/365)`. A "small" daily growth of `1.0137` is ≈ 140× per year. Store yearly
values in content where designers think in years and derive the daily factor once at load time
with `Fixed64` arithmetic, then pin the derived value in a test.

### 3. Faucet / sink ledger

For each resource, sum per day what enters (production, trade income, loot) and what leaves
(upkeep, construction, consumption, losses). A healthy economy has sinks that **scale with
size** (upkeep per unit, administrative cost per region). If every sink is flat while faucets
scale, the economy runs away; report the ledger ratio `sinks / faucets` per country per year.

### 4. Combat sanity curves

With the single `CombatResolver`, run fixed matchups (equal forces, 2:1, 3:1, entrenched
defender, river crossing, no supply) over many seeds and check outcome rates against bands:
equal forces ≈ 40–60 % attacker success, 3:1 against entrenched ≈ 50–70 %, no supply → steadily
falling strength. A flat curve means modifiers do not matter; a cliff means one modifier
dominates.

### 5. Snowball check

Plot "share of regions held by the leader" over time across seeds. A curve that keeps
accelerating needs counter-forces that grow with size: war exhaustion, overextension, unrest in
occupied regions, coalition behaviour in diplomacy. Add the counter-force as a rule, then tune
its strength with a sensitivity sweep.

## Gotchas

- **Tuning on one seed.** The fix works on seed 1 and breaks seeds 2–32. Always sweep.
- **Two knobs per change.** You cannot attribute the effect; revert one.
- **Hidden AI bonuses.** They mask broken numbers and violate the AI's fog-of-war rule.
- **Magic numbers in code.** A constant in a system class escapes the balance pass and the
  content tests. Move it to a definition.
- **Unit mismatch.** Daily vs yearly, per-cohort vs total, percent vs fraction (`25` vs
  `0.25`): pin each conversion with a test.
- **Rounding drift.** Many small `Fixed64` multiplications round each time; for ratios use
  `Fixed64.MulDiv` (single rounding) and check the ledger still balances to the last unit.
- **Balancing a non-deterministic build.** If replay hashes differ, balance data is noise.
- **Exact-number gates.** Balance tests check bands; exact values belong only in golden
  determinism tests, which change only on purpose.

## Related skills

- `ai-behavior-trees-utility-ai`: when the numbers are fine but AI choices are not.
- `game-ai`: pathfinding and perception that change how often fights happen.
- `performance-optimization`: when the 365-day headless sweep gets too slow to run in CI.
- `game-docs`: where the band tables and their reasons are written down.
