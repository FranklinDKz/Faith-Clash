using UnityEditor;
using UnityEngine;

namespace FaithClash.EditorTools
{
    /// <summary>Configura os sons: efeitos curtos em mono (normaliza volume), ambientes em streaming.</summary>
    public class ImportadorAudio : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            string caminho = assetPath.Replace('\\', '/');
            if (!caminho.Contains("/Resources/Sons/")) return;
            var imp = (AudioImporter)assetImporter;
            bool ambiente = caminho.Contains("/Ambiente_");
            imp.forceToMono = true;           // também normaliza o volume (alguns arquivos vieram muito baixos)
            imp.loadInBackground = false;
            var s = imp.defaultSampleSettings;
            s.loadType = ambiente ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            imp.defaultSampleSettings = s;
        }
    }
}
