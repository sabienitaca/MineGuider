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
    public class UICycleIntension : BaseTransformationEditorUI<CycleIntensionFilter>
    {
        public UICycleIntension(CycleIntensionFilter t, TransformationRegion info) : base(t, info)
        {
        }

        public override string Name => "Cycle intension";

        public override string Description => "Converts an node with self-arcs into a single node representing a cyclical task";              

        public override FrameworkElement getVisual()
        {
            var res = new BasicDescription();
            res.AddItem("Name:", Transformation.NewName);
            res.AddItem("Node:", Transformation.Node.Name);
            res.AddItem("Loop maximum:", Transformation.Maximum?.ToString() ?? "");
            res.AddItem("Loop condition:", Transformation.Condition ?? "");
            return res;
        }

        protected override bool SetFilterProperties()
        {            
            var newName = Editor.GetAnswers()[NewNameQuestion];
            int? max = int.TryParse(Editor.GetAnswers()[maximumQuestion], out int res) ? res : null;
            string? cond = Editor.GetAnswers()[conditionQuestion];
            Transformation.SetInfo(newName, max, cond, Information);
            return true;
        }

        private BasicPropertiesEditor Editor;
        private string NewNameQuestion = "Name:";
        private string maximumQuestion = "Maximum:";
        private string conditionQuestion = "Condition:";

        protected override IPropertiesEditor GetPropertiesEditor()
        {
            Editor = new BasicPropertiesEditor()
            {
                Text = "Cycle intension properties",
                DialogWidth = 400,
                DialogHeight = 250,
            };
            //Editor.AddQuestion(NewNameQuestion, true);
            Editor.AddNewNameQuestion(NewNameQuestion, Information.Nodes.First().Name);// .AddQuestion(NewNameQuestion, true, (value) => (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("@"), "The field cannot begin with the @ symbol"));
            //Editor.AddQuestion(maximumQuestion, false, (value) => (string.IsNullOrWhiteSpace(value) || int.TryParse(value, out int _), "The field is not a valid integer"));
            Editor.AddQuestion(maximumQuestion, false, (value) => (string.IsNullOrWhiteSpace(value) || (int.TryParse(value, out int intValue) && intValue > 0), "The field is not a valid integer or is equal to cero"));
            Editor.AddQuestion(conditionQuestion, false);
            return Editor;
        }
    }

    public class UICycleExtensional : BaseTransformationEditorUI<CycleExtensionFilter>
    {
        public UICycleExtensional(CycleExtensionFilter t, TransformationRegion info) : base(t, info)
        {
        }

        public override string Name => "Cycle extension";

        public override string Description => "Converts an node with self-arcs into the explicit repetition of several nodes composing a cyclical task";

        public override FrameworkElement getVisual()
        {
            var res = new BasicDescription();
            res.AddItem("Name:", Transformation.NewName);
            res.AddItem("Node:", Transformation.Node.Name);
            res.AddItem("Loop maximum:", Transformation.Maximum?.ToString() ?? "");
            return res;
        }

        protected override bool SetFilterProperties()
        {
            var newName = Editor.GetAnswers()[NewNameQuestion];
            int? max = int.TryParse(Editor.GetAnswers()[maximumQuestion], out int res) ? res : null;
            Transformation.SetInfo(newName, max, Information);
            return true;
        }

        private BasicPropertiesEditor Editor;
        private string NewNameQuestion = "Name:";
        private string maximumQuestion = "Maximum:";

        protected override IPropertiesEditor GetPropertiesEditor()
        {
            Editor = new BasicPropertiesEditor()
            {
                Text = "cycle extension properties",
                DialogWidth = 400,
                DialogHeight = 220,
            };
            Editor.AddNewNameQuestion(NewNameQuestion, Information.Nodes.First().Name);
            //Editor.AddQuestion(maximumQuestion, false, (value) => (string.IsNullOrWhiteSpace(value) || int.TryParse(value, out int _), "The field is not a valid integer"));
            Editor.AddQuestion(maximumQuestion, false, (value) => (string.IsNullOrWhiteSpace(value) || (int.TryParse(value, out int intValue) && intValue > 0), "The field is not a valid integer or is equal to cero"));
            return Editor;
        }
    }
}
