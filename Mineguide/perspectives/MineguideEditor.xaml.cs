using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using com.espertech.esper.compat.collections;
using Mineguide.perspectives.annotationcompiler;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using Mineguide.perspectives.interactiveannotation.tpaprocessors;
using Mineguide.perspectives.semantics;
using Mineguide.perspectives.tpacontrol.mouse.contexts;
using Mineguide.perspectives.transformationsui;
using Mineguide.perspectives.transformationsui.transformations;
using pm4h.algorithm.palia.ipalia;
using pm4h.runner;
using pm4h.semantics.datamodel;
using pm4h.tpa.ipi;
using pm4h.utils;
using pm4h.utils.saver;
using pm4h.windows.interfaces;
using pm4h.windows.ui;
using pm4h.windows.ui.blocks;
using pm4h.windows.ui.fragments.perspective;
using pm4h.windows.ui.fragments.perspective.layouts;
using pm4h.windows.ui.fragments.tpaviewer;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.helpers;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements;
using pm4h.windows.ui.fragments.tpaviewer.renders;
using pm4h.windows.ui.fragments.tpaviewer.renders.modelrenders.exploitable;
using pm4h.windows.ui.windows;
using pm4h.windows.utils;
using Sabien.Utils;
using Sabien.Windows.Visual.Utils.WPF;

namespace Mineguide.perspectives
{
    /// <summary>
    /// Lógica de interacción para MineguideEditor.xaml
    /// </summary>
    public partial class MineguideEditor : UserControl
    {
        public static string PerspectiveId = "MineguideEditor";

        private PerspectiveLayout layout;
        public IPM4HPerspectiveLayout GetLayout() => layout;

        private MainPerspectiveArgs args;
        private TPAViewer viewer;

        private iTPAModel modelActual;
        private iTPAModel modelOriginal;
        private ModelInfo info;

        TPASelectionMouseContext mouseContext;

        TransformationsEditor transformationsEditor;
        SemanticTerminologies semanticTerminologies;
        ModelSemanticEditor modelSemanticEditor;
        ModelSemanticEditor nodesSemanticEditor;

        IEnumerable<ITransformationFilter> FilterList => transformationsEditor.GetFilters();

        ModelInfo ModelInfoLocal { get; set; }

        public MineguideEditor(MainPerspectiveArgs a)
        {
            InitializeComponent();

            args = a;

            // -- layout --
            layout = new PerspectiveLayout(args.ContextId, PerspectiveId) { Name = "laylay" };
            main.Children.Add(layout); // add layout to the main grid

            // -- tpa viewer --
            viewer = new TPAViewer(ITPAModelHelper.TVEEngineType.ProForma);

            viewer.Margin = new Thickness(0);
            mouseContext = new TPASelectionMouseContext(viewer.tve); // cambio el contexto de raton del tve para usar selección especial
            // Enlazo los eventos de raton del tve a mi objeto
            viewer.tve.ContextoRaton.OnRightClick += Tve_OnRightClick;
            viewer.tve.ContextoRaton.OnStateClick += Tve_OnStateClick;
            viewer.tve.ContextoRaton.OnStateRightClick += Tve_OnStateRightClick;
            viewer.tve.ContextoRaton.OnStateDoubleClick += Tve_OnStateDoubleClick;
            viewer.tve.ContextoRaton.OnTransitionClick += Tve_OnTransitionClick;
            viewer.tve.ContextoRaton.OnTransitionRightClick += Tve_OnTransitionRightClick;
            viewer.tve.ContextoRaton.OnTransitionDoubleClick += Tve_OnTransitionDoubleClick;
            viewer.tve.ContextoRaton.OnMovementFinished += ContextoRaton_OnMovementFinished;
            layout.AddMainContent(viewer); // add tpa viewer to the layout

            this.KeyDown += MineguideEditor_KeyDown;
            this.KeyUp += MineguideEditor_KeyUp;

            // -- add menu secions --
            // Transformaciones
            transformationsEditor = new TransformationsEditor(args.ContextId, PerspectiveId, layout.GetLoading(), viewer);
            transformationsEditor.OnRequestRefreshModel += TransformationsEditor_OnRequestRefreshModel; // boton de refrescar modelo representado pq hay transformaciones nuevas o borradas
            transformationsEditor.OnRequestNewRunner += TransformationsEditor_OnRequestNewRunner; // boton de obtener el nuevo runner con las transformaciones y semantica, para extraerno o para salvarlo.
            transformationsEditor.OnSaveExperiment += TransformationsEditor_OnSaveExperiment;
            string sectionId_Transformations = "Transformations";
            layout.AddMenuSection(new PerspectiveSectionOptions(sectionId_Transformations, "Transformations", "TRANSFORMATIONS", StyleHelper.GetIcon("pm4h.Resources.IconPath.Mineguide.Transformations"))); //StyleHelper.GetIcon("pm4h.Resources.IconPath.SemanticEditor")));
            layout.AddToMenuSection(sectionId_Transformations, transformationsEditor); // add code editor to the menu section           

            // Semantic Terminologies
            string sectionId_SemanticTerminologies = "SemanticTerminologies";
            layout.AddMenuSection(new PerspectiveSectionOptions(sectionId_SemanticTerminologies, "Semantic Terminologies", "SEMANTIC TERMINOLOGIES", StyleHelper.GetIcon("pm4h.Resources.IconPath.Mineguide.SemanticTerminologies")));//StyleHelper.GetIcon("pm4h.Resources.IconPath.CircleAndDots")));
            semanticTerminologies = new SemanticTerminologies();
            layout.AddToMenuSection(sectionId_SemanticTerminologies, semanticTerminologies); // add semantic terminologies to the menu section

            // Model Semantic Data
            string sectionId_SemanticData = "SemanticData";
            layout.AddMenuSection(new PerspectiveSectionOptions(sectionId_SemanticData, "Model Semantic Data", "MODEL SEMANTIC DATA", StyleHelper.GetIcon("pm4h.Resources.IconPath.Mineguide.SemanticModelData")));
            modelSemanticEditor = new ModelSemanticEditor();
            layout.AddToMenuSection(sectionId_SemanticData, modelSemanticEditor); // add semantic data to the menu section            

            // Nodes Semantic Data
            string sectionId_NodesSemanticData = "NodesSemanticData";
            layout.AddMenuSection(new PerspectiveSectionOptions(sectionId_NodesSemanticData, "Nodes Semantic Data", "NODES SEMANTIC DATA", StyleHelper.GetIcon("pm4h.Resources.IconPath.Mineguide.SemanticNodeData")));
            nodesSemanticEditor = new ModelSemanticEditor();
            layout.AddToMenuSection(sectionId_NodesSemanticData, nodesSemanticEditor); // add semantic data to the menu section


            // -- load stored DATA --
            InitializeShowData(); // inicializo los datos del modelo y menus
        }

        private void InitializeShowData()
        {
            layout.GetLoading()?.Enable();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    // -- Show model --
                    if (args.TPAContainer.getViews().FirstOrDefault()?.tve.getITPAModel() is iTPAModel selectedModel)
                    {
                        modelOriginal = selectedModel;
                        ShowModel(selectedModel);
                    }

                    // SACO COPIA LOCAL DEL MODELINFO PARA PODER HACER CAMBIOS SIN AFECTAR AL ORIGINAL
                    if (GetOriginalModelInfo() is ModelInfo mi)
                    {
                        ModelInfoLocal = mi.GetCopy();
                    }
                    else
                    {
                        // creo un modelinfo desde cero si no existe
                        if (modelOriginal != null)
                        {
                            ModelInfoLocal = ModelInfo.Factory(new List<iTPAModel>() { modelOriginal });
                        }
                    }

                    // -- load stored transformations --
                    InvokeHelper.InvokeAction(() => transformationsEditor.LoadStoredTransformations(modelOriginal, false)); // load stored transformations from the model and NOT refresh the model
                    RefreshModel(); // refresh the model outside to avoid multiple theads calls and have a good sincronization of data

                    // -- load stored semantic data from the runner block "MineguideModelInfoTPAProcessor" if exist --
                    if (modelOriginal != null)
                    {
                        MineguideModelInfoTPAProcessor.UpdatedModelInfo(modelOriginal, ModelInfoLocal, this); // añado la semantica del modelinfo almacenado al local, modelinfo generado desde el runner
                    }

                    // -- load semantic data stored by the editor (Edition in progress) --
                    if (modelOriginal != null)
                    {
                        if (modelOriginal.LoadStoredModelInfo(MODEL_INFO_EDITOR_METADATA_ID) is ModelInfo smi) // load stored semantic data from the model
                        {
                            ModelInfoLocal.MergeSemanticAnnotations(smi); // añado la semantica del modelinfo almacenado al local, modelinfo generado desde el editor (edicion en curso)
                        }
                    }

                    RefreshSemanticEditors();
                }
                catch (Exception ex)
                {
                    PM4HMessageBox.Show(ex.Message);
                }
                finally
                {
                    layout.GetLoading()?.Disable();
                }
            });
        }

        private IEnumerable<BasicInfo> LocalSubprocessSemanticData = new List<BasicInfo>();

        private void RefreshSemanticEditors()
        {
            modelSemanticEditor?.SetModelInformation(ModelInfoLocal?.Tracedata.IterateDataInfo()); // set model information

            #region Nodes Semantic Data and Subprocess Semantic Data
            var nodesSemantic = ModelInfoLocal?.NodesInfo.Values.Where(i => !i.Name.StartsWith("@")) ?? new List<NodeInfo>();  // nodes information

            #region Subprocess Semantic Data          
            LocalSubprocessSemanticData = new List<BasicInfo>(); // reinicio la lista de subprocessos guardados en memoria
            var modelSubprocessSemanticData = ModelInfoLocal?.ReadSubprocessSemantic() ?? new List<NodeInfo>(); // leo subprocess information y la guardo para cuando haya que salvarla
            var subprocesses = GroupsHelper.GetTemplateGroups(modelActual.getTPATemplate());
            foreach (var sp in subprocesses.Values)
            {
                // USAMOS EL NOMBRE COMO IDENTIFICADOR PQ SI COMPARTEN NOMBRE COMPARTEN SEMANTICA
                if (LocalSubprocessSemanticData.Any(s => s.Name == sp.Name)) continue; // si ya existe este subproceso en la semantica --> no lo añado (pq ya lo tengo en memoria)

                // USAMOS EL NOMBRE COMO IDENTIFICADOR PQ SI COMPARTEN NOMBRE COMPARTEN SEMANTICA
                var info = modelSubprocessSemanticData.FirstOrDefault(s => s.Name == sp.Name);
                if (info == null) // si no existe este subproceso en la semantica --> lo creo
                {
                    LocalSubprocessSemanticData = LocalSubprocessSemanticData.Append(new BasicInfo()
                    {
                        Id = sp.Name, // USAMOS EL NOMBRE COMO IDENTIFICADOR PQ SI COMPARTEN NOMBRE COMPARTEN SEMANTICA
                        Name = sp.Name
                    }); // añado nuevo subproceso sin semantica a la lista de subprocessos guardados en memoria
                }
                else // si existe este subproceso en la semantica --> lo añado
                {
                    LocalSubprocessSemanticData = LocalSubprocessSemanticData.Append(info); // añado el subproceso con semantica a la lista de subprocessos guardados en memoria
                }
            }
            ModelInfoLocal?.AddSubprocessSemantic(LocalSubprocessSemanticData); // añado los elementos al model info por si se han añadido nuevos
            #endregion

            var semanticData = nodesSemantic?.Concat(LocalSubprocessSemanticData); // combine nodes and subprocess information
            nodesSemanticEditor?.SetModelInformation(semanticData); // set nodes and subprocess information
            #endregion

            semanticTerminologies.SetModelInformation(ModelInfoLocal); // set terminologies
        }

        private void TransformationsEditor_OnSaveExperiment(object? sender, EventArgs e)
        {
            SaveExperiment();
        }

        private IPortableRunner TransformationsEditor_OnRequestNewRunner(object? sender)
        {
            return BuildRunner();
        }

        private void TransformationsEditor_OnRequestRefreshModel(object? sender, EventArgs e)
        {
            layout.GetLoading()?.Enable();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    RefreshModel();
                }
                catch (Exception ex)
                {
                    PM4HMessageBox.Show(ex.Message);
                }
                finally
                {
                    layout.GetLoading()?.Disable();
                }
            });
        }

        private void ShowModel(iTPAModel m)
        {
            InvokeHelper.InvokeAction(() =>
            {
                if (m != null)
                {
                    modelActual = m;
                    //mouseContext.SelectedStates.Clear(); // limpio la selección de raton
                    viewer.ShowITPAModel(modelActual);   // muestro el modelo
                    transformationsEditor?.RenderTransformations(viewer.tve); // aplica transformaciones gráficas al modelo tipo bloquear nodos, etc.
                }
            });
        }

        public void RefreshModel()
        {
            var rnr = (UIRunner)BuildRunner(); // obtengo el runner actual del experimento para que ya tenga todos los bloque extra añadidos (ITPAProcessors, etc.)
            var logx = PMAppWinHelper.GetILogProvider(null, this.args.ContextId).getLog(); // obtengo el log del experimento
            var log = new CloneLogFilter().ProcessLog(logx); // clonamos el log para no modificar el original

            // ---- Aplico los filtros de transformaciones al log ----
            // aplico los filtros de transformaciones al log (filtro el log original para optimizar: no aplicar toda la lectura y filtrado del runner nuevo desde cero)
            foreach (var filter in FilterList)
            {
                log = filter.ProcessLog(log);
            }
            // --- Fin aplico los filtros de transformaciones al log ----           

            // creamos el model
            var nmodel = rnr.ProcessLog(log); // creo modelo


            foreach (var f in rnr.TPAs.CreateRunnerElements(rnr.ExperimentId, rnr))
            {
                try
                {
                    nmodel = f.ProcessTPA(nmodel).ToArray();
                }
                catch (Exception ex)
                {

                }
            }

            //var xmodel = ModelCorrelator.CoordinateNodeIds(modelActual, nmodel.First()); // EL MAPEADO SE HACE AHORA EN UN BLOQUE mapeamos los IDs del modelo previo con el nuevo.
            var xmodel = nmodel.First();
            PMAppHelper.SetContextId(xmodel, this.args.ContextId);


            ShowModel(xmodel); // muestro el modelo

            // --- Refresco la semantica del modelInfo ---
            var modelInfo = ModelInfo.Factory(log.FirstOrDefault()); // creacion desde el nuevo log
            ModelInfoLocal = modelInfo.MergeSemanticAnnotations(ModelInfoLocal); // añado la semantica en curso al modelinfo generado
            RefreshSemanticEditors(); // actualizo lo que visualizan los editores de semantica
            // --- Fin refresco la semantica del modelInfo ---

        }

        public IPortableRunner BuildRunner()
        {
            var rnr = (UIRunner)PMAppWinHelper.GetRunner(null, this.args.ContextId).Clone(); // Copio el runner actual del experimento
            ModelTransformationFilter f = new ModelTransformationFilter(); // bloque para almacenar las transformaciones en el runner
            f.SetFilters(FilterList); // añado las transformaciones en el bloque
            rnr.Filters.Add(RunnerElementWrapperHelper.BuildWrapperFromRunnerElement(f)); // añado el bloque con las transformaciones al runner

            // -- Add TPAProcessors -- Block for correlate IDs -- IMPORTANT It must be the first TPAProcessor block to be executed previous to other Mineguide blocks --
            var correlationBlock = new MineguideModelCorrelateIDsTPAProcessor();
            correlationBlock.SetCorrelationModel(modelActual); // añado el modelo para correlacionar los IDs de los nodos
            UpsertTPAProcessorToRunner<MineguideModelCorrelateIDsTPAProcessor>(rnr, correlationBlock); // añado el bloque para correlacionar los IDs de los nodos

            // SEMANTICA
            MineguideModelInfoTPAProcessor nmip = new MineguideModelInfoTPAProcessor(); // bloque para almacenar la semantica en el runner
            SaveModelInfoChanges(); // actualizo los cambios realizados en la interfaz en el objeto de semantica
            nmip.AddModelInfo(ModelInfoLocal); // añado la semantica en el bloque
            // -- añado el bloque con la semantica al runner --
            UpsertTPAProcessorToRunner<MineguideModelInfoTPAProcessor>(rnr, nmip);

            // METADATOS DE TRANSFORMACIONES
            MineguideAddEventMetadataTPAProcessor copyMetadataBlock = new MineguideAddEventMetadataTPAProcessor();
            UpsertTPAProcessorToRunner<MineguideAddEventMetadataTPAProcessor>(rnr, copyMetadataBlock);

            return rnr;
        }

        private void UpsertTPAProcessorToRunner<T>(UIRunner rnr, T tpaProcessor) where T : ITPAProcessor
        {
            if (RunnerElementWrapperHelper.BuildWrapperFromRunnerElement(tpaProcessor) is RunnerElementWrapper newWrapper) // creo el wrapper para el nuevo bloque
            {
                if (rnr.TPAs.IterateWrappers<T>().FirstOrDefault() is RunnerElementWrapper oldWrapper) // si existe el wrapper en el runner
                {
                    int pos = rnr.TPAs.GetWrapperPosition(oldWrapper); // obtengo la posicion del wrapper en el runner
                    rnr.TPAs.InsertWrapper(newWrapper, pos); // añado el nuevo wrapper en la misma posicion
                    rnr.TPAs.RemoveWrapper(oldWrapper); // borro el wrapper anterior
                }
                else // si no existe el wrapper en el runner se añade
                {
                    rnr.TPAs.Add(newWrapper); // añado el nuevo wrapper, lo añade en la ultima posicion
                }
            }
        }

        // function to get an ITPAProcessor from the runner
        private T GetTPAProcessorFromRunner<T>(UIRunner rnr) where T : ITPAProcessor
        {
            if (rnr.TPAs.IterateWrappers<T>().FirstOrDefault() is RunnerElementWrapper wrapper) // si existe el wrapper en el runner
            {
                T res = (T)wrapper.CreateInstance(Guid.Empty, rnr);
                return res;
            }
            return default(T); // si no existe devuelvo null
        }


        private void UpsertTPARenderMap<T>(UIRunner rnr, T render) where T : ITPARenderMap
        {
            if (RunnerElementWrapperHelper.BuildWrapperFromRunnerElement(render) is RunnerElementWrapper newWrapper) // creo el wrapper para el nuevo bloque
            {
                if (rnr.TPAsRender.IterateWrappers<T>().FirstOrDefault() is RunnerElementWrapper oldWrapper) // si existe el wrapper en el runner
                {
                    int pos = rnr.TPAsRender.GetWrapperPosition(oldWrapper); // obtengo la posicion del wrapper en el runner
                    rnr.TPAsRender.InsertWrapper(newWrapper, pos); // añado el nuevo wrapper en la misma posicion
                    rnr.TPAsRender.RemoveWrapper(oldWrapper); // borro el wrapper anterior
                }
                else // si no existe el wrapper en el runner se añade
                {
                    rnr.TPAsRender.Add(newWrapper); // añado el nuevo wrapper, lo añade en la ultima posicion
                }
            }
        }

        // function to get an ITPARenderMap from the runner
        private T GetTPARenderMapFromRunner<T>(UIRunner rnr) where T : ITPARenderMap
        {
            if (rnr.TPAsRender.IterateWrappers<T>().FirstOrDefault() is RunnerElementWrapper wrapper) // si existe el wrapper en el runner
            {
                T res = (T)wrapper.CreateInstance(Guid.Empty, rnr);
                return res;
            }
            return default(T); // si no existe devuelvo null
        }

        private ModelInfo SaveModelInfoChanges()
        {
            modelSemanticEditor?.UpdateModelInformation();
            nodesSemanticEditor?.UpdateModelInformation();
            // la semantica de subprocessos hay que guardarla a mano
            ModelInfoLocal?.AddSubprocessSemantic(LocalSubprocessSemanticData); // añado los elementos que he editado en el editor de semantica de nodos
            semanticTerminologies?.UpdateModelInformation();
            return ModelInfoLocal;
        }

        public const string MODEL_INFO_EDITOR_METADATA_ID = "MINEGUIDE_EDITOR_MODELINFO";

        private void StoreLocalModelInfo(iTPAModel model)
        {
            if (model != null && ModelInfoLocal != null)
            {
                SaveModelInfoChanges(); // actualizo los cambios realizados en la interfaz en el objeto de semantica
                model.StoreModelInfo(ModelInfoLocal, MODEL_INFO_EDITOR_METADATA_ID);
            }
        }

        private async void SaveExperiment() // ---------- BASADO EN CODIGO DE PMAppClientWinConfig.cs ------------------
        {
            var rnr = (UIRunner)PMAppWinHelper.GetRunner(null, this.args.ContextId).Clone(); // Leo el runner original del experimento y lo duplico para no modificarlo

            if (rnr != null)
            {
                layout.GetLoading()?.Enable();

                string fileDialog = FileHelper.SaveFileDialogo("Packed Runner(*.rnr.zip)|*.rnr.zip");
                if (fileDialog != null)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        var zip = new ZIPPackRunner(fileDialog); // creo el objeto para guardar el experimento                        

                        // PASO 1 ---- Almacenamos el runner ----
                        zip.Pack(rnr);

                        // PASO 2 ---- Modificamos el modelo para guardar las transformaciones y la semantica ----
                        transformationsEditor.StoreTranformations(modelOriginal); // añado las transformaciones al modelo para poder recuperarlas luego                        
                        StoreLocalModelInfo(modelOriginal); // añado la semantica al modelo para poder recuperarla luego                        

                        // PASO 3 ---- Guardamos el modelo modificado ----
                        zip.AddResults(new List<iTPAModel>() { modelOriginal }); // añado el modelo a salvar

                        // PASO 4 ---- BORRAMOS LAS TRANSFORMACIONES Y LA SEMANTICA DEL MODELO  PARA DEJARLO COMO ESTABA ----
                        // es el modelo original del experimento, si salvasen el expermiento principal o quiero descartar mis cambios dejaría residuos en el modelo
                        transformationsEditor.ClearStoredTransformations(modelOriginal); // borro las transformaciones del modelo
                        modelOriginal.ClearStoredModelInfo(MODEL_INFO_EDITOR_METADATA_ID); // borro la semantica del modelo
                    });
                    PM4HMessageBox.Show($"File {fileDialog} saved.", "Information", icon: PM4HMessageBoxIcons.Information);
                }

                layout.GetLoading()?.Disable();
            }
        }

        private void MineguideEditor_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.RightCtrl:
                case Key.LeftCtrl:
                    mouseContext.State = TPASelectionMouseContext.ContextState.Default;
                    break;
            }
        }

        private void MineguideEditor_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.RightCtrl:
                case Key.LeftCtrl:
                    mouseContext.State = TPASelectionMouseContext.ContextState.ClickSelection;
                    break;
            }
        }

        private void ContextoRaton_OnMovementFinished(object sender, MouseButtonEventArgs args)
        {
            viewer.tve.InformLayersTPAChanged();
        }



        #region Mouse Events
        private void Tve_OnRightClick(object sender, MouseButtonEventArgs args)
        {
            //throw new NotImplementedException();
        }

        #region State Mouse Events
        private void Tve_OnStateClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Estado sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {
            //semanticDataService.ShowInformation();
            //if (GetModelInfo().NodesInfo.TryGetValue(sender.NodeText, out NodeInfo info))
            //{
            //    semanticDataService.SetEventInfo(info);                
            //}
            //else
            //{
            //    semanticDataService.ClearEventInfo();
            //}
        }

        public TransformationRegion GetCurrentTransformationRegion()
        {
            //var tpa = modelActual.getTPATemplate();
            var tr = new TransformationRegion(modelActual, mouseContext.GetSelectedStates().ToArray());
            return tr;
        }

        private Action CreateMenuContextualAction(Action action)
        {
            return () =>
            {
                layout.GetLoading()?.Enable();
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        PM4HMessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        layout.GetLoading()?.Disable();
                    }
                });
            };
        }

        private Action CreateMenuContextualAction(IUITransformation transformation)
        {
            return CreateMenuContextualAction(() =>
            {
                var result = InvokeHelper.InvokeFunction(() => transformation.ShowEditor());
                if (result) // request filter data and adds to it
                {
                    InvokeHelper.InvokeAction(() => transformationsEditor.AddTransformation(transformation)); // Add to left menu
                                                                                                              //ExecuteFilter(transformation.Filter); // Execute filter
                    RefreshModel();
                }
            });

        }

        private ContextMenuItem CreateMenuItem(string text, IUITransformation uiTransformation)
        {
            return new ContextMenuItem(text, CreateMenuContextualAction(uiTransformation));
        }

        public List<ContextMenuItem> CreateContextMenu(Estado sender)
        {
            List<ContextMenuItem> menuOptions = new List<ContextMenuItem>();
            var tr = GetCurrentTransformationRegion();
            {
                if (!sender.IsParallelismBlocked() && new CompositionTransformationFilter() is CompositionTransformationFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("Composition", new UIComposition(f0, tr)));
                }
            }
            {
                if (new SubprocessTransformationFilter() is SubprocessTransformationFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("Subprocess", new UISubprocess(f0, tr, modelActual.getTPATemplate())));
                }
            }
            {
                if (!sender.IsParallelismBlocked() && new ParallelTransformationsFilter() is ParallelTransformationsFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("Parallelism", new UIParallelism(f0, tr)));
                }
            }
            {
                if (!sender.IsParallelismBlocked() && new ForzeFusionTransformationFilter() is ForzeFusionTransformationFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("Forze Fusion", new UIForzeFusion(f0, tr)));
                }
            }
            {
                if (!sender.IsParallelismBlocked() && new CycleIntensionFilter() is CycleIntensionFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("Cycle Intension", new UICycleIntension(f0, tr)));
                }
            }
            {
                if (!sender.IsParallelismBlocked() && new CycleExtensionFilter() is CycleExtensionFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("Cycle Extension", new UICycleExtensional(f0, tr)));
                }
            }
            {
                if (!sender.IsParallelismBlocked() && new SingleDecisionFilter() is SingleDecisionFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("Decision", new UIDecisionSame(f0, tr)));
                }
            }
            {
                if (!sender.IsParallelismBlocked() && new ExtendedDecisionFilter() is ExtendedDecisionFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("New Decision", new UIDecisionNew(f0, tr)));
                }
            }
            return menuOptions.OrderBy(opt => opt.Header).ToList();
        }

        private void Tve_OnStateRightClick(Estado sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {
            args.Handled = true;

            //var point = args.GetPosition(viewer.cvbase); // posicion del raton en el tve

            List<ContextMenuItem> menuOptions = new List<ContextMenuItem>();

            // CREAMOS MENU CONTEXTUAL
            // -- Normal selected nodes --
            if (sender.IsSelected() && !sender.IsBlocked())
            //if (sender.IsSelected())
            {
                menuOptions = CreateContextMenu(sender);
            }
            // -- @Start node --
            else if (sender is EstadoInicial startState)
            {
                mouseContext.ClearSelection(); // borro la selección de nodos existente

                // Add NewDecision Transformation
                var tr = new TransformationRegion(modelActual, new Estado[1] { startState });
                if (new ExtendedDecisionFilter() is ExtendedDecisionFilter f0 && f0.IsApplicable(tr))
                {
                    menuOptions.Add(CreateMenuItem("New Decision", new UIDecisionNew(f0, tr)));
                }
            }

            // mostrar menu contextual
            if (menuOptions.Any())
            {
                ContextualMenuFactory cmf = new ContextualMenuFactory(menuOptions.ToArray());
                cmf.Show(new Point(0, 0));
            }
            return;
        }

        private void Tve_OnStateDoubleClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Estado sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {
            //throw new NotImplementedException();
        }
        #endregion State Mouse Events

        #region Transition Mouse Events
        private void Tve_OnTransitionClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Transicion sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {
            //throw new NotImplementedException();
        }

        private void Tve_OnTransitionRightClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Transicion sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {
            //throw new NotImplementedException();
        }

        private void Tve_OnTransitionDoubleClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Transicion sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {
            //throw new NotImplementedException();
        }
        #endregion Transition Mouse Events

        #endregion Mouse Events


        public ModelInfo GetOriginalModelInfo()
        {
            if (PMAppWinHelper.GetModelInfoProvider(this, args.ContextId) is IModelInfoProvider mp)
            {
                return mp.GetModelInfo();
            }

            return null;
        }

    }


}
