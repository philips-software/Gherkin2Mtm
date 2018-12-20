# Gherkin2Mtm

**Description**: When you have your manual tests represented in Gherkin for various reasons such as business, reviwable, etc., and have the testing be managed thru MTM, you would want the Gherkins be available in MTM. You can then use this tool to publish the Gherkins in the local storage to the Microsoft test management hosted in either the on-prem TFS or Azure DevOps. 

  - **Technology stack**: .Net framework, Gherkin parser, TFS API
  - **Key concepts** 
	- Define mapping of test case fields to tags on a scenario, matching the test case template. The tool will do the validation of field mappings
	- History of changes thru the tool to a test case are maintained, so as to satisfy regulatory requirements
	- A test case will be linked to the scenario thru a tag, MTMID, which will be added to every scenario by the tool after a successfule first publish
	- A parameterized test case will be created for the corresponding scenario outline
	- Steps of a Background be prepended to steps list of a test case for every scenario of that feature
  - **Status**:  1.0.0
  - There is similar tool in the market, https://www.specsolutions.eu/services/specsync/, but it does not help populating test case fields. Like, this toold does, with the help of a field mapper.
    Also, specsync is a commercial solution.   

## Dependencies

All the dependences are defined in the project so, you just need to have an active internet connection to get them

## Installation

So far, this is been being used as a build artifact in a pipeline. Creating the installer will be the future work.

## Configuration

## Usage
Once the solution is built, use the following commandline to kick off the publish process

`Gherkin2Mtm.exe --server <serverurl> --project <name of the project> --creds <creds> --personalAccessToken <pat> --area <area path> --featuresPath <path to feature files>`

- **server/t:** The URL of the either TFS or Azure DevOps - _**Required**_
- **project/p:** Name of the project to which the Gherkins are to be published - _**Required**_
- **creds/c:** username:password@domain - _**Optional**_
- **personalAccessToken/a** the required personal access token - _**Optional**_ But missing either this or the creds option, falls back to NT authentication
- **area/h** area path that every test case belongs to - _**Optional**_
- **featuresPath/f** path to feature files, which is an input and can be mentioned in 3 ways - _**Required**_
	- path to a single file
    - path to a folder
    - multiple file paths separated by a semi-colon  

## How to test the software
Feature files with different data are available inside tests folder to test the software with the usage given above. Plan is to automate these tests

## Known issues

- Does not support parallel execution
- Only supports either TFS or Azure Devops - idea is to develop adaptors for different test case management solutions 
- This only publishes scenarios tagged as @Manual

## Contact / Getting help

[Maintainers](MAINTAINERS.md)

## License

[LICENSE](LICENSE.md)


