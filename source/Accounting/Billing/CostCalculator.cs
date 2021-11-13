using System;

namespace Accounting.Billing
{
    public interface ICostCalculator
    {
        float GetAssignCost();

        float GetCompleteCost();
    }

    public class CostCalculator : ICostCalculator
    {
        public float GetAssignCost()
        {
            return new Random().Next(10, 20);
        }

        public float GetCompleteCost()
        {
            return new Random().Next(20, 40);
        }
    }
}