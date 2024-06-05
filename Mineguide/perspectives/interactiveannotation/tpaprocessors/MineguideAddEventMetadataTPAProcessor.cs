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
    public class MineguideAddEventMetadataTPAProcessor : ITPAProcessor
    {
       
        public IEnumerable<iTPAModel> ProcessTPA(IEnumerable<iTPAModel> tpa)
        {
            // store the model info in the new tpa metadata to be readed by the Mineguide Editor
            foreach (var t in tpa)
            {
                var template = t.getTPATemplate();
                var log = t.AssociatedLog;
                if (template != null && log != null) 
                {
                    foreach (var node in template.Nodes)
                    {
                        var events = t.getNodesExecutions(node.Id).Select(n => n.getEvent(log));
                        foreach(var evt in events)
                        {
                            foreach(var prop in evt.Properties)
                            {
                                node.Metadata[prop.Key] = prop.Value; // sobreescribe y gana el ultimo evento que escribe
                            }
                        }
                    }
                }
            }
            return tpa;
        }
    }
}

