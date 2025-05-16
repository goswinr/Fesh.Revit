namespace Fesh.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open Fesh


module ScriptingSyntax =


    let private transact (doc:Document) (action:unit->unit)=
        use t = new Transaction(doc, "Fesh F# script")
        let s = t.Start()
        match s with
        |TransactionStatus.Started -> ()      //transaction has begun (until committed or rolled back)

        |TransactionStatus.Committed         //simply committed, ended an empty transaction, flushed all, or undo is disabled
        |TransactionStatus.Uninitialized    //initial value, the transaction has not been started yet in this status
        |TransactionStatus.RolledBack       //rolled back (aborted)
        |TransactionStatus.Pending          //returned from error handling that took over managing the transaction
        |TransactionStatus.Error            //error while committing or rolling back
        |TransactionStatus.Proceed ->         //while still in error handling (internal status)
                eprintfn "Transaction.Start returned: %A" s
        |_ ->   eprintfn "Transaction.Start returned unknown state: %A" s

        try
            action()
        with ex ->
            match DebugUtils.Fesh with
            |None -> ()
            |Some fesh -> fesh.Log.PrintfnColor 240  0 0 "Function in transaction failed with:\r\n%A" ex

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



    /// Runs a function in a transaction using a Document
    /// Will log errors to Fesh Log if transaction has problems
    /// This function ca be invoked from any thread, it will switch to the Revit UI thread
    let run (transaction: Document -> unit)  =
        let action () =
            FeshAddin.Instance.RunOnDoc (fun (doc:Document) -> transact doc (fun () -> transaction doc))
        Fittings.SyncWpf.doSync action


    /// Runs a function in a transaction using an UIApplication
    /// (app.ActiveUIDocument.Document gets the active document)
    /// Will log errors to Fesh Log if transaction has problems
    /// This function ca be invoked from any thread, it will switch to the Revit UI thread
    let runApp (transaction: UIApplication-> unit)  =
        let action () =
            FeshAddin.Instance.RunOnApp (fun (app:UIApplication) -> transact app.ActiveUIDocument.Document (fun () -> transaction app))
        Fittings.SyncWpf.doSync action
