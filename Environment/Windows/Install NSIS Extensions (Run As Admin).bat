@setlocal enableextensions
@cd /d "%~dp0"
@echo off

robocopy ".\NSIS Extensions" "%PROGRAMFILES(x86)%\NSIS" /s