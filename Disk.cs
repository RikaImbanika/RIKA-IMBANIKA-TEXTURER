using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_TEXTURER
{
    public static class Disk
    {
        public static string _programFiles;

        static Disk()
        {
            _programFiles = $"{Environment.CurrentDirectory}\\ProgramFiles\\";
        }
    }
}
