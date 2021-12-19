using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using EnsekCodingChallenge.Domain;

namespace EnsekCodingChallenge.Application.DataAccess
{
    public interface IMeterReadingsDataAccess
    {
        public Task<IList<ReadingDateTimeDto>> GetReads();
        Task SaveReads(IList<MeterReadingEntry> entries);
    }

    public class MeterReadingsDataAccess : IMeterReadingsDataAccess
    {
        private readonly ConnectionString _connectionString;

        public MeterReadingsDataAccess(ConnectionString connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
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

        // Quick and dirty. Would use ORM and repository pattern in real-world scenario.
        private string ToScript(MeterReadingEntry entry)
        {
            var dateTime = entry.DateTime.ToString("yyyy/MM/dd HH:mm");

            var script =
                @$"UPDATE Reading SET [DateTime] = '{dateTime}', [Value] = {entry.Value} WHERE AccountId = {entry.AccountId};
                IF @@ROWCOUNT = 0
                INSERT INTO Reading (AccountId, [DateTime], [Value]) VALUES({entry.AccountId}, '{dateTime}', {entry.Value});";

            return script;
        }
    }
}
