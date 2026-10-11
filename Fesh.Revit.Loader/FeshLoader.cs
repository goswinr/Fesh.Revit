using System;
using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;

namespace Fesh.Revit.Loader
{
    /// <summary>
    /// The entry point in Revit 2025 and later, registered in Fesh.addin. Don't rename it.
    /// Loads Fesh.Revit.dll from the folder net8.0 or net10.0 next to the folder of the loader, for the .NET runtime that Revit runs on,
    /// and passes Revit's calls on to it. The ribbon button of Fesh refers to that dll, the loader is only needed at start and shutdown.
    /// </summary>
    public class FeshLoader : IExternalApplication
    {
        IExternalApplication? fesh;

        /// <summary>The path of Fesh.Revit.dll for the runtime of this process.</summary>
        static string FeshPath()
        {
            string loaderFolder = Path.GetDirectoryName(typeof(FeshLoader).Assembly.Location)!;
            string framework = Environment.Version.Major >= 10 ? "net10.0" : "net8.0";
            return Path.Combine(Path.GetDirectoryName(loaderFolder)!, framework, "Fesh.Revit.dll");
        }

        /// <summary>Loads Fesh.Revit and starts it.</summary>
        public Result OnStartup(UIControlledApplication application)
        {
            string path = FeshPath();
            try
            {
                // its dependencies, e.g. FSharp.Core, are loaded from its folder
                Assembly assembly = Assembly.LoadFrom(path);
                Type type = assembly.GetType("Fesh.Revit.FeshAddin", throwOnError: true)!;
                fesh = (IExternalApplication)Activator.CreateInstance(type)!;
            }
            catch (Exception e)
            {
                TaskDialog.Show("Load error", $"Fesh failed to load {path}:{Environment.NewLine}{e.Message}");
                return Result.Failed;
            }
            return fesh.OnStartup(application);
        }

        /// <summary>Shuts Fesh.Revit down.</summary>
        public Result OnShutdown(UIControlledApplication application) =>
            fesh is null ? Result.Succeeded : fesh.OnShutdown(application);
    }
}
