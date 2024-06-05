using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using pm4h.tpa.ipi;
using pm4h.windows.interfaces;
using pm4h.windows.ui.fragments.tpaviewer;

namespace Mineguide.perspectives.transformationsui.transformations
{
    public interface IUITransformation : IVisual
    {
        string Name { get; }
        string Description { get; }

        ITransformationFilter Filter { get; set; }
        TransformationRegion Information { get; set; }

        bool ShowEditor();

        //void BlockNodes();
        //void UnblockNodes();

        //void SelectNodes();
        //void UnselectNodes();

        public void RenderTransformation(TPAViewerEngine tve);
    }
}
