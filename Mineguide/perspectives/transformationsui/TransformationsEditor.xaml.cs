using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using Mineguide.perspectives.transformationsui.transformations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pm4h.runner;
using pm4h.tpa.ipi;
using pm4h.windows.ui.blocks;
using pm4h.windows.ui.fragments.progress;
using pm4h.windows.ui.fragments.tpaviewer;
using pm4h.windows.ui.windows;
using pm4h.windows.utils;
using Sabien.Windows.Visual.Utils.WPF;

namespace Mineguide.perspectives.transformationsui
{
    public delegate IPortableRunner OnRequestNewRunnerDelegate(object? sender);

    /// <summary>
    /// Lógica de interacción para AnnotationEditor.xaml
    /// </summary>
    public partial class TransformationsEditor : UserControl
    {
        public string ContextId { get; set; }
        public string PerspectiveId { get; set; }

        public event EventHandler OnRequestRefreshModel;
        public event OnRequestNewRunnerDelegate OnRequestNewRunner;
        public event EventHandler OnSaveExperiment;

        private ILoading Busy;

        private TPAViewer Viewer;

        public TransformationsEditor(string contextId, string perspectiveId, ILoading busy, TPAViewer viewer)
        {
            InitializeComponent();

            ContextId = contextId;
            PerspectiveId = perspectiveId;
            Busy = busy;
            Viewer = viewer;

            //LoadStoredTransformations();            
        }

        public IEnumerable<ITransformationFilter> GetFilters()
        {
            return InvokeHelper.InvokeFunction(() =>
            {
                var FilterList = new List<ITransformationFilter>();
                foreach (object o in transformationContainer.Children)
                {
                    if (o is UIItem item)
                    {
                        if (item.UITransformation is IUITransformation transformation)
                        {
                            FilterList.Add(transformation.Filter);
                        }
                    }
                }
                return FilterList;
            });
        }

        public void AddTransformation(IUITransformation transformation)
        {
            var item = new UIItem(transformation);
            item.OnDelete += Item_OnDelete;
            EnableDeleteLastItem(false); // disable delete button of last item
            this.transformationContainer.Children.Add(item); // add new item
        }

        private void EnableDeleteLastItem(bool enable)
        {
            if (this.transformationContainer.Children.Count > 0) // disable delete button of last item
            {
                if (this.transformationContainer.Children[this.transformationContainer.Children.Count - 1] is UIItem lastItem)
                {
                    lastItem.DeleteEnabled = enable;
                }
            }
        }

        private void Item_OnDelete(object? sender, EventArgs e)
        {
            if (sender is UIItem item)
            {
                if (PM4HMessageBox.Show("Are you sure you want to delete this transformation?", "Delete transformation", PM4HMessageBoxButtons.YesNo, PM4HMessageBoxIcons.Question) is bool b && b)
                {
                    this.transformationContainer.Children.Remove(item);
                    EnableDeleteLastItem(true); // enable delete button of last item
                    OnRequestRefreshModel?.Invoke(this, EventArgs.Empty); // refresh model
                }
            }
        }

        /// <summary>
        /// Si alguna transformación ha de bloquear nodos o cambiar colores, se hace aquí
        /// </summary>
        public void RenderTransformations(TPAViewerEngine tve)
        {
            if (tve == null) return;

            foreach (object o in transformationContainer.Children)
            {
                if (o is UIItem item)
                {
                    if (item.UITransformation is IUITransformation transformation)
                    {
                        InvokeHelper.InvokeAction(() => transformation.RenderTransformation(tve));
                        //transformation.RenderTransformation(viewer.tve);
                    }
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            OnRequestRefreshModel?.Invoke(this, EventArgs.Empty);
        }

        private void btnExtractGroup_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                Busy?.Enable();
                if (OnRequestNewRunner?.Invoke(this) is IUIRunner rnr)
                {
                    var logx = rnr.GetLog(); // pido al runner el log y eso ya me lo da filtrado pq ejecuta toda la parte central hasta PALIA
                    PMAppWinHelper.ExecutePMAappExperimentExtraction((IUIRunner)rnr, logx);
                }
            }
            finally
            {
                Busy?.Disable();
            }
        }

        private void btnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (PM4HMessageBox.Show("Are you sure you want to delete all transformations?", "Delete all transformations", PM4HMessageBoxButtons.YesNo, PM4HMessageBoxIcons.Question) is bool b && b)
            {
                transformationContainer.Children.Clear();
                OnRequestRefreshModel?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            //SaveExperiment();
            OnSaveExperiment?.Invoke(this, EventArgs.Empty);
        }

        #region SaveExperiment


        public const string TPA_METADATA_TRANSFORMATIONS_ID = "MINEGUIDE_TRANSFORMATIONS";

        public void StoreTranformations(iTPAModel model)
        {
            var filtersJSON = SerializeFilters(GetFilters());
            model.set(TPA_METADATA_TRANSFORMATIONS_ID, filtersJSON); // añado las transformaciones al modelo para poder recuperarlas luego
        }

        public void ClearStoredTransformations(iTPAModel model)
        {
            model.set(TPA_METADATA_TRANSFORMATIONS_ID, null); // vacio por si Remove no hace lo esperado
            model.getMetaData().Remove(TPA_METADATA_TRANSFORMATIONS_ID); // elimino las transformaciones del modelo
        }

        public void LoadStoredTransformations(iTPAModel model, bool requestRefreshModel)
        {
            if (model == null) return;

            if (model.get<string>(TPA_METADATA_TRANSFORMATIONS_ID) is string json) // obtengo transformaciones previas si existen
            {
                var filters = DeserializeFilters(json); // deserializo las transformaciones
                                                        // añado las transformaciones al editor buscando por el tipo del filtro
                foreach (var f in filters)
                {
                    if (f is CycleIntensionFilter ci)
                    {
                        AddTransformation(new UICycleIntension(ci, null));
                    }
                    else if (f is CycleExtensionFilter ce)
                    {
                        AddTransformation(new UICycleExtensional(ce, null));
                    }
                    else if (f is SingleDecisionFilter sd)
                    {
                        AddTransformation(new UIDecisionSame(sd, null));
                    }
                    else if (f is ExtendedDecisionFilter ed)
                    {
                        AddTransformation(new UIDecisionNew(ed, null));
                    }
                    else if (f is ForzeFusionTransformationFilter ff)
                    {
                        AddTransformation(new UIForzeFusion(ff, null));
                    }
                    else if (f is ParallelTransformationsFilter p)
                    {
                        AddTransformation(new UIParallelism(p, null));
                    }
                    else if (f is CompositionTransformationFilter c)
                    {
                        AddTransformation(new UIComposition(c, null));
                    }
                    else if (f is SubprocessTransformationFilter s)
                    {
                        AddTransformation(new UISubprocess(s, null, null));
                    }
                    else
                    {
                        PM4HMessageBox.Show($"Transformation {f.GetType().Name} not implemented yet", "Transformations load error", icon: PM4HMessageBoxIcons.Error);
                    }
                }

                // El refresco del modelo se puede querer hacer fuera ya que no se refresca sincronamente, pq el evento se lanza desde el hilo de la UI, lo podemos hacer fuera para tener control
                if (requestRefreshModel && filters.Any()) OnRequestRefreshModel?.Invoke(this, EventArgs.Empty);
            }
        }

        public string SerializeFilters(IEnumerable<ITransformationFilter> filters)
        {
            var filtersList = filters.ToList();
            var filtersJson = new JArray();
            foreach (var filter in filtersList)
            {
                var filterJson = new JObject();
                filterJson.Add("$type", JToken.FromObject(filter.GetType().AssemblyQualifiedName));
                RunnerElementWrapper wrapper = RunnerElementWrapperHelper.BuildWrapperFromRunnerElement(filter);
                filterJson.Add("Filter", JToken.FromObject(wrapper, new JsonSerializer() { TypeNameHandling = TypeNameHandling.All, MissingMemberHandling = MissingMemberHandling.Ignore }));
                filtersJson.Add(filterJson);
            }
            return filtersJson.ToString();
        }

        public IEnumerable<ITransformationFilter> DeserializeFilters(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<ITransformationFilter>();

            var filtersJson = JArray.Parse(json);
            var filters = new List<ITransformationFilter>();
            foreach (var filterJson in filtersJson)
            {
                var typeName = filterJson["$type"].Value<string>();
                Type targetType = Type.GetType(typeName);
                var wrapper = filterJson["Filter"].ToObject<RunnerElementWrapper>(new JsonSerializer() { TypeNameHandling = TypeNameHandling.All, MissingMemberHandling = MissingMemberHandling.Ignore });
                var filter = wrapper.CreateInstanceAs<ITransformationFilter>(Guid.NewGuid(), null);
                filters.Add(filter);
            }
            return filters;
        }
        #endregion
    }
}
