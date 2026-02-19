param([string]$PkgdefPath, [string]$DllName)
$content = [System.IO.File]::ReadAllText($PkgdefPath)
if ($content -notmatch 'CodeBase') {
    $codeBaseLine = "`"CodeBase`"=`"`$PackageFolder`$\$DllName`""
    # Insert CodeBase inside the Packages section, right after AllowsBackgroundLoad
    $content = $content.Replace(
        '"AllowsBackgroundLoad"=dword:00000001',
        "`"AllowsBackgroundLoad`"=dword:00000001`r`n$codeBaseLine"
    )
    $encoding = [System.Text.Encoding]::Unicode
    [System.IO.File]::WriteAllText($PkgdefPath, $content, $encoding)
}
