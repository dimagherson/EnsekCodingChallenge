using System;

namespace EnsekCodingChallenge.Domain
{
    public class MeterReadingEntry
    {
        public int AccountId { get; }
        public DateTime DateTime { get; }
        public int Value { get; }

        public MeterReadingEntry(int accountId, DateTime dateTime, int value)
        {
            AccountId = accountId;
            DateTime = dateTime;
            Value = value;
        }
    }
}
