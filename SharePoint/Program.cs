using FilesOperations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePoint
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FilesTree spResponse = new FilesTree();
            spResponse.FileDataList = new List<FileData>();
            spResponse.FileDataList.Add(new FileData() { FileName="File1a.xls" });
            spResponse.FileDataList.Add(new FileData() { FileName = "File1b.xls" });
            spResponse.Children = new List<FilesTree>();

            FilesTree spResponseChild = new FilesTree();
            spResponseChild.FileDataList = new List<FileData>();
            spResponseChild.FileDataList.Add(new FileData() { FileName = "File1aa.xls" });
            spResponseChild.FileDataList.Add(new FileData() { FileName = "File1ab.xls" });

            spResponse.Children.Add(spResponseChild);

            spResponseChild = new FilesTree();
            spResponseChild.FileDataList = new List<FileData>();
            spResponseChild.FileDataList.Add(new FileData() { FileName = "File1aaa.xls" });
            spResponseChild.FileDataList.Add(new FileData() { FileName = "File1aba.xls" });

            spResponse.Children.Add(spResponseChild);

            var spResponseChild1 = new FilesTree();
            spResponseChild1.FileDataList = new List<FileData>();
            spResponseChild1.FileDataList.Add(new FileData() { FileName = "File2aaa.xls" });
            spResponseChild1.FileDataList.Add(new FileData() { FileName = "File2aba.xls" });

            spResponseChild.Children = new List<FilesTree> { spResponseChild1 };


            var ww1_1 = spResponse.Where(x => x.FileName.Contains("aaa"));
            var ww1_2 = spResponse.Where(x => x.FileName.Contains("2aaa"));

            var ww2_1 = ww1_1.Any(x => x.FileName.Contains("aa"));
            var ww2_2 = ww1_1.Any(x => x.FileName.Contains("aaa"));
            var ww2_3 = spResponse.Any(x => x.FileName.Contains("aba"));
            var ww2_4 = spResponse.Any(x => x.FileName.Contains("none"));

            var ww3_1 = spResponse.FirstOrDefault(x => x.FileName.Contains("aaa"));
            var ww3_2 = spResponse.FirstOrDefault(x => x.FileName.Contains("ab"));
            var ww3_3 = spResponse.FirstOrDefault(x => x.FileName.Contains("aa"));
            var ww3_4 = spResponse.FirstOrDefault(x => x.FileName.Contains("aba"));
            var ww3_5 = spResponse.FirstOrDefault(x => x.FileName.Contains("none"));
            var ww3_6 = spResponse.FirstOrDefault();

            var ww4_1 = spResponse.Count(x => x.FileName.Contains("1a"));
            var ww4_2 = spResponse.Count(x => x.FileName.Contains("aba"));
            var ww4_3 = spResponse.Count(x => x.FileName.Contains("none"));
            var ww4_4 = spResponse.Count();

            var ww4_5 = ww1_1.Count(x => x.FileName.Contains("1af"));
            var ww4_6 = ww1_1.Count();

            var ww5 = spResponse.Flatten(x => x.Children).ToList();

            var ww6 = spResponse.FlattenFiles().ToList();


            var ttt1 = FilesDownloader.DownloadFilesFromFs("d:\\Personal\\VSProjects\\SharePoint\\SharePoint\\");
            var ttt2 = FilesDownloader.DownloadFilesFromFs("d:\\Personal\\VSProjects\\SharePoint\\SharePoint\\", true,
                (x) =>
                {
                    return Path.GetFileName(x).EndsWith(".cs");
                });
        }
    }
}
