Feature: Test
	This is a Gherkin2Mtm testing feature

@SRS:SentryAlert_MenuAndToolbar @Release:4.2 
@Requirement:SA-MT-TC-17 @Manual
@TagOtherThanRequiredAndManual @MTMID:1350
Scenario: TC-17
	This is a Gherkin2Mtm testing scenario
	Given an Automation User accessed the systems
	When they login to the application with valid credentials
	Then they should see the home page
