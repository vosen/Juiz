using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Vosen.MAL.Tests
{
    static class Helper
    {
        internal static string LoadFile(string path)
        {
            using (var stream = new StreamReader(path, Encoding.UTF8))
            {
                return stream.ReadToEnd();
            }
        }
    }
}
