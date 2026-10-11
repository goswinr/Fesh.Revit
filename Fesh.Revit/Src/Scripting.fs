namespace Fesh.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open Fesh

module Scripting =

    /// Runs the action in a transaction. Commits it if the action succeeds, rolls it back if the action throws,
    /// so that a failing script leaves no half done changes in the document.
    let private transact (doc:Document) (action:unit->unit)=
        use t = new Transaction(doc, "Fesh F# script")
        let s = t.Start()
        match s with
        |TransactionStatus.Started ->      //transaction has begun (until committed or rolled back)
            try
                action()
                let r = t.Commit()
                match r with
                |TransactionStatus.Committed  -> ()        //simply committed, ended an empty transaction, flushed all, or undo is disabled

                |TransactionStatus.Uninitialized    //initial value, the transaction has not been started yet in this status
                |TransactionStatus.Started          //transaction has begun (until committed or rolled back)
                |TransactionStatus.RolledBack       //rolled back (aborted)
                |TransactionStatus.Pending          //returned from error handling that took over managing the transaction
                |TransactionStatus.Error            //error while committing or rolling back
                |TransactionStatus.Proceed ->         //while still in error handling (internal status)
                        eprintfn "Transaction.Commit returned: %A" r
                |_ ->   eprintfn "Transaction.Commit returned unknown state: %A" r
            with ex ->
                if t.GetStatus() = TransactionStatus.Started then // Commit may have ended it already
                    t.RollBack() |> ignore
                match DebugUtils.Fesh with
                |None      -> eprintfn "Function in transaction failed, the transaction was rolled back:\r\n%A" ex
                |Some fesh -> fesh.Log.PrintfnColor 240  0 0 "Function in transaction failed, the transaction was rolled back:\r\n%A" ex

        |TransactionStatus.Committed         //simply committed, ended an empty transaction, flushed all, or undo is disabled
        |TransactionStatus.Uninitialized    //initial value, the transaction has not been started yet in this status
        |TransactionStatus.RolledBack       //rolled back (aborted)
        |TransactionStatus.Pending          //returned from error handling that took over managing the transaction
        |TransactionStatus.Error            //error while committing or rolling back
        |TransactionStatus.Proceed ->         //while still in error handling (internal status)
                eprintfn "Transaction.Start returned: %A, the function was not run." s
        |_ ->   eprintfn "Transaction.Start returned unknown state: %A, the function was not run." s



    /// Runs a function in a Revit API transaction.
    /// You need to use this when making changes to the current Revit document.
    /// Will log errors to Fesh Log if transaction has problems
    /// This function can be invoked from any thread, it will switch to the Revit UI thread.
    /// It only queues the function and returns before it has run: Revit runs it when it is idle,
    /// for a script that runs on the UI thread only after the script has finished.
    let transactWithDoc (transaction: Document -> unit)  =
        let action () = FeshAddin.Instance.RunOnDoc (fun (doc:Document) -> transact doc (fun () -> transaction doc))
        Fittings.SyncWpf.doSync action


    /// Runs a function in a RevitAPI transaction using an UIApplication
    /// You need to use this when making changes to the current Revit document.
    /// (app.ActiveUIDocument.Document gets the active document)
    /// Will log errors to Fesh Log if transaction has problems
    /// This function can be invoked from any thread, it will switch to the Revit UI thread.
    /// It only queues the function and returns before it has run: Revit runs it when it is idle,
    /// for a script that runs on the UI thread only after the script has finished.
    let transactWithApp (transaction: UIApplication-> unit)  =
        let action () = FeshAddin.Instance.RunOnApp (fun (app:UIApplication) -> transact app.ActiveUIDocument.Document (fun () -> transaction app))
        Fittings.SyncWpf.doSync action

    /// Runs a function with the current Document
    /// Without a document transaction.
    /// Will log errors to Fesh Log if transaction has problems
    /// This function can be invoked from any thread, it will switch to the Revit UI thread.
    /// It only queues the function and returns before it has run: Revit runs it when it is idle,
    /// for a script that runs on the UI thread only after the script has finished.
    let doWithDoc(action: Document -> unit)  =
        let f () = FeshAddin.Instance.RunOnDoc (fun (doc:Document) -> action doc)
        Fittings.SyncWpf.doSync f

    /// Runs a function with the current UIApplication.
    /// Without a document transaction.
    /// Will log errors to Fesh Log if transaction has problems
    /// This function can be invoked from any thread, it will switch to the Revit UI thread.
    /// It only queues the function and returns before it has run: Revit runs it when it is idle,
    /// for a script that runs on the UI thread only after the script has finished.
    let doWithApp(action: UIApplication -> unit)  =
        let f () = FeshAddin.Instance.RunOnApp (fun (app:UIApplication) -> action app)
        Fittings.SyncWpf.doSync f







[<Obsolete("Renamed: Use Fesh.Revit.Scripting module functions instead")>]
module ScriptingSyntax =

    [<Obsolete("Renamed: Use Fesh.Revit.Scripting.transactWithDoc function instead")>]
    let run (transaction: Document -> unit)  =
        Scripting.transactWithDoc transaction

    [<Obsolete("Renamed:Use Fesh.Revit.Scripting.transactWithApp function instead")>]
    let runApp (transaction: UIApplication-> unit)  =
        Scripting.transactWithApp transaction
