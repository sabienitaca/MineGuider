using Mineguide.perspectives.interactiveannotation.annotationFilters;
using pm4h.data;
using pm4h.filter;
using pm4h.runner;
using pm4h.tpa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{

    public class RenameTransformationFilter : BaseTransformationFilter
    {
        protected override Guid[] GetTraceabilityIds() => Nodes.Select(n => n.Id).ToArray();
        

        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.Rename;


        [RunnerProperty]
        public List<NodeReference> Nodes { get; set; }

        [RunnerProperty]
        public string NewName { get; set; }

        public override bool IsApplicable(TransformationRegion info)
        {
            return info.Nodes.Any(); // if there are nodes, it is applicable 
        }

        public void SetInfo(string newName, TransformationRegion info)
        {
            if (IsApplicable(info))
            {
                NewName = newName;
                Nodes = new List<NodeReference>();
                foreach (var n in info.Nodes)
                {
                    Nodes.Add(NodeReference.FromNode(n, info.TPA));
                }
            }
        }

        public void SetInfo(string newName, List<NodeReference> nodeReferences)
        {
            NewName = newName;
            Nodes = nodeReferences;
        }

        public static IEnumerable<IPMLog> StaticProcessLog(NodeReference node, string newName, IEnumerable<IPMLog> _log)
        {
            return RenameTransformationFilter.StaticProcessLog(new List<NodeReference>() { node }, newName, _log);
        }

        public static IEnumerable<IPMLog> StaticProcessLog(List<NodeReference> nodes, string newName, IEnumerable<IPMLog> _log)
        {
            RenameTransformationFilter RenameFilter = new RenameTransformationFilter();
            RenameFilter.SetInfo(newName, nodes);
            return RenameFilter.ProcessLog(_log);            
        }

        public override IEnumerable<PMTrace> ProcessTrace(PMTrace _trace, TraceMetadata Metadata)
        {
            var traces = base.ProcessTrace(_trace, Metadata).ToArray(); // ToArray is needed to force execution of YIELD RETURN

            // Change events to new name
            if (Metadata["EventsToRename"] is List<PMEvent> toRename)
            {
                foreach (var trc in traces)
                {
                    foreach (var ev in toRename)
                    {
                        ev.ActivityName = NewName; // rename node
                    }
                    yield return trc;
                }
            }
            else
            {
                foreach (var trc in traces)
                {
                    yield return trc;
                }
            }
        }

        public override IEnumerable<PMEvent> ProcessEvent(PMEvent _event, TraceMetadata Metadata)
        {

            if (Nodes.Any(n => n.IsEquivalent(_event, Metadata.newTrace.Events.ToArray()))) // si alguno de los nodos es equivalente
            {
                List<PMEvent> eventsToRename;
                if (Metadata["EventsToRename"] is List<PMEvent> stored) // leemos la lista de eventos a renombrar si existe
                {
                    eventsToRename = stored;
                }
                else // si no existe la lista se crea una nueva
                {
                    eventsToRename = new List<PMEvent>();
                }
                eventsToRename.Add(_event); // añadimos el evento a la lista
                Metadata["EventsToRename"] = eventsToRename; // salvamos la lista modificada otra vez en el metadata, machacando el valor anterior
            }
            yield return _event;

        }
    }



}
