module FileSystem
open WixSharp
open WixSharp.CommonTasks

// Get root directory
let private dll = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().FullName)
let src = dll.Directory.Parent


/// Recursively adds a source directory and its files to a Wix Dir.
let rec getDirRec (srcPath: string) (feature: Feature) =
    let srcDir = new System.IO.DirectoryInfo(srcPath)
    let files = srcDir.EnumerateFiles()
                |> Seq.map (fun f -> new File(feature, f.FullName))
                |> Seq.toArray

    let targetDir = new Dir(feature, srcDir.Name)
    targetDir.AddFiles(files) |> ignore

    for d in srcDir.EnumerateDirectories() do
        let subDir = getDirRec d.FullName feature
        targetDir.AddDir(subDir) |> ignore

    targetDir

/// Adds a source directory and its files to a Wix Dir.
let rec getDir (srcPath: string) (feature: Feature) =
    let srcDir = new System.IO.DirectoryInfo(srcPath)
    let files = srcDir.EnumerateFiles()
                |> Seq.map (fun f -> new File(feature, f.FullName))
                |> Seq.toArray

    let targetDir = new Dir(feature, srcDir.Name)
    targetDir.AddFiles(files) |> ignore
    targetDir

/// Renames a WixSharp target Dir
let renameDir (name: string) (dir: Dir) =
    dir.Name <- name
    dir

let renameFile (name: string) (file: File) =
    file.TargetFileName <- name
    file