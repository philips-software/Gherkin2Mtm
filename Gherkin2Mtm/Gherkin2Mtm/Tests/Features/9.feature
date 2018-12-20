Feature: Test
	This is a Gherkin2Mtm testing feature

Background: Test Background
	Given a background
	| Test Column |
	| 1           |
	| 2           |
	| 3           |
	| 4           |

@SRS:SentryAlert_MenuAndToolbar @Release:4.2 
@Requirement:SA-MT-TC-9
@Manual
Scenario: TC-9-1
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with valid credentials
	Then they should see the home page

@SRS:SentryAlert_MenuAndToolbar @Release:4.2 
@Requirement:SA-MT-TC-9 @Manual
Scenario Outline: TC-9-2
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with credentials, <Test Column>
	Then they should see the home page
	Examples: 
	| Test Column |
	| 1           |
	| 2           |
	| 3           |
