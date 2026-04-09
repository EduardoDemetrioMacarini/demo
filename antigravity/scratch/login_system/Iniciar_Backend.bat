@echo off
echo Iniciando o Servidor Backend (API de Login)...
echo Por favor, mantenha esta janela aberta enquanto estiver usando o sistema.
echo.

cd /d "%~dp0backend"
dotnet run
pause
