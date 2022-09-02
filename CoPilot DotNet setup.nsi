;!include nsDialogs.nsh
!include LogicLib.nsh


; example1.nsi
;
; This script is perhaps one of the simplest NSIs you can make. All of the
; optional settings are left to their default settings. The installer simply 
; prompts the user asking them where to install, and drops a copy of example1.nsi
; there. 

XPStyle on

;--------------------------------

; The name of the installer
Name "Testright CoPilot"

; The file to write
OutFile "CoPilot DotNet setup.exe"

; The default installation directory
InstallDir "C:\CoPilot.Net"

; Request application privileges for Windows Vista
RequestExecutionLevel admin

;--------------------------------


; Pages

Page directory
Page instfiles


;--------------------------------


; The stuff to install
Section "" ;No components page, name is not important


  nsProcess::_FindProcess "Automation Adapter.exe"
  Pop $R0
  ${If} $R0 = 0
    nsProcess::_KillProcess "Automation Adapter.exe"
    Pop $R0
 
    Sleep 500
  ${EndIf}

  nsProcess::_FindProcess "AutomationAdapter.exe"
  Pop $R0
  ${If} $R0 = 0
    nsProcess::_KillProcess "AutomationAdapter.exe"
    Pop $R0
 
    Sleep 500
  ${EndIf}

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "CoPilot\bin\Release\*.dll"
  File "CoPilot\bin\Release\*.xml"
  File "CoPilot\bin\Release\CoPilot.exe"
  File "CoPilot\bin\Release\CoPilot.pdb"
  File "CoPilot.exe.config"
  File "psubst.bat"
  File "psubstgit.bat"
  File "psubsttfs.bat"
  File "control_automation_machine_StructureDump.sql"
  File "blank_release_1_se_project_StructureDump.sql"
  File "Settings\bin\Release\Settings.exe"
  File "ExecutionGroupCreateUpdate\bin\Release\ExecutionGroupCreateUpdate.exe"
  File "ExecutionGroupCreateUpdate\bin\Release\ExecutionGroupCreateUpdate.pdb"
  File "AutomationAdapter\bin\Release\*.dll"
  File "AutomationAdapter\bin\Release\AutomationAdapter.exe"
  File "AutomationAdapter\bin\Release\AutomationAdapter.pdb"
  File "RunExecutionGroup\bin\Release\RoboSharp.dll"
  File "RunExecutionGroup\bin\Release\RunExecutionGroup.exe"
  File "RunExecutionGroup\bin\Release\RunExecutionGroup.pdb"
  File "runeg\bin\Release\BetterConsoleTables.dll"
  File "runeg\bin\Release\runeg.exe"

  SetOutPath $INSTDIR\data
  
  File "SharedProject\data\*.jpg"

  CreateDirectory "$SMPROGRAMS\CoPilot.Net"
  
  CreateShortCut "$SMPROGRAMS\CoPilot.Net\CoPilot.lnk" "$INSTDIR\CoPilot.exe"
  CreateShortCut "$DESKTOP\CoPilot.lnk" "$INSTDIR\CoPilot.exe"
  
  CreateShortCut "$SMPROGRAMS\CoPilot.Net\AutomationAdapter.lnk" "$INSTDIR\AutomationAdapter.exe"
  CreateShortCut "$DESKTOP\AutomationAdapter.lnk" "$INSTDIR\AutomationAdapter.exe"

SectionEnd ; end the section
