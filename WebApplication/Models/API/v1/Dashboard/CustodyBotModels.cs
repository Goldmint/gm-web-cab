using Goldmint.DAL.CustodyBotModels;

namespace Goldmint.WebApplication.Models.API.v1.Dashboard
{

    public class BotsInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string SumusAddress { get; set; }
    }

    public class BotsPagerView : BasePagerView<BotsInfo>
    {
    }

    public class PawnsPagerView : BasePagerView<Custodies>
    {
    }

}
