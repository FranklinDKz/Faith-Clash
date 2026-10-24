#!/bin/bash
# Uso: ver_golpe.sh <animacao.fbx> <quadro_ini> <quadro_fim> <n> <saida.png>
B="/c/Program Files/Blender Foundation/Blender 5.2/blender.exe"
D="$(cd "$(dirname "$0")" && pwd)"
J="$(mktemp --suffix=.json)"
"$B" -b -P "$D/esqueleto_dump.py" -- "$1" "$J" "$2" "$3" "$4" > /dev/null 2>&1
python "$D/desenhar_esqueleto.py" "$J" "$5"
rm -f "$J"
