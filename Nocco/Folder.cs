using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nocco
{
    public class Folder
    {
        public Folder Parent { get; set; }
        public string Name { get; set; }
        public List<Folder> Folders { get; set; }
        public List<FileUrl> Files { get; set; }
    }
}
