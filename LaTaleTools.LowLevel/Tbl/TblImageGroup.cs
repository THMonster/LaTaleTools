namespace LaTaleTools.LowLevel.Tbl
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TblImageGroup
    {
        private const int GroupNameBytesLength = 16;
        private const int UnknownDataLength = 116;

        public int SubImageCount;

        private fixed byte GroupNameBytes[GroupNameBytesLength];
        public fixed byte UnknownData[UnknownDataLength];

        public string GroupName
        {
            get
            {
                fixed (TblImageGroup* group = &this)
                {
                    return Encoding.ASCII.GetString(group->GroupNameBytes, GroupNameBytesLength)
                        .TrimEnd((char) 0);
                }
            }
        }
    }
}