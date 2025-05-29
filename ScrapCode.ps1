$basePath = "C:\Users\windo\source\repos\MindMapO1\MindMap"
$outputFile = ".\output.txt"

Remove-Item -Path $outputFile -ErrorAction SilentlyContinue

Get-ChildItem -Path $basePath -Recurse -File | Where-Object {
    ($_.Extension -eq ".cs" -or $_.Extension -eq ".xaml") -and
    (-not $_.Name.EndsWith(".AssemblyInfo.cs")) -and
    (-not $_.Name.EndsWith(".g.cs")) -and
    (-not $_.Name.EndsWith(".g.i.cs")) -and
    (-not $_.Name.EndsWith(".g.i.xaml.cs")) -and
    (-not $_.Name.EndsWith(".AssemblyAttributes.cs"))
} | ForEach-Object {
    echo "Processing file: $($_.FullName)"
    $relativePath = $_.FullName.Substring($basePath.Length).TrimStart('\','/')
    "``````" | Out-File -FilePath $outputFile -Encoding utf8 -Append
    "// $relativePath" | Out-File -FilePath $outputFile -Encoding utf8 -Append
    Get-Content -Path $_.FullName | Out-File -FilePath $outputFile -Encoding utf8 -Append
    "``````" | Out-File -FilePath $outputFile -Encoding utf8 -Append
    "" | Out-File -FilePath $outputFile -Encoding utf8 -Append
}
