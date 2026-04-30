using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace PBL3.Services;

public static class CodeGenerator
{
    public static async Task<string?> GenerateFromSequenceAsync(
        DbContext context,
        string sequenceName,
        string prefix,
        int digits)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(sequenceName))
        {
            throw new ArgumentException("Sequence name is required.", nameof(sequenceName));
        }

        if (sequenceName.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '.'))
        {
            throw new ArgumentException("Sequence name contains invalid characters.", nameof(sequenceName));
        }

        if (digits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), "Digits must be greater than zero.");
        }

        var maxValue = (long)Math.Pow(10, digits) - 1;
        var connection = context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT CAST(NEXT VALUE FOR {sequenceName} AS bigint)";
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            var result = await command.ExecuteScalarAsync();
            var nextValue = Convert.ToInt64(result);

            if (nextValue > maxValue)
            {
                return null;
            }

            return prefix + nextValue.ToString($"D{digits}");
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
