using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mineguide.perspectives.semantics;
using Newtonsoft.Json;
using pm4h.runner;
using pm4h.semantics.datamodel;
using pm4h.tpa.ipi;
using pm4h.windows.interfaces;
using pm4h.windows.utils;

namespace Mineguide.perspectives.interactiveannotation.tpaprocessors
{
    [PrivateRunnerElement]
    public class MineguideModelInfoTPAProcessor : ITPAProcessor
    {
        public const string MODEL_INFO_RUNNER_METADATA_ID = "MINEGUIDE_RUNNER_MODELINFO"; //MODEL_INFO_EDITOR_METADATA_ID

        [RunnerExperimentId]
        public Guid ExpId { get; set; }

        //[HiddenRunnerProperty]
        //public byte[] ModelInfoInferred { get; set; }

        [HiddenRunnerProperty]
        public string ModelInfoInferred { get; set; }

        public IEnumerable<iTPAModel> ProcessTPA(IEnumerable<iTPAModel> tpa)
        {
            // store the model info in the new tpa metadata to be readed by the Mineguide Editor
            foreach (var t in tpa)
            {
                t.set(MODEL_INFO_RUNNER_METADATA_ID, ModelInfoInferred);
            }
            return tpa;
        }

        public void AddModelInfo(ModelInfo info)
        {
            if (info != null)
            {
                //ModelInfoInferred = BinarizableHelper.BinarizeObject(info);
                ModelInfoInferred = JsonConvert.SerializeObject(info);
            }
            else
            {
                ModelInfoInferred = null;
            }
        }

        /// <summary>
        /// Get the ModelInfo stored in the TPA metadata from the runner block or null if not exist
        /// </summary>
        public static ModelInfo GetStoredModelInfo(iTPAModel tpa, object sender)
        {
            // -- load stored semantic data from the runner block if exist --
            if (tpa != null)
            {
                return tpa.LoadStoredModelInfo(MODEL_INFO_RUNNER_METADATA_ID); // load stored semantic data from the model
            }
            return null;
        }

        /// <summary>
        /// Get stored sematic stored in the TPA metadata from the runner block and merge them with the ModelInfo passed as parameter
        /// </summary>
        /// <param name="tpa"></param>
        /// <param name="infoToUpdate"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static ModelInfo UpdatedModelInfo(iTPAModel tpa, ModelInfo infoToUpdate, object sender)
        {
            // -- load stored semantic data from the runner block if exist --
            if (tpa != null)
            {
                var smi = tpa.LoadStoredModelInfo(MODEL_INFO_RUNNER_METADATA_ID); // load stored semantic data from the model
                if (smi != null)
                {
                    //ModelInfoResult = MineguideEditor.UpsertModelInfoSemantic(smi, ModelInfoResult); // añado la semantica del modelinfo almacenado al local, modelinfo generado desde el runner
                    infoToUpdate = infoToUpdate.MergeSemanticAnnotations(smi); // añado la semantica del modelinfo almacenado al local, modelinfo generado desde el runner
                }
            }

            return infoToUpdate;
        }

        /// <summary>
        /// Get the ModelInfo from the EXPERIMENT and the ModelInfo stored in the TPA metadata from the runner block and merge them
        /// </summary>
        public static ModelInfo GetUpdatedModelInfo(iTPAModel tpa, object sender)
        {
            ModelInfo ModelInfoResult;

            // SACO COPIA LOCAL DEL MODELINFO PARA PODER HACER CAMBIOS SIN AFECTAR AL ORIGINAL
            if (tpa.GetExperimentModelInfo(sender) is ModelInfo mi)
            {
                ModelInfoResult = mi.GetCopy();
            }
            else
            {
                // creo un modelinfo desde cero si no existe
                ModelInfoResult = ModelInfo.Factory(new List<iTPAModel>() { tpa });
            }

            //// -- load stored semantic data from the runner block if exist --
            //if (tpa != null)
            //{
            //    var smi = tpa.LoadStoredModelInfo(MODEL_INFO_RUNNER_METADATA_ID); // load stored semantic data from the model
            //    if (smi != null)
            //    {
            //        //ModelInfoResult = MineguideEditor.UpsertModelInfoSemantic(smi, ModelInfoResult); // añado la semantica del modelinfo almacenado al local, modelinfo generado desde el runner
            //        ModelInfoResult = ModelInfoResult.UpsertSemantic(smi); // añado la semantica del modelinfo almacenado al local, modelinfo generado desde el runner
            //    }
            //}

            ModelInfoResult = UpdatedModelInfo(tpa, ModelInfoResult, sender);

            return ModelInfoResult;
        }
    }
}

