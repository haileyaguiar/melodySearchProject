using System;
using System.Collections.Generic;

namespace melodySearchProject.Models;

public partial class MeiFile
{
    public int FileId { get; set; }

    public string? FileName { get; set; }

    public string? FileContent { get; set; }
}
