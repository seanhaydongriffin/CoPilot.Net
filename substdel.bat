@echo off
cd /d %~dp0
net.exe use R: /delete /y
net.exe use L: /delete /y
subst r: /D
subst l: /D

rem cut off fractional seconds
set t=%time:~0,8%
rem replace colons with dashes
set t=%t::=%
set mydate=test-automation-%date:~10,4%%date:~7,2%%date:~4,2%-%t%


@echo off
:: BatchGotAdmin
::-------------------------------------
REM  --> Check for permissions
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    set params = %*:"="
    echo UAC.ShellExecute "cmd.exe", "/c %~s0 %params%", "", "runas", 1 >> "%temp%\getadmin.vbs"

    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"
::--------------------------------------

::ENTER YOUR CODE BELOW:

subst r: /D
subst l: /D
rename "%USERPROFILE%\Source\Repos\test-automation\Automation" "Automation2"
rename "%USERPROFILE%\Source\Repos\test-automation" %mydate%
