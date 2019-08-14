using Autofac;
using Autofac.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Plugin.Payments.BrainTree.Data;
using Nop.Plugin.Payments.BrainTree.Domain;
using Nop.Plugin.Payments.BrainTree.Services;
using Nop.Web.Framework.Infrastructure.Extensions;

namespace Nop.Plugin.Payments.BrainTree.Infrastructure
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<BrainTreeService>().As<IBrainTreeService>().InstancePerLifetimeScope();

            //data context
            builder.RegisterPluginDataContext<BrainTreeObjectContext>("nop_object_context_brain_tree");

            //override required repository with our custom context
            builder.RegisterType<EfRepository<BrainTreeMerchantRecord>>().As<IRepository<BrainTreeMerchantRecord>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_brain_tree"))
                .InstancePerLifetimeScope();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order => 1;
    }
}