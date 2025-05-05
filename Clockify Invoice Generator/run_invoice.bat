@echo off
setlocal

REM Define venv and script paths
set VENV_DIR=.venv
set ACTIVATE_PATH=%VENV_DIR%\Scripts\activate.bat

REM Create virtual environment if it doesn't exist
if not exist %VENV_DIR% (
    echo Creating virtual environment...
    python -m venv %VENV_DIR%
)

REM Activate the virtual environment
if exist %ACTIVATE_PATH% (
    call %ACTIVATE_PATH%
) else (
    echo ERROR: Could not activate virtual environment.
    exit /b 1
)

REM Upgrade pip and install requirements using official PyPI index
echo Installing dependencies from PyPI...
pip install --upgrade pip --index-url https://pypi.org/simple
pip install -r requirements.txt --index-url https://pypi.org/simple

REM Run the Python invoice generator
echo Running invoice generator...
python main.py

REM Virtual environment will exit when this script ends
echo Done.
echo.
pause
