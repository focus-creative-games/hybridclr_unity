using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using UnityEditor;
using IAssemblyResolver = HybridCLR.Editor.Meta.IAssemblyResolver;

namespace HybridCLR.Editor.Link
{
    public class Analyzer
    {
        private readonly IAssemblyResolver _resolver;
        private readonly bool _analyzeAssetType;

        public Analyzer(IAssemblyResolver resolver, bool analyzeAssetType)
        {
            _resolver = resolver;
            _analyzeAssetType = analyzeAssetType;
        }

        public HashSet<TypeRef> CollectRefs(List<string> rootAssemblies)
        {
            using (var assCollector = new AssemblyCache(_resolver))
            {
                var rootAssemblyNames = new HashSet<string>(rootAssemblies);

                var typeRefs = new HashSet<TypeRef>(TypeEqualityComparer.Instance);
                foreach (var rootAss in rootAssemblies)
                {
                    var dnAss = assCollector.LoadModule(rootAss, _analyzeAssetType);
                    foreach (var type in dnAss.GetTypeRefs())
                    {
                        if (!rootAssemblyNames.Contains(type.DefinitionAssembly.Name.ToString()))
                        {
                            typeRefs.Add(type);
                        }
                    }
                }

                if (_analyzeAssetType)
                {
                    var modsExludeRoots = assCollector.LoadedModules
                        .Where(e => !rootAssemblyNames.Contains(e.Key))
                        .ToDictionary(e => e.Key, e => e.Value);
                    CollectObjectTypeInAssets(modsExludeRoots, typeRefs);
                }

                assCollector.Dispose();
                return typeRefs;
            }
        }

        public void CollectObjectTypeInAssets(Dictionary<string, ModuleDefMD> mods, HashSet<TypeRef> typeRefs)
        {
            var objTypes = new HashSet<Type>();
            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Type mainAssetType =  AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (mainAssetType == typeof(SceneAsset))
                {

                }
                else
                {
                    UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (UnityEngine.Object obj in objs)
                    {
                        if (obj != null)
                        {
                            objTypes.Add(obj.GetType());
                        }
                    }
                }
            }

            var importers = mods.ToDictionary(e => e.Key, e => new Importer(e.Value));
            //Debug.Log($"importers count:{importers.Count} importers:{string.Join(",", importers.Keys)}");
            foreach (var type in objTypes)
            {
                if (importers.TryGetValue(type.Assembly.GetName().Name, out var im))
                {
                    typeRefs.Add((TypeRef)im.Import(type));
                    //Debug.Log($"== add asset type:{type}");
                }
                else
                {
                    //Debug.Log($"== ignore asset type:{type} {type.Assembly.GetName().Name}");
                }
            }
        }
    }
}
