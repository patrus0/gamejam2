@echo off
echo Dobavlenie vsekh izmeneniy...
git add .
set /p commit_msg="Vvedite soobshchenie kommita: "
git commit -m "%commit_msg%"
echo Kommit sozdan.
pause