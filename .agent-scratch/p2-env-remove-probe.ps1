$ErrorActionPreference = 'Continue'
foreach ($scope in @('Process','User','Machine')) {
    $v = [Environment]::GetEnvironmentVariable('GROK_PLUGIN_ROOT', $scope)
    $h = [Environment]::GetEnvironmentVariable('GROK_HOME', $scope)
    Write-Output ("SCOPE=$scope PLUGIN_SET=" + (-not [string]::IsNullOrWhiteSpace($v)) + " HOME_SET=" + (-not [string]::IsNullOrWhiteSpace($h)))
}
$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.UseShellExecute = $false
$before = $psi.Environment.ContainsKey('GROK_PLUGIN_ROOT')
$removed = $psi.Environment.Remove('GROK_PLUGIN_ROOT')
$after = $psi.Environment.ContainsKey('GROK_PLUGIN_ROOT')
Write-Output ("BEFORE=$before REMOVED=$removed AFTER=$after")
$copy = [System.Diagnostics.ProcessStartInfo]::new()
$copy.UseShellExecute = $false
foreach ($entry in $psi.Environment.GetEnumerator()) {
    $copy.Environment[$entry.Key] = $entry.Value
}
Write-Output ("COPY_HAS_PLUGIN=" + $copy.Environment.ContainsKey('GROK_PLUGIN_ROOT'))
$sourceKeys = [System.Collections.Generic.HashSet[string]]::new([string[]]$psi.Environment.Keys, [StringComparer]::OrdinalIgnoreCase)
$dropped = 0
foreach ($key in @($copy.Environment.Keys)) {
    if (-not $sourceKeys.Contains($key)) {
        [void]$copy.Environment.Remove($key)
        $dropped++
    }
}
Write-Output ("DROPPED=$dropped COPY_HAS_PLUGIN_AFTER=" + $copy.Environment.ContainsKey('GROK_PLUGIN_ROOT'))
