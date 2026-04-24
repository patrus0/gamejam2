@echo off
set /p branch_name="Vvedite imya novoy vetki: "
git checkout -b %branch_name%
echo Sozdana i aktivirovana vetka '%branch_name%'.
pause