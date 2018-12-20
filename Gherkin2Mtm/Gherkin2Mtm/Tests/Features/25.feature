Feature: Test
	This is a Gherkin2Mtm testing feature

Background: Test Background
	Given a background
	| Test Column |
	| 1           |
	| 2           |
	| 3           |
	| 4           |

@SRS:Example2 @Release:4.2 
@Requirement:SA-MT-TC-3
@Manual
Scenario: TC-25
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with valid credentials
	Then they should see the home page
