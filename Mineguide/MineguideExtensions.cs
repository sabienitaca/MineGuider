using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using pm4h.data;
using pm4h.semantics;
using pm4h.tpa;

namespace Mineguide
{
    public static class MineguideExtensions
    {
        public static bool IsDecision(this PMEvent evt) => DecisionTransformationFilter.IsDecision(evt);

        public static bool IsDecision(this TPATemplate.Node node) => DecisionTransformationFilter.IsDecision(node);

    }
}
