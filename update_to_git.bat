@echo off
REM === Auto Git Update Script ===

REM Go to your project folder
cd /d "C:\Users\Admin\Desktop\Test\TodoAppSolution"

REM Add all changes
git add .

REM Create a commit message with current date and time
set datetime=%date% %time%
git commit -m "Auto update on %datetime%"

REM Push to GitHub main branch
git push origin main

echo.
echo ✅ Project successfully updated to GitHub!
pause
