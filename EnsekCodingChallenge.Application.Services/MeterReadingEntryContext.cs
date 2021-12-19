using System;
using EnsekCodingChallenge.Domain;

namespace EnsekCodingChallenge.Application.Services
{
    public class MeterReadingEntryContext
    {
        public int LineNumber { get; private set; }
        public bool IsValid { get; private set; } = true;
        public string Error { get; private set; }
        public MeterReadingEntry Entry { get; private set; }

        private MeterReadingEntryContext(int lineNumber)
        {
            if (lineNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber), "Cannot be less than 1.");
            }

            LineNumber = lineNumber;
        }

        public static MeterReadingEntryContext CreateValid(int lineNumber, MeterReadingEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var context = new MeterReadingEntryContext(lineNumber)
            {
                Entry = entry
            };

            return context;
        }

        public static MeterReadingEntryContext CreateInvalid(int lineNumber, string error)
        {
            var context = new MeterReadingEntryContext(lineNumber);
            context.Invalidate(error);

            return context;
        }

        public void Invalidate(string error)
        {
            Error = error;
            IsValid = false;
        }
    }
}
