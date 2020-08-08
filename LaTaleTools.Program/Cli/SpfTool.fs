module LaTaleTools.Cli.SpfTool

open System
open System.IO
open Argu
open LaTaleTools.FileTools.SpfTools
open LaTaleTools.Utils
open LaTaleTools.Utils.Paths

type SpfToolArgs =
  | [<Mandatory; ExactlyOnce; CustomCommandLine("--out")>] OutDir of string
  | [<MainCommand; ExactlyOnce; Last>] Path of filePaths: string list

  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | OutDir _ -> "The output directory"
      | Path _ -> "The file path to load spf files from"

let processAndValidatePaths (paths: string list) =
  let processedPaths =
    paths
    |> List.collect(fun path ->
      if Paths.isDirectory path
      then Directory.GetFiles(path, "*.spf") |> List.ofArray
      else List.singleton path)

  processedPaths
  |> List.tryFind(fun path -> System.IO.Path.GetExtension(path).ToLowerInvariant() <> ".spf")
  |> Option.bind(fun filePath -> failwith $"%s{filePath} does not have an SPF extension (is it a correct file?)")
  |> ignore

  processedPaths

let processSpfFile (filePath: string) (outDir: string) =
  async {
    use spfCollection = new SpfEntriesCollection(filePath)

    for entry in spfCollection.Entries do
      let outPath = outDir +/ entry.FilePath |> Path.GetFullPath
      Path.GetDirectoryName(outPath) |> Directory.CreateDirectory |> ignore

      Console.WriteLine entry.FilePath

      use outputStream = new FileStream(outPath, FileMode.Create)
      do! entry.CreateContentStream().CopyToAsync(outputStream) |> Async.AwaitTask
  }

let spfToolMain (parseResults: ParseResults<SpfToolArgs>): Async<int> =
  let outDir = parseResults.GetResult(<@ OutDir @>)
  let paths = parseResults.PostProcessResult(<@ Path @>, processAndValidatePaths)

  async {
    for path in paths do
      do! processSpfFile path outDir

    return 0
  }