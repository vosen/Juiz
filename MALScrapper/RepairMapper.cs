using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Collections;

namespace Vosen.MAL
{
    internal class RepairMapper : Mapper
    {
        public RepairMapper() : base() { }

        public override void Run()
        {
            IEnumerable<int> ids;
            using (var conn = OpenConnection())
            {
                ids = conn.Query<long>("SELECT Id FROM Users WHERE Name IS NULL").Select(l => (int)l);
            }
            // we've got a list of indices, now time to run.
            ScanAndFill(ids);
        }
    }
}
