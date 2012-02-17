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
            int limit = -1;
            bool log = false;
            OptionSet modes = new OptionSet()
            {
                {
                    "r|repair",
                    "rescan ids that failed last scan",
                    s => mode += 1
                },
                {
                    "n|next",
                    "continue scanning ids, starting from the highest",
                    s => mode += 2
                },
                {
                    "h|help",
                    "show this message",
                    s => mode += 4
                },
            };

            OptionSet options = new OptionSet()
            {
                {
                    "c|concurreny=",
                    "limit amount of concurrent scrapping tasks",
                    s => limit = Int32.Parse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)
                },
                {
                    "l|log",
                    "logs the queries, filename is \"mal.mapper date time\"",
                    s => log = true
                }
            };

            try
            {
                ParseJoinedOptions(args, options, modes);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, "TitleScrapper");
                return;
            }

            switch (mode)
            {
                case 1:
                    new RepairScrapper(log, limit).Run();
                    return;
                case 2:
                    new ContinueScrapper(log, limit).Run();
                    return;
                case 4:
                    ShowHelp(modes, options, "TitleScrapper");
                    return;
            }
            ShowHelp(modes, options, "TitleScrapper");
        }
    }
}
