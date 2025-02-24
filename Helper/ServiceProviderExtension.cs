using Microsoft.Extensions.DependencyInjection;
using System.Windows.Markup;

namespace MFAWPF.Helper;

public class ServiceProviderExtension : MarkupExtension
{
    public Type ServiceType { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return App.Services.GetRequiredService(ServiceType);
    }
}
