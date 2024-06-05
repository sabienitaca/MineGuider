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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Antlr4.Runtime.Tree;
using Castle.Core.Internal;
using com.espertech.esper.core.context.factory;
using pm4h.semantics.datamodel;
using Sabien.Windows.Visual.Utils.WPF;

namespace Mineguide.perspectives.semantics
{
    /// <summary>
    /// Lógica de interacción para ModelSemantic.xaml
    /// </summary>
    public partial class ModelSemanticEditor : UserControl
    {
        public ModelSemanticEditor()
        {
            InitializeComponent();

            this.DataContainer.ItemsSource = modelInformationItems;
        }

        private ObservableCollection<SemanticInformationItem> modelInformationItems = new ObservableCollection<SemanticInformationItem>();       

        public void SetModelInformation(IEnumerable<BasicInfo>? dataCollection)
        {
            InvokeHelper.InvokeAction(() =>
            {
                modelInformationItems.Clear();

                if (dataCollection == null) return;

                foreach (var data in dataCollection.OrderBy(d => d.Name))
                {
                    modelInformationItems.Add(new SemanticInformationItem(data));
                }
            });
        }

        public void UpdateModelInformation()
        {
            foreach (var item in modelInformationItems)
            {
                item.SaveInformation();
            }
        }

        private void btnAddOtherBinding_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.DataContext as SemanticInformationItem;
            if (item != null)
            {
                item.AddEmptyOtherBinding();
            }
        }

        private void btnDeleteOtherBinding_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.DataContext as OtherBindingItem;
            if (item != null)
            {
                var parent = item.ParentPropertyId;
                var parentItem = modelInformationItems.FirstOrDefault(i => i.Id == parent);
                if (parentItem != null)
                {
                    parentItem.OtherBindings.Remove(item);
                }
            }
        }
    }



}
