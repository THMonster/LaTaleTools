module LaTaleTools.FileTools.TblTools

open System
open System.IO
open System.IO.MemoryMappedFiles
open LaTaleTools.LowLevel.Tbl

type public TblEntryCollection(filePath: string) =
  let memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0L, MemoryMappedFileAccess.Read)
  let fileAccessor = memoryMappedFile.CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.Read)

  do
    if fileAccessor.ReadInt32(0L) <> 100 then failwith "Wrong file tag. Expecting a 32-bit 100 at position 0"

  member this.GroupCount = fileAccessor.ReadInt32(4L)

  member this.ReadImageGroup (startPos) =
    let groupStructSize = sizeof<TblImageGroup>
    let group = fileAccessor.Read<TblImageGroup>(startPos)

    {|
       Group = group
       SubImages =
         seq { 0 .. group.SubImageCount - 1 }
         |> Seq.map (fun n -> fileAccessor.Read<TblSubImage>(startPos + int64(groupStructSize + n * sizeof<TblSubImage>)))
         |> Array.ofSeq
    |}

  member this.ReadAllImageGroups (startPos) =
    let rec read pos n =
      seq {
        if n = this.GroupCount then ()
        else
          let group = this.ReadImageGroup(pos)
          yield group

          yield! read (pos + int64(sizeof<TblImageGroup> + group.Group.SubImageCount * sizeof<TblSubImage>)) (n + 1)
      }

    read startPos 0

  member this.AllImageGroups = this.ReadAllImageGroups(8L)

  interface IDisposable with
    member this.Dispose() =
      do
        fileAccessor.Dispose();
        memoryMappedFile.Dispose()
