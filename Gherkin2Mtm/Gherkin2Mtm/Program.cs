using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Gherkin2Mtm.Helpers;
using Gherkin2MtmApi.Helpers;
using Gherkin2MtmApi.Utils;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using WindowsCredential = Microsoft.VisualStudio.Services.Common.WindowsCredential;

[assembly:ComVisible(false)]
namespace Gherkin2Mtm
{
    public static class Program
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            var commandLineOptions = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, commandLineOptions))
            {
                commandLineOptions.GetUsage();
                return;
            }

            if (!IsValidCreds(commandLineOptions))
            {
                Logger.Info(Gherkin2Mtm_Resources.CREDS_ERROR);
                return;
            }

            if (commandLineOptions.Verbose)
            {
                Logger.Info(
                    $"{commandLineOptions.Server} : {commandLineOptions.Project} : {commandLineOptions.FeaturesPath}");
            }

            var teamProject = GetProject(
                commandLineOptions.Server,
                commandLineOptions.Project,
                commandLineOptions.Creds,
                commandLineOptions.PersonalAccessToken);
            FeatureHelper.WorkoutFeatures(
                teamProject,
                commandLineOptions.FeaturesPath,
                commandLineOptions.Area,
                FieldMapperHelper.GetFieldsList());
        }

        private static bool IsValidCreds(CommandLineOptions commandLineOptions)
        {
            if (commandLineOptions.Creds == null) return true;

            var regExGroups = Regex.Match(commandLineOptions.Creds, SyncUtil.CredsPatternDomain).Groups;

            return regExGroups.Count == 4;
        }

        private static ITestManagementTeamProject GetProject(string server, string project, string creds, string personalAccessToken)
        {
            var tfs = GetTfsTeamProjectCollection(server, creds, personalAccessToken);
            tfs.Authenticate();
            var tms = tfs.GetService<ITestManagementService>();
            return tms.GetTeamProject(project);
        }

        private static TfsTeamProjectCollection GetTfsTeamProjectCollection(string server, string creds, string personalAccessToken)
        {
            var uri = TfsTeamProjectCollection.GetFullyQualifiedUriForName(server);
            if (creds == null)
            {
                Logger.Info("using personalAccessToken");
                return personalAccessToken == null
                    ? new TfsTeamProjectCollection(uri, new VssClientCredentials())
                    : new TfsTeamProjectCollection(uri, new VssBasicCredential(string.Empty, personalAccessToken));
            }

            Logger.Info("using creds");
            var credsLocal = Regex.Match(creds, SyncUtil.CredsPatternDomain).Groups;
            var networkCred = new NetworkCredential(credsLocal[1].Value, credsLocal[2].Value, credsLocal[3].Value);
            var windowsCred = new WindowsCredential(networkCred);
            return new TfsTeamProjectCollection(uri, new VssClientCredentials(windowsCred, CredentialPromptType.DoNotPrompt));

        }
    }
}
