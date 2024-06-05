using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.transformationsui.transformations.description;
using Mineguide.perspectives.transformationsui.transformations.propertiesEditor;
using System.Windows;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using pm4h.tpa.ipi;
using Mineguide.perspectives.tpacontrol.mouse.contexts;
using pm4h.tpa;
using pm4h.windows.ui.fragments.tpaviewer;

namespace Mineguide.perspectives.transformationsui.transformations
{
    public class UIParallelism : BaseTransformationEditorUI<ParallelTransformationsFilter>
    {

        public UIParallelism(ParallelTransformationsFilter t, TransformationRegion info) : base(t, info)
        {

        }

        public override string Name => "Parallelism";

        public override string Description => "Defines that some activities that are represented sequentially in the model could be performed simultaneously";

        public override FrameworkElement getVisual()
        {
            var res = new BasicDescription();
            int branch = 1;
            foreach (var b in Transformation.ParallelNodes)
            {
                res.AddItem($"Branch {branch++}:", string.Join(", ", b.Select(n => n.Name)));
            }
            return res;
        }

        protected override bool SetFilterProperties()
        {
            Transformation.SetInfo(Information, Editor.GetBranchesNodes());
            //BlockNodes(); // bloquea su uso en otras transformaciones // NO FUNCIONA PQ EL MODELO SE REGENERARÁ LUEGO
            return true;
        }

        //public override void BlockNodes()
        //{
        //    //base.BlockNodes(); NO PUEDO USAR EL BASE PORQUE NO SE SELECCIONAN PREVIAMENTE TODOS LOS NODOS
        //    if (Editor != null)
        //    {
        //        foreach(var branch in Editor.GetBranchesStates())
        //        {
        //            foreach (var state in branch)
        //            {
        //                state.Block();
        //            }
        //        }
        //    }
        //}

        private ParallelismPropertiesEditor Editor;

        protected override IPropertiesEditor GetPropertiesEditor()
        {
            Editor = new ParallelismPropertiesEditor(Information)
            {
                DialogWidth = 1024,
                DialogHeight = 768,
            };

            return Editor;
        }

        public override void RenderTransformation(TPAViewerEngine tve)
        {
            base.RenderTransformation(tve);            

            // bloqueo los nodos del paralelismo
            foreach (var b in Transformation.ParallelNodes)
            {
                foreach (var n in b)
                {
                    if(tve.nodos.TryGetValue(n.Id, out var node))
                    {
                        node.ParallelismBlock();
                    }
                }
            }

            
        }

        //public override iTPAModel RenderTransformation(iTPAModel model)
        //{
        //    iTPAModel res = base.RenderTransformation(model);

        //    var tpa = model.getTPATemplate();
        //    foreach (var b in Transformation.ParallelNodes)
        //    {
        //        foreach (var n in b)
        //        {
        //            if (tpa.FindNodebyId(n.Id) is TPATemplate.Node)
        //            {

        //            }
        //        }
        //    }
        //    return res;
        //}
    }
}
