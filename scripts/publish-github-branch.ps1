[CmdletBinding()]
param(
    [switch]$PlanOnly,
    [ValidateSet('Auto', 'Available', 'Unavailable')]
    [string]$ProbeOverride = 'Auto',
    [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')]
    [string]$Repository = 'kirakimo2000-dot/Hajimao-DesktopShop',
    [ValidatePattern('^[A-Za-z0-9._/-]+$')]
    [string]$BaseBranch = 'main',
    [ValidatePattern('^[A-Za-z0-9._/-]+$')]
    [string]$FeatureBranch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$probeSeconds = 5

function Invoke-GitText {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }

    return ($output -join [Environment]::NewLine).Trim()
}

function Invoke-NativeBytes {
    param(
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)][string]$Arguments
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $FileName
    $startInfo.Arguments = $Arguments
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $process = [System.Diagnostics.Process]::Start($startInfo)
    $memory = New-Object System.IO.MemoryStream
    try {
        $process.StandardOutput.BaseStream.CopyTo($memory)
        $errorText = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            throw "$FileName $Arguments failed: $errorText"
        }

        return $memory.ToArray()
    }
    finally {
        $memory.Dispose()
        $process.Dispose()
    }
}

function Invoke-GitPushOnce {
    param([Parameter(Mandatory = $true)][string]$Branch)

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'git.exe'
    $startInfo.Arguments = (
        '-c http.lowSpeedLimit=1 -c http.lowSpeedTime=5 ' +
        "push --set-upstream origin HEAD:refs/heads/$Branch")
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        $output = $process.StandardOutput.ReadToEnd()
        $errorText = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        return [pscustomobject]@{
            Succeeded = $process.ExitCode -eq 0
            Output = ($output + [Environment]::NewLine + $errorText).Trim()
        }
    }
    finally {
        $process.Dispose()
    }
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('POST', 'PATCH')][string]$Method,
        [Parameter(Mandatory = $true)][string]$Endpoint,
        [Parameter(Mandatory = $true)][object]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($json)
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'gh.exe'
    $startInfo.Arguments = "api --method $Method $Endpoint --input -"
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        $process.StandardInput.BaseStream.Write($bytes, 0, $bytes.Length)
        $process.StandardInput.BaseStream.Close()
        $output = $process.StandardOutput.ReadToEnd()
        $errorText = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            throw "gh api $Endpoint failed: $errorText"
        }

        return $output | ConvertFrom-Json
    }
    finally {
        $process.Dispose()
    }
}

function Test-GitHubWebEndpoint {
    if ($ProbeOverride -eq 'Available') {
        return $true
    }

    if ($ProbeOverride -eq 'Unavailable') {
        return $false
    }

    $null = & curl.exe `
        --silent `
        --head `
        --connect-timeout $probeSeconds `
        --max-time $probeSeconds `
        https://github.com/ 2>$null
    return $LASTEXITCODE -eq 0
}

function Get-PublishingPlan {
    $transport = if (Test-GitHubWebEndpoint) { 'git' } else { 'api' }
    return [ordered]@{
        transport = $transport
        probeSeconds = $probeSeconds
        normalPushAttempts = 1
    }
}

function Get-ChangedPaths {
    param(
        [Parameter(Mandatory = $true)][string]$BaseSha,
        [Parameter(Mandatory = $true)][string]$Filter
    )

    $output = Invoke-GitText -Arguments @(
        'diff', '--no-renames', '--name-only', "--diff-filter=$Filter", "$BaseSha..HEAD", '--')
    if ([string]::IsNullOrWhiteSpace($output)) {
        return @()
    }

    return @($output -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Publish-WithGitDataApi {
    param(
        [Parameter(Mandatory = $true)][string]$Repo,
        [Parameter(Mandatory = $true)][string]$Branch,
        [Parameter(Mandatory = $true)][string]$RemoteBaseSha,
        [Parameter(Mandatory = $true)][string]$LocalTreeSha
    )

    $treeEntries = New-Object System.Collections.Generic.List[object]
    foreach ($path in (Get-ChangedPaths -BaseSha $RemoteBaseSha -Filter 'ACMRTUXB')) {
        $treeLine = Invoke-GitText -Arguments @('ls-tree', 'HEAD', '--', $path)
        if ($treeLine -notmatch '^(?<mode>[0-9]{6}) blob (?<sha>[0-9a-f]{40})\t') {
            throw "Could not read Git blob metadata for '$path'."
        }

        $mode = $Matches.mode
        $localBlobSha = $Matches.sha
        $blobBytes = Invoke-NativeBytes -FileName 'git.exe' -Arguments "cat-file blob $localBlobSha"
        $blob = Invoke-GhJson `
            -Method POST `
            -Endpoint "repos/$Repo/git/blobs" `
            -Body ([ordered]@{
                content = [Convert]::ToBase64String($blobBytes)
                encoding = 'base64'
            })
        if ($blob.sha -ne $localBlobSha) {
            throw "Local blob SHA $localBlobSha does not match GitHub blob SHA $($blob.sha) for '$path'."
        }

        $treeEntries.Add([ordered]@{
            path = $path.Replace('\', '/')
            mode = $mode
            type = 'blob'
            sha = $localBlobSha
        })
    }

    foreach ($path in (Get-ChangedPaths -BaseSha $RemoteBaseSha -Filter 'D')) {
        $treeEntries.Add([ordered]@{
            path = $path.Replace('\', '/')
            mode = '100644'
            type = 'blob'
            sha = $null
        })
    }

    if ($treeEntries.Count -eq 0) {
        throw 'The feature branch has no file changes relative to the base branch.'
    }

    $tree = Invoke-GhJson `
        -Method POST `
        -Endpoint "repos/$Repo/git/trees" `
        -Body ([ordered]@{
            base_tree = $RemoteBaseSha
            tree = $treeEntries.ToArray()
        })
    if ($tree.sha -ne $LocalTreeSha) {
        throw "Local tree SHA $LocalTreeSha does not match GitHub tree SHA $($tree.sha)."
    }

    $commitMessage = Invoke-GitText -Arguments @('log', '-1', '--format=%B')
    $commit = Invoke-GhJson `
        -Method POST `
        -Endpoint "repos/$Repo/git/commits" `
        -Body ([ordered]@{
            message = $commitMessage
            tree = $LocalTreeSha
            parents = @($RemoteBaseSha)
        })
    $null = Invoke-GhJson `
        -Method POST `
        -Endpoint "repos/$Repo/git/refs" `
        -Body ([ordered]@{
            ref = "refs/heads/$Branch"
            sha = $commit.sha
        })

    return [ordered]@{
        transport = 'api'
        remoteCommit = $commit.sha
        remoteBranch = $Branch
        treeVerified = $true
    }
}

$plan = Get-PublishingPlan
if ($PlanOnly) {
    $plan | ConvertTo-Json -Compress
    exit 0
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $repoRoot
try {
    if (-not [string]::IsNullOrWhiteSpace((Invoke-GitText -Arguments @('status', '--porcelain')))) {
        throw 'Commit or clean all local changes before publishing.'
    }

    $currentBranch = Invoke-GitText -Arguments @('branch', '--show-current')
    if ([string]::IsNullOrWhiteSpace($FeatureBranch)) {
        $FeatureBranch = $currentBranch
    }

    if ($currentBranch -ne $FeatureBranch) {
        throw "Current branch '$currentBranch' does not match feature branch '$FeatureBranch'."
    }

    if ($FeatureBranch -eq $BaseBranch) {
        throw 'Refusing to publish the base branch as a feature branch.'
    }

    $localBaseSha = Invoke-GitText -Arguments @('rev-parse', "refs/heads/$BaseBranch")
    $localHeadSha = Invoke-GitText -Arguments @('rev-parse', 'HEAD')
    $localTreeSha = Invoke-GitText -Arguments @('rev-parse', 'HEAD^{tree}')
    $mergeBaseSha = Invoke-GitText -Arguments @('merge-base', $localBaseSha, $localHeadSha)
    if ($mergeBaseSha -ne $localBaseSha) {
        throw "Feature branch is not based on local '$BaseBranch' HEAD $localBaseSha."
    }

    $remoteBaseJson = & gh api "repos/$Repository/git/ref/heads/$BaseBranch" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Could not read remote base through GitHub API: $($remoteBaseJson -join [Environment]::NewLine)"
    }

    $remoteBaseSha = (($remoteBaseJson -join [Environment]::NewLine) | ConvertFrom-Json).object.sha
    if ($remoteBaseSha -ne $localBaseSha) {
        throw "Local base SHA $localBaseSha does not match remote base SHA $remoteBaseSha."
    }

    if ($plan.transport -eq 'git') {
        $pushResult = Invoke-GitPushOnce -Branch $FeatureBranch
        if ($pushResult.Succeeded) {
            [ordered]@{
                transport = 'git'
                remoteCommit = $localHeadSha
                remoteBranch = $FeatureBranch
                treeVerified = $true
            } | ConvertTo-Json -Compress
            exit 0
        }

        Write-Warning "The single normal push failed; switching to Git Data API: $($pushResult.Output)"
    }

    Publish-WithGitDataApi `
        -Repo $Repository `
        -Branch $FeatureBranch `
        -RemoteBaseSha $remoteBaseSha `
        -LocalTreeSha $localTreeSha | ConvertTo-Json -Compress
}
finally {
    Pop-Location
}
