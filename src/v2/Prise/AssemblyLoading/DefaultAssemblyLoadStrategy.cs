using System;
using System.Linq;
using System.Reflection;

namespace Prise.AssemblyLoading
{
    public class DefaultAssemblyLoadStrategy : IAssemblyLoadStrategy
    {
        protected IPluginDependencyContext pluginDependencyContext;

        public DefaultAssemblyLoadStrategy(IPluginDependencyContext pluginDependencyContext)
        {
            this.pluginDependencyContext = pluginDependencyContext;
        }

        public virtual AssemblyFromStrategy LoadAssembly(string initialPluginLoadDirectory, AssemblyName assemblyName,
            Func<string, AssemblyName, ValueOrProceed<AssemblyFromStrategy>> loadFromDependencyContext,
            Func<string, AssemblyName, ValueOrProceed<AssemblyFromStrategy>> loadFromRemote,
            Func<string, AssemblyName, ValueOrProceed<AssemblyFromStrategy>> loadFromAppDomain)
        {
            if (assemblyName.Name == null)
                return null;

            ValueOrProceed<AssemblyFromStrategy> valueOrProceed = ValueOrProceed<AssemblyFromStrategy>.FromValue(null, true);

            var isHostAssembly = IsHostAssembly(assemblyName);
            var isRemoteAssembly = IsRemoteAssembly(assemblyName);

            if (isHostAssembly && !isRemoteAssembly) // Load from Default App Domain (host)
            {
                valueOrProceed = loadFromAppDomain(initialPluginLoadDirectory, assemblyName);
                if (valueOrProceed.Value != null)
                    return null; // fallback to default loading mechanism
            }

            if (valueOrProceed.CanProceed)
                valueOrProceed = loadFromDependencyContext(initialPluginLoadDirectory, assemblyName);


            if (valueOrProceed.CanProceed)
                valueOrProceed = loadFromRemote(initialPluginLoadDirectory, assemblyName);

            return valueOrProceed.Value;
        }

        public virtual NativeAssembly LoadUnmanagedDll(string fullPathToPluginAssembly, string unmanagedDllName,
            Func<string, string, ValueOrProceed<string>> loadFromDependencyContext,
            Func<string, string, ValueOrProceed<string>> loadFromRemote,
            Func<string, string, ValueOrProceed<IntPtr>> loadFromAppDomain)
        {
            ValueOrProceed<string> valueOrProceed = ValueOrProceed<string>.FromValue(String.Empty, true);
            ValueOrProceed<IntPtr> ptrValueOrProceed = ValueOrProceed<IntPtr>.FromValue(IntPtr.Zero, true);

            valueOrProceed = loadFromDependencyContext(fullPathToPluginAssembly, unmanagedDllName);

            if (valueOrProceed.CanProceed)
                ptrValueOrProceed = loadFromAppDomain(fullPathToPluginAssembly, unmanagedDllName);

            if (valueOrProceed.CanProceed && ptrValueOrProceed.CanProceed)
                valueOrProceed = loadFromRemote(fullPathToPluginAssembly, unmanagedDllName);

            return NativeAssembly.Create(valueOrProceed.Value, ptrValueOrProceed.Value);
        }

        protected virtual bool IsHostAssembly(AssemblyName assemblyName) => this.pluginDependencyContext.HostDependencies.Any(h => h.DependencyName.Name == assemblyName.Name);
        protected virtual bool IsRemoteAssembly(AssemblyName assemblyName) => this.pluginDependencyContext.RemoteDependencies.Any(r => r.DependencyName.Name == assemblyName.Name);
    }
}