using System.Collections.Generic;

namespace EnsekCodingChallenge.Api.Contracts.Output
{
    public class MeterReadingsModel
    {
        public int Successes { get; set; }
        public int Failures { get; set; }
        public IList<MeterReadingEntryErrorModel> Errors { get; set; } = new List<MeterReadingEntryErrorModel>();
    }
}
