using Accounting.Context;

namespace Accounting.Models
{
    public class StatisticsModel
    {
        public float? Bill { get; set; }

        public Payment[] Payments { get; set; }
    }
}