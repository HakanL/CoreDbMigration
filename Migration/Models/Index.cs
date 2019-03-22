using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haukcode.Migration.Models
{
    public class Index
    {
        //
        // Summary:
        //     The index name.
        public string Name { get; set; }

        //
        // Summary:
        //     The ordered list of columns that make up the index.
        public IEnumerable<string> Columns { get; set; }

        //
        // Summary:
        //     Indicates whether or not the index constrains uniqueness.
        public bool IsUnique { get; set; }

        //
        // Summary:
        //     The filter expression, or null if the index has no filter.
        public string Filter { get; set; }


        public Index()
        {
        }

        public Index(DatabaseIndex source)
        {
            Name = source.Name;
            Columns = source.Columns.Select(x => x.Name).ToList();
            IsUnique = source.IsUnique;
            Filter = source.Filter;
        }
    }
}
