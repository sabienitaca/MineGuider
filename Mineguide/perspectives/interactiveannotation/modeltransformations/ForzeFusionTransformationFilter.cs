using Mineguide.perspectives.interactiveannotation.annotationFilters;
using pm4h.data;
using pm4h.filter;
using pm4h.filter.fineanalysis;
using pm4h.runner;
using pm4h.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{
    [PrivateRunnerElement]
    public class ForzeFusionTransformationFilter : BaseTransformationFilter
    {
        //public override string TransformationName { get; set; } = "Fuse equivalent";
        //public override string TransformationDescription { get; set; } = "Fuses two equivalent nodes in a single one";

        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.ForzeFusion;

        protected override Guid[] GetTraceabilityIds() => nodes.Select(n => n.Id).ToArray();

        [RunnerProperty]
        public NodeReference[] nodes { get; set; }

        [RunnerProperty]
        public string  IdFusion { get; set; } = Guid.NewGuid().ToString("N");


        public void SetInfo(TransformationRegion info)
        {
            if (IsApplicable(info))
            {
                nodes = info.Nodes.Select(x => NodeReference.FromNode(x, info.TPA)).ToArray();
            }
        }


        public override IEnumerable<PMEvent> ProcessEvent(PMEvent _event, TraceMetadata Metadata)
        {
            if (nodes.Any(e=>e.IsEquivalent(_event,Metadata.newTrace.Events.ToArray())))
            {
                _event.SetIdKey(SyncByNodesFilter.MILESTONEKEY, IdFusion);
                AddTransformationMetadata(_event);
            }

            yield return _event;
        }


        public override bool IsApplicable(TransformationRegion info)
        {
            if (info.Nodes.Count() < 2) return false;
            var ix = info.Nodes.First();
            foreach (var x in info.Nodes.Skip(1))
            {
               if (!PMLogHelper.IsEquivalent(ix, x)) return false;
             }
            return true;
        }
    }
}
