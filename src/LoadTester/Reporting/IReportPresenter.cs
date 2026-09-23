// Presentation contract for load test reporting and summaries.

using LoadTester.Analysis;
using LoadTester.Metrics;

namespace LoadTester.Reporting;

public interface IReportPresenter
{
    void ShowProgress(long total, long failed, double rps);
    void ShowRoundSummary(RoundResult result);
    void ShowSuiteComparison(IReadOnlyList<RoundResult> rounds, DegradationReport degradationReport);
    void ShowHeader(string title);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    void ShowInfo(string message);
}

