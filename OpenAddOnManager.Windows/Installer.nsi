Unicode true
SetCompressor /FINAL /SOLID lzma

!include MUI2.nsh
!include nsProcess.nsh
!include LogicLib.nsh
!include UAC.nsh
!include WinMessages.nsh
!include x64.nsh

!getdllversion "bin\Release\netcoreapp3.0\publish\OpenAddOnManager.Windows.exe" "AppVersion_"
!define /date CURRENT_YEAR "%Y"

!define ExecTimeout "!insertmacro ExecTimeout"

!macro ExecTimeout commandline timeout_ms terminate var_exitcode
  Timeout::ExecTimeout '${commandline}' '${timeout_ms}' '${terminate}'
  Pop ${var_exitcode}
!macroend

!macro IfKeyExists ROOT MAIN_KEY KEY
  Push $R0
  Push $R1
  Push $R2
 
  # XXX bug if ${ROOT}, ${MAIN_KEY} or ${KEY} use $R0 or $R1
 
  StrCpy $R1 "0" # loop index
  StrCpy $R2 "0" # not found
 
  ${Do}
    EnumRegKey $R0 ${ROOT} "${MAIN_KEY}" "$R1"
    ${If} $R0 == "${KEY}"
      StrCpy $R2 "1" # found
      ${Break}
    ${EndIf}
    IntOp $R1 $R1 + 1
  ${LoopWhile} $R0 != ""
 
  ClearErrors
 
  Exch 2
  Pop $R0
  Pop $R1
  Exch $R2
!macroend

!macro ShellExecWait verb app param workdir show exitoutvar ;only app and show must be != "", every thing else is optional
#define SEE_MASK_NOCLOSEPROCESS 0x40 
System::Store S
System::Call '*(&i60)i.r0'
System::Call '*$0(i 60,i 0x40,i $hwndparent,t "${verb}",t $\'${app}$\',t $\'${param}$\',t "${workdir}",i ${show})i.r0'
System::Call 'shell32::ShellExecuteEx(ir0)i.r1 ?e'
${If} $1 <> 0
	System::Call '*$0(is,i,i,i,i,i,i,i,i,i,i,i,i,i,i.r1)' ;stack value not really used, just a fancy pop ;)
	System::Call 'kernel32::WaitForSingleObject(ir1,i-1)'
	System::Call 'kernel32::GetExitCodeProcess(ir1,*i.s)'
	System::Call 'kernel32::CloseHandle(ir1)'
${EndIf}
System::Free $0
!if "${exitoutvar}" == ""
	pop $0
!endif
System::Store L
!if "${exitoutvar}" != ""
	pop ${exitoutvar}
!endif
!macroend
 
Name "Open Add-On Manager"
OutFile "oam-setup.exe"
VIAddVersionKey /LANG=0 "ProductName" "Open Add-On Manager"
VIAddVersionKey /LANG=0 "CompanyName" "Open Add-On Manager Team"
VIAddVersionKey /LANG=0 "FileDescription" "Setup software for Open Add-On Manager"
VIAddVersionKey /LANG=0 "FileVersion" "${AppVersion_1}.${AppVersion_2}.${AppVersion_3}.${AppVersion_4}"
VIAddVersionKey /LANG=0 "ProductVersion" "${AppVersion_1}.${AppVersion_2}.${AppVersion_3}.${AppVersion_4}"
VIAddVersionKey /LANG=0 "OriginalFilename" "oam-setup.exe"
VIProductVersion ${AppVersion_1}.${AppVersion_2}.${AppVersion_3}.${AppVersion_4}
RequestExecutionLevel user
CRCCheck force
ManifestDPIAware true
BrandingText "Open Add-On Manager ${AppVersion_1}.${AppVersion_2}.${AppVersion_3}.${AppVersion_4}"
Icon "${NSISDIR}\Contrib\Graphics\Icons\orange-install.ico"
UninstallIcon "${NSISDIR}\Contrib\Graphics\Icons\orange-uninstall.ico"

InstallDir "$LOCALAPPDATA\OpenAddOnManager"
InstallDirRegKey HKCU "Software\Open Add-On Manager" "Install_Dir"

!define MUI_LANGDLL_REGISTRY_ROOT "HKCU" 
!define MUI_LANGDLL_REGISTRY_KEY "Software\Open Add-On Manager" 
!define MUI_LANGDLL_REGISTRY_VALUENAME "Installer Language"

!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\orange-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\orange-uninstall.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "SetupHeader.bmp"
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADER_TRANSPARENT_TEXT

!define MUI_TEXT_WELCOME_INFO_TITLE "Welcome to Open Add-On Manager Setup"
!define MUI_WELCOMEFINISHPAGE_BITMAP "SetupLeft.bmp"

!define MUI_TEXT_COMPONENTS_TITLE "Setup Options"
!define MUI_TEXT_COMPONENTS_SUBTITLE "Choose how you want Open Add-On Manager to be installed."
!define MUI_COMPONENTSPAGE_TEXT_TOP "Check the options you prefer and uncheck the ones you do not. Click Next to continue."
!define MUI_COMPONENTSPAGE_NODESC

#!define MUI_TEXT_DIRECTORY_TITLE "Install Location"

!insertmacro MUI_PAGE_WELCOME
;!insertmacro MUI_PAGE_LICENSE "EULA.rtf"
!insertmacro MUI_PAGE_COMPONENTS
;!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

!insertmacro MUI_RESERVEFILE_LANGDLL

Section

  SectionIn RO
  IfRebootFlag 0 NoRebootRequired
  MessageBox MB_YESNO "A restart is required before setup may proceed. Do you wish to reboot now?" /SD IDNO \
    IDNO NoReboot
  Reboot
  NoReboot:
  Abort
  NoRebootRequired:

SectionEnd

SectionGroup "!Install Open Add-On Manager"

Section "Shutdown Any Grindstone 4 Instances"
  SectionIn RO
 
  ${nsProcess::CloseProcess} "OpenAddOnManager.Windows.exe" $0
  StrCmp $0 0 ShutdownSatisfied
  StrCmp $0 603 ShutdownSatisfied
  MessageBox MB_ICONSTOP "A check for running instances of Open Add-On Manager has failed."
  Abort
  ShutdownSatisfied:

SectionEnd

Section "Setup Open Add-On Manager Application Files"
  SectionIn RO

  SetOutPath $INSTDIR
  File /r bin\Release\netcoreapp3.0\publish\*.*

  ; Write the installation path into the registry
  WriteRegStr HKCU "SOFTWARE\Open Add-On Manager" "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" DisplayIcon '"$INSTDIR\OpenAddOnManager.Windows.exe",0'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" DisplayName "Open Add-On Manager"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" DisplayVersion "${AppVersion_1}.${AppVersion_2}.${AppVersion_3}.${AppVersion_4}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" HelpLink "https://github.com/OpenAddOnManager/OpenAddOnManager/issues"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" InstallLocation $INSTDIR
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" NoModify 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" NoRepair 1
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" Publisher 'Open Add-On Manager Team'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" UninstallString '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" URLInfoAbout "https://github.com/OpenAddOnManager/OpenAddOnManager"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" VersionMajor 0
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Grindstone 4" VersionMinor 0
  WriteUninstaller "uninstall.exe"
    
SectionEnd

SectionGroupEnd

SectionGroup /e "Create Shortcuts"

Section "Start Menu"
  SectionIn RO

  SetShellVarContext current
  CreateDirectory "$SMPROGRAMS\Open Add-On Manager"
  CreateShortCut "$SMPROGRAMS\Open Add-On Manager\Open Add-On Manager.lnk" "$INSTDIR\OpenAddOnManager.Windows.exe" "" "$INSTDIR\OpenAddOnManager.Windows.exe" 0
  CreateShortCut "$SMPROGRAMS\Open Add-On Manager\Uninstall Open Add-On Manager.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
  CreateShortCut "$SMPROGRAMS\Open Add-On Manager\Support.lnk" "https://github.com/OpenAddOnManager/OpenAddOnManager/issues"
  
SectionEnd

Section /o "Desktop"

  IfSilent +3
  SetShellVarContext current
  CreateShortCut "$DESKTOP\Open Add-On Manager.lnk" "$INSTDIR\OpenAddOnManager.Windows.exe" "" "$INSTDIR\OpenAddOnManager.Windows.exe" 0
  
SectionEnd

Section /o "Quick Launch"

  IfSilent +3
  SetShellVarContext current
  CreateShortCut "$QUICKLAUNCH\Open Add-On Manager.lnk" "$INSTDIR\OpenAddOnManager.Windows.exe" "" "$INSTDIR\OpenAddOnManager.Windows.exe" 0
  
SectionEnd

SectionGroupEnd

Section "Start Open Add-On Manager with Windows"

  IfSilent +2
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "Open Add-On Manager" '"$INSTDIR\OpenAddOnManager.Windows.exe" -startMinimized'

SectionEnd

Section

  IfRebootFlag 0 NoRebootFlag
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "The computer needs to reboot before you can start using Grindstone. Would you like to reboot now?" /SD IDNO \
    IDNO SkipReboot
  Reboot
  Quit
  SkipReboot:
  Quit
  NoRebootFlag:

SectionEnd

Section "Start Open Add-On Manager when Setup Finishes"

  SetAutoClose true
  IfSilent +2
  Exec '"$INSTDIR\OpenAddOnManager.Windows.exe"'

SectionEnd

Section "Uninstall"

  SetShellVarContext current

  ${nsProcess::CloseProcess} "OpenAddOnManager.Windows.exe" $0
  StrCmp $0 0 UninstallShutdownSatisfied
  StrCmp $0 603 UninstallShutdownSatisfied
  MessageBox MB_ICONSTOP "A check for running instances of Open Add-On Manager has failed."
  Abort
  UninstallShutdownSatisfied:

  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "Open Add-On Manager"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Open Add-On Manager"
  DeleteRegKey HKCU "SOFTWARE\Open Add-On Manager"

  RMDir /r "$SMPROGRAMS\Open Add-On Manager"
  Delete "$DESKTOP\Open Add-On Manager.lnk"
  Delete "$QUICKLAUNCH\Open Add-On Manager.lnk"

  RMDir /r "$INSTDIR"
  
  MessageBox MB_ICONINFORMATION|MB_OK|MB_DEFBUTTON1 "Open Add-On Manager was successfully removed from your computer."

  SetAutoClose true
  
SectionEnd

Function un.onInit

  !insertmacro MUI_UNGETLANGUAGE
  
FunctionEnd