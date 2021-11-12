using Accounting.Context;

namespace Accounting.Models
{
    public class HomeModel
    {
        public float? Bill { get; set; }

        public Transaction[] Transactions { get; set; }
    }
}