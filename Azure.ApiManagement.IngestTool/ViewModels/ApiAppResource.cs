using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.ApiManagement.IngestTool.ViewModels
{
    public class ApiAppResource
    {
        public string Name { get; set; }
        public string ResourceGroup { get; set; }
        public string ResourceId { get; set; }
        public string SwaggerUrl { get; set; }

        public string DisplayName
        {
            get
            {
                return string.Format($"{this.Name} ({this.ResourceGroup})");
            }
        }
    }
}
