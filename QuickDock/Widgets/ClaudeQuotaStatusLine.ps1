# Claude Code sends its status-line JSON through standard input.
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
try {
    $status = [Console]::In.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
} catch {
    exit 0
}

$limits = [ordered]@{}
$labels = @()
foreach ($window in @(@{ Name = 'five_hour'; Label = '5h' }, @{ Name = 'seven_day'; Label = '7d' })) {
    $value = $status.rate_limits.($window.Name)
    if ($null -eq $value.used_percentage -or $null -eq $value.resets_at) { continue }
    $limits[$window.Name] = @{
        used_percentage = $value.used_percentage
        resets_at = $value.resets_at
    }
    $remaining = [Math]::Min(100, [Math]::Max(0, 100 - [double]$value.used_percentage))
    $labels += ('{0} {1:0.#}% left' -f $window.Label, $remaining)
}

if ($limits.Count -eq 0) { exit 0 }

$directory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'QuickDock'
$target = Join-Path $directory 'claude-quota.json'
$temporary = Join-Path $directory ([Guid]::NewGuid().ToString('N') + '.tmp')
try {
    [void][IO.Directory]::CreateDirectory($directory)
    [IO.File]::WriteAllText($temporary, ($limits | ConvertTo-Json -Compress), [Text.UTF8Encoding]::new($false))
    if (![IO.File]::Exists($target)) {
        try { [IO.File]::Move($temporary, $target) }
        catch [IO.IOException] {
            if (![IO.File]::Exists($target)) { throw }
        }
    }
    if ([IO.File]::Exists($temporary)) { [IO.File]::Replace($temporary, $target, [NullString]::Value) }
} catch {
    Write-Verbose "Unable to update the QuickDock quota cache."
} finally {
    if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
}

'Claude: ' + ($labels -join ' | ')
