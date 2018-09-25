
namespace Goldmint.WebApplication.Models.API.v1.Dashboard
{

    public class BotsInfo
    {
        public long Id { get; set; }

        public string Name { get; set; }

        //public ClientRole Role { get; set; }
      
        public string SumusAddress { get; set; }

        //public long OrgId { get; set; }
    }

    public class BotsPagerView : BasePagerView<BotsInfo>
    {
    }
}
