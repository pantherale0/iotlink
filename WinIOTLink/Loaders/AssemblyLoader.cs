﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WinIOTLink.API;
using WinIOTLink.Engine;
using WinIOTLink.Helpers;

namespace WinIOTLink.Loaders
{
    internal class AssemblyLoader
    {
        internal static bool LoadAppAssembly(ref AddonInfo addonInfo)
        {
            try
            {
                string filename = Path.Combine(addonInfo.AddonPath, addonInfo.AddonFile);

                if (File.Exists(filename))
                {
                    byte[] bytes = File.ReadAllBytes(filename);
                    Assembly asb = Assembly.Load(bytes);

                    // Load AddonScript from the assembly.
                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;

                    IEnumerable<Type> scriptTypeList = asb.GetExportedTypes().Where(w => w.IsSubclassOf(typeof(AddonScript)));

                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= CurrentDomain_ReflectionOnlyAssemblyResolve;
                    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

                    Type scriptType = null;
                    if (scriptTypeList.Count() > 0)
                        scriptType = scriptTypeList.FirstOrDefault();

                    if (scriptType != null)
                    {
                        LoggerHelper.Debug(typeof(AssemblyLoader), "Found AddonScript!");
                        addonInfo.ScriptClass = (AddonScript)Activator.CreateInstance(scriptType);
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception e)
            {
                LoggerHelper.Debug(typeof(AssemblyLoader), "Unhandled Exception: {0}", e.ToString());
            }

            return false;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains(typeof(MainEngine).Assembly.GetName().Name))
            {
                return Assembly.GetExecutingAssembly();
            }
            return null;
        }

        private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly asb = AppDomain.CurrentDomain.GetAssemblies().Where(w => w.FullName == args.Name).FirstOrDefault();
            return asb;
        }
    }
}
