using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnsekCodingChallenge.Application.DataAccess;

namespace EnsekCodingChallenge.Application.Services
{
    public interface IMeterReadingsService
    {
        Task<MeterReadingContext> ProcessStream(Stream stream);
    }

    public class MeterReadingsService : IMeterReadingsService
    {
        private readonly IMeterReadingEntryParser _parser;
        private readonly IMeterReadingsDataAccess _dataAccess;

        public MeterReadingsService(IMeterReadingEntryParser parser, IMeterReadingsDataAccess dataAccess)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
        }

        public async Task<MeterReadingContext> ProcessStream(Stream stream)
        {
            var context = new MeterReadingContext();
            
            InitiateContext(context, stream);
            ValidateLatestIncomingReadsPerAccount(context);
            await ValidateLatestReadDateTime(context);
            await SaveValidReads(context);

            return context;
        }

        private void InitiateContext(MeterReadingContext context, Stream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException(nameof(stream), "Cannot process stream");
            }

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
        }

        private MeterReadingEntryContext GetEntryContext(string line, int lineNumber)
        {
            var entry = _parser.Parse(line);

            var context = entry != null ?
                MeterReadingEntryContext.CreateValid(lineNumber, entry) :
                MeterReadingEntryContext.CreateInvalid(lineNumber, "Parsing failed.");

            return context;
        }

        private void ValidateLatestIncomingReadsPerAccount(MeterReadingContext context)
        {
            // need only latest valid read per accountId
            var groups = context.ValidEntryContexts.GroupBy(c => c.Entry.AccountId);

            foreach (var group in groups)
            {
                var older = group.OrderByDescending(c => c.Entry.DateTime).Skip(1);

                foreach (var olderEntry in older)
                {
                    olderEntry.Invalidate("Newer read exists.");
                }
            }
        }

        private async Task ValidateLatestReadDateTime(MeterReadingContext context)
        {
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
        }

        private async Task SaveValidReads(MeterReadingContext context)
        {
            if (context.ValidCount > 0)
            {
                await _dataAccess.SaveReads(context.ValidEntryContexts.Select(c => c.Entry).ToList());
            }
        }
    }
}
