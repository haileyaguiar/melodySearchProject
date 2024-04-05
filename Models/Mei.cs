using System;
using System.Collections.Generic;

namespace melodySearchProject.Models;

public partial class Mei
{
    public int FileId { get; set; }

    public string? FileName { get; set; }

    public string? FileData { get; set; }

    public string? DownloadLink { get; set; }
}
