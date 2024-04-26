﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI
{
    internal static class StringExtensions
    {
        public static string NormalizedPath(this string path) => path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        public static bool PathEquals(this string path1, string path2) =>
            path1.NormalizedPath().Equals(path2.NormalizedPath(), StringComparison.InvariantCultureIgnoreCase);
    }
}