@echo off
rem Prepara con Blender los muebles del dormitorio de Daniel (modelos de Tripo del usuario, 19/11/2026).
rem Entrada: ArtSource\Tripo\<nombre>\tripo-out\usuario\model.glb   Salida: Assets\Models\Retro\Tripo\<nombre>\
rem Registro: ArtSource\Tripo\_prep\dormitorio.log
cd /d "%~dp0.."
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
set LOG=ArtSource\Tripo\_prep\dormitorio.log
if not exist ArtSource\Tripo\_prep mkdir ArtSource\Tripo\_prep
echo INICIO %date% %time% > %LOG%
if not exist %BLENDER% echo Error: no se encuentra Blender en %BLENDER% >> %LOG%
call :prep cama 12000 1.09
call :prep mesita_noche 6000 0.72
call :prep escritorio 10000 0.77
call :prep libreria 12000 1.90
call :prep radiador 10000 0.75
call :prep alfombra 2000 0.066
call :prep corcho 3000 0.90
call :prep ventana_cortinas 10000 1.20
call :prep planta_colgante 10000 0.80
echo FIN %date% %time% >> %LOG%
goto :eof

:prep
echo %1 ... >> %LOG%
%BLENDER% -b --factory-startup -P tools\tripo_prepare.py -- "ArtSource\Tripo\%1\tripo-out\usuario\model.glb" "ArtSource\Tripo\_prep\%1" %1 %2 %3 2>&1 | findstr /C:"TRIPO_PREPARE" /C:"Error" /C:"Traceback" >> %LOG%
if not exist "ArtSource\Tripo\_prep\%1\%1.gltf" (echo %1: FALLO en Blender >> %LOG% & goto :eof)
if not exist "Assets\Models\Retro\Tripo\%1" mkdir "Assets\Models\Retro\Tripo\%1"
copy /y "ArtSource\Tripo\_prep\%1\*" "Assets\Models\Retro\Tripo\%1\" > nul
goto :eof
