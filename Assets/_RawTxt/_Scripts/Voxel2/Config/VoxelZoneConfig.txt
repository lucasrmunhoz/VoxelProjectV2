using UnityEngine;

namespace Voxel2.Config
{
    /// <summary>
    /// Configuração global do núcleo Voxel2 (Fase A).
    /// Carregada via Resources em Voxel2RuntimeBootstrap.
    /// </summary>
    [CreateAssetMenu(fileName = "VoxelZoneConfig", menuName = "Voxel2/Zone Config", order = 0)]
    public class VoxelZoneConfig : ScriptableObject
    {
        [Header("Grid / Chunk")]
        [Tooltip("Tamanho do chunk em células (travado em 16 na Fase A).")]
        public int chunkSize = 16;

        [Header("Budgets por frame (Fase A)")]
        [Tooltip("Máximo de diffs (mudanças de célula) aplicados por frame.")]
        public int maxCellDiffsPerFrame = 2048;

        [Tooltip("Máximo de células 'sujas' processadas pelo renderer por frame.")]
        public int maxDirtyCellsToRenderPerFrame = 1024;

        [Tooltip("Máximo de chunks remeshed por frame.")]
        public int maxChunksRemeshPerFrame = 1;

        [Tooltip("Máximo de chunks com upload para GPU por frame.")]
        public int maxChunksUploadPerFrame = 1;

        [Header("Defaults / Flags")]
        [Tooltip("Material padrão (placeholder na Fase A).")]
        public ushort defaultMaterialId = 1;

        [Tooltip("Flags padrão por micro-voxel (placeholder).")]
        public byte defaultFlags = 0;

        [Header("Debug / Smoke Test")]
        [Tooltip("Exibe overlay de telemetria (implementado no PR-A-07).")]
        public bool enableTelemetryOverlay = true;

        [Tooltip("Tecla para alternar o overlay (PR-A-07).")]
        public KeyCode telemetryToggleKey = KeyCode.F12;

        [Tooltip("Se true, o bootstrap agenda um seeding básico (PR-A-08).")]
        public bool seedOnStart = true;

        [Tooltip("Mínimo do AABB para seeding da SmokeTest.")]
        public Vector3Int seedMin = new Vector3Int(0, 0, 0);

        [Tooltip("Máximo do AABB para seeding da SmokeTest (exclusivo no eixo Y).")]
        public Vector3Int seedMax = new Vector3Int(8, 1, 8);

        [Tooltip("Habilita logs de bootstrap.")]
        public bool logBootstrap = true;

        /// <summary>
        /// Validação em tempo de edição.
        /// </summary>
        private void OnValidate()
        {
            if (chunkSize != 16)
            {
                Debug.LogWarning($"[Voxel2][Config] chunkSize ajustado para 16 na Fase A (valor atual: {chunkSize}).");
                chunkSize = 16;
            }

            maxCellDiffsPerFrame = Mathf.Max(0, maxCellDiffsPerFrame);
            maxDirtyCellsToRenderPerFrame = Mathf.Max(0, maxDirtyCellsToRenderPerFrame);
            maxChunksRemeshPerFrame = Mathf.Max(0, maxChunksRemeshPerFrame);
            maxChunksUploadPerFrame = Mathf.Max(0, maxChunksUploadPerFrame);
        }
    }
}
