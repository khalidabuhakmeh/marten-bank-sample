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
            // Setup / Create accounts
            var store = DocumentStore.For(_ =>
            {
                _.Connection("host=localhost;database=marten_test;password=postgres;username=postgres");

                _.AutoCreateSchemaObjects = AutoCreate.All;

                _.Events.AddEventTypes(new[] {
                    typeof(AccountCreated),
                    typeof(AccountCredited),
                    typeof(AccountDebited)
                });

                _.Events.InlineProjections.AggregateStreamsWith<Account>();
                //_.Events.InlineProjections.AggregateStreamsWith<Income>();
            });

            var joshua = new AccountCreated
            {
                Owner = "Joshua",
                AccountId = Guid.NewGuid(),
                StartingBalance = 1000m
            };

            var jonas = new AccountCreated
            {
                Owner = "Jonas",
                AccountId = Guid.NewGuid()
            };

            using (var session = store.OpenSession())
            {
                // create banking accounts
                session.Events.Append(joshua.AccountId, joshua);
                session.Events.Append(jonas.AccountId, jonas);

                session.SaveChanges();
            }


            // 
            using (var session = store.OpenSession())
            {
                // load khalid's account
                var account = session.Load<Account>(joshua.AccountId);
                // let's be generous
                var amount = 100m;
                var give = new AccountDebited
                {
                    Amount = amount,
                    To = jonas.AccountId,
                    From = joshua.AccountId,
                    Description = "Jonas helped me out with some code."
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
                var account = session.Load<Account>(jonas.AccountId);
                // let's try to over spend
                var amount = 1000m;
                var spend = new AccountDebited
                {
                    Amount = amount,
                    From = jonas.AccountId,
                    To = joshua.AccountId,
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


            using (var session = store.OpenSession())
            {
                // Simple lookup "current" state / This will return null, if Inline projection is removed
                var income = session.Load<Income>(jonas.AccountId);
            }

            using (var session = store.OpenSession())
            {
                // Rebuild current state from events
                var liveAccountProjection = session.Events.AggregateStream<Account>(joshua.AccountId);
                var liveAccountProjectionAtVersion1 = session.Events.AggregateStream<Account>(joshua.AccountId, 1);
            }

            using (var session = store.OpenSession())
            {
                // Remember to Delete Inline projection before running this.
                var liveAccountProjection = session.Events.AggregateStream<Income>(jonas.AccountId);
            }


            using (var session = store.LightweightSession())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("----- Final TotalIncome ------");

                var accounts = session.LoadMany<Account>(joshua.AccountId, jonas.AccountId);

                foreach (var account in accounts)
                {
                    Console.WriteLine(account);
                }
            }

            using (var session = store.LightweightSession())
            {
                foreach (var account in new[] { joshua, jonas })
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
