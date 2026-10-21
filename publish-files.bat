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
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$repoUrl='https://github.com/TheMorgxx/Copying-and-transferring-files-from-a-flash-drive'
$repoPattern='^https://github\.com/TheMorgxx/Copying-and-transferring-files-from-a-flash-drive(?:\.git)?/?$'
function Invoke-GitChecked([string[]]$Arguments) {
    $output=@(& git.exe -c core.quotepath=false @Arguments)
    if($LASTEXITCODE -ne 0){throw ('Команда git не выполнена: '+($Arguments -join ' '))}
    return $output
}
function Test-GitHead {
    & git.exe rev-parse --verify --quiet HEAD > $null
    return ($LASTEXITCODE -eq 0)
}
# Запрещённые к публикации пути: сборочный мусор, готовые бинарники, архивы, секреты, зашифрованные бэкапы.
function Check-Paths([string[]]$Paths) {
    foreach($path in $Paths){
        if($path -match '(^|/)(bin|obj|release|\.vs|packages|TestResults)/|(^|/)Backup_\d{8}_\d{6}/|(^|/)known_roots\.json$|\.(zip|rar|7z|bak|enc|exe|dll|pdb|user|suo|pfx|p12|pem|key|snk)$|(^|/)\.env(?:\.|$)') {
            throw "В истории или индексе есть локальный/закрытый файл: $path. Публикация остановлена."
        }
    }
}
try {
    if(-not (Get-Command git -ErrorAction SilentlyContinue)){throw 'Установите Git for Windows и войдите в GitHub через Git Credential Manager.'}
    if(-not (Test-Path -LiteralPath '.gitignore')){throw 'Нет файла .gitignore рядом с батником. Без него в репозиторий попадут bin/obj. Публикация остановлена.'}
    if(-not (Test-Path -LiteralPath '.git')){
        # Первый запуск: папка ещё не git-репозиторий.
        Invoke-GitChecked @('init') | Out-Host
        Invoke-GitChecked @('symbolic-ref','HEAD','refs/heads/main') | Out-Host
    }
    $remotes=@(& git.exe remote)
    if($remotes -notcontains 'origin'){Invoke-GitChecked @('remote','add','origin',$repoUrl) | Out-Host}
    $remote=(@(Invoke-GitChecked @('remote','get-url','origin')) -join '').Trim()
    if($remote -notmatch $repoPattern){throw "origin должен указывать на $repoUrl"}
    $userName=(@(& git.exe config user.name) -join '').Trim()
    $userEmail=(@(& git.exe config user.email) -join '').Trim()
    if(-not $userName -or -not $userEmail){throw 'Задайте имя и почту: git config --global user.name "Имя" и git config --global user.email "почта"'}
    $branch=(@(Invoke-GitChecked @('branch','--show-current')) -join '').Trim()
    if($branch -ne 'main'){throw 'Переключитесь на main перед публикацией. Ветки автоматически не заменяются.'}
    # main на GitHub может ещё не существовать (пустой репозиторий).
    $remoteHasMain=(@(Invoke-GitChecked @('ls-remote','origin','refs/heads/main'))).Count -gt 0
    if($remoteHasMain){Invoke-GitChecked @('fetch','origin','main') | Out-Host}
    if(-not (Test-GitHead) -and $remoteHasMain){
        # Восстановить связь с существующим GitHub, не заменяя файлы на диске.
        Invoke-GitChecked @('reset','--mixed','origin/main') | Out-Host
        Invoke-GitChecked @('branch','--set-upstream-to=origin/main','main') | Out-Host
        # Файлы, которые есть на GitHub, но отсутствуют на диске (README, LICENSE), возвращаем,
        # чтобы коммит не удалил их по ошибке. Существующие файлы на диске не трогаются.
        foreach($missing in @(Invoke-GitChecked @('ls-files','--deleted'))){Invoke-GitChecked @('checkout','--',$missing) | Out-Host}
    }
    if($remoteHasMain){
        & git.exe merge-base --is-ancestor origin/main HEAD
        if($LASTEXITCODE -ne 0){throw 'Удалённая main содержит новые коммиты. Сначала согласуйте изменения; force push не используется.'}
    }
    if(Test-GitHead){
        $range='HEAD'
        if($remoteHasMain){$range='origin/main..HEAD'}
        foreach($commit in @(Invoke-GitChecked @('rev-list',$range))){Check-Paths @(Invoke-GitChecked @('ls-tree','-r','--name-only',$commit))}
    }
    # Публикуются ВСЕ файлы папки (включая новые), кроме исключённых через .gitignore.
    Invoke-GitChecked @('add','-A') | Out-Host
    $tracked=@(Invoke-GitChecked @('ls-files'));Check-Paths $tracked
    foreach($path in $tracked){
        if((Test-Path -LiteralPath $path -PathType Leaf) -and (Get-Item -LiteralPath $path).Length -gt 20MB){throw "Слишком большой файл для публикации исходников: $path"}
    }
    # Выводится только имя файла, никогда найденный секрет.
    $secretPatterns=@('ghp_[A-Za-z0-9]{30,}','github_pat_[A-Za-z0-9_]{40,}','sk-proj-[A-Za-z0-9_-]{30,}','-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----')
    foreach($path in $tracked|Where-Object {$_ -match '\.(cs|csproj|props|targets|sln|resx|config|json|xml|md|bat|txt|yml|yaml)$'}){
        if((Test-Path -LiteralPath $path) -and (Select-String -LiteralPath $path -Pattern $secretPatterns -Quiet)){throw "Обнаружен возможный секрет в $path; отправка остановлена."}
    }
    & git.exe diff --cached --quiet
    if($LASTEXITCODE -eq 1){
        Invoke-GitChecked @('commit','-m',('Update ConfigBackupTool sources '+(Get-Date -Format 'yyyy-MM-dd HH:mm'))) | Out-Host
    }elseif($LASTEXITCODE -ne 0){throw 'Не удалось проверить подготовленные изменения'}
    if(-not (Test-GitHead)){throw 'Нет ни одного коммита: в папке нечего публиковать.'}
    Invoke-GitChecked @('push','-u','origin','main') | Out-Host
    $head=(@(Invoke-GitChecked @('rev-parse','HEAD')) -join '').Trim()
    $remoteHead=(@(Invoke-GitChecked @('ls-remote','origin','refs/heads/main')) -join '') -split '\s+'
    if($remoteHead[0] -ne $head){throw 'GitHub не подтвердил отправленный коммит'}
    Write-Host "Готово: $repoUrl/commit/$head"
    exit 0
}catch{Write-Host ('ОШИБКА: '+$_.Exception.Message) -ForegroundColor Red;exit 1}
