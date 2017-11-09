using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPzip.Core;
using System.IO;

namespace TPzip.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            AutoZip autoZip = new AutoZip();
            autoZip.Start();
            System.Console.ReadKey();
        }
    }
}
