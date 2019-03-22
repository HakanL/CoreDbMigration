using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haukcode.Migration.Models
{
    public class UniqueConstraint
    {
        //
        // Summary:
        //     The name of the constraint.
        public string Name { get; set; }

        //
        // Summary:
        //     The ordered list of columns that make up the constraint.
        public IEnumerable<string> Columns { get; set; }

        public UniqueConstraint()
        {
        }

        public UniqueConstraint(DatabaseUniqueConstraint source)
        {
            Name = source.Name;
            Columns = source.Columns.Select(x => x.Name).ToList();
        }
    }
}
