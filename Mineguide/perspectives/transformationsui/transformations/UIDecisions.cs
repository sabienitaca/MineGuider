using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using Mineguide.perspectives.transformationsui.transformations.description;
using Mineguide.perspectives.transformationsui.transformations.propertiesEditor;
using System.Windows;
using pm4h.tpa;
using Accord.MachineLearning.DecisionTrees;

namespace Mineguide.perspectives.transformationsui.transformations
{
    public class UIDecisionSame : BaseTransformationEditorUI<SingleDecisionFilter>
    {
        public UIDecisionSame(SingleDecisionFilter t, TransformationRegion info) : base(t, info)
        {
        }

        public override string Name => "Decision";

        public override string Description => "Converts into a decision";

        public override FrameworkElement getVisual()
        {
            var res = new BasicDescription();
            res.AddItem("Name:", Transformation.DecisionName);
            res.AddItem("Node:", Transformation.node.Name);

            // Transitions expression
            //foreach (var result in Editor.GetResults())
            //{
            //    string value = result.Value;
            //    if (result.IsDefault) { value = "(default) " + value; }
            //    res.AddItem($"Transition to {result.Name}:", value);
            //}
            //else
            if (Transformation.TransitionsExpression != null)
            {
                foreach (var t in Transformation.TransitionsExpression)
                {
                    if (Transformation.DefaultTransitionEndNode != null && Transformation.DefaultTransitionEndNode.Id == t.Key.Id)
                    {
                        res.AddItem($"Transition to {t.Key.Name}:", "(default) " + t.Value);
                    }
                    else
                    {
                        res.AddItem($"Transition to {t.Key.Name}:", t.Value);
                    }
                }
            }

            return res;
        }

        protected override bool SetFilterProperties()
        {
            Dictionary<NodeReference, string> transitions = new Dictionary<NodeReference, string>();
            NodeReference? defaultTransition = null;
            var template = Information.TPA;
            foreach (var result in Editor.GetResults())
            {
                var nodeRef = NodeReference.FromNode(result.EndNode, template);
                transitions.Add(nodeRef, result.Value);
                if (result.IsDefault)
                {
                    defaultTransition = nodeRef;
                }
            }
            Transformation.SetInfo(Information, Editor.DecisionName, transitions, defaultTransition);
            return true;
        }

        private DecisionPropertiesEditor Editor;

        protected override IPropertiesEditor GetPropertiesEditor()
        {
            var template = Information.TPA;

            //var decisionName = Information.Nodes.First().Name;           

            Editor = new DecisionPropertiesEditor(Information.Nodes.First(), template, DecisionPropertiesEditor.DecisionTypes.Decision)
            {
                DialogWidth = 600,
                DialogHeight = 350
            };

            foreach (var n in Information.Nodes)
            {
                foreach (var t in n.getOutTransitions(template))
                {
                    Editor.AddResult(t);
                }
            }

            return Editor;
        }
    }

    public class UIDecisionNew : BaseTransformationEditorUI<ExtendedDecisionFilter>
    {
        public UIDecisionNew(ExtendedDecisionFilter t, TransformationRegion info) : base(t, info)
        {
        }

        public override string Name => "New decision";

        public override string Description => "Adds a new decision node";

        public override FrameworkElement getVisual()
        {
            var res = new BasicDescription();
            res.AddItem("Name:", Transformation.DecisionName);
            res.AddItem("Previous node:", Transformation.node.Name);

            // Transitions expression
            //if (Editor != null)
            //{
            //    foreach (var result in Editor.GetResults())
            //    {
            //        string value = result.Value;
            //        if (result.IsDefault) { value = "(default) " + value; }
            //        res.AddItem($"Transition to {result.Name}:", value);
            //    }
            //}
            //else 
            if (Transformation.TransitionsExpression != null)
            {
                foreach (var t in Transformation.TransitionsExpression)
                {
                    if (Transformation.DefaultTransitionEndNode != null && Transformation.DefaultTransitionEndNode.Id == t.Key.Id)
                    {
                        res.AddItem($"Transition to {t.Key.Name}:", "(default) " + t.Value);
                    }
                    else
                    {
                        res.AddItem($"Transition to {t.Key.Name}:", t.Value);
                    }
                }
            }

            return res;
        }

        protected override bool SetFilterProperties()
        {
            Dictionary<NodeReference, string> transitions = new Dictionary<NodeReference, string>();
            NodeReference? defaultTransition = null;
            var template = Information.TPA;
            foreach (var result in Editor.GetResults())
            {
                var nodeRef = NodeReference.FromNode(result.EndNode, template);
                transitions.Add(nodeRef, result.Value);
                if (result.IsDefault)
                {
                    defaultTransition = nodeRef;
                }
            }
            Transformation.SetInfo(Information, Editor.DecisionName, transitions, defaultTransition);
            return true;
        }

        private DecisionPropertiesEditor Editor;

        protected override IPropertiesEditor GetPropertiesEditor()
        {
            //var decisionName = Information.Nodes.First().Name;
            //if (decisionName == "@Start")
            //{
            //    decisionName = "Start";
            //}

            var template = Information.TPA;

            Editor = new DecisionPropertiesEditor(Information.Nodes.First(), template, DecisionPropertiesEditor.DecisionTypes.NewDecision)
            {
                DialogWidth = 600,
                DialogHeight = 450
            };

            foreach (var n in Information.Nodes)
            {
                foreach (var t in n.getOutTransitions(template))
                {
                    if(t.EndNodes.Contains(n.Id) && n.IsSelfCycle(template)) // skip self-cycles                    
                    {
                        continue; // skip self-cycles
                    }
                    Editor.AddResult(t);
                }
            }

            return Editor;
        }
    }
}
