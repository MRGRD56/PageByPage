using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PageByPage.Extensions
{
    public static class FileInfoExtensions
    {
        public static string GetFileNameWithoutExtension(this FileInfo fileInfo)
        {
            return fileInfo.Exists
                ? Path.GetFileNameWithoutExtension(fileInfo.FullName)
                : throw new FileNotFoundException();
        }
    }
}
