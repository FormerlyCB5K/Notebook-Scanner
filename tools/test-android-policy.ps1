$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!$env:JAVA_HOME) { throw 'Set JAVA_HOME to a JDK 17 installation first.' }
$output = Join-Path $projectRoot '.tools/java-policy-tests'
New-Item -ItemType Directory -Force $output | Out-Null
& "$env:JAVA_HOME/bin/javac.exe" -d $output "$projectRoot/android/app/src/main/java/org/notebookscanner/DeviceHistory.java" "$projectRoot/tests/android/DeviceHistoryTest.java"
if ($LASTEXITCODE -ne 0) { throw 'Android policy compilation failed.' }
& "$env:JAVA_HOME/bin/java.exe" -cp $output org.notebookscanner.DeviceHistoryTest
if ($LASTEXITCODE -ne 0) { throw 'Android policy checks failed.' }
