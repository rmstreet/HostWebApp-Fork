﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Xml.Linq;
using Frankstein.Common.Configuration;
using Frankstein.DbFileSystem;

namespace Frankstein.PluginLoader
{
    /*
     * http://shazwazza.com/post/Developing-a-plugin-framework-in-ASPNET-with-medium-trust
     */

    public class PluginLoaderEntryPoint
    {
        public static readonly DirectoryInfo PluginFolder;

        private static bool _initialized;

        private static readonly PluginStorage Storage;


        static PluginLoaderEntryPoint()
        {
            //determinar probingPath

            var privatePath = "~/_plugins";

            try
            {
                var configFile = XElement.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

                var probingElement = (from runtime in configFile.Descendants("runtime")
                                      from assemblyBinding in runtime.Elements(XName.Get("assemblyBinding", "urn:schemas-microsoft-com:asm.v1"))
                                      from probing in assemblyBinding.Elements(XName.Get("probing", "urn:schemas-microsoft-com:asm.v1"))
                                      select probing).FirstOrDefault();

                if (probingElement != null)
                {
                    var paths = probingElement.Attribute("privatePath").Value;
                    //pode conter vários paths, separados por ';'
                    Trace.TraceInformation("[PluginLoader]:Private Paths are '{0}'", paths);
                    privatePath = paths.Split(';')[0];
                    if (privatePath.IndexOf('/') != 0)
                    {
                        privatePath = "/" + privatePath;
                    }
                }

            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Error reading probing privatePath in web.config. {0}", ex.Message);
            }


            PluginFolder = new DirectoryInfo(HostingEnvironment.MapPath(privatePath));

            if (!PluginFolder.Exists)
            {
                PluginFolder.Create();
            }

            Storage = new PluginStorage(PluginFolder);
        }

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            var existingAssemblies = PluginFolder.GetFiles("*.dll", SearchOption.AllDirectories);
            var filenames = existingAssemblies.Select(fileInfo => fileInfo.FullName).ToList();

            var cfg = BootstrapperSection.Instance;

            if (cfg.PluginLoader.DeleteFiles)
            {
                foreach (var filename in filenames)
                {
                    if (File.Exists(filename))
                    {
                        try
                        {
                            File.Delete(filename);
                        }
                        catch (Exception ex)
                        {

                            Trace.TraceError(ex.Message);
                        }
                    }
                }

                filenames.Clear();
            }

            if (cfg.PluginLoader.LoadFromDb)
            {
                var assemblies = LoadFromDb();
                var dbAssemblies = WriteToDisk(assemblies);
                filenames.AddRange(dbAssemblies);
            }

            LoadAndRegisterPlugins(filenames.ToArray());
        }

        static void LoadAndRegisterPlugins(params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                if (File.Exists(fileName))
                {
                    PluginStorage.LoadAndRegister(fileName);
                }
            }
        }

        public static void SaveAndLoadAssembly(string fileName, byte[] bytes)
        {
            Trace.TraceInformation("[PluginLoader]:Writing and Loading new compiled assembly: '{0}'", fileName);
            var kvp = new KeyValuePair<string, byte[]>(fileName, bytes);

            var fileNames = WriteToDisk(new[] { kvp });
            LoadAndRegisterPlugins(fileNames.ToArray());
        }

        private static Dictionary<string, byte[]> LoadFromDb()
        {
            var assemblies = new Dictionary<string, byte[]>();
            using (var ctx = new DbFileContext())
            {
                var files = ctx.DbFiles
                    .Where(x => !x.IsHidden && !x.IsDirectory && x.IsBinary && x.Extension.Equals(".dll"))
                    .ToList();

                foreach (var s in files)
                {
                    Trace.TraceInformation("[PluginLoader]: Found assembly from Database: {0}", s.VirtualPath);
                    assemblies.Add(s.Name, s.Bytes);
                }
            }

            return assemblies;
        }

        private static IEnumerable<string> WriteToDisk(IEnumerable<KeyValuePair<string, byte[]>> assemblies)
        {
            var result = new List<string>();
            try
            {
                foreach (var assembly in assemblies)
                {
                    var fileName = assembly.Key;
                    if (!Path.HasExtension(assembly.Key))
                        fileName = assembly.Key + ".dll";

                    var fullFileName = Path.Combine(PluginFolder.FullName, fileName);

                    if (File.Exists(fullFileName))
                        File.Delete(fullFileName);

                    File.WriteAllBytes(fullFileName, assembly.Value);

                    result.Add(fullFileName);

                    Trace.TraceInformation("[PluginLoader]: Assembly written to disk: {0}", fullFileName);

                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message);
            }

            return result;
        }
    }
}