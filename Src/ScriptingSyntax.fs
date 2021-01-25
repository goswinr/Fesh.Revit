//namespace Revit.Scripting
namespace Seff.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open System.Windows
open Seff
open Seff.Model
open Seff.Config
open System.Windows.Input
open System.Reflection
open System.Collections.Concurrent
open System.Diagnostics


module ScriptingSyntax =
    
    /// runs a function in a transaction
    let run (f:Document-> unit)  = 
        SeffAddin.Instance.RunDoc (fun (doc:Document) ->  
            use t = new Transaction(doc, "Seff F# script")        
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
                f(doc)
            with ex -> 
                Current.seff.Log.printfnColor 240  0 0 "Function in transaction failed with:\r\n%A" ex

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
            )

