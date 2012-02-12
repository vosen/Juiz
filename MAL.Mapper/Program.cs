using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using Dapper;
using System.IO;
using System.Reflection;
using NDesk.Options;
using System.Globalization;

namespace Vosen.MAL
{
    class Program : ProgramBase
    {
        static void Main(string[] args)
        {
            int mode = 0;
            int start = -1, end = -1, limit = -1;
            string db = "mal.db";
            bool log = false;
            OptionSet modes = new OptionSet()
            {
                {
                    "f|fill",
                    "try to fill gaps between scanned ids",
                    s => mode += 2
                },
                {
                    "r|repair",
                    "rescan ids that failed last scan",
                    s => mode += 4
                },
                {
                    "n|next",
                    "continue scanning ids, starting from the highest",
                    s => mode += 8
                },
                {
                    "h|help",
                    "show this message",
                    s => mode += 16
                },
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
                    "logs the queries, filename is \"mal.mapper date time\"",
                    s => log = true
                },
                { 
                    "s|start=",
                    "starting index, pair with -e",
                    s => start = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                },
                { 
                    "e|end=",
                    "ending index, pair with -s",
                    s => end = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                }
            };

            try
            {
                ParseJoinedOptions(args, options, modes);
            }
            catch(Exception ex)
            {
                ShowError(ex.Message, "Mapper");
                return;
            }

            switch (mode)
            {
                case 2:
                    new FillingMapper(start, end, log, limit, db).Run();
                    return;
                case 4:
                    new RepairMapper(start, end, log, limit, db).Run();
                    return;
                case 8:
                    new ContinueMapper(start, end, log, limit, db).Run();
                    return;
                case 16:
                    ShowHelp(modes, options, "Mapper");
                    return;
            }
            ShowHelp(modes, options, "Mapper");
        }
    }
}
