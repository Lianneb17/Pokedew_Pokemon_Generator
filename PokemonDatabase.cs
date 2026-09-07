using System.Text.Json;
using Microsoft.Data.Sqlite;
using PokemonJsonGenerator.Models;
using PokemonJsonGenerator.Models.Pokemon;

namespace PokemonJsonGenerator;

public sealed class PokemonDatabase
{
    private readonly string connectionString;

    public PokemonDatabase(string filePath)
    {
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = filePath
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS groups (
                base_pokemon_name TEXT PRIMARY KEY,
                hatch_cycle TEXT NOT NULL,
                levelspeed TEXT NOT NULL,
                color TEXT NOT NULL,
                egg_groups TEXT NOT NULL,
                generations TEXT NOT NULL DEFAULT '[]',
                sprite_width INTEGER NOT NULL,
                sprite_height INTEGER NOT NULL,
                should_be_for_sale INTEGER NOT NULL,
                evolutions TEXT NOT NULL DEFAULT '[]'
            );

            CREATE TABLE IF NOT EXISTS pokemon (
                base_pokemon_name TEXT NOT NULL,
                name TEXT NOT NULL,
                gender TEXT NOT NULL,
                barn_type TEXT NOT NULL,
                types TEXT NOT NULL,
                base_stats_total TEXT NOT NULL,
                has_gender_variants INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (base_pokemon_name, name, gender),
                FOREIGN KEY (base_pokemon_name) REFERENCES groups(base_pokemon_name)
            );
            """;
        await command.ExecuteNonQueryAsync();
        await EnsureEvolutionsColumnAsync(connection);
        await EnsureGenerationsColumnAsync(connection);
        await EnsureGenderVariantsColumnAsync(connection);
        await NormalizeExistingPokemonAsync(connection);
    }

    public async Task SaveGroupAsync(Group group)
    {
        if (string.IsNullOrWhiteSpace(group.BasePokemonName))
            throw new InvalidOperationException("Een groep moet een basis-Pokémon hebben.");

        await using var connection = await OpenConnectionAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        await ExecuteAsync(connection, transaction,
            "DELETE FROM pokemon WHERE base_pokemon_name = $name; DELETE FROM groups WHERE base_pokemon_name = $name;",
            ("$name", (object)group.BasePokemon()));

        await ExecuteAsync(connection, transaction, """
            INSERT INTO groups
                (base_pokemon_name, hatch_cycle, levelspeed, color, egg_groups,
                 generations, sprite_width, sprite_height, should_be_for_sale, evolutions)
            VALUES ($name, $hatchCycle, $levelspeed, $color, $eggGroups,
                    $generations, $spriteWidth, $spriteHeight, $forSale, $evolutions);
            """,
            ("$name", group.BasePokemon()),
            ("$hatchCycle", JsonSerializer.Serialize(group.HatchCycle)),
            ("$levelspeed", JsonSerializer.Serialize(group.Levelspeed)),
            ("$color", JsonSerializer.Serialize(group.Color)),
            ("$eggGroups", JsonSerializer.Serialize(group.EggGroups)),
            ("$generations", JsonSerializer.Serialize(group.Generations.Distinct(StringComparer.OrdinalIgnoreCase).ToList())),
            ("$spriteWidth", group.SpriteWidth),
            ("$spriteHeight", group.SpriteHeight),
            ("$forSale", group.ShouldBeForSale ? 1 : 0),
            ("$evolutions", JsonSerializer.Serialize(group.Evolutions)));

        var hasExtraTextureColumn = await HasColumnAsync(connection, "pokemon", "has_extra_texture");
        foreach (var pokemon in NormalizePokemon(group.Pokemon))
        {
            var sql = hasExtraTextureColumn
                ? """
                    INSERT INTO pokemon
                        (base_pokemon_name, name, gender, barn_type, types,
                         base_stats_total, has_gender_variants, has_extra_texture)
                    VALUES ($group, $name, $gender, $barnType, $types,
                        $baseStatsTotal, $hasGenderVariants, $hasExtraTexture);
                    """
                : """
                    INSERT INTO pokemon
                        (base_pokemon_name, name, gender, barn_type, types,
                         base_stats_total, has_gender_variants)
                    VALUES ($group, $name, $gender, $barnType, $types,
                        $baseStatsTotal, $hasGenderVariants);
                    """;

            var parameters = new List<(string Name, object Value)>
            {
                ("$group", group.BasePokemon()),
                ("$name", pokemon.Name),
                ("$gender", pokemon.Gender.ToString()),
                ("$barnType", pokemon.BarnType.ToString()),
                ("$types", JsonSerializer.Serialize(pokemon.Types)),
                ("$baseStatsTotal", JsonSerializer.Serialize(pokemon.BaseStatsTotal)),
                ("$hasGenderVariants", pokemon.HasGenderVariants ? 1 : 0)
            };

            if (hasExtraTextureColumn)
                parameters.Add(("$hasExtraTexture", 0));

            await ExecuteAsync(connection, transaction, sql, [.. parameters]);
        }

        await transaction.CommitAsync();
    }

    public async Task<Group> LoadGroupAsync(string basePokemonName)
    {
        await using var connection = await OpenConnectionAsync();
        await using var groupCommand = connection.CreateCommand();
        groupCommand.CommandText = "SELECT * FROM groups WHERE base_pokemon_name = $name;";
        groupCommand.Parameters.AddWithValue("$name", basePokemonName);

        await using var groupReader = await groupCommand.ExecuteReaderAsync();
        if (!await groupReader.ReadAsync())
            throw new InvalidOperationException($"Geen groep gevonden voor '{basePokemonName}'.");

        var group = new Group
        {
            BasePokemonName = groupReader.GetString(groupReader.GetOrdinal("base_pokemon_name")),
            HatchCycle = Deserialize<HatchCycleData>(groupReader.GetString(groupReader.GetOrdinal("hatch_cycle"))),
            Levelspeed = Deserialize<LevelspeedData>(groupReader.GetString(groupReader.GetOrdinal("levelspeed"))),
            Color = Deserialize<ColorData>(groupReader.GetString(groupReader.GetOrdinal("color"))),
            EggGroups = Deserialize<List<EggGroup>>(groupReader.GetString(groupReader.GetOrdinal("egg_groups"))),
            Generations = Deserialize<List<string>>(groupReader.GetString(groupReader.GetOrdinal("generations"))),
            SpriteWidth = groupReader.GetInt32(groupReader.GetOrdinal("sprite_width")),
            SpriteHeight = groupReader.GetInt32(groupReader.GetOrdinal("sprite_height")),
            ShouldBeForSale = groupReader.GetInt32(groupReader.GetOrdinal("should_be_for_sale")) != 0,
            Evolutions = Deserialize<List<Evolution>>(groupReader.GetString(groupReader.GetOrdinal("evolutions")))
        };

        await groupReader.CloseAsync();

        await using var pokemonCommand = connection.CreateCommand();
        pokemonCommand.CommandText = "SELECT * FROM pokemon WHERE base_pokemon_name = $name ORDER BY rowid;";
        pokemonCommand.Parameters.AddWithValue("$name", basePokemonName);
        await using var pokemonReader = await pokemonCommand.ExecuteReaderAsync();

        while (await pokemonReader.ReadAsync())
        {
            group.Pokemon.Add(new Pokemon
            {
                Name = pokemonReader.GetString(pokemonReader.GetOrdinal("name")),
                Gender = Enum.Parse<Gender>(pokemonReader.GetString(pokemonReader.GetOrdinal("gender"))),
                HasGenderVariants = pokemonReader.GetInt32(pokemonReader.GetOrdinal("has_gender_variants")) != 0,
                BarnType = Enum.Parse<BarnType>(pokemonReader.GetString(pokemonReader.GetOrdinal("barn_type"))),
                Types = Deserialize<List<PokemonType>>(pokemonReader.GetString(pokemonReader.GetOrdinal("types"))),
                BaseStatsTotal = Deserialize<BaseStatsTotalData>(pokemonReader.GetString(pokemonReader.GetOrdinal("base_stats_total")))
            });
        }

        return group;
    }

    public async Task<List<Group>> LoadGroupsAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT base_pokemon_name FROM groups ORDER BY base_pokemon_name;";

        var groupNames = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            groupNames.Add(reader.GetString(0));

        var groups = new List<Group>();
        foreach (var groupName in groupNames)
            groups.Add(await LoadGroupAsync(groupName));

        return groups;
    }

    public async Task<int> UpdateAllGenerationsAsync(PokeApiClient pokeApiClient)
    {
        var groups = await LoadGroupsAsync();
        await using var connection = await OpenConnectionAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        foreach (var group in groups)
        {
            var generations = await pokeApiClient.GetGenerationsAsync(group.Pokemon.Select(pokemon => pokemon.Name));
            await ExecuteAsync(connection, transaction,
                "UPDATE groups SET generations = $generations WHERE base_pokemon_name = $name;",
                ("$generations", JsonSerializer.Serialize(generations)),
                ("$name", group.BasePokemonName));
        }

        await transaction.CommitAsync();
        return groups.Count;
    }

    public async Task DeleteGroupAsync(string basePokemonName)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

        await ExecuteAsync(connection, transaction,
            "DELETE FROM pokemon WHERE base_pokemon_name = $name; DELETE FROM groups WHERE base_pokemon_name = $name;",
            ("$name", basePokemonName));

        await transaction.CommitAsync();
    }

    private static async Task EnsureEvolutionsColumnAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(groups);";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), "evolutions", StringComparison.OrdinalIgnoreCase))
                return;
        }

        await reader.CloseAsync();
        command.CommandText = "ALTER TABLE groups ADD COLUMN evolutions TEXT NOT NULL DEFAULT '[]';";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureGenderVariantsColumnAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(pokemon);";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), "has_gender_variants", StringComparison.OrdinalIgnoreCase))
                return;
        }

        await reader.CloseAsync();
        command.CommandText = "ALTER TABLE pokemon ADD COLUMN has_gender_variants INTEGER NOT NULL DEFAULT 0;";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureGenerationsColumnAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(groups);";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), "generations", StringComparison.OrdinalIgnoreCase))
                return;
        }

        await reader.CloseAsync();
        command.CommandText = "ALTER TABLE groups ADD COLUMN generations TEXT NOT NULL DEFAULT '[]';";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task NormalizeExistingPokemonAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE pokemon
            SET gender = 'MaleOrFemale', has_gender_variants = 1
            WHERE rowid IN (
                SELECT MIN(rowid)
                FROM pokemon
                GROUP BY base_pokemon_name, name
                HAVING COUNT(*) > 1
            );

            DELETE FROM pokemon
            WHERE rowid NOT IN (
                SELECT MIN(rowid)
                FROM pokemon
                GROUP BY base_pokemon_name, name
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> HasColumnAsync(
        SqliteConnection connection,
        string tableName,
        string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static List<Pokemon> NormalizePokemon(IReadOnlyList<Pokemon> pokemon)
    {
        return pokemon
            .GroupBy(entry => entry.Name)
            .Select(group =>
            {
                var first = group.First();
                var hasGenderVariants = group.Any(entry => entry.HasGenderVariants)
                    || group.Select(entry => entry.Gender).Distinct().Count() > 1;

                return new Pokemon
                {
                    Name = first.Name,
                    Gender = hasGenderVariants ? Gender.MaleOrFemale : first.Gender,
                    HasGenderVariants = hasGenderVariants,
                    BarnType = first.BarnType,
                    Types = first.Types,
                    BaseStatsTotal = first.BaseStatsTotal
                };
            })
            .ToList();
    }

    private async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        await command.ExecuteNonQueryAsync();
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json)
        ?? throw new InvalidOperationException("Ongeldige data in de Pokémon-database.");
}