namespace LaTaleTools.FileToolsTests

open System
open System.IO
open FsUnit
open LaTaleTools.FileTools.LdtTools
open LaTaleTools.FileTools.TblTools
open LaTaleTools.Utils.Paths
open Microsoft.FSharp.Reflection
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
[<DeploymentItem("DataFiles")>]
type TestClass () =

    [<TestMethod>]
    member this.TestTbl () =
      use collection = new TblEntryCollection("DataFiles" +/ "SYSTEM.TBL")
      let groups = collection.AllImageGroups |> Array.ofSeq

      groups |> should haveLength 1

      for group in groups do
        for subImage in group.SubImages do
          subImage.ImageFileName |> should startWith "System"

    [<TestMethod>]
    member this.TestLdt () =
      let filePath = "DataFiles" +/ @"FX_LIST.LDT"
      use table = new LdtTable(filePath)
      let rows = table.AllRows

      table.ColumnNames |> String.concat(",") |> Console.WriteLine
      table.ColumnDataTypes |> Seq.map (fun t -> t.ToString()) |> String.concat(",") |> Console.WriteLine
      for row in rows do
        row
        |> Array.map (fun data -> FSharpValue.GetUnionFields (data, data.GetType())
                                  |> snd
                                  |> Seq.head)
        |> ignore

      ()