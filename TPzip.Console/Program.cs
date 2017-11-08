using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TPzip.Core;

namespace TPzip.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            AutoZipper autoZipper = new AutoZipper();

            autoZipper.Test();

            System.Console.ReadKey();
        }
    }
}
