using CommandLine;
using CommandLine.Text;

namespace Gherkin2Mtm.Helpers
{
    public class CommandLineOptions
    {
        [Option('t', "server", Required = true)]
        public string Server { get; set; }
        [Option('p', "project", Required = true)]
        public string Project { get; set; }
        [Option('h', "area", Required = false)]
        public string Area { get; set; }
        [Option('c', "creds", Required = false)]
        public string Creds { get; set; }
        [Option('a', "personalAccessToken", Required = false)]
        public string PersonalAccessToken { get; set; }
        [Option('f', "featuresPath", Required = true)]
        public string FeaturesPath { get; set; }
        [Option('v', "verbose", DefaultValue = true,
            HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }
        [ParserState]
        public IParserState LastParserState { get; set; }
        [HelpOption]

        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
