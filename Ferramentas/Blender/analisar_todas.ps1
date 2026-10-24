# Analisa e extrai TODAS as animacoes do Mixamo em D:\Faith Clash\Jogador (sem a malha).
# Saidas: Assets/_FaithClash/Art/Characters/Jogador/Animacoes2 (FBX leves), Ferramentas/analise/{previews,metricas}
param([string]$Origem = "D:\Faith Clash\Jogador", [string]$Projeto = "C:\Users\frank\Faith Clash")
$blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
$script = "$Projeto\Ferramentas\Blender\analisar_e_extrair.py"
$saida = "$Projeto\Assets\_FaithClash\Art\Characters\Jogador\Animacoes2"
$prev = "$Projeto\Ferramentas\analise\previews"; $met = "$Projeto\Ferramentas\analise\metricas"
$log = "$Projeto\Ferramentas\analise\log.txt"
New-Item -ItemType Directory -Force "$Projeto\Ferramentas\analise" | Out-Null
"" | Set-Content $log
$vistos = @{}
Get-ChildItem $Origem -Filter *.fbx | Sort-Object Name | ForEach-Object {
    if ($_.Name -eq "Ch06_nonPBR.fbx") { return }
    $base = $_.BaseName -replace "^Ch06_nonPBR@", ""
    $txt = $base -replace "\s*\((\d+)\)\s*$", " v`$1"
    $nome = ($txt -split "[\s_-]+" | ForEach-Object { if ($_.Length) { $_.Substring(0,1).ToUpper() + $_.Substring(1) } }) -join ""
    $nome = $nome -replace "[^A-Za-z0-9]", ""
    # o mesmo nome vindo da leva antiga (prefixo Ch06_nonPBR@) e da nova: guarda as duas
    if ($_.Name -like "Ch06_nonPBR@*") { $nome = $nome + "_Antiga" }
    & $blender -b -P $script -- $_.FullName $nome $saida $prev $met 2>&1 | Select-String "^OK|Error|Traceback" | ForEach-Object { $_.Line } | Add-Content $log
}
"FIM" | Add-Content $log
