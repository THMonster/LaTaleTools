namespace LaTaleTools

open Argu
open LaTaleTools.Cli.LdtTool
open LaTaleTools.Cli.SpfTool
open LaTaleTools.Cli.TblTool

module Program =
  type LaTaleToolsArgs =
    | [<CliPrefix(CliPrefix.None); CustomCommandLine("spf")>] InvokeSpfTool of ParseResults<SpfToolArgs>
    | [<CliPrefix(CliPrefix.None); CustomCommandLine("tbl")>] InvokeTblTool of ParseResults<TblToolArgs>
    | [<CliPrefix(CliPrefix.None); CustomCommandLine("ldt")>] InvokeLdtTool of ParseResults<LdtToolArgs>

    interface IArgParserTemplate with
      member this.Usage =
        match this with
        | InvokeSpfTool _ -> "Load and extract contents from the given SPF file(s)"
        | InvokeTblTool _ -> "Load and extract contents from TBL files in a directory into a sqlite database"
        | InvokeLdtTool _ -> "Load and extract contents from LDT files in a directory into a sqlite database"

  [<EntryPoint>]
  let main argv =
    let parser = ArgumentParser.Create<LaTaleToolsArgs>()

    try
      let result = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
      match result.GetSubCommand() with
      | InvokeSpfTool args -> spfToolMain(args)
      | InvokeTblTool args -> tblToolMain(args)
      | InvokeLdtTool args -> ldtToolMain(args)
      |> Async.RunSynchronously
    with e ->
      printfn "%s" e.Message

      1
