@echo off
ECHO SOURCE BRANCH IS %BUILD_SOURCEBRANCH%
IF %BUILD_SOURCEBRANCH% == refs/heads/master (
   ECHO Building master branch so no merge is needed.
   EXIT
)
git config --global user.email "sree.puttagunta@philips.com"
git config --global user.name "Sree Puttagunta"
SET sourceBranch=origin/%BUILD_SOURCEBRANCH:refs/heads/=%
ECHO GIT ADD
git add -A
ECHO GIT COMMIT
git commit -m "Changes made by the Gherkin2Mtm tool ***NO_CI***"
ECHO GIT STATUS
git status
ECHO GIT PUSH %sourceBranch%
git push origin HEAD:%BUILD_SOURCEBRANCH%
ECHO GIT STATUS
git status