namespace Fesh.Revit

open System.Threading
open System.Windows.Threading


type Sync private () = 

    static let mutable ctx : SynchronizationContext = null  // will be set in main UI STAThread

    /// the UI SynchronizationContext to switch to inside async CEs
    static member syncContext = ctx

    /// to ensure SynchronizationContext is set up.
    static member installSynchronizationContext () = 
        if SynchronizationContext.Current = null then
            DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher) |> SynchronizationContext.SetSynchronizationContext
        ctx <- SynchronizationContext.Current
        // see https://github.com/fsprojects/FsXaml/blob/c0979473eddf424f7df83e1b9222a8ca9707c45a/src/FsXaml.Wpf/Utilities.fs#L132


