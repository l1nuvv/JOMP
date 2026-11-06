$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$dest=Join-Path $repo 'Assets/JustOneMorePeel/Art/PolyHaven'
New-Item -ItemType Directory -Force $dest | Out-Null
$manifest=@()
foreach($assetId in @('tool_cart','painted_wooden_bench','painted_plaster_wall')) {
    $info=Invoke-RestMethod ('https://api.polyhaven.com/info/'+$assetId)
    $files=Invoke-RestMethod ('https://api.polyhaven.com/files/'+$assetId)
    $info | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $PSScriptRoot ($assetId+'-info.json'))
    $chosen=@()
    if($files.fbx) { $chosen+= $files.fbx.'1k'.fbx }
    $chosen+=$files.Diffuse.'1k'.jpg
    $chosen+=$files.nor_gl.'1k'.jpg
    foreach($entry in $chosen) {
        if(!$entry.url) { throw "Missing file for $assetId" }
        $path=Join-Path $dest ([IO.Path]::GetFileName(([uri]$entry.url).AbsolutePath))
        if(!(Test-Path $path)) { Invoke-WebRequest $entry.url -OutFile $path }
        $hash=(Get-FileHash -LiteralPath $path -Algorithm MD5).Hash.ToLowerInvariant()
        if($hash -ne $entry.md5) { throw "Hash mismatch: $path" }
        $manifest += [pscustomobject]@{asset=$assetId;url=$entry.url;file=[IO.Path]::GetFileName($path);md5=$hash;bytes=(Get-Item $path).Length}
    }
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $PSScriptRoot 'download-manifest.json')
Write-Output ("Verified {0} files, {1} bytes" -f $manifest.Count,($manifest | Measure-Object bytes -Sum).Sum)
