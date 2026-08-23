Add-Type -AssemblyName System.Drawing

$projectRoot = "e:\Game2\Cooking-Game-2D"
$inputDir = "$projectRoot\art_raw_input"
$previewDir = "$projectRoot\art_raw_input\PREVIEW_KIEM_TRA_O_DAT"
$assetsBase = "$projectRoot\Assets\Assetsgame"
$plotPath = "$projectRoot\Assets\maptitle\plotdat-removebg-preview.png"

if (-not (Test-Path $inputDir)) { New-Item -ItemType Directory -Path $inputDir -Force | Out-Null }
if (-not (Test-Path $previewDir)) { New-Item -ItemType Directory -Path $previewDir -Force | Out-Null }

$cropPoints = @(
    @{ X = 350; Y = 60 },
    @{ X = 270; Y = 100 }, @{ X = 430; Y = 100 },
    @{ X = 190; Y = 140 }, @{ X = 350; Y = 140 }, @{ X = 510; Y = 140 },
    @{ X = 120; Y = 180 }, @{ X = 270; Y = 180 }, @{ X = 430; Y = 180 }, @{ X = 580; Y = 180 },
    @{ X = 200; Y = 220 }, @{ X = 350; Y = 220 }, @{ X = 500; Y = 220 },
    @{ X = 280; Y = 260 }, @{ X = 420; Y = 260 },
    @{ X = 350; Y = 295 }
)
$sortedPoints = $cropPoints | Sort-Object { $_.Y } | Select-Object -First 12

$categoryMap = @{
    "rice" = "hatgiong\rice"; "lua" = "hatgiong\rice"
    "bapcai" = "hatgiong\bapcai"; "cabbage" = "hatgiong\bapcai"
    "ngo" = "hatgiong\ngo"; "corn" = "hatgiong\ngo"
    "cachua" = "hatgiong\cachua"; "tomato" = "hatgiong\cachua"
    "carot" = "hatgiong\carot"; "carrot" = "hatgiong\carot"
    "khoaitay" = "hatgiong\khoaitay"; "potato" = "hatgiong\khoaitay"
    "watermelon" = "hatgiong\watermelon"; "duahau" = "hatgiong\watermelon"
    "pumpkin" = "hatgiong\pumpkin"; "bido" = "hatgiong\pumpkin"
    "nam" = "hatgiong\nam"; "mushroom" = "hatgiong\nam"
    "mia" = "hatgiong\mia"; "sugarcane" = "hatgiong\mia"
    "chanh" = "hatgiong\chanh"; "lemon" = "hatgiong\chanh"
    "ot" = "hatgiong\ot"; "chili" = "hatgiong\ot"
    "tieu" = "hatgiong\tieu"; "pepper" = "hatgiong\tieu"
    "huongduong" = "hoa\huongduong"; "sunflower" = "hoa\huongduong"
    "hoahong" = "hoa\hoahong"; "rose" = "hoa\hoahong"
    "lavender" = "hoa\lavender"; "oaihuong" = "hoa\lavender"
    "hoacuctrang" = "hoa\hoacuctrang"; "daisy" = "hoa\hoacuctrang"
    "hoalan" = "hoa\hoalan"; "orchid" = "hoa\hoalan"
    "tulip" = "hoa\tulip"
    "hoacucvantho" = "hoa\hoacucvantho"; "marigold" = "hoa\hoacucvantho"
    "hoamaudon" = "hoa\hoamaudon"; "peony" = "hoa\hoamaudon"
    "hoacamtucau" = "hoa\hoacamtucau"; "hydrangea" = "hoa\hoacamtucau"
    "hoaanhthao" = "hoa\hoaanhthao"; "primrose" = "hoa\hoaanhthao"
    "house_01" = "Nhà\stages\house_01"; "home1" = "Nhà\stages\house_01"
    "house_02" = "Nhà\stages\house_02"; "home2" = "Nhà\stages\house_02"
    "house_03" = "Nhà\stages\house_03"; "home3" = "Nhà\stages\house_03"
    "house_04" = "Nhà\stages\house_04"; "home4" = "Nhà\stages\house_04"
    "house_05" = "Nhà\stages\house_05"; "home5" = "Nhà\stages\house_05"
    "train" = "Taulua"; "taulua" = "Taulua"
}

function Clean-And-Trim-Sprite {
    param([System.Drawing.Bitmap]$srcBmp)
    
    $w = $srcBmp.Width
    $h = $srcBmp.Height
    $outBmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    
    $minX = $w; $minY = $h; $maxX = 0; $maxY = 0
    
    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            $px = $srcBmp.GetPixel($x, $y)
            
            $isMagenta = ($px.R -gt 170 -and $px.B -gt 170 -and $px.G -lt 130)
            
            $isSoilOrFringe = $false
            if ($y -gt ($h * 0.82)) {
                if ($px.R -gt 60 -and $px.R -lt 145 -and $px.G -gt 35 -and $px.G -lt 95 -and $px.B -lt 60) {
                    $isSoilOrFringe = $true
                }
            }
            
            if ($isMagenta -or $isSoilOrFringe) {
                $outBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            } else {
                $outBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $px.R, $px.G, $px.B))
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    
    if ($minX -ge $maxX -or $minY -ge $maxY) { return $outBmp }
    
    $trimRect = [System.Drawing.Rectangle]::new($minX, $minY, ($maxX - $minX + 1), ($maxY - $minY + 1))
    $trimmed = $outBmp.Clone($trimRect, $outBmp.PixelFormat)
    $outBmp.Dispose()
    return $trimmed
}

function Generate-PlotPreview {
    param([System.Drawing.Bitmap]$plantSprite, [string]$assetName, [string]$outPreviewPath)
    
    if (-not (Test-Path $plotPath)) { return }
    $plotBmp = [System.Drawing.Bitmap]::FromFile($plotPath)
    
    $resW = 750
    $resH = 450
    $res = New-Object System.Drawing.Bitmap($resW, $resH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($res)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    
    $g.Clear([System.Drawing.Color]::FromArgb(255, 75, 130, 55))
    
    $plotX = [int](($resW - $plotBmp.Width) / 2)
    $plotY = 65
    $g.DrawImage($plotBmp, $plotX, $plotY, $plotBmp.Width, $plotBmp.Height)
    
    $targetHeight = 85
    $scale = $targetHeight / [Math]::Max($plantSprite.Height, 1)
    if ($scale -gt 0.45) { $scale = 0.45 }
    $pW = [int]($plantSprite.Width * $scale)
    $pH = [int]($plantSprite.Height * $scale)
    
    foreach ($pt in $sortedPoints) {
        $drawX = $plotX + $pt.X - [int]($pW / 2)
        $drawY = $plotY + $pt.Y - $pH + 8
        $g.DrawImage($plantSprite, $drawX, $drawY, $pW, $pH)
    }
    
    $font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 30, 20, 10))
    $g.FillRectangle($bgBrush, 20, 15, 420, 32)
    $g.DrawString("TRONG THU 12 CAY: $assetName", $font, $brush, 28, 20)
    
    $g.Dispose()
    $plotBmp.Dispose()
    
    $res.Save($outPreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $res.Dispose()
}

$inputFiles = Get-ChildItem -Path $inputDir -File | Where-Object { $_.Extension -match '\.(png|jpg|jpeg)$' }

if ($inputFiles.Count -eq 0) {
    Write-Host "Thu muc art_raw_input dang trong!" -ForegroundColor Yellow
    exit 0
}

Write-Host "=== BAT DAU XU LY $($inputFiles.Count) ANH ===" -ForegroundColor Cyan

foreach ($file in $inputFiles) {
    $fileName = $file.BaseName.ToLower()
    Write-Host "-> Dang xu ly: $($file.Name)" -ForegroundColor Green
    
    $targetSubDir = "misc"
    $isCropOrFlower = $false
    foreach ($key in $categoryMap.Keys) {
        if ($fileName -like "*$key*") {
            $targetSubDir = $categoryMap[$key]
            if ($targetSubDir -like "hatgiong*" -or $targetSubDir -like "hoa*") {
                $isCropOrFlower = $true
            }
            break
        }
    }
    
    $destFolder = Join-Path $assetsBase $targetSubDir
    if (-not (Test-Path $destFolder)) { New-Item -ItemType Directory -Path $destFolder -Force | Out-Null }
    
    $srcBmp = [System.Drawing.Bitmap]::FromFile($file.FullName)
    $cleanBmp = Clean-And-Trim-Sprite -srcBmp $srcBmp
    
    $outFileName = "$($file.BaseName).png"
    $outFilePath = Join-Path $destFolder $outFileName
    $cleanBmp.Save($outFilePath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "   + [BAN GIAO] Da luu: Assets/Assetsgame/$targetSubDir/$outFileName" -ForegroundColor White
    
    if ($isCropOrFlower) {
        $previewOutFile = Join-Path $previewDir "PREVIEW_$($file.BaseName)_TRONG_THU_O_DAT.png"
        Generate-PlotPreview -plantSprite $cleanBmp -assetName $file.BaseName -outPreviewPath $previewOutFile
        Write-Host "   + [PREVIEW] Da xuat anh trong thu 12 cay!" -ForegroundColor Yellow
    }
    
    $cleanBmp.Dispose()
    $srcBmp.Dispose()
}

Write-Host "=== HOAN TAT BAN GIAO VA XUAT PREVIEW! ===" -ForegroundColor Cyan
