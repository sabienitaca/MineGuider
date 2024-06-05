using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using Mineguide.perspectives.transformationsui.transformations.description;
using Mineguide.perspectives.transformationsui.transformations.propertiesEditor;

namespace Mineguide.perspectives.transformationsui.transformations
{
    public class UIForzeFusion : BaseTransformationUI<ForzeFusionTransformationFilter>
    {
        public UIForzeFusion(ForzeFusionTransformationFilter t, TransformationRegion info) : base(t, info)
        {
        }

        public override string Name => "Fuse equivalent";

        public override string Description => "Fuses two equivalent nodes in a single one";              

        public override FrameworkElement getVisual()
        {
            var res = new BasicDescription();
            res.AddItem("Nodes:", string.Join(", ", Transformation.nodes.Select(x => x.Name)));
            return res;
        }

        protected override bool SetFilterProperties()
        {
            Transformation.SetInfo(Information);
            return true;
        }
    }
}
