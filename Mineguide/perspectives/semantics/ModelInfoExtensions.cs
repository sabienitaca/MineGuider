using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Internal;
using System.Windows.Markup;
using com.espertech.esper.epl.spec;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using pm4h.semantics.datamodel;
using pm4h.tpa.ipi;
using pm4h.utils;
using pm4h.windows.interfaces;
using pm4h.windows.utils;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Mineguide.perspectives.semantics
{
    public static class ModelInfoExtensions
    {
        #region STORE MODEL INFO IN THE TPA
        //public const string EDITOR_MODEL_INFO_ID = "MINEGUIDE_EDITOR_MODELINFO";

        public static string SerializeModelInfo(ModelInfo info)
        {
            //return BinarizableHelper.BinarizeObject(info);
            return JsonConvert.SerializeObject(info);
        }

        public static ModelInfo? DeserializeModelInfo(string data)
        {
            //return BinarizableHelper.DebinarizeObject<ModelInfo>(data);
            return JsonConvert.DeserializeObject<ModelInfo>(data);
        }

        public static void StoreModelInfo(this iTPAModel model, ModelInfo info, string MetadataId)
        {
            if (model != null && info != null)
            {
                model.set(MetadataId, SerializeModelInfo(info));// BinarizableHelper.BinarizeObject(info));
            }
        }

        public static void ClearStoredModelInfo(this iTPAModel model, string MetadatId)
        {
            if (model != null)
            {
                model.set(MetadatId, null); // borro la semantica del modelo por si Remove no hace lo que quiero
                model.getMetaData().Remove(MetadatId); // borro la semantica del modelo
            }
        }

        public static ModelInfo LoadStoredModelInfo(this iTPAModel model, string MetadataId)
        {
            //if (model != null && model.get(MetadataId) is byte[] data)
            //{
            //    return BinarizableHelper.DebinarizeObject<ModelInfo>(data);
            //}
            //return null;
            if (model != null && model.get(MetadataId) is string data)
            {
                return DeserializeModelInfo(data);
            }
            return null;
        }

        public static bool HasStoredModelInfo(this iTPAModel model, string MetadataId)
        {
            if (model != null)
            {
                //return model.get(MetadataId) is byte[];
                return (model.get(MetadataId) != null);
            }
            return false;
        }

        #endregion

        #region GET MODEL INFO ASSOCIATED TO A TPA

        public static ModelInfo GetExperimentModelInfo(this iTPAModel tpa, object sender)
        {
            if (PMAppWinHelper.GetModelInfoProvider(sender, tpa.GetContextId()) is IModelInfoProvider mp)
            {
                return mp.GetModelInfo();
            }
            return null;
        }

        public static ModelInfo GetCopy(this ModelInfo info)
        {
            //return info.Deserialize(info.Serialize());
            return DeserializeModelInfo(SerializeModelInfo(info));
        }
        #endregion

        #region MERGE MODEL INFO SEMANTICS

        public static string MODELINFO_METATADA_TERMINOLOGIES_ID = "MINEGUIDE_TERMINOLOGIES";

        public static IEnumerable<string> ReadTerminologies(this ModelInfo info)
        {
            if (info.Properties.TryGetValue(MODELINFO_METATADA_TERMINOLOGIES_ID, out var md) && md is string json)
            {
                if (JsonConvert.DeserializeObject<List<string>>(json) is List<string> terms)
                {
                    return terms;
                }
            }
            return new List<string>();
        }
        public static void AddTerminologies(this ModelInfo info, IEnumerable<string> terminologies)
        {
            if (terminologies != null && terminologies.Any())
            {
                string json = JsonConvert.SerializeObject(terminologies.ToList());
                info.Properties[MODELINFO_METATADA_TERMINOLOGIES_ID] = json;
            }
            else
            {
                info.Properties.Remove(MODELINFO_METATADA_TERMINOLOGIES_ID);
            }
        }

        public static string MODELINFO_METATADA_SUBPROCESS_SEMANTIC_ID = "MINEGUIDE_SUBPROCESS_SEMANTIC";

        public static IEnumerable<BasicInfo> ReadSubprocessSemantic(this ModelInfo info)
        {
            if (info.Properties.TryGetValue(MODELINFO_METATADA_SUBPROCESS_SEMANTIC_ID, out var md) && md is string json)
            {
                if (JsonConvert.DeserializeObject<List<BasicInfo>>(json) is List<BasicInfo> terms)
                {
                    return terms;
                }
            }
            return new List<BasicInfo>();
        }

        public static void AddSubprocessSemantic(this ModelInfo info, IEnumerable<BasicInfo> subprocessSemantic)
        {
            if (subprocessSemantic != null && subprocessSemantic.Any())
            {
                string json = JsonConvert.SerializeObject(subprocessSemantic.ToList());
                info.Properties[MODELINFO_METATADA_SUBPROCESS_SEMANTIC_ID] = json;
            }
            else
            {
                info.Properties.Remove(MODELINFO_METATADA_SUBPROCESS_SEMANTIC_ID);
            }
        }

        //public static void InsertToSubprocessSemanticList(this ModelInfo info, BasicInfo subprocessSemantic)
        //{
        //    var subprocesses = ReadSubprocessSemantic(info).ToList();
        //    subprocesses.Add(subprocessSemantic);
        //    AddSubprocessSemantic(info, subprocesses);
        //}

        //public static void UpsertToSubprocessSemanticList(this ModelInfo info, BasicInfo subprocessSemantic)
        //{
        //    var subprocesses = ReadSubprocessSemantic(info).ToList();
        //    var subprocess = subprocesses.FirstOrDefault(sp => sp.Id == subprocessSemantic.Id);
        //    if (subprocess != null)
        //    {
        //        subprocesses.Remove(subprocess);
        //    }
        //    subprocesses.Add(subprocessSemantic);
        //    AddSubprocessSemantic(info, subprocesses);
        //}

        //public static void DeleteFromSubprocessSemanticList(this ModelInfo info, string subprocessId)
        //{
        //    var subprocesses = ReadSubprocessSemantic(info).ToList();
        //    var subprocess = subprocesses.FirstOrDefault(sp => sp.Id == subprocessId);
        //    if (subprocess != null)
        //    {
        //        subprocesses.Remove(subprocess);
        //        AddSubprocessSemantic(info, subprocesses);
        //    }
        //}

        public static ModelInfo MergeSemanticAnnotations(this ModelInfo baseModelInfo, ModelInfo semanticModelInfo)
        {
            // TERMINOLOGIAS
            // leo las terminologias existentes
            var baseTerminologies = baseModelInfo.ReadTerminologies();
            var semanticTerminologies = semanticModelInfo.ReadTerminologies();
            // las combino           
            baseTerminologies = baseTerminologies.Union(semanticTerminologies).ToList(); // uso la union pq son strings
            // las añado al modelo base
            baseModelInfo.AddTerminologies(baseTerminologies);

            // SUBPROCESOS
            // leo los subprocesos existentes
            var baseSubprocesses = baseModelInfo.ReadSubprocessSemantic().ToDictionary(s => s.Id);
            var semanticSubprocesses = semanticModelInfo.ReadSubprocessSemantic();
            // los combino
            foreach(var semSp in semanticSubprocesses)
            {
                if(baseSubprocesses.TryGetValue(semSp.Id, out var baseSp)) // si existe el subproceso en el modelo base
                {
                    baseSp.Annotations.Merge(semSp.Annotations); // añado la semantica al subproceso del modelo base
                }
                else
                {
                    baseSubprocesses.Add(semSp.Id, semSp); // añado el subproceso al modelo base
                }
            }
            // los añado al modelo base
            baseModelInfo.AddSubprocessSemantic(baseSubprocesses.Values);

            //TRAZAS
            // busco en el modelo base si existe el dato de de semantica
            // si existe, añado la semantica al dato del modelo base
            foreach (var baseDataInfo in baseModelInfo.Tracedata.IterateDataInfo())
            {
                if (semanticModelInfo.Tracedata.Data.TryGetValue(baseDataInfo.Id, out var semanticData)) // si existe el dato en el modelo con semantica
                {
                    //baseDataInfo.Annotations = baseDataInfo.Annotations.Merge(semanticData.Annotations); // añado la semantica al dato del modelo base
                    baseDataInfo.Annotations.Merge(semanticData.Annotations); // añado la semantica al dato del modelo base
                }
            }

            // NODOS INFO
            // busco en el modelo base si existe el nodo de de semantica
            // si existe, añado la semantica al nodo del modelo base
            foreach (var baseNodeInfo in baseModelInfo.NodesInfo.Values)
            {
                if (semanticModelInfo.NodesInfo.TryGetValue(baseNodeInfo.Name, out var semanticNode)) // si existe el nodo en el modelo con semantica
                {
                    //baseNodeInfo.Annotations = baseNodeInfo.Annotations.Merge(semanticNode.Annotations); // añado la semantica al nodo del modelo base
                    baseNodeInfo.Annotations.Merge(semanticNode.Annotations); // añado la semantica al nodo del modelo base
                }
            }

            return baseModelInfo;
        }

        /// <summary>
        /// merge the semantic annotations of the semantic model info into the base model info
        /// </summary>
        /// <param name="baseCollection"></param>
        /// <param name="semanticCollection"></param>
        /// <returns></returns>
        public static SemanticAnnotationCollection Merge(this SemanticAnnotationCollection baseCollection, SemanticAnnotationCollection semanticCollection)
        {
            foreach (var semItem in semanticCollection.SemanticAnnotations)
            {
                baseCollection[semItem.Key] = semItem.Value;
            }
            return baseCollection;
        }
        #endregion

        #region MineguideSemantic

        public static MineguideSemantic GetMineguideSemantic(this BasicInfo info)
        {
            return new MineguideSemantic(info);
        }

        public static void ClearSemantic(this BasicInfo info)
        {
            info.Annotations.SemanticAnnotations.Clear();
        }

        public static void ClearSemanticTag(this BasicInfo info)
        {
            info.Annotations.SemanticAnnotations.Remove(MineguideSemantic.SEMANTIC_TAG_ID);
        }

        public static void ClearMainBinding(this BasicInfo info)
        {
            info.Annotations.SemanticAnnotations.Remove(MineguideSemantic.MAIN_BINDING_ID);
        }

        public static void ClearOtherBindings(this BasicInfo info)
        {
            foreach (var semItem in info.Annotations.IterateAnnotations().OrderBy(a => a.Id))
            {
                if (semItem.Id != MineguideSemantic.SEMANTIC_TAG_ID && semItem.Id != MineguideSemantic.MAIN_BINDING_ID)
                {
                    info.Annotations.SemanticAnnotations.Remove(semItem.Id);
                }
            }
        }

        #endregion
    }


    public class MineguideSemantic
    {
        //public BasicInfo Info { get; set; }

        public const string SEMANTIC_TAG_ID = "SemanticTag";
        public const string MAIN_BINDING_ID = "MainBinding";
        public const string OTHER_BINDING_ID_PREFIX = "OtherBinding";

        public SemanticAnnotation? SemanticTag { get; set; } = null;
        public SemanticAnnotation? MainBinding { get; set; } = null;
        public List<SemanticAnnotation>? OtherBindings { get; set; } = null;

        public MineguideSemantic() { }

        public MineguideSemantic(BasicInfo info)
        {
            //Info = info;
            foreach (var semItem in info.Annotations.IterateAnnotations().OrderBy(a => a.Id))
            {
                if (semItem.Id == SEMANTIC_TAG_ID)
                {
                    SemanticTag = semItem;
                }
                else if (semItem.Id == MAIN_BINDING_ID)
                {
                    MainBinding = semItem;
                }
                else
                {
                    if (OtherBindings == null) { OtherBindings = new List<SemanticAnnotation>(); }
                    OtherBindings.Add(semItem);
                }
            }
        }


    }

}
