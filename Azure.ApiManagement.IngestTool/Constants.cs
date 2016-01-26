using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.ApiManagement.IngestTool
{
    public static class Constants
    {
        public const string CONNECTED_SERVICE_NAME = "Azure.ApiManagement.IngestTool";
        internal const string APIM_RESOURCE_TYPE = "Microsoft.ApiManagement/service";
        internal const string APIA_V1_SWAGGER_URL_TEMPLATE = "subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.AppService/apiapps/{2}/apiDefinitions/swagger-2.0-standard?api-version=2015-03-01-preview";
        internal const string SWAGGER_CONTENT_TYPE = "application/vnd.swagger.doc+json";
        internal const string CONNECTED_SERVICE_METADATA_KEY_FOR_SWAGGER = "SwaggerKey";
        internal const string CONNECTED_SERVICE_METADATA_KEY_FOR_PRODUCTS = "ApimProducts";
        internal const string CONNECTED_SERVICE_METADATA_KEY_FOR_ADAL_TOKEN = "AdalToken";
        internal const string CONNECTED_SERVICE_METADATA_KEY_FOR_SUB_ID = "SubscriptionId";
        internal const string CONNECTED_SERVICE_METADATA_KEY_FOR_APIM_INSTANCE = "ApimInstance";
        internal const string CONNECTED_SERVICE_METADATA_KEY_FOR_API_APP = "SelectedApiApp";
        internal const string URL_APP_SERVICES_WITH_METADATA_DEFINITION = "https://management.azure.com/subscriptions/{0}/providers/Microsoft.Web/sites?api-version=2015-08-01&propertiesToInclude=SiteConfig";
    }
}
