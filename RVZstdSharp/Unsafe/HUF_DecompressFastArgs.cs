namespace RVZstdSharp.Unsafe
{
    /**
     * The input/output arguments to the Huffman fast decoding loop:
     *
     * ip [in/out] - The input pointers, must be updated to reflect what is consumed.
     * op [in/out] - The output pointers, must be updated to reflect what is written.
     * bits [in/out] - The bitstream containers, must be updated to reflect the current state.
     * dt [in] - The decoding table.
     * ilimit [in] - The input limit, stop when any input pointer is below ilimit.
     * oend [in] - The end of the output stream. op[3] must not cross oend.
     * iend [in] - The end of each input stream. ip[i] may cross iend[i],
     *             as long as it is above ilimit, but that indicates corruption.
     */
    public unsafe struct HUF_DecompressFastArgs
    {
        public _ip_e__FixedBuffer ip;
        public _op_e__FixedBuffer op;
        public fixed ulong bits[4];
        public void* dt;
        public byte* ilimit;
        public byte* oend;
        public _iend_e__FixedBuffer iend;
        public unsafe struct _ip_e__FixedBuffer
        {
            public byte* e0;
            public byte* e1;
            public byte* e2;
            public byte* e3;
        }

        public unsafe struct _op_e__FixedBuffer
        {
            public byte* e0;
            public byte* e1;
            public byte* e2;
            public byte* e3;
        }

        public unsafe struct _iend_e__FixedBuffer
        {
            public byte* e0;
            public byte* e1;
            public byte* e2;
            public byte* e3;
        }
    }
}