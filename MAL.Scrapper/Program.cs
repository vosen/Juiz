using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDesk.Options;
using System.Globalization;

namespace Vosen.MAL
{
    class Program : ProgramBase
    {
        static void Main(string[] args)
        {
            int mode = 0;
            string db = "mal.db";
            int limit = -1;
            bool log = false;
            OptionSet modes = new OptionSet()
            {      
                {
                    "f|fill",
                    "filling mode, scrap all not yet scrapped profiles",
                    s => mode += 1
                },
                {
                    "clean",
                    "delete all entries from db",
                    s => mode += 2
                },
                { 
                    "h|help",
                    "show this message",
                    s => mode += 4
                }
            };

            OptionSet options = new OptionSet()
            {
                { 
                    "d|db=",
                    "database name, default is mal.db",
                    s => db = s 
                },
                {
                    "c|concurreny=",
                    "limit amount of concurrent scrapping tasks",
                    s => limit = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                },
                {
                    "l|log",
                    "logs the queries, filename is \"mal.scrapper date time\"",
                    s => log = true
                },
            };

            try
            {
                ParseJoinedOptions(args, options, modes);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, "Scrapper");
                return;
            }

            switch(mode)
            {
                case 1:
                    new Scrapper(log, limit, db).Run();
                    return;
                case 2:
                    Scrapper.CleanDB(db);
                    Console.WriteLine("Finished cleaning.");
                    return;
                case 4:
                    ShowHelp(modes, options, "Scrapper");
                    return;
            }
            ShowHelp(modes, options, "Scrapper");
        }
    }
}
