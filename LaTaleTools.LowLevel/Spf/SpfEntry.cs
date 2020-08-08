namespace LaTaleTools.LowLevel.Spf
{
    using System.Runtime.InteropServices;
    using Utils;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SpfEntry
    {
        private const int PathBytesLength = 128;

        private fixed byte PathBytes[PathBytesLength];

        public readonly int Offset;
        public readonly int Length;
        public readonly int Index;

        public string FullPath
        {
            get
            {
                fixed (SpfEntry* entry = &this)
                {
                    return Encodings.Encoding_Korean.GetString(entry->PathBytes, PathBytesLength)
                        .TrimEnd((char) 0);
                }
            }
        }
    }
}