using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JustOneMorePeel
{
    /// <summary>Combines the stationary meshes on the first door, grouped by render state.</summary>
    [DefaultExecutionOrder(100)]
    internal sealed class DoorPaperBatch : MonoBehaviour
    {
        sealed class Group
        {
            public MeshRenderer renderer;
            public Mesh mesh;
            public readonly Dictionary<PeelPosterStack, List<CombineInstance>> parts = new();
            public CombineInstance[] buffer;
        }

        readonly Dictionary<(Material, int, uint, ShadowCastingMode, bool, LightProbeUsage, ReflectionProbeUsage), Group> groups = new();
        readonly Dictionary<PeelPosterStack, HashSet<Group>> stackGroups = new();
        readonly HashSet<PeelPosterStack> dirtyStacks = new();
        readonly HashSet<Group> dirtyGroups = new();
        readonly List<Material> materials = new();
        Transform batchRoot;

        internal void Initialize(IReadOnlyList<PeelPosterStack> stacks)
        {
            if (stacks == null || stacks.Count == 0) return;
            batchRoot = new GameObject("Door paper batches").transform;
            foreach (var stack in stacks)
            {
                if (!stack || stackGroups.ContainsKey(stack)) continue;
                stack.DoorBatch = this;
                stackGroups.Add(stack, new HashSet<Group>());
                MarkDirty(stack);
            }
            LateUpdate();
        }

        internal void MarkDirty(PeelPosterStack stack) => dirtyStacks.Add(stack);

        void LateUpdate()
        {
            foreach (var stack in dirtyStacks) RefreshStack(stack);
            dirtyStacks.Clear();
            foreach (var group in dirtyGroups) Rebuild(group);
            dirtyGroups.Clear();
        }

        void RefreshStack(PeelPosterStack stack)
        {
            var previous = stackGroups[stack];
            foreach (var group in previous)
            {
                group.parts.Remove(stack);
                dirtyGroups.Add(group);
            }
            previous.Clear();
            if (!stack) return;

            foreach (var source in stack.RestingRenderers())
            {
                source.forceRenderingOff = true;
                if (!source.enabled) continue;
                var mesh = source.GetComponent<MeshFilter>().sharedMesh;
                source.GetSharedMaterials(materials);
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    if (mesh.GetIndexCount(i) == 0 || !materials[i]) continue;
                    var group = GetGroup(source, materials[i]);
                    if (!group.parts.TryGetValue(stack, out var parts))
                    {
                        parts = new List<CombineInstance>();
                        group.parts.Add(stack, parts);
                    }
                    parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = i, transform = source.localToWorldMatrix });
                    previous.Add(group);
                    dirtyGroups.Add(group);
                }
            }
        }

        Group GetGroup(MeshRenderer source, Material material)
        {
            var key = (material, source.gameObject.layer, source.renderingLayerMask, source.shadowCastingMode,
                source.receiveShadows, source.lightProbeUsage, source.reflectionProbeUsage);
            if (groups.TryGetValue(key, out var group)) return group;
            var go = new GameObject(material.name);
            go.transform.SetParent(batchRoot, false);
            go.layer = source.gameObject.layer;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderingLayerMask = source.renderingLayerMask;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            renderer.lightProbeUsage = source.lightProbeUsage;
            renderer.reflectionProbeUsage = source.reflectionProbeUsage;
            var mesh = new Mesh { name = "Door paper material batch" };
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            group = new Group { renderer = renderer, mesh = mesh };
            groups.Add(key, group);
            return group;
        }

        static void Rebuild(Group group)
        {
            int count = 0;
            foreach (var parts in group.parts.Values) count += parts.Count;
            group.renderer.enabled = count > 0;
            if (count == 0) return;
            if (group.buffer == null || group.buffer.Length != count) group.buffer = new CombineInstance[count];
            int index = 0;
            foreach (var parts in group.parts.Values)
                foreach (var part in parts) group.buffer[index++] = part;
            group.mesh.CombineMeshes(group.buffer, true, true);
        }

        void OnEnable()
        {
            if (batchRoot) batchRoot.gameObject.SetActive(true);
            foreach (var stack in stackGroups.Keys) if (stack) MarkDirty(stack);
        }

        void OnDisable()
        {
            if (batchRoot) batchRoot.gameObject.SetActive(false);
            foreach (var stack in stackGroups.Keys)
                if (stack)
                    foreach (var renderer in stack.RestingRenderers()) renderer.forceRenderingOff = !stack.IsRenderVisible;
        }

        void OnDestroy()
        {
            foreach (var group in groups.Values) if (group.mesh) Destroy(group.mesh);
            if (batchRoot) Destroy(batchRoot.gameObject);
        }
    }
}
