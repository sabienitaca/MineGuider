using System;
using System.Collections.Generic;
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

namespace Mineguide.perspectives.transformationsui.transformations
{
    /// <summary>
    /// Lógica de interacción para UIItem.xaml
    /// </summary>
    public partial class UIItem : UserControl
    {
        public event EventHandler OnDelete;

        public IUITransformation UITransformation { get; private set; }
        public UIItem(IUITransformation content)
        {
            InitializeComponent();
            this.UITransformation = content;
            this.container.Child = content.getVisual();
            this.tbkHeader.Text = content.Name;
            this.ToolTip = content.Description;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            OnDelete?.Invoke(this, e);
        }

        public bool DeleteEnabled
        {
            get => this.btnClear.Visibility == Visibility.Visible;

            set
            {
                if (value)
                {
                    this.btnClear.Visibility = Visibility.Visible;
                }
                else
                {
                    this.btnClear.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
