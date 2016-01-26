using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ConnectedServices;
using Azure.ApiManagement.IngestTool.ViewModels;
using Microsoft.Azure.Management.ApiManagement;
using Microsoft.Azure;
using Microsoft.Azure.Management.ApiManagement.SmapiModels;
using Azure.ApiManagement.IngestTool.Utility;
using System.IO;

namespace Azure.ApiManagement.IngestTool
{
    [ConnectedServiceHandlerExport(Constants.CONNECTED_SERVICE_NAME, AppliesTo = "CSharp")]
    internal class ApimConnectedServiceHandler : ConnectedServiceHandler
    {
        public async override Task<AddServiceInstanceResult> AddServiceInstanceAsync(ConnectedServiceHandlerContext context, CancellationToken ct)
        {
            string token = (string)context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_ADAL_TOKEN];
            string subscriptionId = (string)context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SUB_ID];
            string swaggerJson = (string)context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SWAGGER];
            IEnumerable<ApiManagementProduct> products = (IEnumerable<ApiManagementProduct>)context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_PRODUCTS];
            ApiManagementInstance apimInstance = (ApiManagementInstance)context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_APIM_INSTANCE];
            ApiAppResource apiApp = (ApiAppResource)context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_API_APP];

            using (var apiManagementClient = new ApiManagementClient(
                new TokenCloudCredentials(subscriptionId, token)
                ))
            {
                await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information,
                    Resources.ProgressMessageImportingSwaggerTemplate,
                    apiApp.Name,
                    apimInstance.Name);

                // import the api
                using (MemoryStream stream = new MemoryStream())
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(swaggerJson);
                    writer.Flush();
                    stream.Position = 0;

                    apiManagementClient.Apis.Import(
                        ResourceUtilities.GetResourceGroupFromResourceId(apimInstance.ResourceId),
                        apimInstance.Name,
                        apiApp.Name,
                        Constants.SWAGGER_CONTENT_TYPE,
                        stream,
                        apiApp.Name);
                }

                // add the api to products
                foreach (var product in products)
                {
                    await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information,
                        Resources.ProgressMessageAddingApiToProductTemplate,
                        apiApp.Name,
                        product.Name);

                    apiManagementClient.ProductApis.Add(
                        ResourceUtilities.GetResourceGroupFromResourceId(apimInstance.ResourceId),
                        apimInstance.Name,
                        product.Id,
                        apiApp.Name);
                }
            }

            AddServiceInstanceResult result = new AddServiceInstanceResult(
               "AzureApiManagement",
               null);

            return result;
        }
    }
}
