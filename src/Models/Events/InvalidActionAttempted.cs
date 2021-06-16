using System;

namespace Account.Events
{
    public class InvalidOperationAttempted {

        public InvalidOperationAttempted() {
            Time = DateTimeOffset.UtcNow;
        }

        public string Description { get; set;}
        public DateTimeOffset Time {get;set;}

        public override string ToString() {
            return $"{Time} Attempted Invalid Action: {Description}";
        }
    }
}