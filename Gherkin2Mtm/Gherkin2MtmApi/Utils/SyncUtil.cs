namespace Gherkin2MtmApi.Utils
{
    public static class SyncUtil
    {
        public static string ParameterToken { get; } = "-";
        public static string MtmTcTagName { get; } = "MTMID";
        public static string MtmTcLink { get; } = $"@{MtmTcTagName}:";
        public static string MtmTcIdPattern { get; } = $"{MtmTcLink}([0-9]+)";
        public static string MtmTcLinkTagPattern { get; } = $"{MtmTcLink}[0-9]+";
        public static string ManualTag { get; } = "@MANUAL";
        public static string RemovedState { get; } = "Removed";
        public static string ClosedState { get; } = "Closed";
        public static string RemovedTag { get; } = $"@{RemovedState.ToUpperInvariant()}";
        public static string TagNameIdPattern { get; } = @"@(.*):(.*)";

        public static string TagsField { get; } = "Tags";
        public static string VersionField { get; } = "Version";

        public static string BackgroundPrefix { get; } = "Background: ";
        public static string TagsToken { get; } = ",";
        public static string FeatureFilter { get; } = "*.feature";
        public static string TestCaseStateReady { get; } = "Ready";
        public static string CredsPatternDomain { get; } = "^(.*?):(.*?)@(.*?)$";

        public const string DECORATION = "************** {0} ****************";
        public const string ABORTED_MESSAGE = "Aborted: {0}";
        public const string REASON = "Gherkin2Mtm";
        public const string REQUIREMENT_TAG_NAME = "Requirement";
    }
}
