﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using MvcLib.Common;

namespace MvcLib.PluginLoader
{
    public class PluginStorage : IDisposable
    {
        private readonly DirectoryInfo _pluginFolder;
        private static readonly Dictionary<string, Assembly> StoredAssemblies = new Dictionary<string, Assembly>();

        public PluginStorage(DirectoryInfo directoryInfo)
        {
            _pluginFolder = directoryInfo;

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            if (!AppDomain.CurrentDomain.IsFullyTrusted)
            {
                Trace.TraceWarning("We are not in FULL TRUST! We must use private probing path in Web.Config");
            }
        }

        #region events

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GlobalAssemblyCache)
                return;
            var name = args.LoadedAssembly.GetName().Name;

            Trace.TraceInformation("Assembly Loaded... {0}", name);

            if (args.LoadedAssembly.Location.StartsWith(_pluginFolder.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    RegisterWithCheck(args.LoadedAssembly);

                    var types = args.LoadedAssembly.GetExportedTypes();

                    if (types.Any())
                    {
                        Trace.Indent();
                        foreach (var type in types)
                        {
                            Trace.TraceInformation("Type exported: {0}", type.FullName);
                        }
                        Trace.Unindent();
                    }
                    else
                    {
                        Trace.TraceInformation("No types exported by Assembly: '{0}'", name);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation(ex.Message);
                }
            }
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.RequestingAssembly != null)
                return args.RequestingAssembly;

            var assembly = FindAssembly(args.Name);
            if (assembly != null)
            {
                Trace.TraceInformation("Assembly found and resolved: {0} = {1}", assembly.FullName, assembly.Location);
                return assembly;
            }
            return null; //not found
        }

        #endregion

        internal void LoadAndRegister(string fileName)
        {
            if (StoredAssemblies.ContainsKey(fileName))
            {
                return;
            }
            try
            {
                Assembly.LoadFile(fileName); //will trigger AssemblyLoad Event
            }
            catch (Exception ex)
            {
                var msg = "ERRO LOADING ASSEMBLY: {0}: {1}".Fmt(fileName, ex);
                Trace.TraceInformation(msg);
                LogEvent.Raise(ex.Message, ex);
            }
        }

        internal void RegisterWithCheck(Assembly assembly)
        {
            if (StoredAssemblies.ContainsKey(assembly.FullName))
            {
                return;
            }

            StoredAssemblies.Add(assembly.FullName, assembly);
            BuildManager.AddReferencedAssembly(assembly);

            var entry = assembly.GetExportedTypes().FirstOrDefault(x => typeof(IPlugin).IsAssignableFrom(x));
            if (entry != null)
            {
                IPlugin instance = null;
                try
                {
                    instance = Activator.CreateInstance(entry) as IPlugin;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Could not create instance from type '{0}'. {1}", entry, ex.Message);
                }
                finally
                {
                    if (instance != null)
                    {
                        Trace.TraceInformation("Plugin name is {0}", instance.PluginName);
                        instance.Start();
                    }
                }
            }

            Trace.TraceInformation("[PluginLoader]:Plugin registered: {0}", assembly.FullName);
        }

        #region public

        internal Assembly FindAssembly(string fullName)
        {
            return StoredAssemblies.ContainsKey(fullName)
                ? StoredAssemblies[fullName]
                : null;
        }

        public IEnumerable<string> GetPluginNames()
        {
            return StoredAssemblies.Values.Select(item => item.GetName().Name);
        }

        public static IEnumerable<Assembly> GetAssemblies()
        {
            return StoredAssemblies.Select(pair => pair.Value);
        }

        #endregion

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }
    }
}