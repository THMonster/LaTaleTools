namespace LaTaleTools.SqliteExportTools

open System
open System.Collections.Generic
open System.Data
open System.Data.SQLite
open System.Drawing
open System.IO
open LaTaleTools.FileTools.TblTools
open LaTaleTools.FileTools.LdtTools
open LaTaleTools.Utils.Paths
open Microsoft.FSharp.Reflection

type ExportTools (databaseFile: string) =
  let connection: SQLiteConnection =
    if (not (File.Exists databaseFile))
    then SQLiteConnection.CreateFile(databaseFile)

    let connection = new SQLiteConnection(sprintf "Data Source=%s" databaseFile)
    connection.Open()

    connection

  let executeCommand (updateCommand: SQLiteCommand -> unit) =
    use command = new SQLiteCommand(connection)
    updateCommand(command)
    command.ExecuteNonQuery() |> ignore

  do
    executeCommand <| fun command -> command.CommandText <- "PRAGMA encoding=UTF8"

  member this.ExportTbls(baseTblDirectory: string, tableSuffix: string) =
    let tblFiles = Directory.GetFiles(baseTblDirectory, "*.tbl", SearchOption.AllDirectories)

    let tableName = sprintf "TBLDATA_%s" tableSuffix

    printfn "Creating table: %s" tableName
    executeCommand <| fun command -> command.CommandText <- sprintf "DROP TABLE IF EXISTS %s" tableName
    executeCommand <| fun command -> command.CommandText <- sprintf "CREATE TABLE %s (ParentDirectoryName VARTEXT(32), FileName VARTEXT(32), GroupName VARTEXT(32), Pattern INTEGER, OriginalImageFileName VARTEXT(24), X INTEGER, Y INTEGER, AxisX FLOAT, AxisY FLOAT, TopLeftX INTEGER, TopLeftY INTEGER, BottomRightX INTEGER, BottomRightY INTEGER, SubImageBlob BLOB)" tableName
    printfn "Created table: %s" tableName

    for tblFile in tblFiles do
      printfn "Processing TBL file: %s" tblFile
      let directory = Path.GetDirectoryName(tblFile)

      let cache = Dictionary<string, Image>()

      let getImage (imageName: string) =
        match cache.TryGetValue imageName with
        | true, v -> Some v
        | false, _ ->
          let imageFilePath = directory +/ imageName
          if File.Exists(imageFilePath)
          then
            let image = Image.FromFile(directory +/ imageName)
            cache.Add(imageName, image)
            Some image
          else None

      let getSubImageBlob ((x1, y1, x2, y2): int * int * int * int) (image: Image) =
        let mutable imagePtr = image

        let sourceBitmap = imagePtr :?> Bitmap
        let resultBitmap = new Bitmap(x2 - x1, y2 - y1)

        for x in x1 .. x2 - 1 do
          for y in y1 .. y2 - 1 do
            let color =
              if x >= 0 && y >= 0 && x < sourceBitmap.Width && y < sourceBitmap.Height
              then sourceBitmap.GetPixel(x, y)
              else Color.Transparent
            resultBitmap.SetPixel(x - x1, y - y1, color)

        use ms = new MemoryStream()
        resultBitmap.Save(ms, image.RawFormat)
        ms.ToArray()

      use tblTable = new TblEntryCollection(tblFile)

      use transaction = connection.BeginTransaction()
      for imageGroup in tblTable.AllImageGroups do
        let groupMetadata = imageGroup.Group

        for subImage in imageGroup.SubImages do
          let binData =
            getImage(subImage.ImageFileName)
            |> Option.map(getSubImageBlob((subImage.TopLeftX, subImage.TopLeftY, subImage.BottomRightX, subImage.BottomRightY)))

          match binData with
          | Some binary ->
            executeCommand <| fun command ->
              command.CommandText <- sprintf "INSERT INTO %s VALUES(@ParentDirectoryName, @FileName, @GroupName, @Pattern, @OriginalImageFileName, @X, @Y, @AxisX, @AxisY, @TopLeftX, @TopLeftY, @BottomRightX, @BottomRightY, @SubImageBlob)" tableName
              command.Parameters.Add("@ParentDirectoryName", DbType.String).Value <- Path.GetFileName(directory)
              command.Parameters.Add("@FileName", DbType.String).Value <-
                let fileName = Path.GetFileName(tblFile)
                in if fileName.EndsWith(".TBL", StringComparison.OrdinalIgnoreCase) then fileName.Substring(0, fileName.Length - ".TBL".Length) else fileName
              command.Parameters.Add("@GroupName", DbType.String).Value <- groupMetadata.GroupName
              command.Parameters.Add("@Pattern", DbType.Int32).Value <- subImage.Pattern
              command.Parameters.Add("@OriginalImageFileName", DbType.String).Value <- subImage.ImageFileName
              command.Parameters.Add("@X", DbType.Int32).Value <- subImage.X
              command.Parameters.Add("@Y", DbType.Int32).Value <- subImage.Y
              command.Parameters.Add("@AxisX", DbType.Int32).Value <- subImage.AxisX
              command.Parameters.Add("@AxisY", DbType.Int32).Value <- subImage.AxisY
              command.Parameters.Add("@TopLeftX", DbType.Int32).Value <- subImage.TopLeftX
              command.Parameters.Add("@TopLeftY", DbType.Int32).Value <- subImage.TopLeftY
              command.Parameters.Add("@BottomRightX", DbType.Int32).Value <- subImage.BottomRightX
              command.Parameters.Add("@BottomRightY", DbType.Int32).Value <- subImage.BottomRightY
              command.Parameters.Add("@SubImageBlob", DbType.Binary).Value <- binary
          | None -> ()

      transaction.Commit()
      for value in cache.Values do value.Dispose()

  member this.ExportLdts(ldtDirectory: string) =
    let ldtFiles =
      Directory.GetFiles(ldtDirectory, "*.ldt")
      |> Array.map (fun fileName -> let tableName = "LDTDATA_" + Path.GetFileName(fileName).Replace(".", "_") in (tableName, fileName))

    for (tableName, filePath) in ldtFiles do
      use ldtTable = new LdtTable(filePath)

      let columnTypeToSqlType =
        function
        | ColumnDataTypes.Id -> "INTEGER"
        | ColumnDataTypes.Unsigned -> "INTEGER"
        | ColumnDataTypes.Bool -> "BOOLEAN"
        | ColumnDataTypes.Float -> "FLOAT"
        | ColumnDataTypes.String -> "TEXT"
        | ColumnDataTypes.Int -> "INTEGER"
        | _ -> failwith "Unexpected enum value"

      let tableColumns =
        (ldtTable.ColumnNames, ldtTable.ColumnDataTypes)
        ||> Seq.zip
        |> Seq.map (fun (name, columnType) -> sprintf "%s %s" (name.Replace("-", "_")) (columnTypeToSqlType(columnType)))
        |> String.concat ","

      printfn "Creating table: %s" tableName
      executeCommand <| fun command -> command.CommandText <- sprintf "DROP TABLE IF EXISTS %s" tableName
      executeCommand <| fun command -> command.CommandText <- sprintf "CREATE TABLE %s (%s)" tableName tableColumns
      printfn "Created table: %s" tableName

      use transaction = connection.BeginTransaction();
      for row in ldtTable.AllRows do
        let values =
          row
          |> Array.map (fun data -> FSharpValue.GetUnionFields (data, data.GetType())
                                    |> snd
                                    |> Seq.head)
          |> Array.map (fun data -> "'" + data.ToString().Trim('"').Replace("'", "''") + "'")
          |> String.concat(",")

        executeCommand <| fun command -> command.CommandText <- sprintf "INSERT INTO %s VALUES(%s)" tableName values
      transaction.Commit()

      printfn "Completed table: %s" tableName

  interface IDisposable with
    member this.Dispose() =
      connection.Close()
      connection.Dispose()
