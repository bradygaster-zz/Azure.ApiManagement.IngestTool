using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.ApiManagement.IngestTool.ViewModels
{
    public class ApiManagementInstance
    {
        public string Name { get; set; }
        public string ResourceId { get; set; }
    }

    public class ApiManagementProduct : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
