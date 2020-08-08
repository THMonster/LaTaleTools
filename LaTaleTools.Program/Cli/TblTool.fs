module LaTaleTools.Cli.TblTool

open Argu
open LaTaleTools.SqliteExportTools
open LaTaleTools.Utils

type TblToolArgs =
  | [<Mandatory; ExactlyOnce; CustomCommandLine("--out")>] OutSqliteFile of string
  | [<Unique; CustomCommandLine("--table-suffix")>] TableSuffix of string
  | [<MainCommand; ExactlyOnce; Last>] TblDirectory of string

  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | OutSqliteFile _ -> "Ths output sqlite file"
      | TblDirectory _ -> "The directory containing the TBL files"
      | TableSuffix _ -> "The sqlite table suffix"

let tblToolMain (parseResults: ParseResults<TblToolArgs>): Async<int> =
  let outSqliteFile = parseResults.GetResult(<@ OutSqliteFile @>)
  let tblDirectory =
    parseResults.PostProcessResult(<@ TblDirectory @>,
                                   fun path ->
                                     if Paths.isDirectory path
                                     then path
                                     else failwith $"%s{path} is not a directory")
  let tableSuffix = parseResults.GetResult(<@ TableSuffix @>, "CHAR")

  use exportTools = new ExportTools(outSqliteFile)
  exportTools.ExportTbls(tblDirectory, tableSuffix)

  async {
    return 0
  }