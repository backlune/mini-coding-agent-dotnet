# Commit the .NET conversion and publish it to a new GitHub repository.
#
# Usage:
#   .\scripts\push-to-new-repo.ps1 -RepoName <name> [-Visibility public|private] [-Owner <user-or-org>]
#
# Examples:
#   .\scripts\push-to-new-repo.ps1 -RepoName mini-coding-agent-dotnet
#   .\scripts\push-to-new-repo.ps1 -RepoName mini-coding-agent-dotnet -Visibility public -Owner my-org
#
# If `gh` is installed and authenticated, the script creates the GitHub repo
# for you. Otherwise, create an EMPTY repo in the browser first (no README,
# no .gitignore, no license) and re-run with -Owner set.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoName,

    [ValidateSet('public', 'private')]
    [string]$Visibility = 'private',

    [string]$Owner = ''
)

$ErrorActionPreference = 'Stop'

function Test-Command {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

# Move to the repo root (parent of this script's directory).
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# 1. Stage and commit any outstanding changes (the .NET conversion).
$pending = git status --porcelain
if ([string]::IsNullOrWhiteSpace($pending)) {
    Write-Host 'Working tree is clean, skipping commit.'
} else {
    git add -A
    git commit -m 'Port mini-coding-agent from Python to .NET 10 / C#'
    if ($LASTEXITCODE -ne 0) { throw 'git commit failed' }
}

# 2. Create the remote repo (if gh is available) or validate manual setup.
$hasGh = Test-Command 'gh'
if ($hasGh) {
    gh auth status *> $null
    $ghAuthed = ($LASTEXITCODE -eq 0)
} else {
    $ghAuthed = $false
}

if ($ghAuthed) {
    if ([string]::IsNullOrEmpty($Owner)) {
        $Owner = (gh api user --jq .login).Trim()
    }
    $fullName = "$Owner/$RepoName"
    Write-Host "Creating $fullName ($Visibility) via gh..."
    gh repo create $fullName "--$Visibility" --source=. --remote=new-origin --push=$false
    if ($LASTEXITCODE -ne 0) { throw 'gh repo create failed' }
    $targetUrl = (gh repo view $fullName --json sshUrl --jq .sshUrl).Trim()
} else {
    if ([string]::IsNullOrEmpty($Owner)) {
        throw "gh not available or not authenticated. Pass -Owner <your-github-user> and create the empty repo in the browser first."
    }
    $targetUrl = "https://github.com/$Owner/$RepoName.git"
    Write-Host "gh not detected. Make sure $targetUrl already exists (empty) on GitHub."
}

# 3. Retarget origin. Keep the old upstream fork URL as `fork-origin` for reference.
$originUrl = git remote get-url origin 2>$null
if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrEmpty($originUrl)) {
    git remote rename origin fork-origin
    if ($LASTEXITCODE -ne 0) { throw 'git remote rename failed' }
    Write-Host "Old origin preserved as 'fork-origin' -> $originUrl"
}

# Clean up gh's temporary 'new-origin' if it was added.
git remote get-url new-origin *> $null
if ($LASTEXITCODE -eq 0) {
    git remote remove new-origin
}

git remote add origin $targetUrl
if ($LASTEXITCODE -ne 0) { throw 'git remote add origin failed' }

# 4. Push.
git push -u origin main
if ($LASTEXITCODE -ne 0) { throw 'git push failed' }

Write-Host ''
Write-Host "Done. New remote: $targetUrl"
