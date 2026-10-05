param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$storeRoot = Join-Path $ProjectRoot 'Docs/Store'

# ImageGen 원본을 유지하고 업로드용 크기·PNG 색상 형식만 변환한다.
function Export-Png([string]$Source, [string]$Destination, [int]$Width, [int]$Height, [bool]$Alpha) {
    $sourceImage = [Drawing.Image]::FromFile($Source)
    $pixelFormat = if ($Alpha) { [Drawing.Imaging.PixelFormat]::Format32bppArgb } else { [Drawing.Imaging.PixelFormat]::Format24bppRgb }
    $outputImage = [Drawing.Bitmap]::new($Width, $Height, $pixelFormat)
    $graphics = [Drawing.Graphics]::FromImage($outputImage)
    $attributes = [Drawing.Imaging.ImageAttributes]::new()
    try {
        $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $attributes.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
        $scale = [Math]::Max($Width / [double]$sourceImage.Width, $Height / [double]$sourceImage.Height)
        $sourceWidth = $Width / $scale
        $sourceHeight = $Height / $scale
        $graphics.DrawImage($sourceImage, [Drawing.Rectangle]::new(0, 0, $Width, $Height),
            [single](($sourceImage.Width - $sourceWidth) / 2), [single](($sourceImage.Height - $sourceHeight) / 2),
            [single]$sourceWidth, [single]$sourceHeight, [Drawing.GraphicsUnit]::Pixel, $attributes)
        $outputImage.Save($Destination, [Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $attributes.Dispose()
        $graphics.Dispose()
        $outputImage.Dispose()
        $sourceImage.Dispose()
    }
}

Export-Png (Join-Path $storeRoot 'Source/FeatureGraphic-master.png') (Join-Path $storeRoot 'feature-graphic.png') 1024 500 $false
Export-Png (Join-Path $ProjectRoot 'Assets/PocketBloom/Art/AppIcon.png') (Join-Path $storeRoot 'app-icon.png') 512 512 $true

$assetFacts = foreach ($assetName in @('feature-graphic.png', 'app-icon.png')) {
    $assetPath = Join-Path $storeRoot $assetName
    $assetImage = [Drawing.Image]::FromFile($assetPath)
    try {
        [pscustomobject]@{
            file = $assetName; width = $assetImage.Width; height = $assetImage.Height
            pixelFormat = $assetImage.PixelFormat.ToString(); bytes = (Get-Item -LiteralPath $assetPath).Length
            sha256 = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash
        }
    } finally { $assetImage.Dispose() }
}

$textFacts = foreach ($locale in @('ko-KR', 'en-US')) {
    foreach ($field in @('title', 'short-description', 'full-description')) {
        $textPath = Join-Path $storeRoot "$locale/$field.txt"
        $textValue = [IO.File]::ReadAllText($textPath).TrimEnd()
        $maxLength = switch ($field) { 'title' { 30 } 'short-description' { 80 } 'full-description' { 4000 } }
        if ($textValue.Length -gt $maxLength) { throw "$locale/$field exceeds $maxLength characters." }
        [pscustomobject]@{ locale = $locale; field = $field; characters = $textValue.Length; limit = $maxLength }
    }
}
$evidence = [pscustomobject]@{
    utc = [DateTime]::UtcNow.ToString('o')
    scope = 'Local Google Play listing draft: file dimensions, PNG formats and text length. No account upload, app-name clearance or device screenshots verified.'
    assets = @($assetFacts); text = @($textFacts)
}
$evidence | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $ProjectRoot 'Docs/Validation/StoreAssets.json') -Encoding utf8
$evidence | ConvertTo-Json -Depth 5
