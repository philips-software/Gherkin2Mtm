using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Gherkin2MtmApi.Helpers
{
    public class AdoHelper
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(TestCaseHelper));
        private CommandLineOptions _commandLineOptions;

        public AdoHelper(CommandLineOptions commandLineOptions)
        {
            _commandLineOptions = commandLineOptions;
        }

        public async Task<string> GetWorkItemFieldValue(string workItemFieldName, string workItemId)
        {
            try
            {
                var client = GetWorkItemTrackingHttpClient();
                var workItem = await client.GetWorkItemAsync(int.Parse(workItemId));
                return workItem.Fields[workItemFieldName].ToString();
            } catch(Exception exception)
            {
                Logger.Error(exception.StackTrace);
                throw exception;
            }
        }

        private WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient()
        {
            var uri = new Uri(_commandLineOptions.Server);
            var connection = new VssConnection(uri, new VssBasicCredential(string.Empty, _commandLineOptions.PersonalAccessToken));

            return connection.GetClient<WorkItemTrackingHttpClient>();
        }
    }
}
