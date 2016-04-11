
namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Owin
{
    using global::Owin;

    public interface IOwinAppBuilder
    {
        void Configuration(IAppBuilder appBuilder);
    }
}
