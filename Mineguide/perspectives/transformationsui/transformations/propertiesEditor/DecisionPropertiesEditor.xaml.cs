using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using Mineguide.perspectives.interactiveannotation.modeltransformations;
using pm4h.semantics;
using pm4h.tpa;
using pm4h.windows.ui.windows;

namespace Mineguide.perspectives.transformationsui.transformations.propertiesEditor
{
    /// <summary>
    /// Lógica de interacción para DecisionPropertiesEditor.xaml
    /// </summary>
    public partial class DecisionPropertiesEditor : UserControl, IPropertiesEditor
    {
        public enum DecisionTypes
        {
            Decision,
            NewDecision
        }

        TPATemplate.Node SelectedNode;        

        public DecisionPropertiesEditor(TPATemplate.Node selectedNode, TPATemplate tpa, DecisionTypes decisionType) //string originalName, string decisionName, TPATemplate tpa, DecisionTypes decisionType)
        {
            InitializeComponent();
            this.container.ItemsSource = Items;            
            this.TPA = tpa;
            this.DecisionType = decisionType;
            SelectedNode = selectedNode;

            if (SelectedNode.Name.StartsWith("@"))// "@Start"
            {
                // quitamos la @ inicial del nombre
                this.DecisionName = selectedNode.Name[1..]; //"Start";
            }
            else
            {
                this.DecisionName = SelectedNode.Name;
            }
        }

        public string Title { get; set; } = "Decision properties";
        public string Text { get => tbText.Text; set => tbText.Text = value; }
        public double DialogWidth { get; set; } = 800;
        public double DialogHeight { get; set; } = 600;

        public bool ShowDialog()
        {
            PM4HWindowDialog dlg = new PM4HWindowDialog(this, Title, width: DialogWidth, height: DialogHeight, buttons: PM4HWindowDialogButtons.Empty);
            dlg.AddButton("Accept",
                () =>
                {
                    this.tbText.Focus(); // Para que se actualice el binding de las cajas de texto

                    // Validamos el nuevo nombre
                    var nameValidation = BasicPropertiesEditor.NewNameValidationFunction(DecisionName);
                    if (!nameValidation.Item1)
                    {
                        PM4HMessageBox.Show($"The field '{labelName.Text}' is not valid. {nameValidation.Item2}", "Field not valid", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Information);
                        return;
                    }

                    foreach (var t in Items) // validamos el nuevo nombre contra todos los nodos posteriores al nodo origen para evitar fusiones de nodos
                    {
                        if (t.EndNode.Name == DecisionName) // nuevo nombre no puede ser el de un nodo posterior
                        {
                            PM4HMessageBox.Show($"The new decision name '{DecisionName}' cannot match the name of a node that is reached from the source node.",
                                                               "Invalid new name", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Information);
                            return;
                        }
                    }

                    if (DecisionType == DecisionTypes.NewDecision) // restricciones de nombre para cuando se crea un nuevo nodo decision
                    {                       
                        // Validamos que el nuevo nombre no sea el del nodo seleccionado
                        if (SelectedNode.Name == DecisionName)
                        {
                            PM4HMessageBox.Show($"The new decision name '{DecisionName}' cannot match the source node.",
                                                               "Invalid new name", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Information);
                            return;
                        }

                    }

                    if (GetResults().Count < 2) // al menos dos salidas deben estar seleccionadas
                    {
                        PM4HMessageBox.Show($"At least two outputs must be selected for the decision", "Outputs problem", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Information);
                        return;
                    }

                    if(DecisionType == DecisionTypes.NewDecision)
                    {
                        // si el nodo origen es de tipo decisión, se comprueba que no todas las salidas se han seleccionado --> en ese caso el nodo decision origen no tendría sentido pq no decidiría nada
                        if(SelectedNode.IsDecision())
                        {
                            if(GetResults().Count == Items.Count)
                            {
                                PM4HMessageBox.Show($"All outputs cannot be selected if the original node is a decision node", "Outputs problem", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Information);
                                return;
                            }
                        }
                    }                   

                    dlg.DialogResult = true;
                },
                true);
            dlg.AddButtonCancel();
            return dlg.ShowDialog() ?? false;
        }

        DecisionTypes _decisionType = DecisionTypes.Decision;
        public DecisionTypes DecisionType { get => _decisionType; set => _decisionType = value; }

        public string DecisionName { get => tbName.Text; set => tbName.Text = value; }

        public TPATemplate TPA { get; set; }

        private ObservableCollection<DecisionResultItem> Items { get; set; } = new ObservableCollection<DecisionResultItem>();

        public void AddResult(TPATemplate.NodeTransition transition)
        {
            DecisionResultItem item = new DecisionResultItem(transition, transition.getEndNodes(TPA).First());
            switch (DecisionType)
            {
                case DecisionTypes.Decision: // No se pueden seleccionar salidas
                    //item.ShowSelection = Visibility.Collapsed;
                    item.ShowSelectionControls = false;
                    item.IsSelected = true;
                    break;
                case DecisionTypes.NewDecision: // Se pueden seleccionar salidas
                    //item.ShowSelection = Visibility.Visible;
                    item.ShowSelectionControls = true;
                    item.IsSelected = false;
                    break;
            }
            Items.Add(item);
            
            // reordenamos los items por nombre
            var items = Items.OrderBy(i => i.Name).ToArray(); 
            Items.Clear();
            foreach (var i in items)
            {
                Items.Add(i);
            }            
        }

        public List<DecisionResultItem> GetResults() => Items.Where(i => i.IsSelected).ToList(); // obtiene los seleccionados

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Uncheck all other default items
            Guid id = (Guid)((CheckBox)sender).Tag;
            foreach (var item in Items)
            {
                if (item.Id != id)
                {
                    item.IsDefault = false;
                }
            }

            // obtener la lista de Items seleccionados
            var selectedItems = Items.Where(i => i.IsSelected).ToList(); // obtiene los seleccionados




        }

    }


    public class DecisionResultItem : INotifyPropertyChanged
    {
        public DecisionResultItem(TPATemplate.NodeTransition transition, TPATemplate.Node endNode)
        {
            this.Transition = transition;
            this.EndNode = endNode;
        }

        TPATemplate.NodeTransition _transition = null;
        public TPATemplate.NodeTransition Transition
        {
            get { return _transition; }
            set
            {
                _transition = value;
                OnPropertyChanged();
                OnPropertyChanged("Id");
            }
        }

        TPATemplate.Node _endNode = null;
        public TPATemplate.Node EndNode
        {
            get { return _endNode; }
            set
            {
                _endNode = value;
                OnPropertyChanged();
                OnPropertyChanged("Result");
            }
        }

        bool _isSelected = false;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        bool _showSelectionControls = true;
        public bool ShowSelectionControls
        {
            get { return _showSelectionControls; }
            set
            {
                _showSelectionControls = value;
                OnPropertyChanged();
            }
        }

        public bool NotShowSelectionControls => !ShowSelectionControls;

        //Visibility _showSelection = Visibility.Visible;
        //public Visibility ShowSelection
        //{
        //    get { return _showSelection; }
        //    set
        //    {
        //        _showSelection = value;
        //        OnPropertyChanged();
        //    }
        //}



        bool _isDefault = false;
        public bool IsDefault
        {
            get { return _isDefault; }
            set
            {
                _isDefault = value;
                OnPropertyChanged();
            }
        }

        public Guid Id => Transition.Id;        

        public string Name => EndNode.Name;

        string _value = "";
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
