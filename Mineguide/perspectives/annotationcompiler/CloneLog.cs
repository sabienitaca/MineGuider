using pm4h.data;
using pm4h.filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mineguide.perspectives.annotationcompiler
{
    public class CloneLogFilter: PMLogParallelFilter
    {
        public override IEnumerable<PMEvent> ProcessEvent(PMEvent _event, TraceMetadata Metadata)
        {
            return new PMEvent[] { (PMEvent)_event.Clone(true) };
        }
    }
}
