using UnityEditor;
using UnityEngine;

namespace FaithClash.EditorTools
{
    /// <summary>Configura automaticamente as imagens do HUD (sprites, bordas 9-slice) e a textura do rosto.</summary>
    public class ImportadorTexturas : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            var imp = (TextureImporter)assetImporter;
            string caminho = assetPath.Replace('\\', '/');

            if (caminho.Contains("/Resources/HUD/"))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.wrapMode = TextureWrapMode.Clamp;
                string nome = System.IO.Path.GetFileNameWithoutExtension(caminho);
                imp.spriteBorder = nome switch
                {
                    "painel" => new Vector4(26, 26, 26, 26),
                    "barra_fundo" => new Vector4(20, 9, 22, 9),
                    "slot" => new Vector4(8, 8, 8, 8),
                    "tecla" => new Vector4(6, 6, 6, 6),
                    _ => Vector4.zero,
                };
            }
            else if (caminho.Contains("/Characters/Jogador/") && caminho.EndsWith("_Normal.png"))
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.mipmapEnabled = true;
            }
            else if (caminho.EndsWith("Jogador/Rosto.png"))
            {
                imp.mipmapEnabled = true;
                imp.wrapMode = TextureWrapMode.Clamp;
            }
        }
    }
}
