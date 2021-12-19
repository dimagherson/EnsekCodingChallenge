using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Text;

namespace EnsekCodingChallenge.Controllers
{
    // upload file

    // db connection
    // db tables
    // db seed data

    // data validation - NNNNN
    // data validation - account exists
    // data - take valid latest

    // response - number of successes / failures

    [ApiController]
    [Route("/")]
    public class MeterReadingsController : ControllerBase
    {
        [HttpPost("meter-reading-uploads")]
        public async Task<IActionResult> UploadMeterReadings([FromQuery] IFormFile file)
        {
            if (file?.Length > 0)
            {
                var stream = file.OpenReadStream();
                var result = await ProcessStream(stream);

                return Ok(result);
            }

            ModelState.AddModelError(nameof(file), "Cannot be empty.");

            return ValidationProblem();
        }

        private async Task<MeterReadingsResult> ProcessStream(Stream stream)
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

            var dataAccess = new DataAccess();
            var existingReads = await dataAccess.GetReads();

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
                await dataAccess.SaveReads(context.ValidEntryContexts.Select(c => c.Entry).ToList());
            }

            var result = new MeterReadingsResult();
            result.Successes = context.ValidCount;
            result.Failures = context.InvalidCount;

            foreach (var entry in context.InvalidEntryContexts)
            {
                result.Errors.Add(new MeterReadingEntryError
                {
                    LineNumber = entry.LineNumber,
                    Error = entry.Error
                });
            }

            return result;
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

            return new MeterReadingEntry
            {
                AccountId = accountId,
                DateTime = dateTime,
                Value = value
            };
        }
    }

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

            var context = new MeterReadingEntryContext(lineNumber);
            context.Entry = entry;

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

    public class MeterReadingEntry
    {
        public int AccountId { get; set; }
        public DateTime DateTime { get; set; }
        public int Value { get; set; }
    }

    public class DataAccess
    {
        private SqlConnection GetConnection()
        {
            return new SqlConnection(@"Data Source=(localdb)\ProjectsV13;Initial Catalog=EnsekDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        }

        public async Task<IList<ReadingDateTimeDto>> GetReads()
        {
            using var connection = GetConnection();
            connection.Open();

            var query = "SELECT a.AccountId, r.DateTime FROM Account a LEFT OUTER JOIN Reading r on a.AccountId = r.AccountId";

            var data = await connection.QueryAsync<ReadingDateTimeDto>(query);

            return data.ToList();
        }

        public async Task SaveReads(IList<MeterReadingEntry> entries)
        {
            using var connection = GetConnection();
            connection.Open();

            var stringBuilder = new StringBuilder();

            foreach (var entry in entries)
            {
                stringBuilder.AppendLine(ToScript(entry));
            }

            var command = stringBuilder.ToString();

            await connection.ExecuteAsync(command);
        }

        // quick and dirty. Would use ORM in real-world scenario
        private string ToScript(MeterReadingEntry entry)
        {
            var dateTime = entry.DateTime.ToString("yyyy/MM/dd HH:mm");

            var script =
                @$"UPDATE Reading SET [DateTime] = '{dateTime}', [Value] = {entry.Value} WHERE AccountId = {entry.AccountId};
                IF @@ROWCOUNT = 0
                INSERT INTO Reading (AccountId, [DateTime], [Value]) VALUES({entry.AccountId}, '{dateTime}', {entry.Value});
                ";

            return script;
        }
    }

    public class ReadingDateTimeDto
    {
        public int AccountId { get; set; }
        public DateTime? DateTime { get; set; }
    }

    public class MeterReadingsResult
    {
        public int Successes { get; set; }
        public int Failures { get; set; }
        public IList<MeterReadingEntryError> Errors { get; set; } = new List<MeterReadingEntryError>();
    }

    public class MeterReadingEntryError
    {
        public int LineNumber { get; set; }
        public string Error { get; set; }
    }
}
