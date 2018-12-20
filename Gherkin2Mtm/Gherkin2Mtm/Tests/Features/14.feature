Feature: Test
	This is a Gherkin2Mtm testing feature

@Release:4.2 
@Requirement:SA-MT-132
@Manual
Scenario: TC-14-1
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with valid credentials
	Then they should see the home page

@SRS:SentryAlert_MenuAndToolbar 
@Requirement:SA-MT-135 @Manual
Scenario Outline: TC-14-2
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with credentials, <Test Column>
	Then they should see the home page
	Examples: 
	| Test Column |
	| 1           |
	| 2           |
	| 3           |

@SRS:SentryAlert_MenuAndToolbar 
@Requirement:SA-MT-132-1
@Manual
Scenario: TC-14-3
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with valid credentials
	Then they should see the home page
