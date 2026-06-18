using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Xml.XPath;

namespace MVPAPI.WebHook.OpenApi;

public sealed class XmlCommentsOperationTransformer : IOpenApiOperationTransformer
{
    private readonly XPathNavigator? _navigator;

    public XmlCommentsOperationTransformer()
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            _navigator = new XPathDocument(xmlPath).CreateNavigator();
        }
    }

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (_navigator is null) return Task.CompletedTask;

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return Task.CompletedTask;

        var memberName = BuildMemberName(descriptor.MethodInfo);
        var memberNode = _navigator.SelectSingleNode($"/doc/members/member[@name='{memberName}']");
        if (memberNode is null) return Task.CompletedTask;

        if (string.IsNullOrEmpty(operation.Summary))
        {
            var summary = memberNode.SelectSingleNode("summary")?.Value?.Trim();
            if (summary is not null) operation.Summary = summary;
        }

        if (string.IsNullOrEmpty(operation.Description))
        {
            var remarks = memberNode.SelectSingleNode("remarks")?.Value?.Trim();
            if (remarks is not null) operation.Description = remarks;
        }

        foreach (var parameter in operation.Parameters ?? [])
        {
            if (!string.IsNullOrEmpty(parameter.Description)) continue;
            var desc = memberNode.SelectSingleNode($"param[@name='{parameter.Name}']")?.Value?.Trim();
            if (desc is not null) parameter.Description = desc;
        }

        return Task.CompletedTask;
    }

    private static string BuildMemberName(MethodInfo method)
    {
        var typeName = method.DeclaringType!.FullName!.Replace('+', '.');
        var parameters = method.GetParameters();

        if (parameters.Length == 0)
            return $"M:{typeName}.{method.Name}";

        var paramList = string.Join(",", parameters.Select(p => GetTypeName(p.ParameterType)));
        return $"M:{typeName}.{method.Name}({paramList})";
    }

    private static string GetTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var baseName = type.GetGenericTypeDefinition().FullName!;
        baseName = baseName[..baseName.IndexOf('`')];
        var args = string.Join(",", type.GetGenericArguments().Select(GetTypeName));
        return $"{baseName}{{{args}}}";
    }
}
