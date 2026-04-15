# How churn is calculated

This document describes how **Git Churn Calculator** derives file metrics and the **Churn Risk Score**. For running the tool, see the [README](../README.md).

The CLI reports several git-derived metrics plus a **Churn Risk Score**. Only tracked files with at least one commit in history are included (`TotalCommits > 0`).

## Inputs from git

- **TotalCommits** — how many times the file appears across `git log` (each commit that lists the file counts once).
- **FirstCommitDate** / **LastCommitDate** — timestamps from the log for that file’s first and latest appearance.
- **TotalUniqueAuthors** — count of distinct author emails that touched the file over all time (other author columns use the same idea over rolling windows).

## Change frequency (velocity)

Let **now** be the analysis time (UTC), **first** the file’s first commit date, and:

```math
\text{AgeDays} = \max\bigl(1,\ \text{whole days from first to now}\bigr)
```

Average commit rates use fixed day-length denominators (same as the implementation):

```math
\text{ChangesPerWeek} = \frac{\text{TotalCommits}}{\text{AgeDays} / 7}
\qquad
\text{ChangesPerMonth} = \frac{\text{TotalCommits}}{\text{AgeDays} / 30.44}
\qquad
\text{ChangesPerYear} = \frac{\text{TotalCommits}}{\text{AgeDays} / 365.25}
```

These values are rounded for display (two decimal places in output). The risk score uses the unrounded `ChangesPerWeek` internally, then the final score is rounded to four decimals.

## Churn Risk Score

Let $c = \text{ChangesPerWeek}$, $A = \text{TotalUniqueAuthors}$, and $p$ be line coverage in percent ($0 \le p \le 100$) when Cobertura data exists for that file.

**Without coverage** (no `--coverage` file): coverage is not applied and the risk multiplier is always $1$:

```math
\text{ChurnRiskScore} = c \times A
```

**With coverage** (`--coverage` and a mapped Cobertura class for that path):

```math
\text{ChurnRiskScore} = c \times A \times \left(1 - \frac{p}{100}\right)
```

The factor $\left(1 - \frac{p}{100}\right)$ is **higher when coverage is lower** (no coverage → multiplier $1$; full coverage → multiplier $0$, so the score is $0$ aside from rounding). If a Cobertura file is supplied but a git file is **not** matched to any class, it is treated as **$p = 0$** (same multiplier as “no tests” in the formula above).

| Symbol / field | Meaning |
|----------------|---------|
| $c$ (**ChangesPerWeek**) | Commit velocity over the file’s lifetime |
| $A$ (**TotalUniqueAuthors**) | How many different people have touched the file |
| $1 - p/100$ | Test-gap multiplier from line coverage (only when `--coverage` is used and the file maps into the report) |

## Time series mode

When `--series week` or `--series month` is used together with `--from` (and optionally `--to`), the tool runs the full analysis repeatedly — once per bucket end date — over the specified date range.

### How "as of" anchoring works

Each analysis point receives an **`AsOf`** date (the bucket end). The tool passes this date as `--until` to every `git log` query, so history is bounded: commits after `AsOf` are invisible to that point's analysis. This means:

- **`TotalCommits`**, **`FirstCommitDate`**, **`LastCommitDate`**, and **`TotalUniqueAuthors`** only count commits on or before `AsOf`.
- Rolling windows (`CommitsLast7Days`, `UniqueAuthorsLast30Days`, etc.) are calculated relative to `AsOf`, not the wall-clock time the tool is run.
- **`AgeDays`** is the span from a file's first commit (as of that date) to `AsOf`.

### Output shape

| Format | Shape |
|--------|-------|
| JSON | Array of `{ "asOf": "yyyy-MM-dd", "files": [...] }` objects, one per time point |
| CSV | Flat rows with a leading `AsOf` column; one row per `(asOf, file)` pair |
| HTML | Bootstrap page with one collapsible `<details>` section per time point |

### Performance

The tool executes **one full set of git queries per time point**. For a weekly series covering one year that is approximately 52 × 10 = 520 `git log` invocations. On large repositories or long date ranges this can take several minutes.

## Cobertura coverage mapping

The tool maps Cobertura XML `<class filename="...">` entries to git-tracked files using:

1. **Source prefix stripping** — uses `<source>` elements to turn absolute paths into relative ones
2. **Exact match** against git file paths
3. **Suffix match** — finds the git file whose path ends with the coverage filename
4. **Filename match** — falls back to matching just the filename portion
