module LaTaleTools.Utils.MemoryMappedFileAccessorExtensions

open System.IO.MemoryMappedFiles
open System.Text

type MemoryMappedViewAccessor with
  member this.ReadNullTerminatedString(startPos: int64, maxLength: int, encoding: Encoding) =
    let byteArray = Array.zeroCreate<byte>(maxLength)
    this.ReadArray(startPos, byteArray, 0, maxLength) |> ignore

    let stringSize = byteArray |> Array.findIndex (fun b -> b = 0uy)

    encoding.GetString(Array.sub byteArray 0 stringSize)

  member this.ReadString(startPos: int64, maxLength: int, encoding: Encoding) =
    let byteArray = Array.zeroCreate<byte>(maxLength)
    this.ReadArray(startPos, byteArray, 0, maxLength) |> ignore

    encoding.GetString(Array.sub byteArray 0 maxLength)
