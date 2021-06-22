using System;
using Accounting.Events;

namespace Accounting
{
    public class Income
    {
        public Guid Id { get; set; }
        public string Owner { get; set; }
        public decimal TotalIncome { get; set; }


        public void Apply(AccountCreated created)
        {
            Id = created.AccountId;
            Owner = created.Owner;
            TotalIncome = created.StartingBalance;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Income counter created for {Owner} with TotalIncome of {TotalIncome.ToString("C")}");
        }

        public void Apply(AccountCredited credit)
        {
            credit.Apply(this);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Crediting {Owner} {credit.Amount.ToString("C")}");
        }


    }
}