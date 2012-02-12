﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Collections;

namespace Vosen.MAL
{
    internal class RepairMapper : Mapper
    {
        public RepairMapper(int startIndex, int stopIndex, bool logging, int concLimit, string dbname)
            : base(startIndex, stopIndex, logging, concLimit, dbname) { }

        public override void Run()
        {
            IEnumerable<int> ids;
            using (var conn = OpenConnection())
            {
                ids = conn.Query<long>("SELECT Id FROM Users WHERE Name IS NULL AND Id >= @min AND Id <= max", new { min = start, max = stop }).Select(l => (int)l);
            }
            // we've got a list of indices, now time to run.
            if(start != -1 && stop != -1)
                ids = ids.Where(id => id>= start && id <= stop);
            ConcurrentForeach(ids.ToList(), idx => SingleQuery(idx));
        }
    }
}
