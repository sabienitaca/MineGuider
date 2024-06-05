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
using pm4h.tpa;
using pm4h.windows.ui.fragments.tpaviewer.renders.modelrenders.exploitable;

namespace Mineguide.perspectives.transformationsui.transformations
{
    public class UISubprocess : BaseTransformationEditorUI<SubprocessTransformationFilter>
    {
        public UISubprocess(SubprocessTransformationFilter t, TransformationRegion info, TPATemplate template) : base(t, info)
        {
            Template = template;
        }

        TPATemplate Template;

        public override string Name => "Subprocess";

        public override string Description => "Encapsulates some nodes in a single one";

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
                Text = "Subprocess properties",
                DialogWidth = 400,
                DialogHeight = 200,
            };
            //Editor.AddNewNameQuestion(NewNameQuestion);

            Func<string?, (bool, string?)> ValidationFunction = (value) =>
            {
                var (valid, error) = BasicPropertiesEditor.NewNameValidationFunction(value); // Llamada a la funcion por defecto de validacion de null, empty y Starts with @
                if (valid)
                {
                    // controlamos que el nombre del grupo no exista ya en la jerarquia de grupos anidados de los nodos seleccionados
                    foreach(var node in Information.Nodes) // para cada nodo
                    {
                        if (node.GetIdKey(GroupsHelper.GROUP_ID_KEY) is string rawKey) // si el nodo ya esta en algún grupo
                        {                            
                            foreach(var groupKey in GroupsHelper.GetGroupsStringsFromKey(rawKey)) // obtengo la informacion de cada uno de los grupos en los que ya esta el nodo
                            {
                                var gName = GroupsHelper.GetGroupNameFromKey(groupKey); // obtengo el nombre del grupo
                                if (gName == value)
                                {
                                    return (false, $"Invalid subprocess name. The node \"{node.Name}\" is already contained in a subprocess with the same name \"{value}\"");
                                }
                            }
                        }
                    }                    
                }
                if (valid)
                {
                    // miramos que no existan nodos con ese nombre de subprocess
                    if(Template.Nodes.Any(n => n.Name == value))
                    {
                        return (false, $"Invalid subprocess name. There is already an \"{value}\" node in the model with that name");                        
                    }
                }
                return (valid, error);
            };

            Editor.AddQuestion(NewNameQuestion, true, ValidationFunction);


            return Editor;
        }
    }
}
