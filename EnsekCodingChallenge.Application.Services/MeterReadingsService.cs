using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnsekCodingChallenge.Application.DataAccess;
using EnsekCodingChallenge.Domain;

namespace EnsekCodingChallenge.Application.Services
{
    public interface IMeterReadingsService
    {
        Task<MeterReadingContext> ProcessStream(Stream stream);
    }

    public class MeterReadingsService : IMeterReadingsService
    {
        private readonly IMeterReadingsDataAccess _dataAccess;

        public MeterReadingsService(IMeterReadingsDataAccess dataAccess)
        {
            _dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
        }

        public async Task<MeterReadingContext> ProcessStream(Stream stream)
        {
            var context = new MeterReadingContext();
            var streamReader = new StreamReader(stream);

            streamReader.ReadLine(); // skip headers

            var lineNumber = 0;
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                lineNumber++;
                var entryContext = GetEntryContext(line, lineNumber);
                context.AddEntryContext(entryContext);
            }

            // take only latest valid, make others invalid with appropriate error
            var groups = context.ValidEntryContexts.GroupBy(c => c.Entry.AccountId);

            foreach (var group in groups)
            {
                var older = group.OrderByDescending(c => c.Entry.DateTime).Skip(1);

                foreach (var olderEntry in older)
                {
                    olderEntry.Invalidate("Newer read exists.");
                }
            }

            var existingReads = await _dataAccess.GetReads();

            foreach (var entryContext in context.ValidEntryContexts)
            {
                var matchingAccountRead = existingReads.FirstOrDefault(r => r.AccountId == entryContext.Entry.AccountId);

                if (matchingAccountRead == null)
                {
                    entryContext.Invalidate("Account does not exist.");
                }
                else if (matchingAccountRead.DateTime.HasValue && matchingAccountRead.DateTime >= entryContext.Entry.DateTime)
                {
                    entryContext.Invalidate("Provided read must be newer than existing read.");
                }
            }

            if (context.ValidCount > 0)
            {
                // save to db
                await _dataAccess.SaveReads(context.ValidEntryContexts.Select(c => c.Entry).ToList());
            }

            return context;
        }

        private MeterReadingEntryContext GetEntryContext(string line, int lineNumber)
        {
            var entry = Parse(line);

            var context = entry != null ?
                MeterReadingEntryContext.CreateValid(lineNumber, entry) :
                MeterReadingEntryContext.CreateInvalid(lineNumber, "Parsing failed.");

            return context;
        }

        private MeterReadingEntry Parse(string line)
        {
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


            if (!int.TryParse(split[2], out int value) || value < 0 || value > 99999) // todo: redo validation
            {
                return null;
            }

            return new MeterReadingEntry(accountId, dateTime, value);
        }
    }
}
