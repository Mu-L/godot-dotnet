using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Godot.UpgradeAssistant;

/// <summary>
/// Exporter implementation that writes the summary of the results to an HTML file.
/// </summary>
public sealed class HtmlExporter : IExporter
{
    /// <inheritdoc/>
    public async Task ExportAsync(Summary summary, string outputPath, CancellationToken cancellationToken = default)
    {
        using var stream = File.Open(outputPath, FileMode.Create);
        using var writer = new StreamWriter(stream);

        await WriteAsync(writer, """
            <!DOCTYPE html>
            <html>
            """, cancellationToken).ConfigureAwait(false);

        await WriteHeadAsync(writer, cancellationToken).ConfigureAwait(false);

        await WriteAsync(writer, """

                <body>
            """, cancellationToken).ConfigureAwait(false);

        await WriteBodyAsync(writer, summary, cancellationToken).ConfigureAwait(false);

        await WriteAsync(writer, """

                </body>
            </html>

            """, cancellationToken).ConfigureAwait(false);
    }

    // TODO: Use constant StringSyntaxAttribute.Html when added to the BCL.
    // https://github.com/dotnet/runtime/issues/76138
    private static Task WriteAsync(TextWriter writer, [StringSyntax("Html")] string value, CancellationToken cancellationToken = default)
    {
        return writer.WriteAsync(value.AsMemory(), cancellationToken);
    }

    private static async Task WriteHeadAsync(TextWriter writer, CancellationToken cancellationToken = default)
    {
        await WriteAsync(writer, $"""

                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width,minimum-scale=1,initial-scale=1,user-scalable=yes">
                    <title>Godot .NET Upgrade Assistant summary - {DateTime.Now}</title>

            """, cancellationToken).ConfigureAwait(false);
        await WriteStyleAsync(writer, cancellationToken).ConfigureAwait(false);
        await WriteAsync(writer, """

                </head>
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteStyleAsync(TextWriter writer, CancellationToken cancellationToken = default)
    {
        await WriteAsync(writer, "<style>", cancellationToken).ConfigureAwait(false);

        using var cssStream = typeof(HtmlExporter).Assembly.GetManifestResourceStream("Godot.UpgradeAssistant.Core.Exporters.style.css");
        Debug.Assert(cssStream is not null);

        byte[] buffer = new byte[2048];

        int bytesRead;
        while ((bytesRead = cssStream.Read(buffer)) > 0)
        {
            string value = Encoding.UTF8.GetString(buffer.AsSpan(0, bytesRead));
            await WriteAsync(writer, value, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, "</style>", cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteBodyAsync(TextWriter writer, Summary summary, CancellationToken cancellationToken = default)
    {
        await WriteHeaderAsync(writer, summary, cancellationToken).ConfigureAwait(false);

        await WriteAsync(writer, """

                    <div class="container">
            """, cancellationToken).ConfigureAwait(false);

        await WriteGeneralSummaryAsync(writer, summary, cancellationToken).ConfigureAwait(false);

        await WriteSummaryResultAsync(writer, summary, cancellationToken).ConfigureAwait(false);

        if (summary.Problems.Count > 0)
        {
            await WriteProblemsAsync(writer, summary, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, """

                    </div>
            """, cancellationToken).ConfigureAwait(false);

        await WriteFooterAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    private static Task WriteHeaderAsync(TextWriter writer, Summary summary, CancellationToken cancellationToken = default)
    {
        return WriteAsync(writer, $"""

                    <header class="head">
                        <div class="container">
                            <h1>Godot .NET Upgrade Assistant</h1>
                            <p class="head-subtitle">Upgrade summary for Godot {summary.TargetGodotVersion}</p>
                        </div>
                    </header>
            """, cancellationToken);
    }

    private static Task WriteFooterAsync(TextWriter writer, CancellationToken cancellationToken = default)
    {
        return WriteAsync(writer, """

                    <footer>
                        <div class="container">
                            <div class="columns">
                                <div class="col">
                                    <h2>Godot Engine</h2>
                                    <ul>
                                        <li><a href="https://godotengine.org">Website</a></li>
                                        <li><a href="https://docs.godotengine.org">Documentation</a></li>
                                        <li><a href="https://github.com/godotengine">Code Repository</a></li>
                                    </ul>
                                </div>
                            </div>
                            <hr/>
                            <div>
                                <p>
                                    &copy; 2026 Godot .NET Upgrade Assistant.<br/>
                                    <a href="https://github.com/godotengine/godot-dotnet">Tool source code on GitHub</a>
                                </p>
                            </div>
                        </div>
                    </footer>
            """, cancellationToken);
    }

    private static Task WriteGeneralSummaryAsync(TextWriter writer, Summary summary, CancellationToken cancellationToken = default)
    {
        int total = summary.Problems.Count;
        int unresolved = summary.Problems.Count(p => !p.HasFixApplied);
        int fixAvailable = summary.Problems.Count(p => p.HasFixAvailable);
        int fixApplied = summary.Problems.Count(p => p.HasFixApplied);

        return WriteAsync(writer, $"""

                        <p class="summary-date">Generated on {summary.TimeStamp}</p>
                        <div class="summary-stats">
                            <div class="stat-card">
                                <div class="stat-value">{total}</div>
                                <div class="stat-label">Problems Reported</div>
                            </div>
                            <div class="stat-card">
                                <div class="stat-value">{unresolved}</div>
                                <div class="stat-label">Unresolved</div>
                            </div>
                            <div class="stat-card">
                                <div class="stat-value">{fixAvailable}</div>
                                <div class="stat-label">Fixes Available</div>
                            </div>
                            <div class="stat-card">
                                <div class="stat-value">{fixApplied}</div>
                                <div class="stat-label">Fixes Applied</div>
                            </div>
                        </div>
            """, cancellationToken);
    }

    private static async Task WriteSummaryResultAsync(TextWriter writer, Summary summary, CancellationToken cancellationToken)
    {
        if (summary.Problems.Count == 0)
        {
            await WriteAsync(writer, $"""

                            <div class="summary-result success">No problems found. Your project is ready for Godot {summary.TargetGodotVersion}! 🎉</div>
                """, cancellationToken).ConfigureAwait(false);
        }
        else if (!summary.Problems.Any(problem => !problem.HasFixApplied))
        {
            await WriteAsync(writer, $"""

                            <div class="summary-result success">All problems fixed. Your project has been upgraded to Godot {summary.TargetGodotVersion}! 🎉</div>
                """, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WriteAsync(writer, """

                            <div class="summary-result error">⚠ Found problems that need to be fixed manually.</div>
                """, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteProblemsAsync(TextWriter writer, Summary summary, CancellationToken cancellationToken = default)
    {
        await WriteAsync(writer, $"""

                        <h2>Problems found <span class="problem-count">({summary.Problems.Count})</span></h2>
                        <div class="problem-groups">
            """, cancellationToken).ConfigureAwait(false);

        var groups = summary.Problems.GroupBy(problem => problem.AnalysisResult.Id);

        foreach (var group in groups)
        {
            await WriteProblemGroupAsync(writer, group, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, """

                        </div>
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteProblemGroupAsync(TextWriter writer, IGrouping<string, ProblemSummaryData> group, CancellationToken cancellationToken)
    {
        var problems = group.ToList();
        int fixedCount = problems.Count(p => p.HasFixApplied);
        int fixableCount = problems.Count(p => !p.HasFixApplied && p.HasFixAvailable);
        int unfixableCount = problems.Count(p => !p.HasFixApplied && !p.HasFixAvailable);

        bool hasUnresolved = fixableCount > 0 || unfixableCount > 0;
        string openAttr = hasUnresolved ? " open" : "";

        var firstResult = problems[0].AnalysisResult;
        string id = WebUtility.HtmlEncode(firstResult.Id);
        string title = WebUtility.HtmlEncode(firstResult.Title.ToString(writer.FormatProvider));
        string? description = firstResult.Description is not null ? WebUtility.HtmlEncode(firstResult.Description.ToString(writer.FormatProvider)) : null;
        Uri? helpUri = firstResult.HelpUri;

        await WriteAsync(writer, $"""

                            <details class="problem-group"{openAttr}>
                                <summary>
                                    <div class="group-header-main">
                                        <span class="problem-id">{id}</span>
                                        <span class="group-title">{title}</span>
                                    </div>
                                    <div class="group-badges">
            """, cancellationToken).ConfigureAwait(false);

        if (fixedCount > 0)
        {
            await WriteAsync(writer, $"""

                                        <span class="badge fixed">{fixedCount} fixed</span>
                """, cancellationToken).ConfigureAwait(false);
        }

        if (fixableCount > 0)
        {
            await WriteAsync(writer, $"""

                                        <span class="badge fixable">{fixableCount} fixable</span>
                """, cancellationToken).ConfigureAwait(false);
        }

        if (unfixableCount > 0)
        {
            await WriteAsync(writer, $"""

                                        <span class="badge unfixable">{unfixableCount} unfixable</span>
                """, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, """

                                    </div>
                                </summary>
                                <div class="group-body">
            """, cancellationToken).ConfigureAwait(false);

        if (description is not null)
        {
            await WriteAsync(writer, $"""

                                    <p class="group-description">{description}</p>
                """, cancellationToken).ConfigureAwait(false);
        }

        if (helpUri is not null)
        {
            await WriteAsync(writer, $"""

                                    <p class="group-help-link"><a href="{WebUtility.HtmlEncode(helpUri.ToString())}">&#128211; Learn more</a></p>
                """, cancellationToken).ConfigureAwait(false);
        }

        // Sub-group problems by their formatted message. Problems with the same message
        // likely represent the same issue occurring at multiple locations, so we collapse
        // them into a nested group showing the message once and listing each location.
        var messageGroups = problems
            .GroupBy(p => p.AnalysisResult.GetMessage(writer.FormatProvider) ?? string.Empty)
            .ToList();

        await WriteAsync(writer, """

                                    <ul class="problem-list">
            """, cancellationToken).ConfigureAwait(false);

        foreach (var messageGroup in messageGroups)
        {
            var messageProblems = messageGroup.ToList();
            if (messageProblems.Count == 1)
            {
                // No sub-grouping needed: render as a regular problem item.
                await WriteProblemItemAsync(writer, messageProblems[0], cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteProblemSubGroupAsync(writer, messageGroup.Key, messageProblems, writer.FormatProvider, cancellationToken).ConfigureAwait(false);
            }
        }

        await WriteAsync(writer, """

                                    </ul>
                                </div>
                            </details>
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteProblemSubGroupAsync(TextWriter writer, string messageKey, List<ProblemSummaryData> problems, IFormatProvider? formatProvider, CancellationToken cancellationToken)
    {
        int fixedCount = problems.Count(p => p.HasFixApplied);
        int fixableCount = problems.Count(p => !p.HasFixApplied && p.HasFixAvailable);
        int unfixableCount = problems.Count(p => !p.HasFixApplied && !p.HasFixAvailable);

        bool hasUnresolved = fixableCount > 0 || unfixableCount > 0;
        string openAttr = hasUnresolved ? " open" : "";

        string status = fixableCount > 0 || unfixableCount > 0
            ? (fixableCount > 0 ? "fixable" : "unfixable")
            : "fixed";

        string messageText = !string.IsNullOrEmpty(messageKey) ? WebUtility.HtmlEncode(messageKey) : "<em>No message</em>";

        await WriteAsync(writer, $"""

                                        <li class="problem-subgroup-item">
                                            <details class="problem-subgroup"{openAttr}>
                                                <summary class="problem-subgroup-summary">
                                                    <div class="subgroup-header">
                                                        <div class="subgroup-message">{messageText}</div>
                                                        <div class="subgroup-badges">
            """, cancellationToken).ConfigureAwait(false);

        if (fixedCount > 0)
        {
            await WriteAsync(writer, $"""

                                                            <span class="badge fixed">{fixedCount} fixed</span>
                """, cancellationToken).ConfigureAwait(false);
        }

        if (fixableCount > 0)
        {
            await WriteAsync(writer, $"""

                                                            <span class="badge fixable">{fixableCount} fixable</span>
                """, cancellationToken).ConfigureAwait(false);
        }

        if (unfixableCount > 0)
        {
            await WriteAsync(writer, $"""

                                                            <span class="badge unfixable">{unfixableCount} unfixable</span>
                """, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, $"""

                                                        </div>
                                                    </div>
                                                </summary>
                                                <ul class="subgroup-location-list">
            """, cancellationToken).ConfigureAwait(false);

        foreach (var problem in problems)
        {
            await WriteProblemSubGroupItemAsync(writer, problem, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, """

                                                </ul>
                                            </details>
                                        </li>
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteProblemSubGroupItemAsync(TextWriter writer, ProblemSummaryData problem, CancellationToken cancellationToken)
    {
        string status = problem switch
        {
            _ when problem.HasFixApplied => "fixed",
            _ when problem.HasFixAvailable => "fixable",
            _ => "unfixable",
        };

        string outcomeLabel = problem switch
        {
            _ when problem.HasFixApplied => "Fixed ✓",
            _ when problem.HasFixAvailable => "Fix Available",
            _ => "No Fix Available",
        };

        await WriteAsync(writer, $"""

                                                    <li class="subgroup-location-item {status}">
                                                        <span class="outcome-badge {status}">{outcomeLabel}</span>
            """, cancellationToken).ConfigureAwait(false);

        if (problem.AnalysisResult.Location.IsValid)
        {
            await WriteAsync(writer, $"""

                                                        <small class="problem-location">{WebUtility.HtmlEncode(problem.AnalysisResult.Location.ToString())}</small>
                """, cancellationToken).ConfigureAwait(false);
        }

        if (problem.HasFixAvailable)
        {
            await WriteFixesAsync(writer, problem, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, """

                                                    </li>
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteProblemItemAsync(TextWriter writer, ProblemSummaryData problem, CancellationToken cancellationToken)
    {
        string status = problem switch
        {
            _ when problem.HasFixApplied => "fixed",
            _ when problem.HasFixAvailable => "fixable",
            _ => "unfixable",
        };

        string outcomeLabel = problem switch
        {
            _ when problem.HasFixApplied => "Fixed ✓",
            _ when problem.HasFixAvailable => "Fix Available",
            _ => "No Fix Available",
        };

        await WriteAsync(writer, $"""

                                        <li class="problem-item {status}">
                                            <div class="problem-item-header">
                                                <span class="outcome-badge {status}">{outcomeLabel}</span>
            """, cancellationToken).ConfigureAwait(false);

        if (problem.AnalysisResult.Location.IsValid)
        {
            await WriteAsync(writer, $"""

                                                <small class="problem-location">{WebUtility.HtmlEncode(problem.AnalysisResult.Location.ToString())}</small>
                """, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, """

                                            </div>
                                            <div class="problem-item-body">
            """, cancellationToken).ConfigureAwait(false);

        if (problem.AnalysisResult.MessageFormat is not null)
        {
            await WriteAsync(writer, $"""

                                                <p class="problem-message">{WebUtility.HtmlEncode(problem.AnalysisResult.GetMessage(writer.FormatProvider))}</p>
                """, cancellationToken).ConfigureAwait(false);
        }

        await WriteFixesAsync(writer, problem, cancellationToken).ConfigureAwait(false);

        await WriteAsync(writer, """

                                            </div>
                                        </li>
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteFixesAsync(TextWriter writer, ProblemSummaryData problem, CancellationToken cancellationToken = default)
    {
        if (!problem.HasFixAvailable)
        {
            return;
        }

        await WriteAsync(writer, """

                                                <div class="fixes-section">
                                                    <div class="fixes-label">Fixes</div>
                                                    <ul class="fixes-list">
            """, cancellationToken).ConfigureAwait(false);

        if (problem.HasFixApplied)
        {
            await WriteFixAsync(writer, problem.UpgradeActionApplied, isApplied: true, cancellationToken).ConfigureAwait(false);
        }

        foreach (var upgrade in problem.UpgradeActions
            .Where(upgrade => upgrade != problem.UpgradeActionApplied))
        {
            await WriteFixAsync(writer, upgrade, isApplied: false, cancellationToken).ConfigureAwait(false);
        }

        await WriteAsync(writer, """

                                                    </ul>
                                                </div>
            """, cancellationToken).ConfigureAwait(false);
    }

    private static Task WriteFixAsync(TextWriter writer, UpgradeAction upgrade, bool isApplied, CancellationToken cancellationToken = default)
    {
        return WriteAsync(writer, $"""

                                                        <li class="fix-item {(isApplied ? "applied" : "available")}">
                                                            <strong>{WebUtility.HtmlEncode(upgrade.Title)}</strong>
                                                        </li>
            """, cancellationToken);
    }
}
