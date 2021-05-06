using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCADPluginExample;

namespace XBimApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World");

            var commands = new BimCommands();
            commands.CreateBim();

            Console.ReadKey();
        }
    }
}
