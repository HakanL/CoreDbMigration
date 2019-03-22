using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haukcode.Migration.Models
{
    public class PrimaryKey
    {
        //
        // Summary:
        //     The name of the primary key.
        public string Name { get; set; }

        //
        // Summary:
        //     The ordered list of columns that make up the primary key.
        public IEnumerable<string> Columns { get; set; }


        public PrimaryKey()
        {
        }

        public PrimaryKey(DatabasePrimaryKey source)
        {
            Name = source.Name;
            Columns = source.Columns.Select(x => x.Name).ToList();
        }
    }
}
