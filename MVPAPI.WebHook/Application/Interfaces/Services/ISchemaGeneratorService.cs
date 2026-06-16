namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface ISchemaGeneratorService
{
    string Generate(string triggerConfigJson);
}
