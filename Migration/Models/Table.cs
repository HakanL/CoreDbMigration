using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haukcode.Migration.Models
{
    public class Table
    {
        //
        // Summary:
        //     The primary key of the table.
        public PrimaryKey PrimaryKey { get; set; }

        //
        // Summary:
        //     The ordered list of columns in the table.
        public IEnumerable<Column> Columns { get; set; }

        //
        // Summary:
        //     The list of unique constraints defined on the table.
        public IEnumerable<UniqueConstraint> UniqueConstraints { get; set; }

        //
        // Summary:
        //     The list of indexes defined on the table.
        public IEnumerable<Index> Indexes { get; set; }

        //
        // Summary:
        //     The list of foreign key constraints defined on the table.
        public IEnumerable<ForeignKey> ForeignKeys { get; set; }

        //
        // Summary:
        //     The name of the table.
        public string Name { get; set; }

        public Table()
        {
        }

        public Table(DatabaseTable source)
        {
            Name = source.Name;
            PrimaryKey = source.PrimaryKey != null ? new PrimaryKey(source.PrimaryKey) : null;
            Columns = source.Columns.Select(x => new Column(x)).ToList();
            UniqueConstraints = source.UniqueConstraints.Select(x => new UniqueConstraint(x)).OrderBy(x => x.Name).ToList();
            Indexes = source.Indexes.Select(x => new Index(x)).OrderBy(x => x.Name).ToList();
            ForeignKeys = source.ForeignKeys.Select(x => new ForeignKey(x)).OrderBy(x => x.Name).ToList();
        }
    }
}
