module LaTaleTools.Cli.LdtTool

open Argu
open LaTaleTools.SqliteExportTools
open LaTaleTools.Utils

type LdtToolArgs =
  | [<Mandatory; ExactlyOnce; CustomCommandLine("--out")>] OutSqliteFile of string
  | [<MainCommand; ExactlyOnce; Last>] LdtDirectory of string

  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | OutSqliteFile _ -> "Ths output sqlite file"
      | LdtDirectory _ -> "The directory containing the LDT files"

let ldtToolMain (parseResults: ParseResults<LdtToolArgs>): Async<int> =
  let outSqliteFile = parseResults.GetResult(<@ OutSqliteFile @>)
  let tblDirectory =
    parseResults.PostProcessResult(<@ LdtDirectory @>,
                                   fun path ->
                                     if Paths.isDirectory path
                                     then path
                                     else failwith $"%s{path} is not a directory")

  use exportTools = new ExportTools(outSqliteFile)
  exportTools.ExportLdts(tblDirectory)

  async {
    return 0
  }