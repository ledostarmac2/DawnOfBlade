$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$repo = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repo "assets\branding\dawn_of_blade_icon_transparent.png"
$output = Join-Path $repo "assets\branding\dawn_of_blade_icon_transparent.ico"
$sizes = @(16, 32, 48, 64, 128, 256)
$images = @()
$sourceBitmap = [System.Drawing.Bitmap]::FromFile($source)

try {
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap($sourceBitmap, $size, $size)
        $memory = New-Object System.IO.MemoryStream
        $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
        $images += ,$memory.ToArray()
        $memory.Dispose()
    }

    $stream = [System.IO.File]::Create($output)
    $writer = New-Object System.IO.BinaryWriter($stream)
    try {
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count

        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $size = $sizes[$i]
            $writer.Write([Byte]$(if ($size -eq 256) { 0 } else { $size }))
            $writer.Write([Byte]$(if ($size -eq 256) { 0 } else { $size }))
            $writer.Write([Byte]0)
            $writer.Write([Byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$images[$i].Length)
            $writer.Write([UInt32]$offset)
            $offset += $images[$i].Length
        }

        foreach ($image in $images) {
            $writer.Write($image)
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}
finally {
    $sourceBitmap.Dispose()
}
