using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Newtonsoft.Json;
using pm4h.runner;
using pm4h.tpa.ipi;
using pm4h.utils;
using pm4h.utils.saver;
using Sabien.Portable.Utils.Math;

namespace Mineguide.perspectives.interactiveannotation.tpaprocessors
{
    [PrivateRunnerElement]
    public class MineguideModelCorrelateIDsTPAProcessor : ITPAProcessor
    {


        [HiddenRunnerProperty]
        public string ModelForCorrelationJSON { get; set; }

        public void SetCorrelationModel(iTPAModel model)
        {
            // byte[] data = BinarizableHelper.BinarizeObject(model);

            TPAModelSerializableObject data = new TPAModelSerializableObject(model);
            ModelForCorrelationJSON = data.Serialize();
        }

        public iTPAModel? GetCorrelationModel()
        {
            var data = TPAModelSerializableObject.FromJSON(ModelForCorrelationJSON);
            return data.getTPA();            
        }

        public IEnumerable<iTPAModel> ProcessTPA(IEnumerable<iTPAModel> tpa)
        {
            if(GetCorrelationModel() is iTPAModel correlationModel) // si hay un modelo de correlacion, lo usamos para mapear los IDs
            {               
                foreach (var t in tpa)
                {
                    var corr = ModelCorrelator.CoordinateNodeIds(correlationModel, t); // mapeamos los IDs del modelo previo con el nuevo.
                    yield return corr;
                }
                
            }
            else // si no hay modelo de correlacion, devolvemos la lista de TPAs sin modificar
            {
                foreach (var t in tpa)
                {
                    yield return t;
                }
            }
        }
    }
}
