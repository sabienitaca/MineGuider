using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using Mineguide.perspectives.transformationsui.transformations.description;
using Mineguide.perspectives.transformationsui.transformations.propertiesEditor;
using Mineguide.perspectives.tpacontrol.mouse.contexts;
using pm4h.tpa.ipi;
using pm4h.windows.ui.fragments.tpaviewer;

namespace Mineguide.perspectives.transformationsui.transformations
{
    public abstract class BaseTransformationUI<T> : IUITransformation where T : ITransformationFilter
    {
        public T Transformation { get; set; }
        public TransformationRegion Information { get; set; }

        public BaseTransformationUI(T t, TransformationRegion info)
        {
            Transformation = t;
            Information = info;
        }

        public ITransformationFilter Filter
        {
            get => Transformation;
            set
            {
                if (value is T t)
                {
                    Transformation = t;
                }
                else
                {
                    throw new ArgumentException($"The filter must be a {typeof(T)}");
                }
            }
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public virtual bool ShowEditor() => true;

        protected abstract bool SetFilterProperties();

        public abstract FrameworkElement getVisual();

        public virtual void RenderTransformation(TPAViewerEngine tve) { }

        //public virtual void BlockNodes()
        //{
        //    foreach (var n in Information.States)
        //    {
        //        n.Block();
        //    }
        //}
        //public virtual void UnblockNodes()
        //{
        //    foreach (var n in Information.States)
        //    {
        //        n.Unblock();
        //    }
        //}
        //public virtual void SelectNodes()
        //{
        //    foreach (var n in Information.States)
        //    {
        //        n.Select();
        //    }
        //}

        //public virtual void UnselectNodes()
        //{
        //    foreach (var n in Information.States)
        //    {
        //        n.Deselect();
        //    }
        //}
    }
}
