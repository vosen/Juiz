﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MALContent
{
    public enum ExtractionResultType
    {
        Unknown = 0,
        Successs,
        MySQLError,
        InvalidUsername,
        ListIsPrivate,
        TooLarge
    }
}
