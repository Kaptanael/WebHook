using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services;

public class SchemaGeneratorService(IWebhookEndpointRepository webhookEndpointRepository ): ISchemaGeneratorService
{
    public string Generate(string triggerConfigJson)
    {        
        return @"{
            ""type"": ""object"",
            ""properties"": {
                ""CompanyId"": { ""type"": ""integer"" },
                ""DeviceType"": { ""type"": [""integer"", ""null""] },
                ""PanelNo"": { ""type"": [""integer"", ""null""] },
                ""DeviceNo"": { ""type"": [""integer"", ""null""] },
                ""Status"": { ""type"": [""integer"", ""null""] },
                ""Badge"": { ""type"": [""string"", ""null""] },
                ""FacilityNo"": { ""type"": [""integer"", ""null""] }
            },
            ""required"": [""CompanyId""]
        }";
    }
}
