param(
    [string]$Source = 'src/AgeCheck/Assets/Logo.png',
    [string]$Destination = 'src/AgeCheck/Assets/AgeCheck.ico'
)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
Add-Type -AssemblyName PresentationCore
$bitmap = [System.Windows.Media.Imaging.BitmapFrame]::Create([Uri]::new((Resolve-Path -LiteralPath $Source).Path))
$sizes = @(16, 32, 48, 64, 128, 256)
$frames = foreach ($size in $sizes) {
    $scale = $size / [double]$bitmap.PixelWidth
    $resized = [System.Windows.Media.Imaging.TransformedBitmap]::new($bitmap, [System.Windows.Media.ScaleTransform]::new($scale, $scale))
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($resized))
    $stream = [System.IO.MemoryStream]::new()
    $encoder.Save($stream)
    [pscustomobject]@{ Size = $size; Data = $stream.ToArray() }
    $stream.Dispose()
}
$output = [System.IO.File]::Create((Join-Path $PWD $Destination))
try {
    $writer = [System.IO.BinaryWriter]::new($output)
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $writer.Write([byte]($frame.Size % 256))
        $writer.Write([byte]($frame.Size % 256))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frame.Data.Length)
        $writer.Write([uint32]$offset)
        $offset += $frame.Data.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Data) }
    $writer.Flush()
}
finally { $output.Dispose() }
