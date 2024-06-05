using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Internal;
using pm4h.semantics.datamodel;

namespace Mineguide.perspectives.semantics
{
    public class SemanticInformationItem : INotifyPropertyChanged
    {
        //public const string SEMANTIC_TAG_ID = MineguideSemantic.SEMANTIC_TAG_ID;
        //public const string MAIN_BINDING_ID = MineguideSemantic.MAIN_BINDING_ID;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public SemanticInformationItem() { }

        private BasicInfo dataItem;
        public SemanticInformationItem(BasicInfo data) { SetInformation(data); }

        private string _id;
        public string Id { get => _id; set { _id = value; NotifyPropertyChanged(); } }

        private string _name;
        public string Name { get => _name; set { _name = value; NotifyPropertyChanged(); } }

        private string _semanticTag = null;
        public string SemanticTag { get => _semanticTag; set { _semanticTag = value; NotifyPropertyChanged(); } }

        private string _mainBinding = null;
        public string MainBinding { get => _mainBinding; set { _mainBinding = value; NotifyPropertyChanged(); } }

        public ObservableCollection<OtherBindingItem> OtherBindings { get; set; } = new ObservableCollection<OtherBindingItem>();

        public void SetInformation(BasicInfo data)
        {
            dataItem = data; //store data item

            // Create new semantic information item
            Id = data.Id;
            Name = data.Name;
            foreach (var semItem in data.Annotations.IterateAnnotations().OrderBy(a => a.Id))
            {
                if (semItem.Id == MineguideSemantic.SEMANTIC_TAG_ID)
                {
                    SemanticTag = semItem.Value;
                }
                else if (semItem.Id == MineguideSemantic.MAIN_BINDING_ID)
                {
                    MainBinding = semItem.Value;
                }
                else if(semItem.Id.StartsWith(MineguideSemantic.OTHER_BINDING_ID_PREFIX))
                {                    
                    OtherBindings.Add(new OtherBindingItem(Id, semItem));
                }
            }
        }

        public void AddEmptyOtherBinding()
        {
            string obId = MineguideSemantic.OTHER_BINDING_ID_PREFIX;// "OtherBinding";
            if (OtherBindings.LastOrDefault() is OtherBindingItem lastItem)
            {
                int last = int.Parse(lastItem.Id.Remove(0, obId.Length));
                obId += (last + 1);
            }
            else
            {
                obId += "1";
            }
            var newBinding = new OtherBindingItem(this.Id, new SemanticAnnotation(obId, "")); // add empty binding binding to loaded BasicInfo ID
            OtherBindings.Add(newBinding);
            //NotifyPropertyChanged("OtherBindings");
        }

        /// <summary>
        /// Save the information of the data item to local BasicInfo object reference
        /// </summary>
        public void SaveInformation() => SaveInformation(dataItem);

        /// <summary>
        /// Save the information of the data item to the specified BasicInfo object provided
        /// </summary>
        /// <param name="data">BasicInfo where to copy semantic data</param>
        public void SaveInformation(BasicInfo data)
        {
            if (dataItem == null) return;

            data.Annotations.SemanticAnnotations.Clear(); // BORRADOS TODOS LOS SEMANTIC ANNOTATIONS
            if (!SemanticTag.IsNullOrEmpty())
            {
                data.Annotations[MineguideSemantic.SEMANTIC_TAG_ID] = 
                    new SemanticAnnotation(MineguideSemantic.SEMANTIC_TAG_ID, SemanticTag);
            }
            if (!MainBinding.IsNullOrEmpty())
            {
                data.Annotations[MineguideSemantic.MAIN_BINDING_ID] = 
                    new SemanticAnnotation(MineguideSemantic.MAIN_BINDING_ID, MainBinding);
            }
            foreach (var otherBinding in OtherBindings)
            {
                if (!otherBinding.Id.IsNullOrEmpty())
                {
                    data.Annotations[otherBinding.Id] = new SemanticAnnotation(otherBinding.Id, otherBinding.Value);
                }
            }

        }
    }

    public class OtherBindingItem : INotifyPropertyChanged
    {       
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public OtherBindingItem() { }

        public OtherBindingItem(string parentPropertyId, SemanticAnnotation annotation)
        {
            ParentPropertyId = parentPropertyId;
            Id = annotation.Id;
            Value = annotation.Value;
        }

        public string _parentPropertyId;
        public string ParentPropertyId { get => _parentPropertyId; set { _parentPropertyId = value; NotifyPropertyChanged(); } }

        private string _id;
        public string Id { get => _id; set { _id = value; NotifyPropertyChanged(); NotifyPropertyChanged("Name"); } }

        //private string _name;
        public string Name => "Other Binding " + Id[MineguideSemantic.OTHER_BINDING_ID_PREFIX.Length..];// "OtherBinding".Length..]; //get => _name; set { _name = value; NotifyPropertyChanged(); } }

        private string _value;
        public string Value { get => _value; set { _value = value; NotifyPropertyChanged(); } }
    }
}
