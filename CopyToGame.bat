@echo off

set "ENV_FILE=%~dp0.env"

:: Чтение всех переменных из .env
for /f "usebackq delims=" %%i in (`type "%ENV_FILE%" 2^>nul`) do set "%%i"

if not defined DEST (
    echo ERROR: DEST variable not found in %ENV_FILE%
    pause
    exit /b 1
)

git pull

:: Сохраняем PublishedFileId.txt (ID Workshop-предмета) из папки игры в репо,
:: иначе rd ниже полностью стирает папку мода и следующая публикация в Steam
:: создаёт НОВЫЙ предмет вместо обновления существующего.
if exist "%DEST%\About\PublishedFileId.txt" copy /y "%DEST%\About\PublishedFileId.txt" "%~dp0About\PublishedFileId.txt" >nul

rd /s /q "%DEST%"
set "SOURCE=%~dp0About"
xcopy "%SOURCE%" "%DEST%\About\" /e /i /h /k /y /r
set "SOURCE=%~dp0Assemblies"
xcopy "%SOURCE%" "%DEST%\Assemblies\" /e /i /h /k /y /r
set "SOURCE=%~dp0Defs"
xcopy "%SOURCE%" "%DEST%\Defs\" /e /i /h /k /y /r
set "SOURCE=%~dp0Languages"
xcopy "%SOURCE%" "%DEST%\Languages\" /e /i /h /k /y /r
set "SOURCE=%~dp0Textures"
xcopy "%SOURCE%" "%DEST%\Textures\" /e /i /h /k /y /r

start steam://rungameid/294100

:: pause
