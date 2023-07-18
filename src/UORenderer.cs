﻿using Microsoft.Xna.Framework;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace UORenderer;

internal static class UORenderer
{
    static private AssemblyLoadContext _loadContext;
    static private string? _rootDir;

    static private Assembly? LoadFromResource(string resourceName)
    {
        Console.WriteLine($"Loading resource {resourceName}");

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = $"{assembly.GetName().Name}.{resourceName}.dll";
            if (name.StartsWith("System."))
                continue;

            using Stream? s = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourceName}.dll");

            if (s == null || s.Length == 0)
                continue;

            return _loadContext.LoadFromStream(s);
        }

        return null;
    }

    static private Assembly? ResolveAssembly(AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
        Console.WriteLine($"Resolving assembly {assemblyName}");

        if (loadContext != _loadContext)
        {
            throw new Exception("Mismatched load contexts!");
        }

        if (assemblyName == null || assemblyName.Name == null)
        {
            throw new Exception("Unable to load null assembly");
        }

        /* Wasn't in same directory. Try to load it as a resource. */
        return LoadFromResource(assemblyName.Name);
    }

    static private IntPtr ResolveUnmanagedDll(Assembly assembly, string unmanagedDllName)
    {
        Console.WriteLine($"Loading unmanaged DLL {unmanagedDllName} for {assembly.GetName().Name}");

        /* Try the correct native libs directory first */
        string osDir = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            osDir = "x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            osDir = "osx";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            osDir = "lib64";
        }

        var libraryPath = Path.Combine(_rootDir, osDir, unmanagedDllName);

        Console.WriteLine($"Resolved DLL to {libraryPath}");

        if (File.Exists(libraryPath))
            return NativeLibrary.Load(libraryPath);

        return IntPtr.Zero;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetDllDirectory(string lpPathName);

    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        _rootDir = AppContext.BaseDirectory;
        Console.WriteLine($"Root Dir: {_rootDir}");

        _loadContext = AssemblyLoadContext.Default;
        _loadContext.ResolvingUnmanagedDll += ResolveUnmanagedDll;
        _loadContext.Resolving += ResolveAssembly;
        
        NativeLibrary.Load(Path.Combine(_rootDir, "x64", "zlib.dll"));
        
        Log.Start(LogTypes.All);
        UOFileManager.Load(ClientVersion.CV_70796, @"D:\Games\Ultima Online Classic_7_0_95_0_modified", false, "enu");
        
        using (Game g = new UOGame())
        {
            g.Run();
        }
    }

    public static Project CurrentProject = new Project("test");
}