using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDesk.Options;
using System.Globalization;

namespace Vosen.MAL
{
    class Program
    {
        static void Main(string[] args)
        {
            bool clean = false;
            bool fill = false;
            string db = null;
            bool help = false;
            int limit = -1;
            OptionSet op = new OptionSet()
            {
                { 
                    "clean",
                    "delete all entries from db.",
                    s => clean = s != null
                },
                { 
                    "f|fill",
                    "filling mode, scrap all not yet scrapped profiles.",
                    s => fill = s != null
                },
                { 
                    "d|db=",
                    "database name, default is mal.db.",
                    s => db = s 
                },
                {
                    "l|limit=",
                    "limit amount of concurrent scrapping tasks.",
                    s => limit = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                },
                { 
                    "h|help",
                    "show this message.",
                    s => help = s != null
                }
            };

            try
            {
                op.Parse(args);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                return;
            }

            if (help)
            {
                ShowHelp(op);
                return;
            }

            if (limit == 0 || !fill && !clean || clean && fill)
            {
                ShowError("incorrect arguments.");
                return;
            }
            if (fill)
            {
                new Scrapper() { ConcurrencyLimit = limit }.Run();
                Console.WriteLine("Finished querying.");
            }
            else if (clean)
            {
                Scrapper.CleanDB(db);
                Console.WriteLine("Finished cleaning.");
            }
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: Scrapper [OPTIONS]");
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        private static void ShowError(string message)
        {
            Console.Write("Scrapper: ");
            Console.WriteLine(message);
            Console.WriteLine("Try `Scrapper --help' for more information.");
        }

    }
}
