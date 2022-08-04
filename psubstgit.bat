@echo off
cd /d %~dp0
if not exist "%USERPROFILE%\Source\Repos\test-automation\Automation\" mkdir "%USERPROFILE%\Source\Repos\test-automation\Automation"
net.exe use R: /delete /y
call psubst.bat r: /D
call psubst.bat r: /D /P
call psubst.bat r: /D /PF
call psubst.bat r: "%USERPROFILE%\Source\Repos\test-automation\Automation"
call psubst.bat r: "%USERPROFILE%\Source\Repos\test-automation\Automation" /PF
net.exe use L: /delete /y
call psubst.bat l: /D
call psubst.bat l: /D /P
call psubst.bat l: /D /PF
call psubst.bat l: "%USERPROFILE%\Source\Repos\test-automation\Automation"
call psubst.bat l: "%USERPROFILE%\Source\Repos\test-automation\Automation" /PF

echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
echo UAC.ShellExecute "net.exe", "share Automation=""%USERPROFILE%\Source\Repos\test-automation\Automation"" /GRANT:Everyone,FULL", "", "runas", 1 >> "%temp%\getadmin.vbs"
"%temp%\getadmin.vbs"

start AutomationAdapter.exe
exit /B


