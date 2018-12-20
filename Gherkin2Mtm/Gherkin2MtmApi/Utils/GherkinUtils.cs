using System.Collections.Generic;
using System.Linq;
using Gherkin.Ast;

namespace Gherkin2MtmApi.Utils
{
    internal static class GherkinUtils
    {
        public static IList<Tag> GetTags(ScenarioDefinition scenarioDefinition)
        {
            if (scenarioDefinition is ScenarioOutline outline)
            {
                return outline.Tags.ToArray();
            }

            return new List<Tag>(((Scenario)scenarioDefinition).Tags);
        }

        public static Location GetFirstTagLocation(ScenarioDefinition scenarioDefinition)
        {
            if (scenarioDefinition == null)
            {
                return null;
            }

            var tags = GetTags(scenarioDefinition);

            var location = scenarioDefinition.Location;
            if (tags.Count > 0)
            {
                location = tags[0].Location;
            }

            return location;
        }

        public static Location GetFirstStepLocation(ScenarioDefinition scenarioDefinition)
        {
            var firstStep = scenarioDefinition?.Steps.First();

            return firstStep?.Location;
        }
    }
}
