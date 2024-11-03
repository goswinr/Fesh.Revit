#r "C:/Program Files/Autodesk/Revit 2024/RevitAPI.dll"
#r "C:/Program Files/Autodesk/Revit 2024/RevitAPIUI.dll"
#r "Fesh.Revit"
open Autodesk.Revit
open Autodesk.Revit.DB
open Autodesk.Revit.UI

module Louvre =

    let pattern =
        [|  [|  0,3 ;  -2,2 ;  -3,0 ;  -2,-2 ;  0,-3 ;  2,-2 ;  3,0 ;  2,2 |]
            [|  0,3 ;  2,2 ;  2,4 |]
            [|  2,2 ;  3,0 ;  4,2 |]
            [|  2,4 ;  4,4 ;  3,6 |]
            [|  4,2 ;  6,3 ;  4,4 |]
            [|  2,2 ;  4,2 ;  4,4 ;  2,4 |]  |]

    let mapN = Array.map >> Array.map

    let points =  mapN (fun (u,v) -> XYZ(float u, float v, 0.0)) pattern

    let edges  =
        [| for pts in points do
                for i = 0 to pts.Length - 2 do
                    yield Line.CreateBound(pts.[i] , pts.[i+1])
                yield Line.CreateBound(pts.[pts.Length-1] , pts.[0] ) |]  // connect last to first

    // draw grid
    let shiftLine (offX, offY) (l:Line)  =
        let offset = XYZ(offX, offY , 0.0 )
        Line.CreateBound (l.GetEndPoint 0 + offset , l.GetEndPoint 1 + offset)

    let uvs =
        let ext = 6 * 8// step size 6*7, max 19 at 0.013 radians
        [| for u in -ext..6..ext do
            for v in  -ext..6..ext do
                yield float u, float v |]

    let edgesShifted =
        [| for uv in uvs do yield! Array.map (shiftLine uv) edges |]

    // draw sphere
    let setToSphere (pt:XYZ) =
        let uRad = pt.X * 0.014  //to get angle in Radians
        let vRad = pt.Y * 0.014
        let x = sin uRad / cos uRad
        let y = sin vRad / cos vRad
        let f = 65. / sqrt (x*x + y*y + 1.) // Radius of sphere // inverse length to get scaling factor for this vector
        XYZ(x*f, y*f, f)

    let setLineToSphere (l:Line) =
        Line.CreateBound (setToSphere (l.GetEndPoint 0) , setToSphere (l.GetEndPoint 1) )
        :> GeometryObject // cast so it works with DirectShape.CreateElement


Fesh.Revit.ScriptingSyntax.runApp (fun (app:UIApplication)  ->
    let doc = app.ActiveUIDocument.Document
    let ds = DirectShape.CreateElement(doc, ElementId(BuiltInCategory.OST_GenericModel))//, "ME", "MEo")
    Louvre.edgesShifted
    |> Array.map Louvre.setLineToSphere
    |> ds.SetShape
    )

