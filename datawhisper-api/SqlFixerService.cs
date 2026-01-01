using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace DataWhisper.API
{
    public class SqlFixerService
    {
        private readonly ILogger<SqlFixerService> _logger;

        public SqlFixerService(ILogger<SqlFixerService> logger)
        {
            _logger = logger;
        }

        public string FixSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            var originalSql = sql;
            var fixedSql = sql;

            try
            {
                // Fix GROUP BY issues in subqueries
                fixedSql = FixSubqueryGroupBy(fixedSql);

                // Log if we made changes
                if (fixedSql != originalSql)
                {
                    _logger.LogInformation("🔧 SQL Fixed:\nOriginal: {Original}\nFixed: {Fixed}",
                        originalSql, fixedSql);
                }

                return fixedSql;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fix SQL, returning original");
                return originalSql;
            }
        }

        private string FixSubqueryGroupBy(string sql)
        {
            // Fix subqueries with pattern: (SELECT alias.col FROM table alias GROUP BY EXTRACT...)
            // Replace with: (SELECT MAX(alias.col) FROM table alias GROUP BY EXTRACT...)

            // Pattern to find subqueries with the problematic structure
            var pattern = @"\(SELECT\s+(\w+\.\w+)\s+FROM\s+(\w+)\s+\1\s+GROUP BY\s+EXTRACT\(";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var matches = regex.Matches(sql);
            foreach (Match match in matches.Cast<Match>().OrderByDescending(m => m.Index))
            {
                var columnRef = match.Groups[1].Value; // e.g., "o2.order_date"
                var alias = match.Groups[2].Value; // e.g., "o2"

                // Find the complete subquery
                var startIndex = match.Index;
                var endIndex = FindMatchingParenthesis(sql, startIndex);

                if (endIndex > startIndex)
                {
                    var subquery = sql.Substring(startIndex, endIndex - startIndex + 1);

                    // Check if already has aggregate
                    if (!Regex.IsMatch(subquery, $@"SELECT\s+(MAX|MIN|SUM|COUNT|AVG)\s*\(", RegexOptions.IgnoreCase))
                    {
                        // Replace the SELECT clause only
                        var fixedSubquery = Regex.Replace(
                            subquery,
                            $@"SELECT\s+{Regex.Escape(columnRef)}\s+FROM",
                            $"SELECT MAX({columnRef}) FROM",
                            RegexOptions.IgnoreCase
                        );

                        sql = sql.Substring(0, startIndex) + fixedSubquery + sql.Substring(endIndex + 1);
                    }
                }
            }

            return sql;
        }

        private int FindMatchingParenthesis(string sql, int openPos)
        {
            var depth = 1;
            for (int i = openPos + 1; i < sql.Length; i++)
            {
                if (sql[i] == '(') depth++;
                if (sql[i] == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
    }
}
