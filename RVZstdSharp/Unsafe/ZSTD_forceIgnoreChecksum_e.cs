namespace RVZstdSharp.Unsafe
{
    public enum ZSTD_forceIgnoreChecksum_e
    {
        /* Note: this enum controls ZSTD_d_forceIgnoreChecksum */
        ZSTD_d_validateChecksum = 0,
        /* Note: this enum controls ZSTD_d_forceIgnoreChecksum */
        ZSTD_d_ignoreChecksum = 1
    }
}