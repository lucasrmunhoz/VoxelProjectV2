using System;
using UnityEngine;
using Voxel2.Config;

namespace Voxel2.Runtime
{
    /// <summary>
    /// Bootstrap do runtime Voxel2 (Fase A).
    /// - Sobe serviços fundamentais (config, ciclo de frame)
    /// - Exposta uma Handle estável para registro posterior (Zone, Registry, Renderer, RenderService)
    /// - Sem dependência de tipos concretos nesta fase
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    [DisallowMultipleComponent]
    public sealed class Voxel2RuntimeBootstrap : MonoBehaviour
    {
        // --- Constantes ---
        private const string GameObjectName = "Voxel2_Runtime";
        private const string ConfigResourcePath = "Voxel2/VoxelZoneConfig";

        // --- Singleton leve ---
        private static Voxel2RuntimeBootstrap _instance;
        public static bool IsInitialized { get; private set; }
        public static Voxel2RuntimeHandle Handle { get; private set; } = new Voxel2RuntimeHandle();

        [SerializeField]
        [Tooltip("Opcional: referencia direta à config (se vazio, carrega via Resources).")]
        private VoxelZoneConfig overrideConfig;

        // --- Tipos mínimos (contratos) para evitar acoplamento prematuro ---
        // OBS: estes contratos serão movidos/expandido em PRs seguintes conforme combinamos.
        public interface ICellRegistry { /* PR-A-04 implementará */ }
        public interface IVoxelZone3D   { void Tick(); /* PR-A-05 implementará */ }
        public interface IRenderService  { void CommitFrame(); /* PR-A-06 implementará */ }
        public interface IVoxelRenderer  { /* PR-A-06 implementará */ }

        /// <summary>
        /// Handle com referências somente-leitura para os serviços registrados.
        /// O registro é feito via métodos Register* abaixo.
        /// </summary>
        public sealed class Voxel2RuntimeHandle
        {
            public VoxelZoneConfig Config { get; internal set; }

            public ICellRegistry Registry { get; internal set; }
            public IVoxelZone3D  Zone     { get; internal set; }
            public IRenderService RenderService { get; internal set; }
            public IVoxelRenderer Renderer { get; internal set; }
        }

        // --- Bootstrap automático antes de qualquer cena carregar ---
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (_instance != null) return;

            var go = new GameObject(GameObjectName);
            _instance = go.AddComponent<Voxel2RuntimeBootstrap>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Evita duplicata
                Debug.LogWarning("[Voxel2][Bootstrap] Instância duplicada detectada. Destruindo este componente.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Initialize();
        }

        private void Initialize()
        {
            if (IsInitialized) return;

            // 1) Config
            var cfg = overrideConfig ?? Resources.Load<VoxelZoneConfig>(ConfigResourcePath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<VoxelZoneConfig>();
                cfg.name = "VoxelZoneConfig (Runtime Default)";
                Debug.LogWarning("[Voxel2][Config] VoxelZoneConfig não encontrado em Resources/Voxel2. Usando defaults em memória.");
            }
            // Travar/validar parâmetros sensíveis da Fase A
            if (cfg.chunkSize != 16)
            {
                if (cfg.logBootstrap)
                    Debug.LogWarning($"[Voxel2][Config] chunkSize={cfg.chunkSize} → forçado para 16 na Fase A.");
                cfg.chunkSize = 16;
            }

            Handle.Config = cfg;

            if (cfg.logBootstrap)
                Debug.Log("[Voxel2][Bootstrap] Config carregada.");

            // 2) (Reserva para PoolService na PR-A-03)

            // 3) Registry/Zone/Renderer/RenderService serão registrados via Register* em PRs subsequentes
            //    Aqui apenas deixamos o ciclo do frame preparado para acioná-los se já existirem.

            IsInitialized = true;

            if (cfg.logBootstrap)
                Debug.Log("[Voxel2][Bootstrap] Inicializado.");

            // 4) Seed opcional (deferido): apenas agenda; execução real será no PR-A-08
            if (cfg.seedOnStart)
                StartCoroutine(SeedNextFrame());
        }

        private System.Collections.IEnumerator SeedNextFrame()
        {
            // Defer para que outros sistemas possam se registrar no primeiro frame.
            yield return null;

            // Nesta fase não semeamos nada de fato: apenas logamos a intenção.
            // O seeding real ocorrerá no PR-A-08 (Provider simples ou ApplyBatch estático).
            if (Handle.Config != null && Handle.Config.logBootstrap)
            {
                var c = Handle.Config;
                Debug.Log($"[Voxel2][Bootstrap] SeedOnStart ativo (AABB {c.seedMin}..{c.seedMax}). " +
                          "Seeding efetivo será implementado no PR-A-08.");
            }
        }

        private void Update()
        {
            // Tick do núcleo, se já registrado (PR-A-05)
            Handle.Zone?.Tick();

            // (PR-A-07) Telemetria mínima será atualizada aqui
        }

        private void LateUpdate()
        {
            // Commit do renderer, se já registrado (PR-A-06)
            Handle.RenderService?.CommitFrame();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                Shutdown();
        }

        /// <summary>
        /// Desliga/limpa referências do runtime Voxel2.
        /// </summary>
        public static void Shutdown()
        {
            if (!IsInitialized) return;

            // Ordem reversa (quando existirem disposes específicos nas próximas PRs).
            Handle.RenderService = null;
            Handle.Renderer = null;
            Handle.Zone = null;
            Handle.Registry = null;
            Handle.Config = null;

            IsInitialized = false;

            if (_instance != null && _instance.gameObject != null)
            {
                var go = _instance.gameObject;
                _instance = null;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }

            Debug.Log("[Voxel2][Bootstrap] Shutdown concluído.");
        }

        // --- API de Registro (para PRs futuras conectarem serviços concretos) ---

        public static void RegisterConfig(VoxelZoneConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Handle.Config = config;
            if (config.chunkSize != 16)
            {
                Debug.LogWarning($"[Voxel2][Config] chunkSize={config.chunkSize} → forçado para 16 na Fase A.");
                config.chunkSize = 16;
            }
            Debug.Log("[Voxel2][Bootstrap] Config registrada.");
        }

        public static void RegisterRegistry(ICellRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            Handle.Registry = registry;
            Debug.Log("[Voxel2][Bootstrap] CellRegistry registrado.");
        }

        public static void RegisterZone(IVoxelZone3D zone)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            Handle.Zone = zone;
            Debug.Log("[Voxel2][Bootstrap] VoxelZone3D registrada.");
        }

        public static void RegisterRenderer(IVoxelRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            Handle.Renderer = renderer;
            Debug.Log("[Voxel2][Bootstrap] Renderer registrado.");
        }

        public static void RegisterRenderService(IRenderService renderService)
        {
            if (renderService == null) throw new ArgumentNullException(nameof(renderService));
            Handle.RenderService = renderService;
            Debug.Log("[Voxel2][Bootstrap] RenderService registrado.");
        }
    }
}
