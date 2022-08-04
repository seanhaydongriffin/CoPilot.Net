@echo off
cd /d %~dp0
call psubst.bat r: /D
call psubst.bat r: /D /P
call psubst.bat r: /D /PF
call psubst.bat r: "C:\auto"
call psubst.bat r: "C:\auto" /PF
call psubst.bat l: /D
call psubst.bat l: /D /P
call psubst.bat l: /D /PF
call psubst.bat l: "C:\auto"
call psubst.bat l: "C:\auto" /PF
