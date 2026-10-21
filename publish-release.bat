@echo off
setlocal EnableExtensions
chcp 65001 >nul
set "CBT_PUBLISH_SCRIPT=%~f0"
powershell.exe -NoProfile -Command "$source=[IO.File]::ReadAllText($env:CBT_PUBLISH_SCRIPT); & ([scriptblock]::Create(($source -split '(?m)^# POWERSHELL\r?$',2)[1]))"
set "PUBLISH_EXIT=%ERRORLEVEL%"
if /i not "%~1"=="nopause" pause
exit /b %PUBLISH_EXIT%
# POWERSHELL
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetDirectoryName($env:CBT_PUBLISH_SCRIPT)
Set-Location -LiteralPath $root
$OutputEncoding=[Text.UTF8Encoding]::new($false)
[Console]::InputEncoding=[Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$appName='ConfigBackupTool'
$exeName='ConfigBackupTool.exe'
$headers=@{};$credential=@{};$lines=$null
function Github([string]$Method,[string]$Uri,[object]$Body=$null) {
    try {
        if($null -eq $Body){return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers -TimeoutSec 60}
        $json=ConvertTo-Json -InputObject $Body -Depth 8
        return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($json)) -TimeoutSec 60
    }catch{throw ('GitHub API: HTTP '+[int]$_.Exception.Response.StatusCode+'; проверьте права доступа и состояние репозитория.')}
}
try {
    foreach($tool in @('git','dotnet')){if(-not (Get-Command $tool -ErrorAction SilentlyContinue)){throw "Не найден $tool. Установите Git for Windows и .NET 8 SDK."}}
    # version.txt - единственное место, где задаётся версия релиза (X.Y.Z).
    $versionPath=Join-Path $root 'version.txt'
    if(-not (Test-Path -LiteralPath $versionPath)){
        [IO.File]::WriteAllText($versionPath,"1.0.0`n",[Text.UTF8Encoding]::new($false))
        Write-Host 'Создан version.txt с версией 1.0.0'
    }
    $version=([IO.File]::ReadAllText($versionPath)).Trim()
    if($version -notmatch '^\d+\.\d+\.\d+$'){throw 'Задайте версию X.Y.Z в version.txt'}
    $tag='v'+$version
    $env:GIT_TERMINAL_PROMPT='0';$env:GCM_INTERACTIVE='Never'
    $credentialStart=[Diagnostics.ProcessStartInfo]::new()
    $credentialStart.FileName=(Get-Command git.exe).Source
    $credentialStart.Arguments='credential fill'
    $credentialStart.UseShellExecute=$false;$credentialStart.CreateNoWindow=$true
    $credentialStart.RedirectStandardInput=$true;$credentialStart.RedirectStandardOutput=$true
    $credentialProcess=[Diagnostics.Process]::new();$credentialProcess.StartInfo=$credentialStart
    try {
        [void]$credentialProcess.Start()
        $credentialProcess.StandardInput.Write('protocol=https'+[char]10+'host=github.com'+[char]10+[char]10)
        $credentialProcess.StandardInput.Close()
        $lines=$credentialProcess.StandardOutput.ReadToEnd() -split '\r?\n'
        $credentialProcess.WaitForExit()
        if($credentialProcess.ExitCode -ne 0){throw 'Нет авторизации GitHub. Выполните git push и войдите через Git Credential Manager.'}
    }finally{$credentialProcess.Dispose()}
    foreach($line in $lines){if($line -match '^([^=]+)=(.*)$'){$credential[$matches[1]]=$matches[2]}}
    if(-not $credential.password){throw 'Git Credential Manager не вернул авторизацию GitHub'}
    $headers=@{Authorization=('Bearer '+$credential.password);Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='ConfigBackupTool-Publisher'}
    $base='https://api.github.com/repos/TheMorgxx/Copying-and-transferring-files-from-a-flash-drive'
    $existing=$null
    try {$existing=Invoke-RestMethod -Uri ($base+'/releases/tags/'+$tag) -Headers $headers -TimeoutSec 60}
    catch {if([int]$_.Exception.Response.StatusCode -ne 404){throw ('Не удалось проверить релиз: HTTP '+[int]$_.Exception.Response.StatusCode)}}
    if($existing -and -not $existing.draft){
        $head=(& git rev-parse --verify --quiet HEAD)
        if($LASTEXITCODE -eq 0 -and $existing.target_commitish -eq $head -and -not @(& git status --porcelain).Count -and @($existing.assets|Where-Object {$_.name -eq $exeName -and $_.state -eq 'uploaded'}).Count -eq 1){
            Write-Host ('Эта версия уже опубликована: '+$existing.html_url);exit 0
        }
        throw "Релиз $tag уже опубликован. Для новой сборки увеличьте версию в version.txt; опубликованные EXE не перезаписываются."
    }
    # Сборка: все проекты + публикация одного EXE в release\ (см. build.bat).
    & (Join-Path $root 'build.bat') nopause
    if($LASTEXITCODE -ne 0){throw 'Сборка не удалась; релиз не создаётся'}
    $exe=Join-Path $root ('release\'+$exeName)
    if(-not (Test-Path -LiteralPath $exe)){throw "Не найден $exe после сборки"}
    $binary=Get-Item -LiteralPath $exe
    $hasher=[Security.Cryptography.SHA256]::Create();$stream=[IO.File]::OpenRead($exe)
    try {$sha=[BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-','').ToLowerInvariant()}
    finally {$stream.Dispose();$hasher.Dispose()}
    if($binary.Length -eq 0){throw 'Собран пустой EXE'}
    # Публикуются исходники (все файлы папки, кроме .gitignore). Приложение не запускается.
    & (Join-Path $root 'publish-files.bat') nopause
    if($LASTEXITCODE -ne 0){throw 'Исходники не опубликованы; релиз не создаётся'}
    $commit=(& git rev-parse HEAD).Trim();if($LASTEXITCODE -ne 0){throw 'Не определён коммит сборки'}
    $remoteTags=@(& git ls-remote origin "refs/tags/$tag" "refs/tags/$tag^{}")
    if($LASTEXITCODE -ne 0){throw 'Не удалось проверить тег релиза'}
    $tagTarget=$null
    foreach($entry in $remoteTags){$parts=$entry -split '\s+';if(-not $tagTarget -or $parts[1].EndsWith('^{}')){$tagTarget=$parts[0]}}
    if($tagTarget -and $tagTarget -ne $commit){throw 'Тег этой версии уже относится к другому коммиту. Увеличьте версию.'}
    if($existing -and $existing.target_commitish -ne $commit){throw 'Черновик этого тега относится к другому коммиту. Используйте новую версию.'}
    $body="Готовая сборка $appName $version для Windows x64.`n`nСкачайте $exeName из Assets и поместите в отдельную папку. Установка .NET не требуется. Список игр games.json лежит рядом с EXE и редактируется во вкладке `"Игры`" в настройках.`n`nИсходники: $commit`nSHA-256 ${exeName}: $sha`n`nСборка пройдена. Приложение при публикации не запускалось."
    if($existing){$release=$existing}else{$release=Github 'Post' ($base+'/releases') @{tag_name=$tag;target_commitish=$commit;name=("$appName "+$version);body=$body;draft=$true;prerelease=$false}}
    Write-Host ('Черновик релиза: '+$release.id)
    $asset=@($release.assets|Where-Object {$_.name -eq $exeName})
    if($asset.Count -gt 0){
        if($asset.Count -ne 1 -or $asset[0].size -ne $binary.Length -or $asset[0].digest -ne ('sha256:'+$sha)){throw 'В черновике уже есть другой EXE. Он не заменён; создайте новую версию.'}
    }else{
        $upload=$release.upload_url.Split('{')[0]+'?name='+$exeName
        if(([Uri]$upload).Host -ne 'uploads.github.com'){throw 'Неожиданный адрес загрузки GitHub'}
        try {$asset=@(Invoke-RestMethod -Method Post -Uri $upload -Headers $headers -ContentType 'application/octet-stream' -InFile $exe -TimeoutSec 600)}
        catch {throw ('Не удалось загрузить EXE: HTTP '+[int]$_.Exception.Response.StatusCode+'. Черновик сохранён; повторите батку.')}
        if($asset[0].size -ne $binary.Length -or $asset[0].state -ne 'uploaded' -or $asset[0].digest -ne ('sha256:'+$sha)){throw 'GitHub не подтвердил размер и SHA-256 EXE; релиз оставлен черновиком'}
    }
    $published=Github 'Patch' $release.url @{draft=$false;make_latest='true';body=$body}
    $verified=Github 'Get' ($base+'/releases/tags/'+$tag)
    $asset=@($verified.assets|Where-Object {$_.name -eq $exeName})
    if($verified.draft -or $asset.Count -ne 1 -or $asset[0].digest -ne ('sha256:'+$sha)){throw 'Проверка опубликованного релиза не пройдена'}
    Write-Host ('Готово: '+$verified.html_url)
    Write-Host ('Скачать EXE: '+$asset[0].browser_download_url)
    Write-Host ('SHA-256: '+$sha)
    exit 0
}catch{Write-Host ('ОШИБКА: '+$_.Exception.Message) -ForegroundColor Red;exit 1}
finally{$headers.Clear();$credential.Clear();$lines=$null}
