using System;

namespace Accounting.Events
{
    public abstract class Transaction
    {
        public Transaction()
        {
            Time = DateTimeOffset.UtcNow;
        }

        public Guid To { get; set; }
        public Guid From { get; set; }

        public string Description { get; set; }

        public DateTimeOffset Time { get; set; }

        public decimal Amount { get; set; }

        public abstract void Apply(Account account);
    }
}