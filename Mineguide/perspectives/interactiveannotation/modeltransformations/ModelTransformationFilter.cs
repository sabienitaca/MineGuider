using Mineguide.perspectives.interactiveannotation.annotationFilters;
using pm4h.data;
using pm4h.filter;
using pm4h.runner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{
    [PrivateRunnerElement]
    public class ModelTransformationFilter:PMLogParallelFilter
    {

        

        [RunnerProperty(Level = RunnerPropertyLevel.Hidden, Behavior = RunnerPropertyBehavior.Wrapper)]
        public RunnerElementCollection<ITransformationFilter> Filters { get; set; } = new RunnerElementCollection<ITransformationFilter>();

        public void SetFilters(IEnumerable<ITransformationFilter> flts)
        {
            Filters.Clear();
            foreach (var filter in flts)
            {
                var rew = RunnerElementWrapperHelper.BuildWrapperFromRunnerElement(filter);
                Filters.Add(rew);
            }
        }

        public override IEnumerable<IPMLog> ProcessLog(IEnumerable<IPMLog> _log)
        {

            foreach (var f in Filters.CreateRunnerElements(ExpId, CurrentRunner))
            {
                _log = f.ProcessLog(_log).ToArray();
            }

            return _log.ToArray();
        }
    }


    public abstract class BaseTransformationFilter : PMLogParallelFilter, ITransformationFilter
    {

        protected override void InitExecutionOnce()
        {

#if DEBUG

        Maxthreads = 1;

#endif
            CloneLog = true;
            base.InitExecutionOnce();
        }
        public abstract bool IsApplicable( TransformationRegion info);

        protected virtual void AddTransformationMetadata(PMEvent evt)
        {
            if (evt == null) return;
            evt.AddMetadataTraceability(GetTraceabilityIds());
            evt.AddMetadataConversion(GetConversionType());
        }

        protected virtual void AddTransformationMetadata(IEnumerable<PMEvent> evts)
        {
            if (evts == null) return;
            foreach (var item in evts)
            {
                AddTransformationMetadata(item);
            }
        }

        protected abstract Guid[] GetTraceabilityIds();

        protected abstract MetadataExtensions.NodeConversion GetConversionType();

    }


}
