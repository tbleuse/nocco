using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nocco
{
    public class LinkFileToClass
    {
        public List<String> Folders { get; set; }
        public String FileTypeName { get; set; }
        public String MyPath { get; set; }
        public String PathToRoot { get; set; }

        public LinkFileToClass()
        {
            Folders = new List<string>();
        }

        public LinkFileToClass(String path)
        {
            MyPath = path;
            string[] toBase = path.Split(new char[] { '\\' });
            if (toBase.Length > 0)
            {
                var types = toBase[toBase.Length - 1].Split(new char[] { '.' });
                if (types.Length > 0)
                {
                    FileTypeName = types[0].ToUpper();
                }
            }
            var dirs = Path.GetDirectoryName(path).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            Folders = dirs.ToList();
            var depth = dirs.Length;
            PathToRoot = string.Concat(Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth));
        }
    }
}
