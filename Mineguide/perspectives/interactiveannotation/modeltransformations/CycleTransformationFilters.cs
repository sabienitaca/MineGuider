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
using System.Windows.Documents;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{

    public abstract class CycleTransformationFilter : BaseTransformationFilter
    {
        [RunnerProperty]
        public NodeReference Node { get; set; }

        [RunnerProperty]
        public string NewName { get; set; }

        [RunnerProperty]
        public int? Maximum { get; set; }

        public static string METADATA_MAXIMUM_ID = "Cycle_Maximum";

        public void AddMetadataMaximum(PMEvent evt)
        {
            string max = Maximum?.ToString() ?? "";
            evt.SaveToMetadata(METADATA_MAXIMUM_ID, max);
        }

        public static string GetMetadataMaximum(PMEvent evt)
        {
            if (evt.ReadFromMetadata<string>(METADATA_MAXIMUM_ID) is string value) return value;
            return "";
        }

        public static string GetMetadataMaximum(TPATemplate.Node node)
        {
            if (node.ReadFromMetadata<string>(METADATA_MAXIMUM_ID) is string value) return value;
            return "";
        }

        protected override Guid[] GetTraceabilityIds() => new Guid[1] { Node.Id };
        protected override void AddTransformationMetadata(PMEvent evt)
        {
            base.AddTransformationMetadata(evt);
            AddMetadataMaximum(evt);
        }

        public override bool IsApplicable(TransformationRegion info)
        {
            return info.IsSelfCycle();
        }

        public override IEnumerable<IPMLog> ProcessLog(IPMLog _log, IPMLog _target = null)
        {
            return RenameTransformationFilter.StaticProcessLog(Node, NewName, base.ProcessLog(_log, _target)); // rename node
        }
    }

    [PrivateRunnerElement]
    public class CycleIntensionFilter : CycleTransformationFilter
    {
        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.CycleIntensional;
        protected override void AddTransformationMetadata(PMEvent evt)
        {
            base.AddTransformationMetadata(evt);
            AddMetadataCondition(evt);
        }

        [RunnerProperty]
        public string? Condition { get; set; }

        public static string METADATA_CONDITION_ID = "CycleIntension_Condition";
        public void AddMetadataCondition(PMEvent evt)
        {
            string txt = Condition ?? "";
            evt.SaveToMetadata(METADATA_CONDITION_ID, txt);
        }

        public static string GetMetadataCondition(PMEvent evt)
        {
            if (evt.ReadFromMetadata<string>(METADATA_CONDITION_ID) is string value) return value;
            return "";
        }

        public static string GetMetadataCondition(TPATemplate.Node node)
        {

            if (node.ReadFromMetadata<string>(METADATA_CONDITION_ID) is string value) return value;
            return "";
        }


        public void SetInfo(string newName, int? maximum, string? condition, TransformationRegion info)
        {
            if (IsApplicable(info))
            {
                NewName = newName;
                Maximum = maximum;
                Condition = condition;
                Node = NodeReference.FromNode(info.Nodes.First(), info.TPA);
            }
        }

        public override IEnumerable<PMTrace> ProcessTrace(PMTrace _trace, TraceMetadata Metadata)
        {
            var traces = base.ProcessTrace(_trace, Metadata).ToArray(); // ToArray is needed to force execution of YIELD RETURN


            if (Maximum != null && Metadata["Cycles"] is Dictionary<PMEvent, int> cycles) // if maximum is not null and cycles info is not null
            {
                foreach (var trc in traces)
                {
                    bool maxReached = false;
                    foreach (var cycle in cycles)
                    {
                        //cycle.Key.ActivityName = NewName; // rename node
                        if (cycle.Value > Maximum)
                        {
                            maxReached = true;
                            break; // break foreach cycle because maximum is reached
                        }
                    }
                    if (!maxReached)
                    {
                        yield return trc;
                    }
                    else
                    {
                        // GENERAR WARNING AL INFORME PARA INDICAR QUE SE HA QUITADO LA TRAZA
                        ExtractionReportDataWareHouse.Warning(ExpId, this, WarningLevel.SkippedData,
                            $"[MineguideTransformation][CycleIntension] Trace {trc.SampleId} has been removed because the maximum number of cycles {Maximum} has been reached." +
                            $" {trc.SampleId}: {trc.ToString()}");
                    }
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
            // this event is one node of the auto-cycle and the previous event is the first node of the auto-cycle
            if (Node.IsEquivalent(_event) && Metadata.getRelativeNewEvent(-1) is PMEvent last && Node.IsEquivalent(last, Metadata.newTrace.Events.ToArray()))
            {
                if (Maximum != null) // si he de controlar el maximo
                {
                    if (Metadata["Cycles"] is Dictionary<PMEvent, int> cycles)
                    {
                        if (cycles.TryGetValue(last, out int repetitions))
                        {
                            cycles[last] = repetitions + 1; // add 1 to the repetitions
                        }
                        else
                        {
                            cycles[last] = 2; // starts at 2 because of the first event is last and the second is the current
                        }
                        Metadata["Cycles"] = cycles; // update metadata
                    }
                    else
                    {
                        // create new dictionary if it doesn't exist
                        Metadata["Cycles"] = new Dictionary<PMEvent, int>() { { last, 2 } }; // starts at 2 because of the first event is last and the second is the current
                    }
                }

                last.End = _event.End; // Final cycle event                                       
                AddTransformationMetadata(last); // Info to CycleIntension Event
            }
            else
            {
                if (Maximum != null) // si he de controlar el maximo
                {
                    if (Node.IsEquivalent(_event, Metadata.newTrace.Events.ToArray())) // events of the Cycle-Node that only occur once
                    {
                        if (Metadata["Cycles"] is Dictionary<PMEvent, int> cycles)
                        {
                            cycles[_event] = 1; // only one occurence
                            Metadata["Cycles"] = cycles; // update metadata
                        }
                        else
                        {
                            // create new dictionary if it doesn't exist
                            Metadata["Cycles"] = new Dictionary<PMEvent, int>() { { _event, 1 } }; // only one occurence
                        }
                    }
                }

                AddTransformationMetadata(_event); // Info to CycleIntension Event

                yield return _event;
            }
        }
    }

    [PrivateRunnerElement]
    public class CycleExtensionFilter : CycleTransformationFilter
    {
        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.CycleExtensional;

        public void SetInfo(string newName, int? maximum, TransformationRegion info)
        {
            if (IsApplicable(info))
            {
                NewName = newName;
                Node = NodeReference.FromNode(info.Nodes.First(), info.TPA);
                Maximum = maximum;
            }
        }

        public override IEnumerable<PMTrace> ProcessTrace(PMTrace _trace, TraceMetadata Metadata)
        {
            Metadata["DeleteTrace"] = false; // mark trace to not delete
            foreach (var trc in base.ProcessTrace(_trace, Metadata))
            {
                if (Metadata["DeleteTrace"] is bool deleteTrace && !deleteTrace)
                {
                    yield return trc;
                }
                else
                {
                    // GENERAR WARNING AL INFORME PARA INDICAR QUE SE HA QUITADO LA TRAZA
                    ExtractionReportDataWareHouse.Warning(ExpId, this, WarningLevel.SkippedData,
                        $"[MineguideTransformation][CycleExtension] Trace {trc.SampleId} has been removed because the maximum number of cycles {Maximum} has been reached." +
                        $" {trc.SampleId}: {trc.ToString()}");
                }
            }
        }

        public override IEnumerable<PMEvent> ProcessEvent(PMEvent _event, TraceMetadata Metadata)
        {

            if (Node.IsEquivalent(_event, Metadata.newTrace.Events.ToArray()))
            {
                int rep = 0;
                if (Metadata["repetitions"] is int i) // siempre calculo repticiones aunque Maximum sea null pq he de separarlas y uso ese valor para las IdKeys del evento
                {
                    rep = i + 1;
                }
                Metadata["repetitions"] = rep;
                if (Maximum == null || rep < Maximum) // si no tengo maximo o si tengo maximo y no lo he alcanzado
                {
                    _event.SetIdKey("CycleNumber", rep.ToString());

                    // Info to CycleIntension Event
                    _event.AddMetadataTraceability(Node.Id); // add traceability
                    _event.AddMetadataConversion(MetadataExtensions.NodeConversion.CycleExtensional); // add conversion
                    AddMetadataMaximum(_event);
                    yield return _event;
                }
                else
                {
                    // HE ALCANZADO EL MAXIMO DE REPETICIONES LUEGO MARCO LA TRAZA PARA BORRADO
                    Metadata["DeleteTrace"] = true;
                }
            }
            else
            {
                yield return _event;
            }

        }

    }
}
