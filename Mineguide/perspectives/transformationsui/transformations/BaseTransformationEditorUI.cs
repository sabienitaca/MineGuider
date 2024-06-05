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
    //public abstract class BaseEditorTransformationUI<T> : IUITransformation where T : ITransformationFilter  
    public abstract class BaseTransformationEditorUI<T> : BaseTransformationUI<T> where T : ITransformationFilter
    {
        public BaseTransformationEditorUI(T t, TransformationRegion info) : base(t, info) { }        

        public override bool ShowEditor()
        {
            var res = GetPropertiesEditor();
            if (res.ShowDialog())
            {
                return SetFilterProperties();
            }
            return false;
        }

        protected abstract IPropertiesEditor GetPropertiesEditor();

        //protected abstract bool SetFilterProperties();

    }
}
