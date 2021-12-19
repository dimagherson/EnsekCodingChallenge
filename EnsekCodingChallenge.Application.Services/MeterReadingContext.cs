using System;
using System.Collections.Generic;
using System.Linq;

namespace EnsekCodingChallenge.Application.Services
{
    public class MeterReadingContext
    {
        private IList<MeterReadingEntryContext> _entryContexts = new List<MeterReadingEntryContext>();

        public IList<MeterReadingEntryContext> ValidEntryContexts => _entryContexts.Where(e => e.IsValid).ToList();
        public IList<MeterReadingEntryContext> InvalidEntryContexts => _entryContexts.Where(e => !e.IsValid).ToList();
        public int ValidCount => _entryContexts.Count(e => e.IsValid);
        public int InvalidCount => _entryContexts.Count(e => !e.IsValid);

        public void AddEntryContext(MeterReadingEntryContext entryContext)
        {
            if (entryContext == null)
            {
                throw new ArgumentNullException(nameof(entryContext));
            }

            _entryContexts.Add(entryContext);
        }
    }
}
