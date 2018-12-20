Feature: Test
	This is a Gherkin2Mtm testing feature

@SRS:SentryAlert_MenuAndToolbar @Release:4.2
@Requirement:SA-MT-TC-21 @Manual @MTMID:1354
Scenario Outline: TC-21
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with credentials, <Test Column>
	Then they should see the home page
	Examples: 
	| Test Column |
	| 1           |
	| 2           |
	| 3           |
