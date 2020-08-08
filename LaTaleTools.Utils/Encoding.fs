namespace LaTaleTools.Utils

open System.Text

type Encodings() =
  static do
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)

  static member public Encoding_Korean = Encoding.GetEncoding(949)

  static member public Encoding_ChineseSimplified = Encoding.GetEncoding(936)
