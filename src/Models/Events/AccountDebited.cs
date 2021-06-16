namespace Accounting.Events
{
    public class AccountDebited : Transaction
    {
        public override void Apply(Account account)
        {
            account.Balance -= Amount;
        }

        public AccountCredited ToCredit()
        {
            return new AccountCredited
            {
                Amount = Amount,
                To = From,
                From = To,
                Description = Description
            };
        }

        public override string ToString()
        {
            return $"{Time} Debited {Amount.ToString("C")} to {To}";
        }
    }
}