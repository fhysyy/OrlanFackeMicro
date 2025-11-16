using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 分页API响应模型
    /// 格式符合: { success:true,code:200,data=new List<dynamic>, errMsg="", totalNum=00}
    /// </summary>
    [GenerateSerializer]
    public class PaginatedApiResponse<T>
    {
        [Id(0)]
        public bool success { get; set; } = true;
        
        [Id(1)]
        public int code { get; set; } = 200;
        
        [Id(2)]
        public List<T> data { get; set; } = new List<T>();
        
        [Id(3)]
        public string errMsg { get; set; } = "";
        
        [Id(4)]
        public long totalNum { get; set; } = 0;
    }
}