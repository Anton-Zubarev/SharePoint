# Загрузка файлов из Sharepoint или с диска 

Позволяет считать файлы, включая подпапки в иерархическую структуру FilesTree. 
Имеет ряд хелперов для упраления - linq запросы. Поможет преобразовать 
структуру в плоский список файлов. Может создать zip из FileTree.

# FilesTree структура

``` java
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
```

# Зависимости

 * Microsoft.SharePointOnline.CSOM
 * System.IO.Compression
 * System.IO.Compression.ZipFile

 # Пример

``` java
var fileTree1 = FilesDownloader.DownloadFilesFromFs("d:\\Personal\\VSProjects\\SharePoint\\SharePoint\\");
var fileTree2 = FilesDownloader.DownloadFilesFromFs("d:\\Personal\\VSProjects\\SharePoint\\SharePoint\\", true,
    (x) =>
    {
        return Path.GetFileName(x).EndsWith(".cs");
    });
byte[] zipBytes = fileTree2.CreateZipFromStructure();
```

# Пример

``` java
var ww1_1 = fileTree.Where(x => x.FileName.Contains("aaa"));

var ww2_2 = ww1_1.Any(x => x.FileName.Contains("aaa"));

var ww3_1 = fileTree.FirstOrDefault(x => x.FileName.Contains("aaa"));

var ww4_2 = fileTree.Count(x => x.FileName.Contains("aba"));

var ww5 = fileTree.Flatten(x => x.Children).ToList();

var ww6 = fileTree.FlattenFiles().ToList();
```