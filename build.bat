@echo off
echo === Building ScreenTranslator ===
echo.

echo [1/2] Publishing .NET application...
dotnet publish ScreenTranslator -c Release -r win-x64 --self-contained
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo [2/2] Creating zip package...
if not exist output mkdir output
powershell -NoProfile -Command "Compress-Archive -Path 'ScreenTranslator\bin\Release\net8.0-windows\win-x64\publish\*' -DestinationPath 'output\ScreenTranslator-1.0.0.zip' -Force"
if %ERRORLEVEL% neq 0 (
    echo Zip creation failed!
    pause
    exit /b 1
)

echo.
echo === Build complete! ===
echo Output: output\ScreenTranslator-1.0.0.zip
pause
