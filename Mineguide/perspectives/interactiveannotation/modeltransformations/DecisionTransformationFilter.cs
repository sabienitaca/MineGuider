using Accord.Statistics.Kernels;
using Castle.Core.Internal;
using com.espertech.esper.compat;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using pm4h.data;
using pm4h.filter;
using pm4h.runner;
using pm4h.semantics;
using pm4h.stats;
using pm4h.tpa;
using pm4h.tpa.ipi;
using pm4h.windows.ui.blocks;
using pm4h.windows.ui.fragments.tpaviewer;
using pm4h.windows.ui.fragments.tpaviewer.renders;
using pm4h.windows.ui.fragments.tpaviewer.renders.modelrenders.exploitable;
using Sabien.open.utils;
using Sabien.Windows.Visual.Utils.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;
using static pm4h.tpa.TPATemplate;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{

    public abstract class DecisionTransformationFilter : BaseTransformationFilter
    {
        [RunnerProperty]
        public NodeReference node { get; set; }

        [RunnerProperty]
        public string DecisionName { get; set; }

        [RunnerProperty]
        public NodeReference? DefaultTransitionEndNode { get; set; }

        #region TransitionsExpression
        [RunnerProperty]
        public string TransitionsExpressionJSON { get; set; }

        Dictionary<NodeReference, string> _TransitionsExpression { get; set; } = null;
        public Dictionary<NodeReference, string> TransitionsExpression
        {
            get
            {
                if (_TransitionsExpression == null)
                {
                    _TransitionsExpression = TransitionsExpressionFromJSON(TransitionsExpressionJSON);
                }
                return _TransitionsExpression;
            }
            set
            {
                _TransitionsExpression = value;
                TransitionsExpressionJSON = TransitionsExpressionToJSON(_TransitionsExpression);
            }
        }

        public class MineguideTransitionsExpression
        {
            public NodeReference Node { get; set; }
            public string Expression { get; set; }
        }

        private string TransitionsExpressionToJSON(Dictionary<NodeReference, string> transitions)
        {
            List<MineguideTransitionsExpression> data = new List<MineguideTransitionsExpression>();
            foreach (var t in transitions)
            {
                data.Add(new MineguideTransitionsExpression() { Node = t.Key, Expression = t.Value });
            }
            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return jsonData;
        }

        private Dictionary<NodeReference, string> TransitionsExpressionFromJSON(string json)
        {
            Dictionary<NodeReference, string> Transitions = new Dictionary<NodeReference, string>();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MineguideTransitionsExpression>>(json);
            if (data != null)
            {
                foreach (var d in data)
                {
                    Transitions.Add(d.Node, d.Expression);
                }
            }
            return Transitions;
        }
        #endregion TransitionsExpression


        public void SetInfo(TransformationRegion info, string decisionName, Dictionary<NodeReference, string> transitionsExpression, NodeReference? defaultTransitionEndNode)
        {
            if (IsApplicable(info))
            {
                DecisionName = decisionName;
                node = NodeReference.FromNode(info.Nodes.First(), info.TPA);
                TransitionsExpression = transitionsExpression;
                DefaultTransitionEndNode = defaultTransitionEndNode;
            }
        }

        public override bool IsApplicable(TransformationRegion info)
        {
            if (info.Nodes.Count() != 1) // tiene que haber un solo nodo seleccionado
            {
                return false; // no es aplicable
            }

            var decisionNode = info.Nodes.First(); // obtener el nodo seleccionado
            var template = info.TPA;
            if (decisionNode.getOutTransitions(template).Count < 2) // el nodo tiene que tener mas de una transicion de salida
            {
                return false; // no es aplicable
            }

            // Restricción heredada de la transformación de subprocesoos (componente conectada)
            // Si el nodo seleccionado es el nodo salida de un subproceso no es aplicable
            if (GroupsHelper.GetGroupKey(decisionNode) is string decisionSubgroupKey && decisionSubgroupKey != null) // el nodo es parte de un subproceso
            {
                // busco si es nodo de salida del subproceso
                
                var templateGroups = GroupsHelper.GetTemplateGroups(template);
                if (templateGroups.TryGetValue(decisionSubgroupKey, out var subprocess) && subprocess != null)
                {
                    // busco nodo salida del subproceso
                    foreach (var subprocessNode in subprocess.Nodes)
                    {
                        // para todas las transiciones de salida del nodo
                        foreach (var transition in subprocessNode.getOutTransitions(template))
                        {
                            // para cada nodo destino de la transicion
                            foreach (var endNodeId in transition.EndNodes)
                            {
                                if (!subprocess.Nodes.Any(n => n.Id == endNodeId)) // si el nodo destino no es parte del subproceso
                                {
                                    // -- el nodo actual es el nodo de salida del subproceso --
                                    // si el nodo de salida del subproceso es el nodo seleccionado para la decisión --> no se cumple la restricción
                                    if (subprocessNode.Id == decisionNode.Id)
                                    {
                                        return false; // no es aplicable
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Subprocess of the node not found.");
                }
            }

            return true; // si se cumple todas las restricciones
        }

        public static bool IsDecision(PMEvent? evt)
        {
            if (evt == null) return false;
            return evt.SemanticId == SemanticEvent.PROFORMAEnquiryEvent;
        }

        public static bool IsDecision(TPATemplate.Node? node)
        {
            if (node == null) return false;
            return node.SemanticId == SemanticEvent.PROFORMAEnquiryEvent;
        }

        protected virtual void SetAsDecision(PMEvent evt)
        {
            evt.SemanticId = SemanticEvent.PROFORMAEnquiryEvent;
        }

        protected override Guid[] GetTraceabilityIds() => new Guid[1] { node.Id };
    }

    [PrivateRunnerElement]
    public class SingleDecisionFilter : DecisionTransformationFilter
    {
        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.DecisionSame;

        public override IEnumerable<PMEvent> ProcessEvent(PMEvent _event, TraceMetadata Metadata)
        {

            if (node.IsEquivalent(_event, Metadata.newTrace.Events.ToArray()))
            {
                _event.ActivityName = DecisionName;
                SetAsDecision(_event);
                //_event.AddMetadataTraceability(node.Id);
                //_event.AddMetadataConversion(MetadataExtensions.NodeConversion.DecisionSame);
                AddTransformationMetadata(_event);

                Metadata.setData("Decision", _event);
            }
            else
            {
                if (Metadata.HasKey("Decision"))
                {
                    PMEvent decision = Metadata.getData("Decision") as PMEvent;
                    if (decision != null && Metadata.getRelativeNewEvent(-1) is PMEvent last && last == decision)
                    {
                        last.Results = new List<string>(); // limpio los resultdos previos.
                        var tInfo = TransitionsExpression.FirstOrDefault(t => t.Key.IsEquivalent(_event, Metadata.newTrace.Events.ToArray()));
                        if (tInfo.Key != null)
                        {
                            var txt = tInfo.Value ?? "";
                            if (DefaultTransitionEndNode != null && DefaultTransitionEndNode.IsEquivalent(tInfo.Key))
                            {
                                if (txt.IsNullOrEmpty())
                                {
                                    txt = "*";
                                }
                                else
                                {
                                    txt += " (*)";
                                }
                            }
                            if (!txt.IsNullOrEmpty())
                            {
                                last.Results = new List<string>() { txt };
                            }
                        }
                    }
                    Metadata.Properties.Remove("Decision");
                }
            }

            yield return _event;
        }


        public override bool IsApplicable(TransformationRegion info)
        {
            if (base.IsApplicable(info)) // si cumple la base verifico cosas específicas
            {
                if (info.IsSelfCycle()) { return false; } // si es un ciclo no es aplicable

                var node = info.Nodes.First(); // obtener el nodo seleccionado
                if (!IsDecision(node)) // si no es ya un nodo decisión
                {
                    return true;
                }
            }
            return false;
        }
    }

    [PrivateRunnerElement]
    public class ExtendedDecisionFilter : DecisionTransformationFilter
    {
        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.DecisionNew;

        public override IEnumerable<PMEvent> ProcessEvent(PMEvent _event, TraceMetadata Metadata)
        {
            PMEvent? newDecisionEvent = null;
            foreach (var t in TransitionsExpression)
            {
                var nodeRef = t.Key;
                if (nodeRef.IsEquivalent(_event, Metadata.newTrace.Events.ToArray()))
                {
                    if (Metadata.getRelativeNewEvent(-1) is PMEvent last && last != null) // Get previous event that must be the origin of new decision
                    {
                        // if previous event is the origin of new decision (he de quitar un evento a la lista de evetnto nuevos porque el ultimo es el evento a evaluar)
                        if (node.IsEquivalent(last, Metadata.newTrace.Events.ToArray()[..^1]))
                        {
                            // SOLO AÑADIMOS METADATOS DE TRAZABILIDAD
                            last.AddMetadataTraceability(node.Id); // añado la trazabilidad al evento previo que era la decisión
                            //last.AddConversion(MetadataExtensions.NodeConversion.DecisionNew;

                            newDecisionEvent = (PMEvent)last.Clone(true);
                            newDecisionEvent.Start = last.End.Value;
                            newDecisionEvent.End = last.End.Value;
                            newDecisionEvent.ActivityName = DecisionName;
                            SetAsDecision(newDecisionEvent); // añado la semantica para que se muestre como decisión
                            AddTransformationMetadata(newDecisionEvent);

                            // -- Añadimos los resultados de las transiciones --
                            newDecisionEvent.Results = new List<string>(); // limpio los resultados por si el anterior evento era una decisión
                            var txt = t.Value ?? "";
                            if (DefaultTransitionEndNode != null && DefaultTransitionEndNode.IsEquivalent(nodeRef))
                            {
                                if (txt.IsNullOrEmpty())
                                {
                                    txt = "*";
                                }
                                else
                                {
                                    txt += " (*)";
                                }
                            }
                            if (!txt.IsNullOrEmpty())
                            {
                                newDecisionEvent.Results = new List<string>() { txt }; // añado la expresion de la transicion al evento previo que era la decisión
                            }

                            yield return newDecisionEvent;
                        }
                    }
                }
            }
            yield return _event;

        }

        public override bool IsApplicable(TransformationRegion info)
        {
            if (base.IsApplicable(info)) // si cumple la base verifico cosas específicas
            {
                var node = info.Nodes.First(); // obtener el nodo seleccionado
                if (!IsDecision(node)) // si no es ya un nodo decisión no requiere restricciones adicionales por ser decisión
                {
                    if (info.IsSelfCycle())
                    {
                        // el nodo tiene que tener al menos 3 transiciones de salida (2 para la nueva decisión y 1 para el autociclo)
                        if (node.getOutTransitions(info.Model.getTPATemplate()).Count() > 2)
                        {
                            return true;
                        }
                        //else return false;
                    }
                    else // tampoco es autociclo no requiere restricciones adicionales por ser autociclo
                    {
                        return true;
                    }
                }
                else// si es un nodo decisión requiere una validación adicional
                {
                    // el nodo tiene que tener al menos 3 transiciones de salida (2 para la nueva decisión y 1 que se queda la decisión anterior)
                    if (node.getOutTransitions(info.Model.getTPATemplate()).Count() > 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }


}
