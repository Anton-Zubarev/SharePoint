using FilesOperations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharePoint
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FilesTree fileTree = new FilesTree();
            fileTree.FileDataList = new List<FileData>();
            fileTree.FileDataList.Add(new FileData() { FileName="File1a.xls" });
            fileTree.FileDataList.Add(new FileData() { FileName = "File1b.xls" });
            fileTree.Children = new List<FilesTree>();

            FilesTree fileTreeChild = new FilesTree();
            fileTreeChild.FileDataList = new List<FileData>();
            fileTreeChild.FileDataList.Add(new FileData() { FileName = "File1aa.xls" });
            fileTreeChild.FileDataList.Add(new FileData() { FileName = "File1ab.xls" });

            fileTree.Children.Add(fileTreeChild);

            fileTreeChild = new FilesTree();
            fileTreeChild.FileDataList = new List<FileData>();
            fileTreeChild.FileDataList.Add(new FileData() { FileName = "File1aaa.xls" });
            fileTreeChild.FileDataList.Add(new FileData() { FileName = "File1aba.xls" });

            fileTree.Children.Add(fileTreeChild);

            var fileTreeChild1 = new FilesTree();
            fileTreeChild1.FileDataList = new List<FileData>();
            fileTreeChild1.FileDataList.Add(new FileData() { FileName = "File2aaa.xls" });
            fileTreeChild1.FileDataList.Add(new FileData() { FileName = "File2aba.xls" });

            fileTreeChild.Children = new List<FilesTree> { fileTreeChild1 };


            var ww1_1 = fileTree.Where(x => x.FileName.Contains("aaa"));
            var ww1_2 = fileTree.Where(x => x.FileName.Contains("2aaa"));

            var ww2_1 = ww1_1.Any(x => x.FileName.Contains("aa"));
            var ww2_2 = ww1_1.Any(x => x.FileName.Contains("aaa"));
            var ww2_3 = fileTree.Any(x => x.FileName.Contains("aba"));
            var ww2_4 = fileTree.Any(x => x.FileName.Contains("none"));

            var ww3_1 = fileTree.FirstOrDefault(x => x.FileName.Contains("aaa"));
            var ww3_2 = fileTree.FirstOrDefault(x => x.FileName.Contains("ab"));
            var ww3_3 = fileTree.FirstOrDefault(x => x.FileName.Contains("aa"));
            var ww3_4 = fileTree.FirstOrDefault(x => x.FileName.Contains("aba"));
            var ww3_5 = fileTree.FirstOrDefault(x => x.FileName.Contains("none"));
            var ww3_6 = fileTree.FirstOrDefault();

            var ww4_1 = fileTree.Count(x => x.FileName.Contains("1a"));
            var ww4_2 = fileTree.Count(x => x.FileName.Contains("aba"));
            var ww4_3 = fileTree.Count(x => x.FileName.Contains("none"));
            var ww4_4 = fileTree.Count();

            var ww4_5 = ww1_1.Count(x => x.FileName.Contains("1af"));
            var ww4_6 = ww1_1.Count();

            var ww5 = fileTree.Flatten(x => x.Children).ToList();

            var ww6 = fileTree.FlattenFiles().ToList();


            var ttt1 = FilesDownloader.DownloadFilesFromFs("d:\\Personal\\VSProjects\\SharePoint\\SharePoint\\");
            var ttt2 = FilesDownloader.DownloadFilesFromFs("d:\\Personal\\VSProjects\\SharePoint\\SharePoint\\", true,
                (x) =>
                {
                    return Path.GetFileName(x).EndsWith(".cs");
                });
            byte[] zipBytes = ttt2.CreateZipFromStructure();
        }
    }
}
