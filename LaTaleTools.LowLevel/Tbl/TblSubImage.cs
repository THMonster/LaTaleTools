namespace LaTaleTools.LowLevel.Tbl
{
    using System.Runtime.InteropServices;
    using System.Text;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TblSubImage
    {
        private const int ImageFileNameLength = 24;
        private const int UnknownDataLength = 104;

        public readonly int Pattern;
        public readonly int X;
        public readonly int Y;
        public readonly float AxisX;
        public readonly float AxisY;
        public readonly int TopLeftX;
        public readonly int TopLeftY;
        public readonly int BottomRightX;
        public readonly int BottomRightY;

        private fixed byte ImageFileNameBytes[ImageFileNameLength];
        public fixed byte UnknownData[UnknownDataLength];

        public string ImageFileName
        {
            get
            {
                fixed (TblSubImage* subImage = &this)
                {
                    var byteCount = 0;
                    while (*(subImage->ImageFileNameBytes + byteCount) != 0)
                    {
                        byteCount++;
                    }

                    return Encoding.ASCII.GetString(subImage->ImageFileNameBytes, byteCount);
                }
            }
        }
    }
}