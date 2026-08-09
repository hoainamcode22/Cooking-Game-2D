$debugPattern = [regex]'^\s*Debug\.(Log|LogWarning|LogError|LogException|LogFormat|LogWarningFormat|LogErrorFormat)\s*\('

$folders = @(
    "e:/Game2/Cooking-Game-2D/Assets/_Game",
    "e:/Game2/Cooking-Game-2D/Assets/Day_Night/Scripts"
)

$totalModified = 0
$totalLines = 0

foreach ($folder in $folders) {
    $files = Get-ChildItem -Path $folder -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        $lines = [System.IO.File]::ReadAllLines($file.FullName, [System.Text.Encoding]::UTF8)
        $result = New-Object System.Collections.Generic.List[string]
        $skipDepth = 0
        $modified = $false

        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]

            # --- we are inside a multi-line Debug.Log, keep skipping ---
            if ($skipDepth -gt 0) {
                foreach ($c in $line.ToCharArray()) {
                    if ($c -eq '(') { $skipDepth++ }
                    elseif ($c -eq ')') { $skipDepth-- }
                }
                $totalLines++
                if ($skipDepth -lt 0) { $skipDepth = 0 }
                continue
            }

            # --- standalone line starting with Debug.Log ---
            if ($debugPattern.IsMatch($line)) {
                $depth = 0
                foreach ($c in $line.ToCharArray()) {
                    if ($c -eq '(') { $depth++ }
                    elseif ($c -eq ')') { $depth-- }
                }
                $modified = $true
                $totalLines++
                if ($depth -gt 0) {
                    $skipDepth = $depth   # multi-line call
                }
                continue   # drop this line regardless
            }

            # --- inline catch { Debug.LogException(ex); } pattern ---
            if ($line -match '\{\s*Debug\.(Log|LogWarning|LogError|LogException)\s*\(') {
                $newLine = $line -replace '\{\s*Debug\.(Log|LogWarning|LogError|LogException)\s*\((?:[^()]*|\((?:[^()]*)\))*\);\s*\}', '{ }'
                if ($newLine -ne $line) {
                    $modified = $true
                    $totalLines++
                    $result.Add($newLine)
                    continue
                }
            }

            $result.Add($line)
        }

        if ($modified) {
            [System.IO.File]::WriteAllLines($file.FullName, $result.ToArray(), [System.Text.Encoding]::UTF8)
            $totalModified++
            Write-Host "  Cleaned: $($file.FullName.Replace('e:/Game2/Cooking-Game-2D/', ''))"
        }
    }
}

Write-Host ""
Write-Host "Done: $totalModified files modified, ~$totalLines log lines removed."
