using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Orleans;
using SqlSugar;
namespace FakeMicro.Entities.ManagerVersion
{
    [GenerateSerializer]

    public class ManagerVersion:IAuditable
    {
        [Id(0)]
        [SugarColumn(IsPrimaryKey = true, IsOnlyIgnoreInsert = true, ColumnName = "_id")]
        public string Id { get; set; }
     
        [Id(1)]
        public DateTime CreatedAt { get  ; set  ; }
        [Id(2)]
        public string created_by { get  ; set  ; }
        [Id(3)]
        public DateTime? UpdatedAt { get  ; set  ; }
        [Id(4)]
        public string? updated_by { get  ; set  ; }

        [Id(5)]
        public decimal count { get  ; set  ; }

       
    }
}
