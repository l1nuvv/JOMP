using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JustOneMorePeel
{
    // Mesh sections of one continuous wall. All sections exist at the same time.
    public sealed class MillionPosterField : MonoBehaviour
    {
        const int SectionsAcross = 16;
        const int SectionsHigh = 5;
        const int SectionColumns = 100;
        const int SectionRows = 125;
        const int PostersPerCell = SectionColumns * SectionRows;
        const float SectionWidth = SectionColumns * .9f;
        const float SectionHeight = SectionRows;
        const float WallWidth = SectionsAcross * SectionWidth;
        const float WallHeight = SectionsHigh * SectionHeight;
        public const float OverviewDistance = 750f;
        public const int PosterCount = SectionsAcross * SectionsHigh * PostersPerCell;
        // World-space maximum dimensions of ordinary courtyard notices.
        static readonly float[] NoticeSizes = { 1.125f, 1.09f, 1.1075f, .69f, .78f };
        [SerializeField] Material wallMaterial;
        [SerializeField] Material floorMaterial;

        struct Poster
        {
            public Vector3 position;
            public Quaternion rotation;
            public float size, width, height;
            public int design;
        }

        sealed class Cell
        {
            public GameObject root;
            public Collider wall;
            public Mesh mesh;
            public MeshRenderer renderer;
            public int firstId;
            public readonly Poster[] posters = new Poster[PostersPerCell];
            public readonly Dictionary<int, PeelPosterStack> active = new();
        }

        readonly Dictionary<Vector2Int, Cell> cells = new();
        readonly Dictionary<Collider, Cell> walls = new();
        readonly Dictionary<PeelPosterStack, (Cell cell, int index)> active = new();
        readonly BitArray removed = new(PosterCount);
        readonly List<Vector3> vertices = new(PostersPerCell * 4);
        readonly List<Vector3> normals = new(PostersPerCell * 4);
        readonly List<Vector2> uv = new(PostersPerCell * 4);
        readonly List<Material> materials = new();
        List<int>[] indices;
        PeelGame game;
        Unity.Cinemachine.CinemachineCamera followCamera;
        PeelLanguage language;
        GameObject overview;
        RenderTexture overviewTexture;
        Material overviewMaterial;
        bool showingOverview;
        bool overviewDirty;
        bool overviewReady;

        internal void Initialize(PeelGame owner)
        {
            game = owner;
            followCamera = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
            game.view.farClipPlane = 1800f;
            if (followCamera) followCamera.Lens.FarClipPlane = 1800f;
            language = game.Settings.Language;
            indices = new List<int>[game.Settings.PaperCount];
            for (int i = 0; i < indices.Length; i++) indices[i] = new List<int>();
            game.Settings.Changed += OnSettingsChanged;
            game.PosterWasCleared += OnPosterCleared;
            Primitive("Viewing platform", transform, PrimitiveType.Cube,
                new Vector3(0, -.15f, -OverviewDistance), new Vector3(12, .3f, 4), floorMaterial);
            var backing = Primitive("Continuous poster wall", transform, PrimitiveType.Cube,
                new Vector3(0, 0, .15f), new Vector3(WallWidth, WallHeight, .3f), wallMaterial);
            backing.GetComponent<Collider>().enabled = false;
            for (int y = 0; y < SectionsHigh; y++)
                for (int x = 0; x < SectionsAcross; x++)
                    Load(new Vector2Int(x, y));
            CreateOverview();
        }

        void Update()
        {
            if (!game || game.CurrentScreen != PeelScreen.Playing) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool zoom = keyboard != null && keyboard.zKey.isPressed;
            float fov = zoom ? 2f : 60f;
            if (followCamera) followCamera.Lens.FieldOfView = fov;
            else game.view.fieldOfView = fov;
            bool showOverview = overviewReady && !zoom && game.view.transform.position.z < -550f;
            if (showOverview && overviewDirty) BakeOverview();
            SetOverview(showOverview);
        }

        void CreateOverview()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) return;
            overviewTexture = new RenderTexture(3072, 1333, 24)
            {
                name = "Million poster overview", useMipMap = true, autoGenerateMips = true,
                filterMode = FilterMode.Trilinear
            };
            overviewMaterial = new Material(shader);
            overviewMaterial.SetTexture("_BaseMap", overviewTexture);
            overview = Primitive("Full-wall overview", transform, PrimitiveType.Quad,
                new Vector3(0, 0, -.1f), new Vector3(WallWidth, WallHeight, 1), overviewMaterial);
            overview.GetComponent<Collider>().enabled = false;
            overview.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            overview.SetActive(false);
            StartCoroutine(PrepareOverview());
        }

        IEnumerator PrepareOverview()
        {
            yield return null;
            BakeOverview();
            overviewReady = true;
            SetOverview(true);
        }

        void BakeOverview()
        {
            if (!overview) return;
            bool wasOverview = showingOverview;
            SetOverview(false);
            var capture = new GameObject("Wall overview capture").AddComponent<Camera>();
            capture.enabled = false;
            capture.transform.position = transform.TransformPoint(new Vector3(0, 0, -OverviewDistance));
            capture.fieldOfView = 2f * Mathf.Atan(WallHeight * .5f / OverviewDistance) * Mathf.Rad2Deg;
            capture.aspect = WallWidth / WallHeight;
            capture.nearClipPlane = OverviewDistance - 50f;
            capture.farClipPlane = OverviewDistance + 50f;
            capture.clearFlags = CameraClearFlags.SolidColor;
            capture.backgroundColor = Color.black;
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = overviewTexture };
            RenderPipeline.SubmitRenderRequest(capture, request);
            Destroy(capture.gameObject);
            overviewDirty = false;
            SetOverview(wasOverview);
        }

        void SetOverview(bool show)
        {
            if (!overview || showingOverview == show) return;
            showingOverview = show;
            overview.SetActive(show);
            foreach (var cell in cells.Values) cell.renderer.enabled = !show;
        }

        void Load(Vector2Int key)
        {
            var cell = new Cell { firstId = (key.y * SectionsAcross + key.x) * PostersPerCell };
            cell.root = new GameObject($"Paper mesh section {key.x}, {key.y}");
            cell.root.transform.SetParent(transform, false);
            cell.root.transform.localPosition = new Vector3((key.x - (SectionsAcross - 1) * .5f) * SectionWidth,
                (key.y - (SectionsHigh - 1) * .5f) * SectionHeight, 0);
            var collider = cell.root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0, .15f);
            collider.size = new Vector3(SectionWidth, SectionHeight, .3f);
            cell.wall = collider;
            walls.Add(cell.wall, cell);
            cells.Add(key, cell);

            var random = new System.Random(cell.firstId + 9143);
            for (int i = 0; i < PostersPerCell; i++)
            {
                int design = random.Next(indices.Length);
                float aspect = game.Settings.PaperAspect(design);
                float size = NoticeSizes[random.Next(NoticeSizes.Length)];
                float width = size / Mathf.Max(1f, aspect);
                cell.posters[i] = new Poster
                {
                    position = new Vector3((i % SectionColumns - (SectionColumns - 1) * .5f) * .9f + Jitter(random, .10f),
                        i / SectionColumns - (SectionRows - 1) * .5f + Jitter(random, .12f), -.05f + Jitter(random, .03f)),
                    rotation = Quaternion.Euler(0, 0, Jitter(random, 16f)),
                    size = size, width = width, height = width * aspect, design = design
                };
            }
            var paper = new GameObject("Batched poster faces");
            paper.transform.SetParent(cell.root.transform, false);
            cell.mesh = new Mesh { name = "Wall paper" };
            paper.AddComponent<MeshFilter>().sharedMesh = cell.mesh;
            cell.renderer = paper.AddComponent<MeshRenderer>();
            cell.renderer.shadowCastingMode = ShadowCastingMode.Off;
            Build(cell);
        }

        static float Jitter(System.Random random, float spread) => ((float)random.NextDouble() * 2f - 1f) * spread;

        static GameObject Primitive(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        void Build(Cell cell)
        {
            vertices.Clear(); normals.Clear(); uv.Clear(); materials.Clear();
            foreach (var group in indices) group.Clear();
            for (int i = 0; i < cell.posters.Length; i++)
            {
                if (removed[cell.firstId + i] || cell.active.ContainsKey(i)) continue;
                var p = cell.posters[i];
                float x = p.width * .5f, y = p.height * .5f;
                int start = vertices.Count;
                vertices.Add(p.position + p.rotation * new Vector3(-x, y, 0));
                vertices.Add(p.position + p.rotation * new Vector3(x, y, 0));
                vertices.Add(p.position + p.rotation * new Vector3(-x, -y, 0));
                vertices.Add(p.position + p.rotation * new Vector3(x, -y, 0));
                for (int corner = 0; corner < 4; corner++) normals.Add(Vector3.back);
                uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(Vector2.zero); uv.Add(Vector2.right);
                var triangles = indices[p.design];
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start + 1); triangles.Add(start + 3); triangles.Add(start + 2);
            }
            cell.mesh.Clear();
            cell.mesh.SetVertices(vertices); cell.mesh.SetNormals(normals); cell.mesh.SetUVs(0, uv);
            foreach (var group in indices) if (group.Count > 0) materials.Add(null);
            cell.mesh.subMeshCount = materials.Count;
            int submesh = 0;
            for (int design = 0; design < indices.Length; design++)
            {
                if (indices[design].Count == 0) continue;
                materials[submesh] = game.Settings.Paper(design);
                cell.mesh.SetTriangles(indices[design], submesh++, false);
            }
            cell.mesh.RecalculateBounds();
            cell.renderer.SetSharedMaterials(materials);
        }

        internal PeelStrip ResolveTarget(RaycastHit hit, PeelStrip current, Vector3 origin, Vector3 direction)
        {
            if (direction.z <= 0) return current;
            if (!walls.TryGetValue(hit.collider, out var cell))
            {
                if (!current || !current.Stack || !active.TryGetValue(current.Stack, out var location)) return current;
                cell = location.cell;
            }
            var local = cell.root.transform.InverseTransformPoint(origin);
            int nearest = -1;
            int stripIndex = -1;
            float nearestDistance = hit.distance;
            for (int i = cell.posters.Length - 1; i >= 0; i--)
            {
                if (removed[cell.firstId + i] || cell.active.ContainsKey(i)) continue;
                var p = cell.posters[i];
                float distance = (p.position.z - local.z) / direction.z;
                if (distance <= 0 || distance > nearestDistance) continue;
                var point = Quaternion.Inverse(p.rotation) * (local + direction * distance - p.position);
                if (Mathf.Abs(point.x) > p.width * .5f || Mathf.Abs(point.y) > p.height * .5f) continue;
                stripIndex = Mathf.Clamp(Mathf.FloorToInt((point.x / p.width + .5f) * 4), 0, 3);
                nearest = i;
                nearestDistance = distance;
            }
            if (nearest < 0) return current;
            var stack = Activate(cell, nearest, cell.posters[nearest]);
            Build(cell);
            overviewDirty = overview;
            Physics.SyncTransforms();
            return stack.CurrentStrips[stripIndex];
        }

        PeelPosterStack Activate(Cell cell, int index, Poster poster)
        {
            var go = new GameObject($"Poster {cell.firstId + index}");
            go.transform.SetParent(cell.root.transform, false);
            go.transform.SetLocalPositionAndRotation(poster.position + Vector3.forward * .001f, poster.rotation);
            var stack = go.AddComponent<PeelPosterStack>();
            var front = CreateStrips(go.transform);
            var back = CreateStrips(go.transform);
            stack.ConfigureSinglePoster(poster.design, poster.size, front, back);
            stack.Initialize(game);
            cell.active.Add(index, stack);
            active.Add(stack, (cell, index));
            return stack;
        }

        static PeelStrip[] CreateStrips(Transform parent)
        {
            var strips = new PeelStrip[4];
            for (int i = 0; i < strips.Length; i++)
            {
                var go = new GameObject("Paper strip");
                go.SetActive(false);
                go.transform.SetParent(parent, false);
                var strip = go.AddComponent<PeelStrip>();
                strip.uvLeft = i / 4f; strip.uvRight = (i + 1) / 4f;
                go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                go.SetActive(true);
                strips[i] = strip;
            }
            return strips;
        }

        void OnPosterCleared(PeelPosterStack stack)
        {
            if (!active.TryGetValue(stack, out var location)) return;
            int id = location.cell.firstId + location.index;
            removed[id] = true;
            overviewDirty = overview;
            location.cell.active.Remove(location.index);
            active.Remove(stack);
            Destroy(stack.gameObject);
        }

        void OnSettingsChanged()
        {
            if (language == game.Settings.Language) return;
            language = game.Settings.Language;
            foreach (var cell in cells.Values) Build(cell);
            foreach (var stack in active.Keys) stack.RefreshMaterials();
            BakeOverview();
        }

        void OnDestroy()
        {
            if (game && game.Settings)
            {
                game.Settings.Changed -= OnSettingsChanged;
                game.PosterWasCleared -= OnPosterCleared;
            }
            foreach (var cell in cells.Values) if (cell.mesh) Destroy(cell.mesh);
            if (overviewTexture) Destroy(overviewTexture);
            if (overviewMaterial) Destroy(overviewMaterial);
        }
    }
}
