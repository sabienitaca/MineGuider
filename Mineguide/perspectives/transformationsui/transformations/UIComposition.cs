using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Google.Protobuf.WellKnownTypes;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using Mineguide.perspectives.transformationsui.transformations.description;
using Mineguide.perspectives.transformationsui.transformations.propertiesEditor;

namespace Mineguide.perspectives.transformationsui.transformations
{
    public class UIComposition : BaseTransformationEditorUI<CompositionTransformationFilter>
    {
        public UIComposition(CompositionTransformationFilter t, TransformationRegion info) : base(t, info)
        {
        }

        public override string Name => "Composition";

        public override string Description => "Creates a new node that represents all selected nodes";              

        public override FrameworkElement getVisual()
        {
            var res = new BasicDescription();
            res.AddItem("Name:", Transformation.newname);
            res.AddItem("Nodes:", string.Join(", ", Transformation.nodes.Select(x => x.Name)));
            return res;
        }

        protected override bool SetFilterProperties()
        {            
            var newName = Editor.GetAnswers()[NewNameQuestion];
            Transformation.SetInfo(newName, Information);
            return true;
        }

        private BasicPropertiesEditor Editor;
        private string NewNameQuestion = "Name:";
        protected override IPropertiesEditor GetPropertiesEditor()
        {
            Editor = new BasicPropertiesEditor()
            {
                Text = "Composition properties",
                DialogWidth = 400,
                DialogHeight = 200,
            };
            Editor.AddNewNameQuestion(NewNameQuestion);

            return Editor;
        }
    }
}
