using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Data.SQLite;
using Dapper;
using System.IO;
using System.Reflection;
using NDesk.Options;
using System.Globalization;

namespace Vosen.MAL
{
    class Program
    {
        static void Main(string[] args)
        {
            bool help = false;
            bool fill = false;
            int start = 0, end = 0, limit = -1;
            string db = null;
            OptionSet op = new OptionSet()
            {
                { 
                    "s|start=",
                    "starting index, pair with -e.",
                    s => start = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                },
                { 
                    "e|end=",
                    "ending index, pair with -s.",
                    s => end = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                },
                {
                    "f|fill",
                    "try to fill empty id ranges.",
                    s => fill = s != null
                },
                {
                    "l|limit=",
                    "limit amount of concurrent mapping tasks.",
                    s => limit = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                },
                { 
                    "d|db=",
                    "database name, default is mal.db.",
                    s => db = s 
                },
                { 
                    "h|help",
                    "show this message.",
                    s => help = s != null
                },
            };

            try
            {
                op.Parse(args);
            }
            catch(Exception ex)
            {
                ShowError(ex.Message);
                return;
            }

            if (help)
            {
                ShowHelp(op);
                return;
            }

            if ((start == 0 && !fill) || end < start || (start != 0 && fill) || limit == 0)
            {
                ShowError("incorrect arguments.");
                return;
            }

            Mapper mapper;
            if (fill)
                mapper = new FillingMapper() { ConcurrencyLimit = limit };
            else
                mapper = new Mapper(start, end) { ConcurrencyLimit = limit };
            if (db != null)
                mapper.DbName = db;
            mapper.Run();
            Console.WriteLine("Finished querying.");
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: Mapper [OPTIONS]");
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        private static void ShowError(string message)
        {
            Console.Write("Mapper: ");
            Console.WriteLine(message);
            Console.WriteLine("Try `Mapper --help' for more information.");
        }
    }
}
