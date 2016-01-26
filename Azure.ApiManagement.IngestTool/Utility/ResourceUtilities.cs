using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.ApiManagement.IngestTool.Utility
{
    public static class ResourceUtilities
    {
        public static string GetResourceGroupFromResourceId(string resourceId)
        {
            // format is /subscriptions/66c15fe5-2d0b-446c-9207-39d03a993afa/resourceGroups/ApiResources/providers/Microsoft.AppService/apiapps/ContactList
            return resourceId.Split('/')[4];
        }

        internal static string GetSwaggerUrlForApiV1App(string resourceId)
        {
            var arr = resourceId.Split('/');
            return string.Format(Constants.APIA_V1_SWAGGER_URL_TEMPLATE,
                arr[2],
                arr[4],
                arr[8]
                );
        }
    }
}
