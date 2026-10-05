using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ISearchServiceProvider"/> via raw SQL against the generated <c>SearchVector</c>/<c>SearchText</c>
/// columns from the <c>AddCrossDomainSearchVectors</c> migration (ADR-0026) - unmapped in <see cref="AppDbContext"/>'s model
/// </summary>
public class SearchServiceProvider(AppDbContext db) : ISearchServiceProvider
{
	private const int MaxHitsPerDomain = 5;
	private const double TrigramThreshold = 0.3;

	private readonly AppDbContext _db = db;


	public async Task<IReadOnlyList<SearchGroup>> SearchAsync(string query, bool includeInactive, IReadOnlySet<string> domains, CancellationToken cancellationToken = default)
	{
		var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}

		var groups = new List<SearchGroup>();

		if (domains.Contains(SearchDomains.StockItems))
		{
			groups.Add(await this.SearchDomainAsync(connection, SearchDomains.StockItems,
				"""SELECT "Id", "Name", "Code" FROM "StockItems" WHERE {0} ORDER BY {1} DESC, "Name" ASC LIMIT @limit""",
				"""SELECT count(*) FROM "StockItems" WHERE {0}""",
				"""("SearchVector" @@ websearch_to_tsquery('simple', @q) OR word_similarity(@q, "SearchText") > @threshold)""",
				"""ts_rank("SearchVector", websearch_to_tsquery('simple', @q)) + word_similarity(@q, "SearchText")""",
				query, includeInactive, cancellationToken));
		}

		if (domains.Contains(SearchDomains.Customers))
		{
			groups.Add(await this.SearchDomainAsync(connection, SearchDomains.Customers,
				"""SELECT "Id", "Name" || ' ' || "Lastname", "IdentificationNumber" FROM "Customers" WHERE {0} ORDER BY {1} DESC, "Name" ASC LIMIT @limit""",
				"""SELECT count(*) FROM "Customers" WHERE {0}""",
				"""("SearchVector" @@ websearch_to_tsquery('simple', @q) OR word_similarity(@q, "SearchText") > @threshold)""",
				"""ts_rank("SearchVector", websearch_to_tsquery('simple', @q)) + word_similarity(@q, "SearchText")""",
				query, includeInactive, cancellationToken));
		}

		if (domains.Contains(SearchDomains.Suppliers))
		{
			groups.Add(await this.SearchDomainAsync(connection, SearchDomains.Suppliers,
				"""SELECT "Id", "Name", "ContactName" FROM "Suppliers" WHERE {0} ORDER BY {1} DESC, "Name" ASC LIMIT @limit""",
				"""SELECT count(*) FROM "Suppliers" WHERE {0}""",
				"""("SearchVector" @@ websearch_to_tsquery('simple', @q) OR word_similarity(@q, "SearchText") > @threshold)""",
				"""ts_rank("SearchVector", websearch_to_tsquery('simple', @q)) + word_similarity(@q, "SearchText")""",
				query, includeInactive, cancellationToken));
		}

		if (domains.Contains(SearchDomains.Invoices))
		{
			var filter = """(i."SearchVector" @@ websearch_to_tsquery('simple', @q) OR word_similarity(@q, i."SearchText") > @threshold) AND (@includeInactive OR NOT i."IsCancelled")""";
			var rank = """ts_rank(i."SearchVector", websearch_to_tsquery('simple', @q)) + word_similarity(@q, i."SearchText")""";
			groups.Add(await this.SearchDomainAsync(connection, SearchDomains.Invoices,
				"""SELECT i."Id", i."Number", c."Name" || ' ' || c."Lastname" FROM "Invoices" i JOIN "Customers" c ON c."Id" = i."CustomerId" WHERE {0} ORDER BY {1} DESC, i."Date" DESC LIMIT @limit""",
				"""SELECT count(*) FROM "Invoices" i WHERE {0}""",
				filter, rank, query, includeInactive, cancellationToken));
		}

		return groups;
	}

	/// <param name="selectSqlTemplate">{0} = filter, {1} = rank expression</param>
	/// <param name="countSqlTemplate">{0} = filter</param>
	private async Task<SearchGroup> SearchDomainAsync(
		NpgsqlConnection connection, string domain, string selectSqlTemplate, string countSqlTemplate,
		string filter, string rank, string query, bool includeInactive, CancellationToken cancellationToken)
	{
		var items = new List<SearchHit>();

		await using (var command = connection.CreateCommand())
		{
			command.CommandText = string.Format(selectSqlTemplate, filter, rank);
			AddParameters(command, query, includeInactive);
			command.Parameters.AddWithValue("limit", MaxHitsPerDomain);

			await using var reader = await command.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(new SearchHit(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? "" : reader.GetString(2)));
			}
		}

		await using var countCommand = connection.CreateCommand();
		countCommand.CommandText = string.Format(countSqlTemplate, filter);
		AddParameters(countCommand, query, includeInactive);
		var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

		return new SearchGroup(domain, items, totalCount);
	}

	private static void AddParameters(NpgsqlCommand command, string query, bool includeInactive)
	{
		command.Parameters.AddWithValue("q", query);
		command.Parameters.AddWithValue("threshold", TrigramThreshold);
		command.Parameters.AddWithValue("includeInactive", includeInactive);
	}
}
