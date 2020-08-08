module LaTaleTools.FileTools.LdtTools

open System
open System.Collections.Generic
open System.IO
open System.IO.MemoryMappedFiles
open System.Text
open LaTaleTools.Utils
open LaTaleTools.Utils.MemoryMappedFileAccessorExtensions

type CellData =
  | IdCell of int32
  | UnsignedCell of uint32
  | StringCell of string
  | BoolCell of bool
  | IntCell of int32
  | FloatCell of float32

type ColumnDataTypes =
  | Id = -1
  | Unsigned = 0
  | String = 1
  | Bool = 2
  | Int = 3
  | Float = 4

type public LdtTable(filePath: string) =
  let memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0L, MemoryMappedFileAccess.Read)
  let fileAccessor = memoryMappedFile.CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.Read)

  member this.ColumnCount = fileAccessor.ReadInt32(4L) + 1
  member this.RowCount = fileAccessor.ReadInt32(8L)

  member this.ColumnNames =
    Seq.append (Seq.singleton "InternalImplicitId") (
      seq { 0 .. this.ColumnCount - 2 }
      |> Seq.map (fun n -> fileAccessor.ReadNullTerminatedString(12L + int64(n * 64), 64, Encoding.UTF8))
      |> Array.ofSeq
    )

  member this.ColumnDataTypes =
    Seq.append (Seq.singleton ColumnDataTypes.Id) (
      seq { 0 .. this.ColumnCount - 2 }
      |> Seq.map (fun n -> fileAccessor.ReadInt32(8204L + int64(n * 4)))
      |> Seq.map (enum<ColumnDataTypes>)
      |> Array.ofSeq
    )

  member this.ReadAllRows (pos)(n) =
    seq {
      if n = this.RowCount then ()
      else
        let list = List<CellData>()

        let mutable curPos = pos

        for t in this.ColumnDataTypes do
          match t with
          | ColumnDataTypes.Id ->
            list.Add (IdCell (fileAccessor.ReadInt32(curPos)))
            curPos <- curPos + 4L
          | ColumnDataTypes.Unsigned ->
            list.Add (UnsignedCell (fileAccessor.ReadUInt32(curPos)))
            curPos <- curPos + 4L
          | ColumnDataTypes.String ->
            let shortSize = fileAccessor.ReadInt16(curPos)
            list.Add (StringCell (fileAccessor.ReadString(curPos + 2L, int32(shortSize), Encodings.Encoding_ChineseSimplified)))
            curPos <- curPos + 2L + int64(shortSize)
          | ColumnDataTypes.Bool ->
            list.Add (BoolCell (fileAccessor.ReadInt32(curPos) = 1))
            curPos <- curPos + 4L
          | ColumnDataTypes.Int ->
            list.Add (IntCell (fileAccessor.ReadInt32(curPos)))
            curPos <- curPos + 4L
          | ColumnDataTypes.Float ->
            list.Add (FloatCell (fileAccessor.ReadSingle(curPos)))
            curPos <- curPos + 4L
          | v -> failwithf "Unknown enum value %d" (int v)

        yield list |> Array.ofSeq
        yield! this.ReadAllRows (curPos)(n + 1)
    }

  member this.AllRows = this.ReadAllRows(8716L)(0)

  interface IDisposable with
    member this.Dispose() =
      do
        fileAccessor.Dispose();
        memoryMappedFile.Dispose()
