using System;
using EnsekCodingChallenge.Domain;

namespace EnsekCodingChallenge.Application.Services
{
    public interface IMeterReadingEntryParser
    {
        public MeterReadingEntry Parse(string line);
    }

    public class MeterReadingEntryParser : IMeterReadingEntryParser
    {
        private readonly int MinValue = 0;
        private readonly int MaxValue = 99999;

        public MeterReadingEntry Parse(string line)
        {
            if (line == null)
            {
                return null;
            }

            var split = line.Split(',');

            if (split.Length < 3)
            {
                return null;
            }

            if (!int.TryParse(split[0], out int accountId))
            {
                return null;
            }

            if (!DateTime.TryParse(split[1], out DateTime dateTime))
            {
                return null;
            }

            if (!int.TryParse(split[2], out int value) || value < MinValue || value > MaxValue)
            {
                return null;
            }

            return new MeterReadingEntry(accountId, dateTime, value);
        }
    }
}
