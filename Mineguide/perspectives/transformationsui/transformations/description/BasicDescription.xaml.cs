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

namespace Mineguide.perspectives.transformationsui.transformations.description
{
    /// <summary>
    /// Lógica de interacción para BasicDescription.xaml
    /// </summary>
    public partial class BasicDescription : UserControl
    {
        private ObservableCollection<DescItem> Items { get; set; } = new ObservableCollection<DescItem>();
        public BasicDescription()
        {
            InitializeComponent();
            this.container.ItemsSource = Items;
        }

        public void AddItem(string id, string? value)
        {
            if (value == null) return;
            Items.Add(new DescItem() { Id = id, Value = value });
        }

        public void Clear() { Items.Clear(); }

        public void DeleteItem(string id)
        {
            var item = Items.FirstOrDefault(x => x.Id == id);
            if (item != null) Items.Remove(item);
        }

        private class DescItem
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

    }
}
