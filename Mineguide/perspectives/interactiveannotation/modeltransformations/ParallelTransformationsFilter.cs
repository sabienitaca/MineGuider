using Accord.MachineLearning.DecisionTrees;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using OxyPlot;
using pm4h.algorithm.i2palia;
using pm4h.data;
using pm4h.runner;
using pm4h.tpa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{
    [PrivateRunnerElement]
    public class ParallelTransformationsFilter : BaseTransformationFilter
    {
        protected override MetadataExtensions.NodeConversion GetConversionType() => MetadataExtensions.NodeConversion.Parallel;

        protected override Guid[] GetTraceabilityIds()
        {
            List<Guid> traceabilityIds = new List<Guid>();
            foreach(var branch in ParallelNodes)
            {
                foreach(var nRef in branch)
                {
                    traceabilityIds.Add(nRef.Id);
                }
            }
            return traceabilityIds.ToArray();
        }

        [RunnerProperty]
        public NodeReference[][] ParallelNodes { get; set; }

        public void SetInfo(TransformationRegion info, TPATemplate.Node[][] Branches)
        {
            if (IsApplicable(info))
            {
                List<NodeReference[]> res = new List<NodeReference[]>();
                foreach (var xbr in Branches)
                {
                    var nodes = xbr.Select(x => NodeReference.FromNode(x, info.TPA)).ToArray();
                    res.Add(nodes);
                }
                ParallelNodes = res.ToArray();
                model = info.TPA;
                parallelNodes = Branches.Select(n => n.Select(x => Array.IndexOf(model.Nodes.ToArray(), x)).ToArray()).ToArray();
                //node = NodeReference.FromNode(info.Nodes.First(), info.TPA);

            }
        }

        TPATemplate model { get; set; }
        int[][] parallelNodes { get; set; }

        public override IEnumerable<IPMLog> ProcessLog(IEnumerable<IPMLog> _log)
        {
            foreach (var log in _log)
            {
                var md = log.getMetaData();
                md[InteractiveProcessDiscoveryData.KEY] = new InteractiveProcessDiscoveryData()
                {
                    Model = model,
                    ParallelNodes = parallelNodes,
                };
                log.setMetaData(md);
            }
            return base.ProcessLog(_log);
        }


        public override bool IsApplicable(TransformationRegion info)
        {
            return info.IsConnectedComponent();

        }
    }
}
