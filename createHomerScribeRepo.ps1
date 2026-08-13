# createHomerScribeRepo.ps1 -- establishes the HomerScribe repo on
# github.com as JamalMazrui/HomerScribe from the folder this script lives in,
# normally C:\HomerScribe, and pushes the first commit.
#
# ONE-TIME bootstrap. After this, publishing a release is tagRelease's job.
# Idempotent all the same: rerunning it wires up whatever is not yet wired.
#
# Requires git and an authenticated gh; run "gh auth login" once beforehand.
#
# ErrorActionPreference stays Continue: under Windows PowerShell 5.1 with the
# Stop preference, redirecting a native command's error stream (as the probes
# below do with *>) wraps any stderr line in a terminating NativeCommandError,
# so a mere "repo not found" probe would kill the script. Success is judged by
# $LASTEXITCODE after every call instead.

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Continue"
Set-Location -Path $PSScriptRoot

$sOwner = "JamalMazrui"
$sName = "HomerScribe"
$sFull = $sOwner + "/" + $sName
$sUrl = "https://github.com/" + $sFull
$sDescription = "Describes video and transcribes speech on Windows, entirely on your own machine. Writes a described copy of a film with the description as its first audio track, a transcript of what is said, and both interleaved for reading on a braille display. No account, no upload. Part of the Homer Tools series."

Write-Output ("[INFO] Repo setup started " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
Write-Output ("[INFO] Working directory: " + (Get-Location).Path)
if ($DryRun) { Write-Output "[INFO] Dry run: nothing will be changed on this machine or on github.com" }

foreach ($sTool in @("git", "gh")) {
    if (-not (Get-Command $sTool -ErrorAction SilentlyContinue)) {
        Write-Output ("[ERROR] " + $sTool + " was not found on the PATH.")
        exit 1
    }
}

& gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Output "[ERROR] gh is not authenticated; run: gh auth login"
    exit 1
}

# What is already there, so the report below is honest before anything is done.
$sOldName = "HomerDescribe"
$sOldFull = $sOwner + "/" + $sOldName

$bRepoExists = $false
& gh repo view $sFull *> $null
if ($LASTEXITCODE -eq 0) { $bRepoExists = $true }
if ($bRepoExists) { Write-Output ("[INFO] " + $sFull + " already exists on github.com") }
else { Write-Output ("[INFO] " + $sFull + " does not exist on github.com yet") }

# The program was called HomerDescribe until it learnt to transcribe as well.
# If that repository is still there and the new name is not taken, RENAME it
# rather than leaving an abandoned repo beside a new one. GitHub keeps the
# history and redirects the old address, so an existing clone still works.
$bWillRename = $false
if (-not $bRepoExists) {
    & gh repo view $sOldFull *> $null
    if ($LASTEXITCODE -eq 0) {
        $bWillRename = $true
        Write-Output ("[INFO] " + $sOldFull + " exists and " + $sName + " does not, so it will be RENAMED.")
        Write-Output "[INFO] GitHub keeps the history and redirects the old address."
    }
}

$bLocalRepo = Test-Path ".git"
if ($bLocalRepo) { Write-Output "[INFO] The local repository already exists" }
else { Write-Output "[INFO] There is no local repository here yet" }

# Guard against publishing something that should not be published. The build
# products are large and are meant to be release assets, not repository content.
foreach ($sBig in @("ffmpeg.exe", "ffprobe.exe", "yt-dlp.exe", "youtube-dl.exe")) {
    if (Test-Path $sBig) {
        $nMb = [math]::Round((Get-Item $sBig).Length / 1MB, 1)
        Write-Output ("[INFO] " + $sBig + " is here, " + $nMb + " MB. .gitignore keeps it out of the repository.")
    }
}
if (-not (Test-Path ".gitignore")) {
    Write-Output "[ERROR] There is no .gitignore here. Refusing to run, because ffmpeg.exe and the"
    Write-Output "        run logs would be committed. Restore .gitignore and try again."
    exit 1
}

if ($DryRun) {
    if ($bWillRename) { Write-Output ("[INFO] Would rename " + $sOldFull + " to " + $sName + " on github.com") }
    Write-Output "[INFO] Would run: git init -b main, git add -A, git commit"
    if ($bRepoExists -or $bWillRename) { Write-Output ("[INFO] Would point origin at " + $sUrl + " and push") }
    else { Write-Output ("[INFO] Would create " + $sUrl + " as public and push") }
    Write-Output "[INFO] Dry run finished. Nothing was changed."
    exit 0
}

if ($bWillRename) {
    Write-Output ("[INFO] Renaming " + $sOldFull + " to " + $sName)
    & gh repo rename $sName --repo $sOldFull --yes
    if ($LASTEXITCODE -ne 0) {
        Write-Output "[ERROR] The rename failed. Nothing else has been changed."
        Write-Output ("[ERROR] Rename it by hand at " + "https://github.com/" + $sOldFull + "/settings")
        exit 1
    }
    $bRepoExists = $true
    Write-Output ("[INFO] Renamed. The repo is now " + $sUrl)
}

if (-not $bLocalRepo) {
    Write-Output "[INFO] Initializing the local repository"
    & git init -b main
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

# The remote must exist before anything is pushed: git will not create one.
if (-not $bRepoExists) {
    Write-Output ("[INFO] Creating " + $sUrl)
    & gh repo create $sFull --public --description $sDescription
    if ($LASTEXITCODE -ne 0) { exit 1 }
    $bRepoExists = $true
}

# Wire origin, whether or not it was there before.
& git remote get-url origin *> $null
if ($LASTEXITCODE -ne 0) {
    & git remote add origin ($sUrl + ".git")
    if ($LASTEXITCODE -ne 0) { exit 1 }
} else {
    & git remote set-url origin ($sUrl + ".git")
    if ($LASTEXITCODE -ne 0) { exit 1 }
}
Write-Output ("[INFO] origin is " + $sUrl + ".git")

# THE PART THAT MATTERS AFTER A RENAME. The renamed repository still carries all
# of HomerDescribe's history, while a freshly initialised local repository has a
# root commit of its own. The two are unrelated, and git refuses the push --
# rightly, because accepting it would throw the old history away.
#
# So: fetch what is there, move onto it WITHOUT touching the working tree, and
# let the new files become one ordinary commit on top. Nothing is lost and
# nothing is forced.
& git fetch origin *> $null
$bRemoteHasMain = $false
$sHeads = & git ls-remote --heads origin main 2>$null
if ($LASTEXITCODE -eq 0 -and $sHeads) { $bRemoteHasMain = $true }

if ($bRemoteHasMain) {
    $bNeedRebase = $true
    & git rev-parse --verify HEAD *> $null
    if ($LASTEXITCODE -eq 0) {
        & git merge-base --is-ancestor origin/main HEAD *> $null
        if ($LASTEXITCODE -eq 0) { $bNeedRebase = $false }
    }
    if ($bNeedRebase) {
        Write-Output "[INFO] The repository already has history. Placing this work on top of it."
        Write-Output "[INFO] The working files are left exactly as they are."
        & git reset --mixed origin/main
        if ($LASTEXITCODE -ne 0) {
            Write-Output "[ERROR] Could not move onto the existing history."
            exit 1
        }
    }
}

& git add -A
if ($LASTEXITCODE -ne 0) { exit 1 }

& git diff --cached --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Output "[INFO] Nothing new to commit"
} else {
    & git commit -m "HomerScribe, describing video and transcribing speech"
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

& git push -u origin main
if ($LASTEXITCODE -ne 0) {
    Write-Output "[ERROR] The push failed. Nothing local has been lost."
    Write-Output "[ERROR] See the messages above; run this script again once the cause is clear."
    exit 1
}

Write-Output ("[INFO] Done. The repo is at " + $sUrl)
Write-Output "[INFO] Next: run buildHomerScribe.cmd, commit, then tagRelease to publish a release."
