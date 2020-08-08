module LaTaleTools.FileTools.SpfTools

open System
open System.IO
open System.IO.MemoryMappedFiles
open LaTaleTools.LowLevel.Spf

type FilePath = string

type SpfEntriesCollection(filePath: FilePath) =
  let memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0L, MemoryMappedFileAccess.Read)
  let fileAccessor = memoryMappedFile.CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.Read)
  let fileLength = FileInfo(filePath).Length;

  member this.Entries =
    let rec read pos =
      let spfEntry = fileAccessor.Read<SpfEntry>(pos)

      seq {
        yield {|
                  FilePath = spfEntry.FullPath
                  FileLength = int64(spfEntry.Length)
                  CreateContentStream =
                    fun () ->
                      memoryMappedFile
                        .CreateViewStream(int64(spfEntry.Offset), int64(spfEntry.Length), MemoryMappedFileAccess.Read)
                        :> Stream
              |}

        match spfEntry.Offset with
        | 0 -> ()
        | _ -> yield! read (pos - 140L)
      }

    read (fileLength - 280L)

  interface IDisposable with
    member this.Dispose() =
      do
        fileAccessor.Dispose();
        memoryMappedFile.Dispose()