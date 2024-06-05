using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using com.espertech.esper.compat.collections;
using Mineguide.perspectives.interactiveannotation.annotationFilters;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using Mineguide.perspectives.tpacontrol.mouse.contexts;
using pm4h.runner;
using pm4h.tpa;
using pm4h.tpa.ipi;
using pm4h.utils;
using pm4h.windows.ui;
using pm4h.windows.ui.blocks;
using pm4h.windows.ui.fragments.tpaviewer;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.helpers;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements;
using pm4h.windows.ui.windows;
using pm4h.windows.utils;
using Sabien.Windows.Visual.Utils.WPF;

namespace Mineguide.perspectives.transformationsui.transformations.propertiesEditor
{
    /// <summary>
    /// Lógica de interacción para ParallelismPropertiesEditor.xaml
    /// </summary>
    public partial class ParallelismPropertiesEditor : UserControl, IPropertiesEditor
    {

        TransformationRegion TInfoOriginal;
        iTPAModel Model => TInfoOriginal.Model;
        TPAViewer viewer;
        TPASelectionMouseContext mouseContext;

        public string Title { get; set; } = "Properties";
        public double DialogWidth { get; set; } = 1024;
        public double DialogHeight { get; set; } = 768;

        public bool ShowDialog()
        {
            PM4HWindowDialog dlg = new PM4HWindowDialog(this, Title, width: DialogWidth, height: DialogHeight, buttons: PM4HWindowDialogButtons.Empty);
            dlg.AddButton("Accept",
                () =>
                {
                    //validaciones
                    if (BranchItems.Count >= 2) // mínimo 2 ramas
                    {
                        if (!IsValidParallelism())
                        {
                            PM4HMessageBox.Show("Selected branches do not generata a valid parallelism.", "Validation error", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Error);
                        }
                        else
                        {
                            dlg.DialogResult = true;
                        }
                    }
                    else
                    {
                        PM4HMessageBox.Show("You must define at least 2 branches.", "Validation error", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Error);
                    }


                },
                true);
            dlg.AddButtonCancel();
            return dlg.ShowDialog() ?? false;
        }

        public bool IsValidParallelism()
        {
            TransformationRegion info = GetAllBranchesRegion();

            // check if all nodes are in branches
            if (info.Nodes.Count() != TInfoOriginal.Nodes.Count()) return false; // same number of nodes
            foreach (var node in TInfoOriginal.Nodes) // all originally selected nodes are in branches
            {
                if (!info.Nodes.Contains(node))
                {
                    return false;
                }
            }

            return true;

            //TransformationRegion info = GetAllBranchesRegion();
            //var sn = info.GetSplitNodes();
            //var sc = info.GetSyncronizationNodes();
            //if (sn.Length != 1 || sc.Length != 1)
            //{
            //    return false;
            //}
            //var result = info.Nodes.All(x => info.IsIsolated(x, sc.Union(sn)));
            //return result;
        }

        public bool IsSelectionASequence()
        {
            TransformationRegion info = GetSelectedStatesRegion();

            if (info.Nodes.Length < 1) return false;
            if (info.Nodes.Length == 1) return true;
            if (info.GetSequence() is TPATemplate.Node[] seq)
            {
                return true;
            }
            return false;
        }

        public TransformationRegion GetSelectedStatesRegion()
        {
            var tr = new TransformationRegion(Model, mouseContext.GetSelectedStates().ToArray());
            return tr;
        }

        public TransformationRegion GetAllBranchesRegion()
        {
            var allStates = BranchItems.SelectMany(b => b.States).ToArray();

            var tr = new TransformationRegion(Model, allStates);
            return tr;
        }

        public ParallelismPropertiesEditor(TransformationRegion tInfo)
        {
            InitializeComponent();
            this.Loaded += ParallelismPropertiesEditor_Loaded;

            TInfoOriginal = tInfo;

            // Create the viewer
            viewer = new TPAViewer(ITPAModelHelper.TVEEngineType.ProForma);
            viewer.Margin = new Thickness(0);
            mouseContext = new TPASelectionMouseContext(viewer.tve);

            // Enlazo los eventos de raton del tve a mi objeto
            viewer.tve.ContextoRaton.OnStateClick += Tve_OnStateClick;
            viewer.tve.ContextoRaton.OnStateRightClick += Tve_OnStateRightClick;
            viewer.tve.ContextoRaton.OnMovementFinished += ContextoRaton_OnMovementFinished;

            // Enlazo los eventos de teclado del editor a mi objeto
            this.KeyDown += Editor_KeyDown;
            this.KeyUp += Editor_KeyUp;


            // Add viewer to the grid
            Grid.SetColumn(viewer, 2);
            root.Children.Add(viewer);            

            // show model
            viewer.ShowITPAModel(Model);   // muestro el modelo

            // Block not selected nodes
            foreach (var node in Model.IterateNodes())
            {
                if (!TInfoOriginal.Nodes.Contains(node))
                {
                    // Block node
                    if (viewer.tve.nodos.TryGetValue(node.Id, out var estado))
                    {
                        estado.Block();
                    }
                }

            }


            // init branches
            InitBranches();

        }

        private void ParallelismPropertiesEditor_Loaded(object sender, RoutedEventArgs e)
        {
            viewer.FitZoom();
        }

        private void ContextoRaton_OnMovementFinished(object sender, MouseButtonEventArgs args)
        {
            viewer.tve.InformLayersTPAChanged();
        }

        #region Mouse Events
        //private void Tve_OnRightClick(object sender, MouseButtonEventArgs args)
        //{
        //    //throw new NotImplementedException();
        //}

        #region State Mouse Events
        private void Tve_OnStateClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Estado sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {
            if (!sender.IsParallelismBlocked()) // cuando hago click en un nodo seleccionable que no esta ya en una rama, deselecciono el resto
            {
                DeselectBranches();
            }
        }

        private bool IsAnySelectedNodeBlocked()
        {
            return mouseContext.GetSelectedStates().Any(s => s.IsParallelismBlocked());
        }

        public List<ContextMenuItem> CreateContextMenu(Estado sender)
        {
            List<ContextMenuItem> menuOptions = new List<ContextMenuItem>();

            //if (!sender.IsBlocked() && sender.IsSelected()) // -- opciones de nodo normal seleccionado --
            if (!sender.IsBlocked() && !IsAnySelectedNodeBlocked() && sender.IsSelected()) // -- opciones de nodo normal seleccionado --
            {
                if (IsSelectionASequence())
                {
                    var option = new ContextMenuItem("New branch",
                    () =>
                    {
                        // add branch to left menu 
                        AddBranch(mouseContext.GetSelectedStates().ToArray());
                    });
                    menuOptions.Add(option);
                }
            }
            else if (sender.IsParallelismBlocked() && sender.IsHighlighted()) // -- opciones de nodo bloqueado de una rama seleccionada --
            {
                var option = new ContextMenuItem("Delete branch",
                () =>
                {
                    if (BranchItems.FirstOrDefault(b => b.States.Any(s => s == sender)) is BranchItem branch)
                    {
                        DeleteBranch(branch);
                    }
                });
                menuOptions.Add(option);
                var option2 = new ContextMenuItem("Deselect branch",
                () =>
                {
                    if (BranchItems.FirstOrDefault(b => b.States.Any(s => s == sender)) is BranchItem branch)
                    {
                        DeselectBranch(branch);
                    }
                });
                menuOptions.Add(option2);
            }
            else if (sender.IsParallelismBlocked() && !sender.IsHighlighted()) // -- opciones de nodo bloqueado y que no pertenece a una rama seleccionada --
            {
                var option = new ContextMenuItem("Select branch",
                () =>
                {
                    if (BranchItems.FirstOrDefault(b => b.States.Any(s => s == sender)) is BranchItem branch)
                    {
                        SelectBranch(branch);
                    }
                });
                menuOptions.Add(option);
            }



            return menuOptions;
        }

        private void Tve_OnStateRightClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Estado sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        {

            args.Handled = true;

            List<ContextMenuItem> menuOptions = CreateContextMenu(sender);
            if (menuOptions.Any())
            {
                // mostrar menu contextual
                ContextualMenuFactory cmf = new ContextualMenuFactory(menuOptions.ToArray());
                //var point = args.GetPosition(viewer.cvbase); // posicion del raton en el tve
                cmf.Show(new Point(0, 0));
            }

            return;
        }

        //private void Tve_OnStateDoubleClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Estado sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        //{
        //    //throw new NotImplementedException();
        //}
        #endregion State Mouse Events

        #region Transition Mouse Events
        //private void Tve_OnTransitionClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Transicion sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        //{
        //    //throw new NotImplementedException();
        //}

        //private void Tve_OnTransitionRightClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Transicion sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        //{
        //    //throw new NotImplementedException();
        //}

        //private void Tve_OnTransitionDoubleClick(pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements.Transicion sender, TPAViewerEngine viewer, MouseButtonEventArgs args)
        //{
        //    //throw new NotImplementedException();
        //}
        #endregion Transition Mouse Events

        #endregion Mouse Events



        private void Editor_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.RightCtrl:
                case Key.LeftCtrl:
                    mouseContext.State = TPASelectionMouseContext.ContextState.Default;
                    break;
            }
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.RightCtrl:
                case Key.LeftCtrl:
                    mouseContext.State = TPASelectionMouseContext.ContextState.ClickSelection;
                    break;
            }
        }

        #region Branch management

        Brush[] BranchBrushes = new Brush[]
        {
            Brushes.Red, Brushes.Blue, Brushes.Yellow, Brushes.Purple, Brushes.Orange, Brushes.Brown,
            Brushes.Cyan, Brushes.Magenta, Brushes.SlateBlue, Brushes.DarkKhaki, Brushes.LightSalmon, Brushes.Tomato,
            Brushes.Plum, Brushes.DarkSeaGreen, Brushes.DarkTurquoise, Brushes.DarkViolet, Brushes.DeepPink, Brushes.Gold,
            Brushes.LightBlue, Brushes.LightCoral, Brushes.LightGreen, Brushes.LightPink, Brushes.LightSkyBlue, Brushes.LightSteelBlue
        };

        ObservableCollection<BranchItem> BranchItems = new ObservableCollection<BranchItem>();

        public TPATemplate.Node[][] GetBranchesNodes()
        {
            TPATemplate.Node[][] branches = new TPATemplate.Node[BranchItems.Count][];
            int index = 0;
            foreach (var bItem in BranchItems)
            {
                branches[index++] = GetBranchNodes(bItem);
            }
            return branches;
        }

        public Estado[][] GetBranchesStates()
        {
            Estado[][] branches = new Estado[BranchItems.Count][];
            int index = 0;
            foreach (var bItem in BranchItems)
            {
                branches[index++] = bItem.States;
            }
            return branches;
        }

        private void InitBranches()
        {
            BranchItems.Clear();
            this.branchContainer.ItemsSource = BranchItems;
        }

        private void AddBranch(Estado[] states)
        {
            // block selected states to not use un another branck
            mouseContext.GetSelectedStates().ForEach(s => s.ParallelismBlock());
            // add branch to left menu
            var branch = new BranchItem()
            {
                States = states,
                Color = GetFirstFreeBrush()
            };
            BranchItems.Add(branch);
            // clear selection (this not change block color it is managed by selector, but clear mouse context seleted nodes)
            mouseContext.ClearSelection();
        }

        private Brush GetFirstFreeBrush()
        {
            foreach (var brush in BranchBrushes)
            {
                if (!BranchItems.Any(b => b.Color == brush))
                {
                    return brush;
                }
            }
            return BranchBrushes[0]; // return first if all are used
        }

        private void RemoveBranch(BranchItem? branch)
        {
            if (branch == null) return;
            //// Deselect branch nodes if selected from left menu
            //foreach (var state in mouseContext.SelectedStates)
            //{
            //    if (state.NodeColor == branch.Color) // node can be selected but not from branch
            //    {
            //        state.Deselect();
            //    }
            //}
            //// Unblock branch nodes
            foreach (var state in branch.States)
            {
                state.Deselect(); // Deselect branch nodes if selected from left menu
                state.ParallelismUnblock(); // Unblock branch nodes
            }
            BranchItems.Remove(branch);
        }


        //private void RemoveBranchById(Guid id)
        //{
        //    if(BranchItems.FirstOrDefault(b => b.Guid == id) is BranchItem branch)
        //    {
        //        RemoveBranch(branch);
        //    }
        //}

        /// <summary>
        /// Delete a branch after click delete button
        /// </summary>
        private void btnDeleteBranch_Click(object sender, RoutedEventArgs e)
        {
            // Get branch
            Guid id = (Guid)((Button)sender).Tag;
            if (BranchItems.FirstOrDefault(it => it.Id == id) is BranchItem branch)
            {
                DeleteBranch(branch);
            }
        }

        /// <summary>
        /// Delete a branch requesting user confirmation
        /// </summary>
        private void DeleteBranch(BranchItem branch)
        {
            if (PM4HMessageBox.Show("This will delete a branch. Are you sure?", "Delete confirmation",
                               PM4HMessageBoxButtons.YesNo, PM4HMessageBoxIcons.Question) is bool delete && delete)
            {
                // Remove branch
                RemoveBranch(branch);
            }
        }

        #region Branch selection click

        object? branchMouseDownSender = null;
        private void branch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            branchMouseDownSender = sender;
        }

        private void branch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (branchMouseDownSender != null && branchMouseDownSender == sender)
            {
                var idClick = (Guid)((FrameworkElement)sender).Tag;
                var branch = BranchItems.FirstOrDefault(it => it.Id == idClick);
                SelectBranch(branch);
            }
        }

        private void SelectBranch(BranchItem? branch)
        {
            if (branch != null)
            {
                // clear mouse context selection
                mouseContext.ClearSelection();
                //deselect all branches
                DeselectBranches();
                //select this branch
                branch.Selected = true;
                //viewer colorize branch nodes
                branch.States.ForEach(s => s.Highlight(branch.Color));
            }
        }

        private void DeselectBranch(BranchItem? branch)
        {
            if (branch != null && branch.Selected)
            {
                branch.Selected = false; // deselect
                branch.States.ForEach(s => s.UndoHighlight()); // restore block color
            }
        }

        private void DeselectBranches()
        {
            // deselect all branches and restore block color if selected
            BranchItems.ForEach(b => DeselectBranch(b));
        }

        private NodeReference[] GetBranchNodeReferences(BranchItem branch, TPATemplate? tpa = null)
        {
            tpa ??= Model.getTPATemplate(); // Get tpa if not provided
            NodeReference[] res = new NodeReference[branch.States.Length];
            int i = 0;
            foreach (var node in GetBranchNodes(branch, tpa)) // Get nodes from states
            {
                res[i++] = NodeReference.FromNode(node, tpa); // Create NodeReference
            }
            return res;
        }

        private TPATemplate.Node[] GetBranchNodes(BranchItem branch, TPATemplate? tpa = null)
        {
            tpa ??= Model.getTPATemplate(); // Get tpa if not provided
            return branch.States.Select(s => tpa.FindNodebyId(s.ID)).ToArray();
        }

        //private TPATemplate.Node[] GetNodesFromStates(IEnumerable<Estado> estados)
        //{
        //    var tpa = Model.getTPATemplate();
        //    return estados.Select(s => tpa.FindNodebyId(s.ID)).ToArray();            
        //}

        //private TPATemplate.Node[] GetNodesSelected()
        //{
        //    return GetNodesFromStates(mouseContext.SelectedStates);
        //}


        #endregion Branch selection click

        #endregion Branch management
    }

    public class BranchItem : INotifyPropertyChanged
    {
        private Guid _guid = Guid.NewGuid();
        public Guid Id
        {
            get { return _guid; }
            set
            {
                _guid = value;
                OnPropertyChanged();
            }
        }

        private Brush _color = Brushes.Red;
        public Brush Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }


        private Estado[] _states = new Estado[] { };
        public Estado[] States
        {
            get { return _states; }
            set
            {
                _states = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Description));
            }
        }

        public string Description => $"{string.Join(", ", States.Select(n => n.NodeText))}";

        private bool _selected = false;
        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedBrush));
            }
        }

        public Brush SelectedBrush => Selected ? StyleHelper.GetResource<Brush>("pm4h.Resources.Brush.Foreground.Complementary") : StyleHelper.GetResource<Brush>("pm4h.Resources.Brush.Principal.20");



        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
