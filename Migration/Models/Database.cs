using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haukcode.Migration.Models
{
    public class Database
    {
        //
        // Summary:
        //     The list of tables in the database.
        public IEnumerable<Table> Tables { get; set; }

        public bool IsEmpty()
        {
            return !Tables.Any();
        }

        public Database()
        {
        }

        public Database(DatabaseModel source)
        {
            Tables = source.Tables.Select(x => new Table(x)).OrderBy(x => x.Name).ToList();
        }
    }
}
