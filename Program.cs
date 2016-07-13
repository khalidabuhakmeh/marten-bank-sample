using System;
using Account.Events;
using Accounting.Events;
using Marten;

namespace Accounting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var store = DocumentStore.For(_ =>
            {
                _.Connection("host=localhost;database=marten_test;password=postgres;username=postgres");

                _.AutoCreateSchemaObjects = AutoCreate.All;

                _.Events.AddEventTypes(new[] {
                    typeof(AccountCreated),
                    typeof(AccountCredited),
                    typeof(AccountDebited)
                });

                _.Events.AggregateStreamsInlineWith<Account>();
            });

            var khalid = new AccountCreated
            {
                Owner = "Khalid Abuhakmeh",
                AccountId = Guid.NewGuid(),
                StartingBalance = 1000m
            };

            var bill = new AccountCreated
            {
                Owner = "Bill Boga",
                AccountId = Guid.NewGuid()
            };

            using (var session = store.OpenSession())
            {
                // create banking accounts
                session.Events.Append(khalid.AccountId, khalid);
                session.Events.Append(bill.AccountId, bill);

                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                // load khalid's account
                var account = session.Load<Account>(khalid.AccountId);
                // let's be generous
                var amount = 100m;
                var give = new AccountDebited
                {
                    Amount = amount,
                    To = bill.AccountId,
                    From = khalid.AccountId,
                    Description = "Bill helped me out with some code."
                };

                if (account.HasSufficientFunds(give))
                {
                    session.Events.Append(give.From, give);
                    session.Events.Append(give.To, give.ToCredit());
                }
                // commit these changes
                session.SaveChanges();
            }

            using (var session = store.OpenSession())
            {
                // load bill's account
                var account = session.Load<Account>(bill.AccountId);
                // let's try to over spend
                var amount = 1000m;
                var spend = new AccountDebited
                {
                    Amount = amount,
                    From = bill.AccountId,
                    To = khalid.AccountId,
                    Description = "Trying to buy that Ferrari"
                };

                if (account.HasSufficientFunds(spend))
                {
                    // should not get here
                    session.Events.Append(spend.From, spend);
                    session.Events.Append(spend.To, spend.ToCredit());
                } else {
                   session.Events.Append(account.Id, new InvalidOperationAttempted {
                        Description = "Overdraft" 
                    }); 
                }
                // commit these changes
                session.SaveChanges();
            }

            using (var session = store.LightweightSession())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("----- Final Balance ------");

                var accounts = session.LoadMany<Account>(khalid.AccountId, bill.AccountId);

                foreach (var account in accounts)
                {
                    Console.WriteLine(account);
                }
            }

            using (var session = store.LightweightSession())
            {
                foreach (var account in new[] { khalid, bill })
                {
                    Console.WriteLine();
                    Console.WriteLine($"Transaction ledger for {account.Owner}");
                    var stream = session.Events.FetchStream(account.AccountId);
                    foreach (var item in stream)
                    {
                        Console.WriteLine(item.Data);
                    }
                    Console.WriteLine();
                }
            }

            Console.ReadLine();
        }
    }
}
