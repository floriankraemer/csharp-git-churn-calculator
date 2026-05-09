namespace GitChurnCalculator.Models;

/// <summary>
/// Cumulative insertions/deletions aggregated from <c>git log --numstat</c> over a bounded history window.
/// </summary>
public readonly record struct LineChangeTotals(int Added, int Removed);
