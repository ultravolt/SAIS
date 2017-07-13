using System.IO;

namespace SAIS
{
    public class t_moddir 
    {

        public t_moddir(DirectoryInfo directoryInfo)
        {
            this.DirectoryInfo = directoryInfo;
        }

        public DirectoryInfo DirectoryInfo { get; set; }
        public string dir { get { return this.DirectoryInfo.FullName; } }
        public string name { get { return this.name; } }
    }
}