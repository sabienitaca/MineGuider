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
using Microsoft.AspNetCore.Components.Forms;
using pm4h.windows.ui.windows;

namespace Mineguide.perspectives.transformationsui.transformations.propertiesEditor
{
    /// <summary>
    /// Lógica de interacción para BasicPropertiesEditor.xaml
    /// </summary>
    public partial class BasicPropertiesEditor : UserControl, IPropertiesEditor
    {
        public BasicPropertiesEditor()
        {
            InitializeComponent();
            this.container.ItemsSource = Items;
            Loaded += BasicPropertiesEditor_Loaded;
        }

        private void BasicPropertiesEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus first textbox
            if (this.container.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement element)
            {
                FindFirstTextBox(element)?.Focus();
            }
        }

        public string Title { get; set; } = "Properties";
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

                    // Check required fields
                    foreach (var item in Items)
                    {
                        if (item.Required && string.IsNullOrWhiteSpace(item.Value))
                        {
                            PM4HMessageBox.Show($"The field '{item.Id}' is required", "Field required", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Information);
                            return;
                        }
                    }
                    // Check validation functions
                    foreach (var item in Items)
                    {
                        if (!item.Validate(out string msg))
                        {
                            PM4HMessageBox.Show($"The field '{item.Id}' is not valid. {msg}", "Field not valid", PM4HMessageBoxButtons.Accept, PM4HMessageBoxIcons.Information);
                            return;
                        }
                    }

                    dlg.DialogResult = true;
                }, 
                true);
            dlg.AddButtonCancel();
            return dlg.ShowDialog() ?? false;
        }

        private TextBox FindFirstTextBox(FrameworkElement element)
        {
            if (element is TextBox textBox)
            {
                return textBox;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child != null)
                {
                    var textBoxChild = FindFirstTextBox(child);
                    if (textBoxChild != null)
                    {
                        return textBoxChild;
                    }
                }
            }

            return null;
        }

        private ObservableCollection<DescItem> Items { get; set; } = new ObservableCollection<DescItem>();

        public void AddQuestion(string question, bool required, Func<string?, (bool, string?)> validationFunction = null, string? defaultValue = null)
        {
            if (validationFunction != null)
            {
                Items.Add(new DescItem() { Id = question, Required = required, ValidationFunction = validationFunction, Value = defaultValue});
            }
            else
            {
                Items.Add(new DescItem() { Id = question, Required = required, Value = defaultValue });
            }            
        }

        public void AddNewNameQuestion(string question, string? defaultValue = null)
        {
            AddQuestion(question, true, NewNameValidationFunction, defaultValue);
        }
        
        /// <summary>
        /// Propiedad estatica que retorna la funcion para validar que el nombre 
        /// no empieza por el simbolo @ ==>> (value) => (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("@"), "The field cannot begin with the @ symbol") 
        /// </summary>
        public static Func<string?, (bool, string?)> NewNameValidationFunction => 
            (value) => (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("@"), "The field cannot begin with the @ symbol");


        public void Clear() { Items.Clear(); }

        public void DeleteItem(string question)
        {
            var item = Items.FirstOrDefault(x => x.Id == question);
            if (item != null) Items.Remove(item);
        }

        public Dictionary<string, string?> GetAnswers()
        {
            Dictionary<string, string> answers = new Dictionary<string, string>();
            foreach (var item in Items)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    answers.Add(item.Id, item.Value);
                }
                else
                {
                    answers.Add(item.Id, null);
                }
            }
            return answers;
        }

        private class DescItem : INotifyPropertyChanged
        {
            public bool Required { get; set; } = false;

            public Func<string?, (bool, string?)> ValidationFunction { get; set; } = (x) => (true, null);

            public bool Validate(out string validationErrorMsg)
            {
                var validation = ValidationFunction(Value);
                if (validation.Item1)
                {
                    validationErrorMsg = "";
                    return true;
                }
                else
                {
                    validationErrorMsg = validation.Item2 ?? "";
                    return false;
                }
            }

            string _id = Guid.NewGuid().ToString();
            public string Id
            {
                get { return _id; }
                set
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }

            string? _value = null;
            public string? Value
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
}
