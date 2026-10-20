# Rosto do protagonista com KeenTools FaceBuilder

Fotos prontas em `Ferramentas/Blender/FotosFaceBuilder/` (recortes ampliados da folha de referência).
**Se você tiver as fotos originais em alta resolução, use elas no lugar** — quanto maior a foto, mais fiel fica o rosto.

## 1. Instalar
1. Baixe o FaceBuilder para Blender em keentools.io (tem período de teste).
2. No Blender: *Edit > Preferences > Add-ons > Install from Disk* → escolha o `.zip` baixado → ative **KeenTools FaceBuilder**.
3. No painel do addon, clique em **Install Core library** (ele baixa a biblioteca e pede para aceitar a licença).

## 2. Criar a cabeça
1. Arquivo novo (*File > New > General*), apague o cubo.
2. `N` para abrir a barra lateral → aba **FaceBuilder** → **Create a new head**.
3. **Add Images** → selecione as 5 fotos da pasta acima.
4. Para cada foto, clique em **Align face** (ou **Auto align**) e depois ajuste os pinos:
   - comece pela `01_frente`: arraste os pinos para canto dos olhos, ponta do nariz, cantos da boca, queixo e orelhas;
   - repita nas 3/4 e nos perfis. Use poucos pinos, bem colocados.
5. Em **Model**, deixe **Topology: Basic** (boa para jogo) e marque **Eyes**, **Jaw**, **Ears** se aparecerem.

## 3. Textura
1. Painel **Texture** → **Create texture** (tamanho **4096**).
2. Use as fotos da frente e dos perfis; ajuste **Angle strictness** se aparecer emenda.
3. **Export > Save as** → salve a imagem como `Ferramentas/Blender/Rosto_FaceBuilder.png`.

## 4. Salvar para eu importar
1. Renomeie o objeto da cabeça para **`Cabeca_FB`**.
2. *File > Save As* → `Ferramentas/Blender/Cabeca_FaceBuilder.blend`.
3. Me avise. Eu cuido do resto: encaixar no corpo e no esqueleto, cabelo cacheado, sobrancelhas, barba,
   olhos e boca como partes separadas, material na Unity e o retrato do HUD.

## Status
A cabeca ja foi montada no FaceBuilder (5 fotos, Auto Align) e salva em `Ferramentas/Blender/Cabeca_FaceBuilder.blend` + `Rosto_FaceBuilder.png`.
Para regenerar o personagem: defina `FC_FB_BLEND` e `FC_FB_TEX` e rode `gerar_protagonista.py`. Ele cria objetos separados: `Cabeca_Pele` (grupos Nariz, Boca, Olhos, Queixo, Orelhas), `Cabelo` (cachos modelados em espiral), `Sobrancelhas` e `Barba`.
