/*
# Загрузка файлов из Sharepoint или с диска 

Позволяет считать файлы, включая подпапки в иерархическую структуру FilesTree. 
Имеет ряд хелперов для упраления - linq запросы. Поможет преобразовать 
структуру в плоский список файлов. Может создать zip из FileTree.
*/

using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Security.Principal;

namespace FilesOperations
{

    internal static class FilesDownloader
    {
        #region Скачать с диска
        /// <summary>
        /// Скачать мета и данные файлов из папки на диске
        /// </summary>
        /// <param name="path"></param>
        /// <param name="subFolders"></param>
        /// <returns></returns>
        /// <remarks>Исключения в возварщаемом объекте</remarks>
        public static FilesTree DownloadFilesFromFs(string path, bool subFolders = false, Predicate<string> filter = null)
        {
            var filesTree = new FilesTree
            {
                Url = path
            };

            var currentFiles = Directory.GetFiles(path);
            if (currentFiles.Any())
            {
                filesTree.FileDataList = new List<FileData>();

                foreach (string filePath in currentFiles)
                {
                    if (filter != null && !filter(filePath)) continue;

                    var fileData = new FileData
                    {
                        FileName = Path.GetFileName(filePath)
                    };
                    try
                    {
                        fileData.FileContent = System.IO.File.ReadAllBytes(filePath);
                        var metadata = GetFileMetadata(filePath);
                        fileData.AuthorLogin = metadata.Owner;
                        fileData.EditorLogin = metadata.LastModifiedBy;
                        fileData.TimeCreated = metadata.TimeCreated;
                    }
                    catch(Exception ex)
                    {
                        fileData.Exception = ex;
                    }
                    filesTree.FileDataList.Add(fileData);
                }
            }

            if (subFolders)
            {
                var subDirectories = Directory.GetDirectories(path);
                if (subDirectories.Any())
                {
                    filesTree.Children = new List<FilesTree>();

                    foreach (string subDir in subDirectories)
                    {
                        var subDirEntry = DownloadFilesFromFs(subDir, subFolders);
                        filesTree.Children.Add(subDirEntry);
                    }
                }
            }

            return filesTree;
        }

        private static (string Owner, string LastModifiedBy, DateTime TimeCreated) GetFileMetadata(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                (string Owner, string LastModifiedBy, DateTime TimeCreated) securityObj = (GetFileOwner(filePath), GetLastModifiedUser(filePath), fileInfo.CreationTimeUtc);

                return securityObj;
            }
            catch (Exception)
            {
                return default;
            }
        }

        private static string GetFileOwner(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var security = fileInfo.GetAccessControl();
                var owner = security.GetOwner(typeof(NTAccount));

                return owner?.Value ?? "Неизвестен";
            }
            catch
            {
                return "Не удалось определить";
            }
        }

        private static string GetLastModifiedUser(string filePath)
        {
            try
            {
                string query = $"SELECT * FROM CIM_DataFile WHERE Name = '{filePath.Replace("\\", "\\\\")}'"; ;

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject file in searcher.Get())
                    {
                        var lastModifiedBy = file["LastModifiedBy"]?.ToString();
                        return !string.IsNullOrEmpty(lastModifiedBy)
                            ? lastModifiedBy
                            : "Неизвестен";
                    }
                }
            }
            catch
            { }
            return "Не удалось определить";
        }

        #endregion


        #region Скачать из Sharepoint

        /// <summary>
        /// Скачать мета и данные файлов из папки на шарике
        /// </summary>
        /// <param name="urlToFolder"></param>
        /// <param name="startSegmentCount"></param>
        /// <returns></returns>
        /// <remarks>Исключения в возварщаемом объекте</remarks>
        public static FilesTree DownloadFilesFromFolder(string urlToFolder, int startSegmentCount = 3, bool subFolders = false)
        {
            if (!Uri.IsWellFormedUriString(urlToFolder, UriKind.Absolute))
            {
                throw new ArgumentException($"Argument {nameof(urlToFolder)} is incorrect.");
            }

            var uri = new Uri(urlToFolder);
            var part1 = new UriBuilder(uri.Scheme, uri.Host, uri.Port, string.Join("/", uri.LocalPath.Trim(' ', '/').Split('/').Take(startSegmentCount))).Uri;
            var part2 = uri.LocalPath;
            var filesTree = DownloadFilesFromFolder(part1.OriginalString, part2, subFolders);
            if (filesTree != null) filesTree.Url = urlToFolder;
            return filesTree;
        }

        /// <summary>
        /// Получить файл из шарика
        /// </summary>
        /// <param name="urlToFile"></param>
        /// <param name="startSegmentCount"></param>
        /// <returns></returns>
        public static FileData GetSharePointFileDetails(string urlToFile, int startSegmentCount = 3)
        {
            if (!Uri.IsWellFormedUriString(urlToFile, UriKind.Absolute))
            {
                throw new ArgumentException($"Argument {nameof(urlToFile)} is incorrect.");
            }

            var uri = new Uri(urlToFile);
            var part1 = new UriBuilder(uri.Scheme, uri.Host, uri.Port, string.Join("/", uri.LocalPath.Trim(' ', '/').Split('/').Take(startSegmentCount))).Uri;
            var part2 = uri.LocalPath;
            var fileData = GetSharePointFileDetails(part1.OriginalString, part2);

            return fileData;
        }

        private static FileData GetSharePointFileDetails(string siteUrl, string fileUrl)
        {
            var fileInfo = new FileData();

            using (ClientContext context = new ClientContext(siteUrl))
            {
                Microsoft.SharePoint.Client.File spFile = context.Web.GetFileByServerRelativeUrl(fileUrl);

                context.Load(spFile);
                context.Load(spFile.Author);
                context.Load(spFile.ModifiedBy);
                context.Load(spFile, (f) => f.TimeCreated);

                context.ExecuteQuery();

                FileInformation fileData = Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, fileUrl);

                using (MemoryStream ms = new MemoryStream())
                {
                    fileData.Stream.CopyTo(ms);
                    fileInfo.FileContent = ms.ToArray();
                }

                fileInfo.FileName = spFile.Name;
                fileInfo.AuthorLogin = spFile.Author.LoginName;
                fileInfo.EditorLogin = spFile.ModifiedBy.LoginName;
                fileInfo.TimeCreated = spFile.TimeCreated;

                return fileInfo;
            }
        }


        /// <summary>
        /// Скачать мета и данные файлов из папки на шарике
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <param name="folderPath"></param>
        /// <example>var fff = SharePointFileDownloader.DownloadFilesFromFolder("http://sharepoint-1/sites/docs/02152012205328-96", "/sites/docs/02152012205328-96/Leesee/7f512cd6-f1ef-ed11-8194-00155db09b37/Лизинговые сделки/10609976/Договор по предмету лизинга 32820-2023");</example>
        /// <returns></returns>
        private static FilesTree DownloadFilesFromFolder(string siteUrl, string folderPath, bool subFolders = false)
        {
            using (ClientContext context = new ClientContext(siteUrl))
            {
                context.Load(context.Web);
                context.ExecuteQuery();

                Folder folder = context.Web.GetFolderByServerRelativeUrl(folderPath);
                context.Load(folder, u => u.Name);
                try
                {
                    context.ExecuteQuery();
                }
                catch (Exception ex)
                {
                    return null;
                }

                var filesTree = ProcessFolderRecursive(context, folder, subFolders);
                return filesTree;
            }
        }

        private static FilesTree ProcessFolderRecursive(ClientContext context, Folder folder, bool subFolders = false)
        {
            var filesTree = new FilesTree();
            var fileDatas = new List<FileData>();
            filesTree.FileDataList = fileDatas;
            filesTree.Url = folder.Name;
            filesTree.Children = new List<FilesTree>();

            try
            {
                context.Load(folder.Files);
                context.ExecuteQuery();

                foreach (Microsoft.SharePoint.Client.File file in folder.Files)
                {
                    var fileData = new FileData { FileName = file.Name };
                    try
                    {
                        context.Load(file.Author, u => u.LoginName); // sharepoint\\system
                        context.Load(file.ModifiedBy, u => u.LoginName);
                        context.Load(file, u => u.TimeCreated);
                        context.ExecuteQuery();

                        fileData.AuthorLogin = file.Author.LoginName?.ToLower();
                        fileData.EditorLogin = file.ModifiedBy.LoginName?.ToLower();
                        fileData.TimeCreated = file.TimeCreated;

                        FileInformation fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(context, file.ServerRelativeUrl);

                        using (MemoryStream fileStream = new MemoryStream())
                        {
                            fileInfo.Stream.CopyTo(fileStream);
                            fileData.FileContent = fileStream.ToArray();
                        }
                        fileDatas.Add(fileData);
                    }
                    catch (Exception ex)
                    {
                        fileData.Exception = ex;
                    }
                }

                if (subFolders)
                {
                    context.Load(folder.Folders);
                    context.Load(folder, u => u.Name);
                    context.ExecuteQuery();

                    foreach (Folder subFolder in folder.Folders)
                    {
                        filesTree.Children.Add(ProcessFolderRecursive(context, subFolder, subFolders));
                    }
                }
            }
            catch (Exception ex)
            {
                filesTree.Exception = ex;
            }
            return filesTree;
        }

        #endregion
    }

    public static class FilesTreeUtil
    {
        const string AUTHORS = "autors";
        const string EDITORS = "editors";
        const string AUTHORS_EDITORS = "autors_editors";

        public static uint GetCountOfFiles(this FilesTree node)
        {
            return node.Count(x => !x.IsError);
        }

        public static List<string> GetAllUniqAuthors(this FilesTree node)
        {
            HashSet<string> actors = new HashSet<string>();
            GetAllUniqAuthorsInternal(node, actors, AUTHORS);
            return actors.ToList();
        }

        public static List<string> GetAllUniqEditors(this FilesTree node)
        {
            HashSet<string> actors = new HashSet<string>();
            GetAllUniqAuthorsInternal(node, actors, EDITORS);
            return actors.ToList();
        }

        public static List<string> GetAllUniqActors(this FilesTree node)
        {
            HashSet<string> actors = new HashSet<string>();
            GetAllUniqAuthorsInternal(node, actors, AUTHORS_EDITORS);
            return actors.ToList();
        }

        static void GetAllUniqAuthorsInternal(FilesTree node, HashSet<string> res, string who)
        {
            node.FileDataList.ForEach(line =>
            {
                switch (who)
                {
                    case AUTHORS: res.Add(line.AuthorLogin); break;
                    case EDITORS: res.Add(line.EditorLogin); break;
                    case AUTHORS_EDITORS: res.Add(line.AuthorLogin); res.Add(line.EditorLogin); break;
                }
            });
            foreach (var childNode in node.Children)
            {
                GetAllUniqAuthorsInternal(childNode, res, who);
            }
        }
    }

    public static class FilesTreeLinq
    {
        public static IEnumerable<T> Flatten<T>(this T root, Func<T, IEnumerable<T>> getChildren)
        {
            yield return root;
            foreach (var child in getChildren(root) ?? new List<T>())
            {
                foreach (var grandChild in child.Flatten(getChildren))
                {
                    yield return grandChild;
                }
            }
        }

        public static IEnumerable<FilesTree> Flatten(this FilesTree root)
        {
            return root.Flatten(x => x.Children);
        }

        public static IEnumerable<FileData> FlattenFiles(this FilesTree root)
        {
            return root
                .Flatten()
                .Select(x => x.FileDataList)
                .SelectMany(x => x);
        }

        public static FilesTree Where(this FilesTree rootNode, Predicate<FileData> filter = null)
        {
            FilesTree filesTree = new FilesTree();

            WhereInternal(filesTree, rootNode, filter);

            return filesTree;
        }

        private static void WhereInternal(FilesTree filesTree, FilesTree node, Predicate<FileData> filter = null)
        {
            filesTree.Exception = node.Exception;
            filesTree.Url = node.Url;

            if (node.FileDataList != null)
            {
                filesTree.FileDataList = new List<FileData>();
                node.FileDataList.ForEach(line =>
                {
                    if (filter == null || filter(line))
                    {
                        filesTree.FileDataList.Add(line);
                    }
                });
            }

            if (node.Children != null && node.Children.Any())
            {
                filesTree.Children = new List<FilesTree>();

                foreach (var childNode in node.Children)
                {
                    var spResp = new FilesTree();
                    WhereInternal(spResp, childNode, filter);

                    if ((spResp.Children != null && spResp.Children.Any())
                        ||
                        (spResp.FileDataList != null && spResp.FileDataList.Any())
                    )
                    {
                        filesTree.Children.Add(spResp);
                    }
                }
            }
        }

        public static uint Count(this FilesTree rootNode, Predicate<FileData> filter = null)
        {
            return CountInternal(rootNode, filter);
        }

        private static uint CountInternal(FilesTree node, Predicate<FileData> filter = null)
        {
            uint cnt = 0;

            if (node.FileDataList != null)
            {
                node.FileDataList.ForEach(line =>
                {
                    if (filter == null || filter(line))
                    {
                        cnt++;
                    }
                });
            }

            if (node.Children != null)
            {
                foreach (var childNode in node.Children)
                {
                    cnt += CountInternal(childNode, filter);
                }
            }
            return cnt;
        }

        public static FilesTree FirstOrDefault(this FilesTree rootNode, Predicate<FileData> filter = null)
        {
            FilesTree filesTree = new FilesTree();

            return FirstOrDefault(filesTree, rootNode, filter);
        }

        private static FilesTree FirstOrDefault(FilesTree filesTree, FilesTree node, Predicate<FileData> filter = null)
        {
            filesTree.Exception = node.Exception;
            filesTree.Url = node.Url;

            if (node.FileDataList != null)
            {
                filesTree.FileDataList = new List<FileData>();
                foreach(var line in node.FileDataList)
                {
                    if (filter == null || filter(line))
                    {
                        filesTree.FileDataList.Add(line);
                        return filesTree;
                    }
                }
            }

            if (node.Children != null)
            {
                filesTree.Children = new List<FilesTree>();

                foreach (var childNode in node.Children)
                {
                    var spResp = new FilesTree();
                    filesTree.Children.Add(spResp);
                    FirstOrDefault(spResp, childNode, filter);
                    if (spResp.FileDataList.Any()) return spResp;
                }
            }

            return null;
        }

        public static bool Any(this FilesTree rootNode, Predicate<FileData> filter = null)
        {
            return AnyInternal(rootNode, filter);
        }

        private static bool AnyInternal(FilesTree node, Predicate<FileData> filter = null)
        {
            if (node.FileDataList != null)
            {
                foreach (var line in node.FileDataList)
                {
                    if (filter == null || filter(line))
                    {
                        return true;
                    }
                }
            }

            if (node.Children != null)
            {
                foreach (var childNode in node.Children)
                {
                    if (AnyInternal(childNode, filter)) return true;
                }
            }

            return false;
        }
    }

    public static class ZipArchiver
    {
        public static void CreateZipFromStructure(this FilesTree rootNode, string zipPath, bool updateZip = false)
        {
            using (var zipArchive = ZipFile.Open(zipPath, updateZip ? ZipArchiveMode.Update : ZipArchiveMode.Create))
            {
                ArchiveNode(zipArchive, rootNode, string.Empty);
            }
        }
        public static byte[] CreateZipFromStructure(this FilesTree rootNode, Predicate<FileData> filter = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    ArchiveNode(zipArchive, rootNode, string.Empty, filter);
                }

                return memoryStream.ToArray();
            }
        }

        private static void ArchiveNode(ZipArchive zipArchive, FilesTree node, string currentPath, Predicate<FileData> filter = null)
        {
            node.FileDataList.ForEach(line =>
            {
                if (!line.IsError && line.FileContent != null)
                {
                    if (filter == null || filter(line))
                    {
                        var fullPath = Path.Combine(currentPath, line.FileName);
                        var entry = zipArchive.CreateEntry(fullPath);

                        using (var entryStream = entry.Open())
                        {
                            entryStream.Write(line.FileContent, 0, line.FileContent.Length);
                        }
                    }
                }
            });
            foreach (var childNode in node.Children)
            {
                string childPath = Path.Combine(currentPath, childNode.Url);
                ArchiveNode(zipArchive, childNode, childPath, filter);
            }
        }
    }

    public class FilesTree
    {
        public string Url;
        public Exception Exception;

        public bool IsError => Exception != null;

        public List<FileData> FileDataList;
        public List<FilesTree> Children;
    }

    public class FileData
    {
        public string FileName { get; set; }
        public string AuthorLogin { get; set; }
        public string EditorLogin { get; set; }
        public DateTime TimeCreated { get; set; }
        public byte[] FileContent { get; set; }
        public Exception Exception { get; set; }
        public bool IsError => Exception != null;
    }
}