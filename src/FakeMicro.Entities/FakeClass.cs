using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
namespace FakeMicro.Entities
{
    [SqlSugar.SugarTable("fake_class")]
    [GenerateSerializer]

    public class FakeClass :IAuditable, ISoftDeletable
    {
        [SqlSugar.SugarColumn(IsIdentity =true, IsPrimaryKey =true,ColumnName ="id")]
        [Id(0)]
        public long Id { get; set; }
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "created_at")]
        [Id(1)]
        public DateTime created_at { get; set; }
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "updated_at")]
        [Id(2)]
        public DateTime? updated_at { get; set; }

        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "is_deleted")]
        [Id(3)]
        public bool is_deleted { get; set; }
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "deleted_at")]
        [Id(4)]
        public DateTime? deleted_at { get; set; }
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "name")]
        [Id(5)]
        public string Name { get; set; }
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "description ")]
        [Id(6)]
        public string description { get; set; }
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "header_teacher")]
        [Id(7)]
        public string HeadTeacher { get; set; }
      
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "code")]
        [Id(8)]
        public string code { get; set; }
      
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "grade")]
        [Id(9)]
        public string Grade { get; set; }
       
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "grade_name")]
        [Id(10)]
        public string GradeName { get; set; }
        [Id(11)]

        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "created_by")]
        public string created_by { get; set; }

        [Id(12)]

        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "updated_by")]
        public string updated_by { get; set; }

    }
}
