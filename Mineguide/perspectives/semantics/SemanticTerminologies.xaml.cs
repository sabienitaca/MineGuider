using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using Castle.Core.Internal;
using Newtonsoft.Json;
using pm4h.semantics.datamodel;
using Sabien.Windows.Visual.Utils.WPF;

namespace Mineguide.perspectives.semantics
{
    /// <summary>
    /// Lógica de interacción para SemanticTerminologies.xaml
    /// </summary>
    public partial class SemanticTerminologies : UserControl
    {
        public SemanticTerminologies()
        {
            InitializeComponent();
            this.TerminologiesContainer.ItemsSource = terminologies;
        }

        ObservableCollection<TerminologyItem> terminologies = new ObservableCollection<TerminologyItem>();

        public void AddTerminology(string uri)
        {
            if (!uri.IsNullOrEmpty())
            {
                terminologies.Add(new TerminologyItem() { Uri = uri });
            }
        }

        public void AddTerminologies(IEnumerable<string> uris)
        {
            if (uris != null)
            {
                foreach (var uri in uris)
                {
                    terminologies.Add(new TerminologyItem() { Uri = uri });
                }
            }
        }

        public void ClearTerminologies()
        {
            terminologies.Clear();
        }

        public IEnumerable<string> GetTerminologies()
        {
            return terminologies.Where(x => !x.Uri.IsNullOrEmpty()).Select(x => x.Uri).ToList();
        }

        private void btnAddTerminology_Click(object sender, RoutedEventArgs e)
        {
            terminologies.Add(new TerminologyItem());
        }

        private void btnDeleteTerminology_Click(object sender, RoutedEventArgs e)
        {
            string key = (sender as Button).Tag.ToString();
            var item = terminologies.FirstOrDefault(x => x.Id == key);
            if (item != null)
            {
                terminologies.Remove(item);
            }
        }

        // implementar clase TerminologyItem que implementa INotifyPropertyChanged y tiene la propiedad Id que es un guid autogenerado y la propiedad Uri que es un string
        public class TerminologyItem
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Uri { get; set; } = "";
        }


        #region ModelInfo integration

        ModelInfo? modelInfoLocal = null;

        public void SetModelInformation(ModelInfo? modelInfo)
        {
            modelInfoLocal = modelInfo; // to save later

            InvokeHelper.InvokeAction(() =>
            {
                ClearTerminologies();
                if (modelInfo != null)
                {
                    AddTerminologies(modelInfo.ReadTerminologies());
                }
            });
        }

        //public void UpdateModelInformation() => InvokeHelper.InvokeAction(() => modelInfoLocal?.AddTerminologies(GetTerminologies()));

        public void UpdateModelInformation()
        {
            InvokeHelper.InvokeAction(() =>
            {
                if (modelInfoLocal != null)
                {
                    var term = GetTerminologies();
                    modelInfoLocal.AddTerminologies(term);
                }
            });
        }

        #endregion

    }
}
