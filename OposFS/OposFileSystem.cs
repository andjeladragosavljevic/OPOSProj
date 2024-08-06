using DokanNet;
using OposScheduler;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using FileAccess = DokanNet.FileAccess;

namespace OposFS
{
    /// <summary>
    /// Implemntation of IDokanOperations interface, with image processing using custom task scheduler,
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class OposFileSystem : IDokanOperations
    {
        private static readonly string _pathName = "input";
        /// <summary>
        /// Function to check if the work with the file is done.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool FileIsDone(string path)
        {
            try
            {
                using (File.Open(path, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }

            return true;
        }

        public OposFileSystem() { }

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode,
              FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            var result = DokanResult.Success;

            if (info.IsDirectory)
            {
                try
                {
                    switch (mode)
                    {
                        case FileMode.Open:
                            if (!Directory.Exists(fileName))
                            {
                                return DokanResult.PathNotFound;

                            }

                            break;

                        case FileMode.CreateNew:
                            if (Directory.Exists(fileName))
                                return DokanResult.FileExists;

                            Directory.CreateDirectory(fileName);
                            break;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return DokanResult.AccessDenied;
                }
            }
            else
            {
                var pathExists = true;
                var pathIsDirectory = false;

                try
                {
                    pathExists = (Directory.Exists(fileName) || File.Exists(fileName));
                    pathIsDirectory = pathExists && File.GetAttributes(fileName).HasFlag(FileAttributes.Directory);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                switch (mode)
                {
                    case FileMode.Open:

                        if (pathExists)
                        {

                            if (access.Equals(FileAccess.ReadAttributes) || pathIsDirectory)
                            {

                                info.IsDirectory = pathIsDirectory;
                                info.Context = new object();

                                return DokanResult.Success;
                            }
                        }
                        else
                        {
                            return DokanResult.FileNotFound;
                        }
                        break;

                    case FileMode.CreateNew:
                        if (pathExists)
                            return DokanResult.FileExists;
                        break;

                }

                try
                {

                    info.Context = new FileStream(fileName, mode,
                       System.IO.FileAccess.ReadWrite, share, 4096, options);

                    bool fileCreated = mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.OpenOrCreate;
                    if (fileCreated)
                    {

                        File.SetAttributes(fileName, attributes);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    if (info.Context is FileStream fileStream)
                    {

                        fileStream.Dispose();
                        info.Context = null;
                    }
                    return DokanResult.AccessDenied;
                }
                catch (DirectoryNotFoundException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return DokanResult.PathNotFound;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            return result;
        }


        public void Cleanup(string fileName, IDokanFileInfo info)
        {


            (info.Context as FileStream)?.Dispose();
            info.Context = null;

            if (info.DeleteOnClose)
            {
                if (info.IsDirectory)
                {
                    Directory.Delete(fileName);
                }
                else
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (UnauthorizedAccessException ex) { Console.WriteLine($"Error: {ex.Message}"); }
                    catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
                }
            }

        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            (info.Context as FileStream)?.Dispose();
            info.Context = null;

        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            bytesRead = 0;

            if (info.Context == null)
            {
                if (FileIsDone(fileName))
                {
                    try
                    {
                        using FileStream? stream = new(fileName, FileMode.Open, System.IO.FileAccess.Read);
                        stream.Position = offset;
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (IOException ex) { Console.WriteLine($"Error: {ex.Message}"); }
                }
            }
            else
            {
                if (info.Context is FileStream stream)
                {
                    lock (stream)
                    {
                        stream.Position = offset;
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                }
            }
            if (fileName.Contains(_pathName))
            {
                if (FileIsDone(fileName))
                {

                    Sharpen sharpen = new();
                    MyScheduler scheduler = new(2, 4);
                    TaskFactory factory = new(scheduler);
                    Sharpen.Parallelism = 1;
                    sharpen.SharpeningAsync(fileName, factory);
                }
            }

            return DokanResult.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            bytesWritten = 0;
            var append = offset == -1;
            if (info.Context == null)
            {
                using FileStream? stream = new(fileName, append ? FileMode.Append : FileMode.Open, System.IO.FileAccess.Write);
                lock (stream)
                {
                    if (!append)
                    {
                        stream.Position = offset;
                    }

                    var bytesToCopy = buffer.Length;
                    stream.Write(buffer, 0, bytesToCopy);
                    bytesWritten = bytesToCopy;
                }
            }
            else
            {
                if (info.Context is FileStream stream)
                {
                    lock (stream)
                    {
                        if (append)
                        {
                            if (stream.CanSeek)
                            {
                                stream.Seek(0, SeekOrigin.End);
                            }
                            else
                            {
                                bytesWritten = 0;
                                return DokanResult.Error;
                            }
                        }
                        else
                        {
                            stream.Position = offset;
                        }
                        var bytesToCopy = buffer.Length;
                        stream.Write(buffer, 0, bytesToCopy);
                        bytesWritten = bytesToCopy;
                    }
                }
            }

            return DokanResult.Success;
        }



        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {

            var filePath = fileName;
            FileSystemInfo finfo = new FileInfo(filePath);
            if (!finfo.Exists)
                finfo = new DirectoryInfo(filePath);

            fileInfo = new FileInformation
            {
                FileName = fileName,
                Attributes = finfo.Attributes,
                CreationTime = finfo.CreationTime,
                LastAccessTime = finfo.LastAccessTime,
                LastWriteTime = finfo.LastWriteTime,
                Length = (finfo as FileInfo)?.Length ?? 0,
            };
            return DokanResult.Success;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            throw new NotImplementedException();

        }





        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            if (Directory.Exists(fileName) || File.GetAttributes(fileName).HasFlag(FileAttributes.Directory))
                return DokanResult.AccessDenied;

            if (!File.Exists(fileName))
                return DokanResult.FileNotFound;


            return DokanResult.Success;

        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            return
                Directory.EnumerateFileSystemEntries(fileName).Any()
                    ? DokanResult.DirectoryNotEmpty
                    : DokanResult.Success;

        }




        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            try
            {
                ((FileStream)(info.Context)).SetLength(length);
                return DokanResult.Success;
            }
            catch (IOException ex) { Console.WriteLine($"Error: {ex.Message}"); return DokanResult.DiskFull; }
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.Error;


        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.Error;

        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalBytes, out long totalFreeBytes, IDokanFileInfo info)
        {
            freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 512 * 1024 * 1024;
            return DokanResult.Success;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
            out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {

            volumeLabel = "DOKAN";
            fileSystemName = "OPOS";
            maximumComponentLength = 256;
            features = FileSystemFeatures.None;

            return DokanResult.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections,
            IDokanFileInfo info)
        {

            try
            {

                security = info.IsDirectory
                    ? new DirectoryInfo(fileName).GetAccessControl()
                    : new FileInfo(fileName).GetAccessControl();

                return DokanResult.Success;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                security = null;
                return DokanResult.AccessDenied;
            }
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
            IDokanFileInfo info)
        {
            try
            {
                if (info.IsDirectory)
                {
                    new DirectoryInfo(path: fileName).SetAccessControl((DirectorySecurity)security);

                }
                else
                {
                    new FileInfo(fileName: fileName).SetAccessControl((FileSecurity)security);

                }

                return DokanResult.Success;
            }
            catch (UnauthorizedAccessException ex) { Console.WriteLine($"Error: {ex.Message}"); return DokanResult.AccessDenied; }
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation>? files,
            IDokanFileInfo info)
        {
            try
            {
                Regex regex = new("." + @searchPattern);

                files = new DirectoryInfo(fileName)
                     .EnumerateFileSystemInfos()
                     .Where(finfo => regex.IsMatch(finfo.Name))
                   .Select(finfo => new FileInformation
                   {
                       Attributes = finfo.Attributes,
                       CreationTime = finfo.CreationTime,
                       LastAccessTime = finfo.LastAccessTime,
                       LastWriteTime = finfo.LastWriteTime,
                       Length = (finfo as FileInfo)?.Length ?? 0,
                       FileName = finfo.Name
                   }).ToArray();
            }
            catch (UnauthorizedAccessException ex) { Console.WriteLine($"Error: {ex.Message}"); files = null; }


            return DokanResult.Success;
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info) => NtStatus.Success;
        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
            DateTime? lastWriteTime, IDokanFileInfo info) => NtStatus.Error;
        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info) => NtStatus.Error;
        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info) => NtStatus.Success;
        public NtStatus Mounted(IDokanFileInfo info) => NtStatus.Success;
        public NtStatus Unmounted(IDokanFileInfo info) => DokanResult.Success;
        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info) => NtStatus.Success;
        public static NtStatus FindStreams(string fileName, IntPtr enumContext, out string streamName, out long streamSize,
       IDokanFileInfo info)
        {
            streamName = string.Empty;
            streamSize = 0;
            return DokanResult.NotImplemented;
        }
        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = Array.Empty<FileInformation>();
            return DokanResult.NotImplemented;
        }

    }
}