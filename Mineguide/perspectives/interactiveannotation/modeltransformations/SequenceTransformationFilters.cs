using com.espertech.esper.compat;
using com.espertech.esper.events.vaevent;
using Irony.Interpreter.Ast;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using pm4h.data;
using pm4h.filter;
using pm4h.runner;
using pm4h.tpa;
using pm4h.windows.ui.fragments.tpaviewer.renders.modelrenders.exploitable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{

    public abstract class SequenceTransformationFilter : BaseTransformationFilter
    {
        [RunnerProperty]
        public NodeReference[] nodes { get; set; }

        [RunnerProperty]
        public string newname { get; set; }

        protected override Guid[] GetTraceabilityIds() => nodes.Select(n => n.Id).ToArray();

        public void SetInfo(string newNode, TransformationRegion info)
        {
            if (IsApplicable(info))
            {
                newname = newNode;
                //nodes = info.GetSequence().Select(x => NodeReference.FromNode(x, info.TPA)).ToArray();

                // select nodereferences from nodes without duplicates
                var refs = new HashSet<NodeReference>();
                foreach (var n in info.Nodes)
                {
                    refs.Add(NodeReference.FromNode(n, info.TPA));
                }
                nodes = refs.ToArray();
            }
        }


        public override bool IsApplicable(TransformationRegion info)
        {
            return info.IsConnectedComponent();           
        }

        ///// <summary>
        ///// Search if all the output nodes of the decision nodes are included in the selected nodes
        ///// </summary>
        //protected bool AreCyclesFullIncluded(TransformationRegion info)
        //{
        //    if (info.Nodes.Any(n => n.IsDecision()))
        //    {
        //        var template = info.TPA;
        //        foreach (var n in info.Nodes) // busco los nodos decision
        //        {
        //            if (n.IsDecision()) // si es una decisión busco si todos los nodos salida están incluidos entre los nodos seleccionados
        //            {
        //                var endNodes = new List<TPATemplate.Node>();
        //                // busco todos los nodos destino
        //                n.getOutTransitions(template).ToList().ForEach(t =>
        //                {
        //                    endNodes = endNodes.Union(t.getEndNodes(template)).ToList();
        //                });
        //                if (endNodes.Any(n => !info.Nodes.Contains(n))) // verifico que todos los nodos destino esten incluidos
        //                {
        //                    return false; // no todos los nodos salida de la decisión estan incluidos 
        //                }
        //            }
        //        }
        //        return true; // todos los nodos salida de las decisiones estan incluidos
        //    }
        //    else { return true; }
        //}
    }

    [PrivateRunnerElement]
    public class CompositionTransformationFilter : SequenceTransformationFilter
    {
        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.Composition;

        public override bool IsApplicable(TransformationRegion info)
        {
            if (base.IsApplicable(info))
            {
                // 1) si cojo nodos de un subgrupo, tengo que coger el subgrupo completo
                HashSet<string> selectedSubprocess = new HashSet<string>();
                foreach (var n in info.Nodes)
                {
                    var group = GroupsHelper.GetGroupKey(n);
                    if (group!= null && group != "*")
                    {
                        selectedSubprocess.Add(group);
                    }
                }
                if (selectedSubprocess.Count > 1)
                {
                    var template = info.TPA;
                    foreach (var groupKey in selectedSubprocess) // compruebo para cada subgrupo si se ha seleccionado completo
                    {
                        if (template.Nodes.Any(n => GroupsHelper.GetGroupKey(n) == groupKey && !info.Nodes.Contains(n)))
                        {
                            return false; //there are nodes from the same group base key that are not included in the selection
                        }
                    }
                }

                return true;
            }
            return false; // else --> if(base.IsApplicable(info))
        }        

        public override IEnumerable<PMTrace> ProcessTrace(PMTrace _trace, TraceMetadata Metadata)
        {
            List<PMEvent> evs = new List<PMEvent>();
            foreach (var n in nodes)
            {
                evs.AddRange(n.GetEquivalents(_trace.Events));


            }
            if (evs.Count > 0)
            {
                var ev = (PMEvent)evs.First().Clone(true);
                ev.Start = evs.Select(e => e.Start).Min();
                ev.End = evs.Select(e => e.End.Value).Max();
                ev.ActivityName = newname;
                AddTransformationMetadata(ev);
                _trace.InsertByStartTime(ev);
                _trace.Events = _trace.Events.Except(evs).ToList();
            }

            yield return _trace;
        }
    }

    [PrivateRunnerElement]
    public class SubprocessTransformationFilter : SequenceTransformationFilter
    {
        [RunnerProperty]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.Subprocess;

        protected void AddTransformationMetadata(IEnumerable<PMEvent> evts, NodeReference nodeRef)
        {
            AddTransformationMetadata(evts);
            foreach (var evt in evts)
            {
                evt.AddMetadataTraceability(nodeRef.Id); // sobreescribo la trazabilidad que era toda la lista de nodos del grupo
            }
        }

        public override bool IsApplicable(TransformationRegion info)
        {
            if (base.IsApplicable(info))
            {
                // RESTICCION: 1) El anidamiento de jerarquía se hace de arriba a abajo
                // ver si hay nodos de diferentes grupos base
                var baseGroupKey = GroupsHelper.GetGroupKey(info.Nodes.First());
                if (info.Nodes.Any(n => GroupsHelper.GetGroupKey(n) != baseGroupKey))
                {
                    return false; //there are nodes from different groups base key
                }

                // RESTICCION: 2) Si vuelvo a seleccionar todos los nodos de un subproceso, no se puede generar otro subproceso
                if (baseGroupKey != null)
                {
                    var template = info.TPA;
                    // busco todos los nodos del grupo base
                    var groupNodes = template.Nodes.Where(n => GroupsHelper.GetGroupKey(n) == baseGroupKey).ToList();
                    // si todos los nodos del grupo están seleccionados
                    if (info.Nodes.Length == groupNodes.Count && info.Nodes.All(n => groupNodes.Contains(n)))
                    {
                        return false; //all nodes of the group are selected
                    }
                }
                return true;
            }
            return false; // else --> if(base.IsApplicable(info))
        }

        public override IEnumerable<PMTrace> ProcessTrace(PMTrace _trace, TraceMetadata Metadata)
        {
            List<PMEvent> evs = new List<PMEvent>();
            foreach (var n in nodes)
            {
                IEnumerable<PMEvent> equi = n.GetEquivalents(_trace.Events).ToList();
                evs.AddRange(equi);
                AddTransformationMetadata(equi, n);
            }
            if (evs.Count > 0)
            {
                foreach (var nx in evs)
                {
                    var currentgroup = GroupsHelper.GetGroupsRawData(nx);
                    string newGroupKey;
                    if (currentgroup != "*")
                    {
                        newGroupKey = GroupsHelper.CreateGroupKey(Id, newname, currentgroup); // create new group key
                    }
                    else
                    {
                        newGroupKey = GroupsHelper.CreateGroupKey(Id, newname); // create new group key
                    }
                    nx.SetIdKey(GroupsHelper.GROUP_ID_KEY, newGroupKey); // set event new group key
                }
            }

            yield return _trace;
        }
    }

}
