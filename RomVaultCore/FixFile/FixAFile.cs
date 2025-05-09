﻿using System.Collections.Generic;
using System.Linq;
using RomVaultCore.FixFile.Utils;
using RomVaultCore.RvDB;
using RVIO;
using static RomVaultCore.FixFile.FixAZipCore.FindSourceFile;

namespace RomVaultCore.FixFile
{
    public static class FixAFile
    {

        public static ReturnCode FixFile(RvFile fixFile, List<RvFile> fileProcessQueue, ref int totalFixed, out string errorMessage)
        {
            errorMessage = "";

            switch (fixFile.RepStatus)
            {
                case RepStatus.Unknown:
                    return ReturnCode.FindFixesInvalidStatus;


                case RepStatus.UnScanned:
                    return ReturnCode.Good;

                case RepStatus.Missing:
                case RepStatus.MissingMIA:
                    // nothing can be done so moving right along
                    return ReturnCode.Good;

                case RepStatus.Incomplete:
                    // nothing to do here
                    return ReturnCode.Good;

                case RepStatus.Correct:
                case RepStatus.CorrectMIA:
                    // this is correct nothing to be done here
                    FixFileCheckName(fixFile);
                    return ReturnCode.Good;


                case RepStatus.NotCollected:
                    // this is correct nothing to be done here
                    return ReturnCode.Good;

                // Unknown

                case RepStatus.Ignore:
                    // this is correct nothing to be done here
                    return ReturnCode.Good;

                // Corrupt 

                case RepStatus.InToSort:
                    // this is correct nothing to be done here
                    return ReturnCode.Good;


                case RepStatus.Delete:
                    return FixFileDelete(fixFile, out errorMessage);

                case RepStatus.MoveToSort:
                    return FixFileMoveToSort(fixFile, out errorMessage);

                case RepStatus.MoveToCorrupt:
                    return FixFileMoveToCorrupt(fixFile, out errorMessage);

                case RepStatus.CanBeFixed:
                case RepStatus.CanBeFixedMIA:
                case RepStatus.CorruptCanBeFixed:
                    return FixFileCanBeFixed(fixFile, fileProcessQueue, ref totalFixed, out errorMessage);

                case RepStatus.NeededForFix:
                    // this file can be left as is, it will be used to fix a file, and then marked to be deleted.
                    return ReturnCode.Good;

                // this is for a corrupt CHD already in ToSort
                case RepStatus.Corrupt:
                    return ReturnCode.Good;

                case RepStatus.Rename:
                    // this file will be used and mark to be deleted in the CanBeFixed
                    // so nothing to be done to it here
                    return ReturnCode.Good;


                default:
                    ReportError.UnhandledExceptionHandler("Unknown fix file type " + fixFile.RepStatus + " Dat Status = " + fixFile.DatStatus + " GotStatus " + fixFile.GotStatus);
                    return ReturnCode.LogicError;
            }
        }

        private static void FixFileCheckName(RvFile fixFile)
        {
            if (!string.IsNullOrEmpty(fixFile.FileName))
            {
                string sourceFullName = Path.Combine(fixFile.Parent.FullName, fixFile.FileName);
                if (!File.SetAttributes(sourceFullName, FileAttributes.Normal))
                {
                    Report.ReportProgress(new bgwShowError(sourceFullName, $"Error Setting File Attributes to Normal. Before Case correction Rename. {Error.ErrorMessage}"));
                }

                File.Move(sourceFullName, fixFile.FullName);
                fixFile.FileName = null;
            }
        }

        private static ReturnCode FixFileDelete(RvFile fixFile, out string errorMessage)
        {
            ReturnCode retCode = FixFileUtils.DoubleCheckDelete(fixFile, out errorMessage);
            if (retCode != ReturnCode.Good)
            {
                return retCode;
            }

            string filename = fixFile.FullName;
            if (File.Exists(filename))
            {
                if (Settings.rvSettings.DetailedFixReporting)
                {
                    Report.ReportProgress(new bgwShowFix(Path.GetDirectoryName(filename), "", Path.GetFileName(filename), fixFile.Size, "Delete", "", "", ""));
                }

                if (!File.SetAttributes(filename, FileAttributes.Normal))
                {
                    Report.ReportProgress(new bgwShowError(filename, $"Error Setting File Attributes to Normal. Before Delete. {Error.ErrorMessage}"));
                }
                File.Delete(filename);
            }
            // here we just deleted a file so also delete it from the DB,
            // and recurse up deleting unnedded DIR's
            FixFileUtils.CheckDeleteFile(fixFile);
            return ReturnCode.Good;
        }

        private static ReturnCode FixFileMoveToSort(RvFile fixFile, out string errorMessage)
        {
            ReturnCode returnCode;
            returnCode = FixFileUtils.CreateToSortDirs(fixFile, out RvFile outDir, out string toSortFileName);
            if (returnCode != ReturnCode.Good)
            {
                errorMessage = "";
                return returnCode;
            }


            //create new tosort record
            // FileInfo toSortFile = new FileInfo(toSortFullName);
            RvFile toSortRom = new RvFile(FileType.File)
            {
                Name = toSortFileName,
                Size = fixFile.Size,
                CRC = fixFile.CRC,
                //TimeStamp = toSortFile.LastWriteTime,
                DatStatus = DatStatus.InToSort
            };


            string fixFileFullName = fixFile.FullNameCase;
            Report.ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixFileFullName), "", Path.GetFileName(fixFileFullName), fixFile.Size, "-->", outDir.FullName, "", fixFileFullName));

            string toSortFullName = Path.Combine(outDir.FullName, toSortFileName);
            returnCode = FixFileUtils.MoveFile(fixFile, toSortRom, toSortFullName, out bool fileMoved, out errorMessage);
            if (returnCode != ReturnCode.Good)
                return returnCode;

            if (!fileMoved)
            {
                string fixFilePath = fixFile.FullName;
                Report.ReportProgress(new bgwShowError(fixFilePath, $"Logic error moving this file to ToSort"));
                return ReturnCode.LogicError;
            }

            outDir.ChildAdd(toSortRom);

            return ReturnCode.Good;
        }

        private static ReturnCode FixFileMoveToCorrupt(RvFile fixFile, out string errorMessage)
        {
            string corruptDir = Path.Combine(DB.GetToSortPrimary().Name, "Corrupt");
            if (!Directory.Exists(corruptDir))
            {
                Directory.CreateDirectory(corruptDir);
            }
                        
            string toSortCorruptFullName = Path.Combine(corruptDir, fixFile.NameCase);
            string toSortCorruptFileName = fixFile.Name;
            int fileC = 0;
            while (File.Exists(toSortCorruptFullName))
            {
                fileC++;
                toSortCorruptFileName = fixFile.Name + fileC;
                toSortCorruptFullName = Path.Combine(corruptDir, toSortCorruptFileName);
            }

            //create new tosort record
            // FileInfo toSortCorruptFile = new FileInfo(toSortCorruptFullName);
            RvFile toSortCorruptRom = new RvFile(FileType.File)
            {
                Name = toSortCorruptFileName,
                Size = fixFile.Size,
                CRC = fixFile.CRC,
                //TimeStamp = toSortFile.LastWriteTime,
                DatStatus = DatStatus.InToSort
            };

            string fixFileFullName = fixFile.FullNameCase;
            Report.ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixFileFullName), "", Path.GetFileName(fixFileFullName), fixFile.Size, "-->", "Corrupt", "", fixFile.Name));

            ReturnCode returnCode = FixFileUtils.MoveFile(fixFile, toSortCorruptRom, toSortCorruptFullName, out bool fileMoved, out errorMessage);
            if (returnCode != ReturnCode.Good)
                return returnCode;

            if (!fileMoved)
            {
                string fixFilePath = fixFile.FullName;
                Report.ReportProgress(new bgwShowError(fixFilePath, $"Logic error moving this file to ToSort"));
                return ReturnCode.LogicError;
            }

            RvFile toSort = DB.GetToSortPrimary();
            RvFile rvCorruptDir = new RvFile(FileType.Dir) { Name = "Corrupt", DatStatus = DatStatus.InToSort };
            int found = toSort.ChildNameSearch(rvCorruptDir, out int indexCorrupt);
            if (found != 0)
            {
                rvCorruptDir.GotStatus = GotStatus.Got;
                indexCorrupt = toSort.ChildAdd(rvCorruptDir);
            }

            toSort.Child(indexCorrupt).ChildAdd(toSortCorruptRom);

            errorMessage = "";
            return ReturnCode.Good;
        }


        private static ReturnCode FixFilePreCheckFixFile(RvFile fixFile, out string errorMessage)
        {
            errorMessage = "";
            string fileName = fixFile.FullName;

            // find all files in the DB with this name
            // there could be another file if:
            // there is a wrong file with the same name that can just be deleted
            // there is a wrong file with the same name that needs moved to ToSort
            // there is a wrong file with the same name that is needed to fix another file
            List<RvFile> testList = new List<RvFile>();

            RvFile parent = fixFile.Parent;
            // start by finding the first file in the DB. (This should always work, as it will at least find the current file that CanBeFixed
            if (parent.ChildNameSearch(fixFile, out int index) != 0)
            {
                ReportError.Show("Logic error trying to find the file we are fixing " + fileName);
                return ReturnCode.LogicError;
            }
            testList.Add(parent.Child(index++));

            // now loop to see if there are any more files with the same name. (This is a case insensative compare)                        
            while (index < parent.ChildCount && RVSorters.CompareName(fixFile, parent.Child(index)) == 0)
            {
                testList.Add(parent.Child(index));
                index++;
            }

            // if we found more that one file in the DB then we need to process the incorrect file first.
            if (testList.Count > 1)
            {
                foreach (RvFile testChild in testList)
                {
                    if (testChild == fixFile)
                    {
                        continue;
                    }

                    if (testChild.DatStatus != DatStatus.NotInDat)
                    {
                        ReportError.Show("Trying to fix a file that already exists " + fileName);
                        return ReturnCode.LogicError;
                    }

                    RvFile testFile = testChild;
                    if (!testFile.IsFile)
                    {
                        ReportError.Show("Did not find a file logic error while fixing duplicate named file. in FixFile");
                        return ReturnCode.LogicError;
                    }

                    switch (testFile.RepStatus)
                    {
                        case RepStatus.Delete:
                            {
                                ReturnCode ret = FixFileDelete(testFile, out errorMessage);
                                if (ret != ReturnCode.Good)
                                {
                                    return ret;
                                }
                                break;
                            }
                        case RepStatus.MoveToSort:
                            {
                                ReturnCode ret = FixFileMoveToSort(testFile, out errorMessage);
                                if (ret != ReturnCode.Good)
                                {
                                    return ret;
                                }
                                break;
                            }
                        case RepStatus.MoveToCorrupt:
                            {
                                ReturnCode ret = FixFileMoveToCorrupt(testFile, out errorMessage);
                                if (ret != ReturnCode.Good)
                                {
                                    return ret;
                                }
                                break;
                            }
                        case RepStatus.NeededForFix:
                        case RepStatus.Rename:
                            {
                                // so now we have found the file with the same case insensative name and can rename it to something else to get it out of the way for now.
                                // need to check that the .tmp filename does not already exists.
                                File.SetAttributes(testChild.FullName, FileAttributes.Normal);
                                File.Move(testChild.FullName, testChild.FullName + ".tmp");

                                if (!parent.FindChild(testChild, out index))
                                {
                                    ReportError.Show("Unknown file status in Matching File found of " + testFile.RepStatus);
                                    return ReturnCode.LogicError;
                                }
                                parent.ChildRemove(index);
                                testChild.Name = testChild.Name + ".tmp";
                                parent.ChildAdd(testChild);
                                break;
                            }
                        default:
                            {
                                ReportError.Show("Unknown file status in Matching File found of " + testFile.RepStatus);
                                return ReturnCode.LogicError;
                            }
                    }
                }
            }
            else
            {
                // if there is only one file in the DB then it must be the current file that CanBeFixed
                if (testList[0] != fixFile)
                {
                    ReportError.Show("Logic error trying to find the file we are fixing " + fileName + " DB found file does not match");
                    return ReturnCode.LogicError;
                }
            }
            return ReturnCode.Good;
        }

        private static ReturnCode FixFileCanBeFixed(RvFile fixFile, List<RvFile> fileProcessQueue, ref int totalFixed, out string errorMessage)
        {
            string fixFileFullName = fixFile.FullName;
            FixFileUtils.CheckCreateDirectories(fixFile.Parent);

            // check to see if there is already a file with the name of the fixFile, and move it out the way.
            ReturnCode returnCode = FixFilePreCheckFixFile(fixFile, out errorMessage);
            if (returnCode != ReturnCode.Good)
                return returnCode;

            // now we can fix the file.

            List<RvFile> fixFiles = GetFixFileList(fixFile);

            if (DBHelper.IsZeroLengthFile(fixFile))
            {
                RvFile fileIn = new RvFile(FileType.File) { Size = 0 };
                returnCode = FixFileUtils.CopyFile(fileIn, null, fixFile.FullName, fixFile, FixStyle.Zero, out errorMessage);
                if (returnCode != ReturnCode.Good)
                {
                    errorMessage = fixFile.FullName + " " + fixFile.RepStatus + " " + returnCode + " : " + errorMessage;
                    return returnCode;
                }
                // Check the files that we found that where used to fix this file, and if they not listed as correct files, they can be set to be deleted.
                FixFileUtils.CheckFilesUsedForFix(fixFiles, fileProcessQueue, true);

                totalFixed++;
                return ReturnCode.Good;
            }

            RvFile fixingFile = FindSourceToUseForFix(null, fixFile, fixFiles, out FixStyle fixStyle).FirstOrDefault();


            if (fixStyle == FixStyle.ExtractToCache)
            {
                //Dictionary<string, RvFile> filesUsedForFix = new Dictionary<string, RvFile>();
                ReturnCode returnCode1 = Decompress7ZipFile.DecompressSource7ZipFile(fixingFile.Parent, false, null, out errorMessage);
                if (returnCode1 != ReturnCode.Good)
                {
                    ReportError.LogOut($"DecompressSource7Zip: {fixingFile.Parent.FileName} return {returnCode1}");
                    return returnCode1;
                }
                fixFiles = GetFixFileList(fixFile);
                fixingFile = FindSourceToUseForFix(null, fixFile, fixFiles, out fixStyle).FirstOrDefault();
                if (fixStyle == FixStyle.ExtractToCache)
                {
                    ReportError.LogOut($"DecompressSource7Zip: {fixingFile.Parent.FileName}");
                    return ReturnCode.LogicError;
                }
                //List<RvFile> usedFiles = filesUsedForFix.Values.ToList();
                //fixFiles.AddRange(usedFiles);
            }

            // this needs expanded up to see if there is any file in the returned list of files from FindSourcetoUseForFixNew that can be moved to the correct location.
            bool fileMove = FixFileUtils.TestFileMove(fixingFile, fixFile);
            string fts = fixingFile.FullName;
            Report.ReportProgress(new bgwShowFix(Path.GetDirectoryName(fixFileFullName), "", Path.GetFileName(fixFileFullName), fixFile.Size, "<--" + (fileMove ? "Move" : "Copy"), Path.GetDirectoryName(fts), Path.GetFileName(fts), fixingFile.Name));

            // this may move the hash values to the altHash locations.
            fixFile.FileTestFix(fixingFile);

            returnCode = FixFileUtils.MoveFile(fixingFile, fixFile, null, out bool fileMoved, out errorMessage);
            if (returnCode != ReturnCode.Good)
            {
                // if the altHash Values are set move them back.
                fixFile.FileRemove();
                return returnCode;
            }

            if (fileMoved)
            {
                // Check the files that we found that where used to fix this file, and if they not listed as correct files, they can be set to be deleted.
                FixFileUtils.CheckFilesUsedForFix(fixFiles, fileProcessQueue, true);

                totalFixed++;
                return ReturnCode.Good;
            }

            returnCode = FixFileUtils.CopyFile(fixingFile, null, fixFile.FullName, fixFile, FixStyle.Zero, out errorMessage);

            switch (returnCode)
            {
                case ReturnCode.Good: // correct reply to continue;
                    break;

                case ReturnCode.SourceDataStreamCorrupt:
                    {
                        Report.ReportProgress(new bgwShowFixError("CRC Error"));
                        // the file we used for fix turns out to be corrupt

                        // mark the source file as Corrupt
                        fixingFile.GotStatus = GotStatus.Corrupt;

                        return returnCode;
                    }
                case ReturnCode.Cancel:
                    Report.ReportProgress(new bgwShowFixError("Cancelled"));
                    return returnCode;
                default:
                    Report.ReportProgress(new bgwShowFixError("Failed"));
                    return returnCode;
            }


            // Check the files that we found that where used to fix this file, and if they not listed as correct files, they can be set to be deleted.
            FixFileUtils.CheckFilesUsedForFix(fixFiles, fileProcessQueue, true);

            totalFixed++;
            return ReturnCode.Good;
        }


    }
}
