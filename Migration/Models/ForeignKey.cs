using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haukcode.Migration.Models
{
    public class ForeignKey
    {
        //
        // Summary:
        //     The table to which the columns are constrained.
        public string PrincipalTable { get; set; }

        //
        // Summary:
        //     The ordered list of columns that are constrained.
        public IEnumerable<string> Columns { get; set; }

        //
        // Summary:
        //     The ordered list of columns in the Microsoft.EntityFrameworkCore.Scaffolding.Metadata.DatabaseForeignKey.PrincipalTable
        //     to which the Microsoft.EntityFrameworkCore.Scaffolding.Metadata.DatabaseForeignKey.Columns
        //     of the foreign key are constrained.
        public IEnumerable<string> PrincipalColumns { get; set; }

        //
        // Summary:
        //     The foreign key constraint name.
        public string Name { get; set; }

        //
        // Summary:
        //     The action performed by the database when a row constrained by this foreign key
        //     is deleted, or null if there is no action defined.
        public ReferentialAction? OnDelete { get; set; }


        public ForeignKey()
        {
        }

        public ForeignKey(DatabaseForeignKey source)
        {
            Name = source.Name;
            PrincipalTable = source.PrincipalTable.Name;
            PrincipalColumns = source.PrincipalColumns.Select(x => x.Name).ToList();
            Columns = source.Columns.Select(x => x.Name).ToList();
            OnDelete = source.OnDelete;
        }
    }
}
