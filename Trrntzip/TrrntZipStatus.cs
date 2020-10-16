﻿using System;

namespace Trrntzip
{
    [Flags]
    public enum TrrntZipStatus
    {
        Unknown = 0,
        ValidTrrntzip = 1,
        Trrntzipped = 2,
        CorruptZip = 4,
        NotTrrntzipped = 8,
        BadDirectorySeparator = 16,
        Unsorted = 32,
        ExtraDirectoryEnteries = 64,
        RepeatFilesFound = 128
    }
}