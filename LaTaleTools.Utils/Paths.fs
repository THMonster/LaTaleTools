module LaTaleTools.Utils.Paths

open System.IO

let (+/) path1 path2 = Path.Combine(path1, path2)

let isDirectory path = File.GetAttributes(path).HasFlag(FileAttributes.Directory)