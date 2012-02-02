using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDesk.Options;

namespace Vosen.MAL
{
    public class ProgramBase
    {
        protected static void ParseJoinedOptions(string[] args, OptionSet set1, OptionSet set2)
        {
            var temp = new OptionSet();
            foreach (var s in set1)
                temp.Add(s);
            foreach (var s in set2)
                temp.Add(s);
            temp.Parse(args);
        }

        protected static void ShowHelp(OptionSet modes, OptionSet options, string appName)
        {
            Console.WriteLine("Usage: {0} [MODE] [OPTIONS]", appName);
            Console.WriteLine("Modes:");
            modes.WriteOptionDescriptions(Console.Out);
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        protected static void ShowError(string message, string appName)
        {
            Console.Write("{0}: ", appName);
            Console.WriteLine(message);
            Console.WriteLine("Try `{0} --help' for more information.", appName);
        }
    }
}
