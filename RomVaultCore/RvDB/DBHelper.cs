﻿/******************************************************
 *     ROMVault3 is written by Gordon J.              *
 *     Contact gordon@romvault.com                    *
 *     Copyright 2013                                *
 ******************************************************/

using System;
using System.Collections.Generic;
using RomVaultCore.Storage.Dat;
using RomVaultCore.Utils;
using SortMethods;

namespace RomVaultCore.RvDB
{
    public enum EFile
    {
        Keep,
        Delete
    }

    public static class DBHelper
    {
        private static readonly byte[] ZeroByteMD5;
        private static readonly byte[] ZeroByteSHA1;
        private static readonly byte[] ZeroByteCRC;

        static DBHelper()
        {
            ZeroByteMD5 = VarFix.CleanMD5SHA1("d41d8cd98f00b204e9800998ecf8427e", 32);
            ZeroByteSHA1 = VarFix.CleanMD5SHA1("da39a3ee5e6b4b0d3255bfef95601890afd80709", 40);
            ZeroByteCRC = VarFix.CleanMD5SHA1("00000000", 8);
        }

        public static void GetSelectedDirListStart(ref List<RvFile> lstDir, RvFile thisDir)
        {
            if (thisDir == null)
            {
                thisDir = DB.DirRoot;
            }
            else if (thisDir.Tree != null)
            {
                lstDir.Add(thisDir);
            }


            GetSelectedDirList(ref lstDir, thisDir);
        }


        public static void GetSelectedDirList(ref List<RvFile> lstDir, RvFile thisDir)
        {
            for (int i = 0; i < thisDir.ChildCount; i++)
            {
                if (thisDir.DatStatus != DatStatus.InDatCollect)
                {
                    continue;
                }
                RvFile tDir = thisDir.Child(i);
                if (!tDir.IsDirectory)
                {
                    continue;
                }
                if (tDir.Tree == null)
                {
                    continue;
                }
                if (tDir.Tree.Checked != RvTreeRow.TreeSelect.UnSelected)
                {
                    lstDir.Add(tDir);
                }

                GetSelectedDirList(ref lstDir, tDir);
            }
        }



        public static int DatCompare(RvDat var1, RvDat var2)
        {
            //TODO check this
            int retv = RVSorters.CompareDatName(var1, var2);
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.TimeStamp.CompareTo(var2.TimeStamp));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.MultiDatsInDirectory).CompareTo(var2.Flag(DatFlags.MultiDatsInDirectory)));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.MultiDatOverride).CompareTo(var2.Flag(DatFlags.MultiDatOverride)));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.UseDescriptionAsDirName).CompareTo(var2.Flag(DatFlags.UseDescriptionAsDirName)));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.SingleArchive).CompareTo(var2.Flag(DatFlags.SingleArchive)));
            if (retv != 0)
            {
                return retv;
            }
            retv = Math.Sign(var1.SubDirType.CompareTo(var2.SubDirType));
            if (retv != 0)
            {
                return retv;
            }

            return 0;
        }


        public static int DatCompare(RvDat var1, DatImportDat var2)
        {
            //TODO check this
            int retv = RVSorters.CompareDatName(var1, var2);
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.TimeStamp.CompareTo(var2.TimeStamp));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.MultiDatsInDirectory).CompareTo(var2.Flag(DatFlags.MultiDatsInDirectory)));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.MultiDatOverride).CompareTo(var2.Flag(DatFlags.MultiDatOverride)));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.UseDescriptionAsDirName).CompareTo(var2.Flag(DatFlags.UseDescriptionAsDirName)));
            if (retv != 0)
            {
                return retv;
            }

            retv = Math.Sign(var1.Flag(DatFlags.SingleArchive).CompareTo(var2.Flag(DatFlags.SingleArchive)));
            if (retv != 0)
            {
                return retv;
            }
            retv = Math.Sign(var1.SubDirType.CompareTo(var2.SubDirType));
            if (retv != 0)
            {
                return retv;
            }

            return 0;
        }


        // find fix files, if the gotFile has been fully scanned check the SHA1/MD5, if not then just return true as the CRC/Size is all we have to go on.
        // this means that if the gotfile has not been fully scanned this will return true even with the source and destination SHA1/MD5 possibly different.
        public static bool CheckIfMissingFileCanBeFixedByGotFile(RvFile missingFile, RvFile gotFile)
        {
            // should probably be checking that the header type also match
            if (missingFile.HeaderFileType != HeaderFileType.Nothing && gotFile.HeaderFileType != HeaderFileType.Nothing)
            {
                if (missingFile.HeaderFileType != gotFile.HeaderFileType)
                    return false;
            }
            if (missingFile.HeaderFileTypeRequired && (gotFile.HeaderFileType == HeaderFileType.Nothing || !gotFile.FileStatusIs(FileStatus.HeaderFileTypeFromHeader)))
                return false;


            if (missingFile.FileStatusIs(FileStatus.SHA1FromDAT) && gotFile.FileStatusIs(FileStatus.SHA1Verified) && !ArrByte.BCompare(missingFile.SHA1, gotFile.SHA1))
            {
                if (missingFile.FileStatusIs(FileStatus.SHA1FromDAT) && gotFile.FileStatusIs(FileStatus.AltSHA1Verified) && !ArrByte.BCompare(missingFile.SHA1, gotFile.AltSHA1))
                    return false;
            }

            if (missingFile.FileStatusIs(FileStatus.MD5FromDAT) && gotFile.FileStatusIs(FileStatus.MD5Verified) && !ArrByte.BCompare(missingFile.MD5, gotFile.MD5))
            {
                if (missingFile.FileStatusIs(FileStatus.MD5FromDAT) && gotFile.FileStatusIs(FileStatus.AltMD5Verified) && !ArrByte.BCompare(missingFile.MD5, gotFile.AltMD5))
                    return false;
            }

            return true;
        }


        public static bool CheckIfGotfileAndMatchingFileAreFullMatches(RvFile gotFile, RvFile matchingFile)
        {
            if (gotFile.FileStatusIs(FileStatus.SHA1Verified) && matchingFile.FileStatusIs(FileStatus.SHA1Verified) && !ArrByte.BCompare(gotFile.SHA1, matchingFile.SHA1))
                return false;
            if (gotFile.FileStatusIs(FileStatus.MD5Verified) && matchingFile.FileStatusIs(FileStatus.MD5Verified) && !ArrByte.BCompare(gotFile.MD5, matchingFile.MD5))
                return false;

            return true;
        }




        public static bool IsZeroLengthFile(RvFile tFile)
        {
            bool foundOneMatching = false;
            if (tFile.MD5 != null)
            {
                if (!ArrByte.BCompare(tFile.MD5, ZeroByteMD5))
                {
                    return false;
                }
                foundOneMatching = true;
            }

            if (tFile.SHA1 != null)
            {
                if (!ArrByte.BCompare(tFile.SHA1, ZeroByteSHA1))
                {
                    return false;
                }
                foundOneMatching = true;
            }

            if (tFile.CRC != null)
            {
                if (!ArrByte.BCompare(tFile.CRC, ZeroByteCRC))
                {
                    return false;
                }
                foundOneMatching = true;
            }

            if (tFile.Size != null)
            {
                if (tFile.Size != 0)
                {
                    return false;
                }
                foundOneMatching = true;
            }

            // if at least one hash,size matched. & nothing failed to match.
            if (foundOneMatching)
                return true;

            // hashes and size are all null
            // see if we have a directory
            return (tFile.Name.Length > 1 && tFile.Name.Substring(tFile.Name.Length - 1, 1) == "/");
        }

        public static bool RomFromSameGame(RvFile a, RvFile b)
        {
            if (a.Parent == null)
            {
                return false;
            }
            if (b.Parent == null)
            {
                return false;
            }

            return a.Parent == b.Parent;
        }



    }
}