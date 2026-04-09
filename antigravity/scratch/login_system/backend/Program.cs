using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
var app = builder.Build();
app.UseCors();

// --- Global Exception Handler ---
app.Use(async (context, next) => {
    try {
        await next();
    } catch (Exception ex) {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal Server Error", message = ex.Message }));
    }
});

// --- Configuração para Servir Frontend ---
string frontendPath = Path.Combine(builder.Environment.ContentRootPath, "..", "frontend");
if (!Directory.Exists(frontendPath)) {
    frontendPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "frontend");
}

app.UseDefaultFiles(new DefaultFilesOptions {
    FileProvider = new PhysicalFileProvider(frontendPath),
    RequestPath = ""
});
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(frontendPath),
    RequestPath = ""
});
// -----------------------------------------

string? connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if(connStr == null) { throw new Exception("Missing DefaultConnection"); }

const string seniorConnString = "Server=portalrh.gruposetup.com,1433;Database=VETORH;User Id=upquery;Password=Upquery2022@;TrustServerCertificate=true;";

using (var initConn = new SqlConnection("Server=localhost;User Id=sa;Password=!3312setuP-pass*;TrustServerCertificate=true;"))
{
    initConn.Open();
    using var cmd = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LoginSystem') BEGIN CREATE DATABASE LoginSystem; END", initConn);
    cmd.ExecuteNonQuery();
}

using (var dbConn = new SqlConnection(connStr))
{
    dbConn.Open();
    using var cmd1 = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' and xtype='U') BEGIN CREATE TABLE Users (Id INT IDENTITY(1,1) PRIMARY KEY, Username NVARCHAR(50) UNIQUE NOT NULL, PasswordHash NVARCHAR(255) NOT NULL, Email NVARCHAR(100) NOT NULL, CreatedAt DATETIME DEFAULT GETDATE(), IsActive BIT NOT NULL DEFAULT 1) END ELSE BEGIN IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Users' AND COLUMN_NAME='IsActive') BEGIN ALTER TABLE Users ADD IsActive BIT NOT NULL DEFAULT 1 END END", dbConn);
    cmd1.ExecuteNonQuery();
    
    using var cmd2 = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='JobLogs' and xtype='U') BEGIN CREATE TABLE JobLogs (Id INT IDENTITY(1,1) PRIMARY KEY, SystemName NVARCHAR(50), RoutineName NVARCHAR(100), Status NVARCHAR(50), ExecutedAt DATETIME DEFAULT GETDATE(), Message NVARCHAR(MAX)) END", dbConn);
    cmd2.ExecuteNonQuery();
    
    using var cmd3 = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SeniorBirthdays' and xtype='U') BEGIN CREATE TABLE SeniorBirthdays (Registration NVARCHAR(50) PRIMARY KEY, Name NVARCHAR(150), JobTitle NVARCHAR(150), CostCenter NVARCHAR(150), BirthDate NVARCHAR(50), Age INT, UpdatedAt DATETIME DEFAULT GETDATE()) END", dbConn);
    cmd3.ExecuteNonQuery();

    using var cmd4 = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Quotes' and xtype='U') BEGIN CREATE TABLE Quotes (Id INT IDENTITY(1,1) PRIMARY KEY, ClientName NVARCHAR(150), RoleName NVARCHAR(150), Postos INT, Pessoas INT, TotalValue DECIMAL(18,2), CreatedBy NVARCHAR(50), CreatedAt DATETIME DEFAULT GETDATE()) END", dbConn);
    cmd4.ExecuteNonQuery();

    using var cmd5 = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FacilitiesJobs' and xtype='U') BEGIN CREATE TABLE FacilitiesJobs (Id INT IDENTITY(1,1) PRIMARY KEY, JobTitle NVARCHAR(150), EstCar SMALLINT, CodCar NVARCHAR(24), SalaryBase DECIMAL(18,4), UpdatedAt DATETIME DEFAULT GETDATE()) END", dbConn);
    cmd5.ExecuteNonQuery();

    using var cmdVACache = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ValeAlimentacaoCache' and xtype='U') BEGIN
        CREATE TABLE ValeAlimentacaoCache (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Matricula NVARCHAR(20),
            Nome NVARCHAR(200),
            Nascimento NVARCHAR(20),
            CPF NVARCHAR(20),
            Situacao NVARCHAR(100),
            Admissao NVARCHAR(20),
            Demissao NVARCHAR(20),
            Funcao NVARCHAR(200),
            CentroCusto NVARCHAR(200),
            Escala NVARCHAR(50),
            DiasPrevistos DECIMAL(10,2),
            DiasExtras DECIMAL(10,2),
            Ausencias DECIMAL(10,2),
            ValorDiario DECIMAL(18,2),
            ValorVA DECIMAL(18,2),
            CompetenciaInicio DATE,
            CompetenciaFim DATE,
            UpdatedAt DATETIME DEFAULT GETDATE()
        )
    END ELSE BEGIN
        -- Garantir que colunas novas existam caso a tabela já tenha sido criada antes
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ValeAlimentacaoCache' AND COLUMN_NAME='DiasExtras')
            ALTER TABLE ValeAlimentacaoCache ADD DiasExtras DECIMAL(10,2) DEFAULT 0;
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ValeAlimentacaoCache' AND COLUMN_NAME='Ausencias')
            ALTER TABLE ValeAlimentacaoCache ADD Ausencias DECIMAL(10,2) DEFAULT 0;
    END", dbConn);
    cmdVACache.ExecuteNonQuery();

    // Atualiza/Cria a tabela de configurações de Sindicato
    using var cmdVASettings = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ValeAlimentacaoSettings' and xtype='U') BEGIN
        CREATE TABLE ValeAlimentacaoSettings (
            NumEmp INT,
            CodFil INT,
            NomeFilial NVARCHAR(255),
            CodSin INT,
            NomeSindicato NVARCHAR(255),
            ValorDiario DECIMAL(18,2) DEFAULT 0,
            PRIMARY KEY (NumEmp, CodFil, CodSin)
        )
    END ELSE BEGIN
        -- Se a tabela antiga existia baseada em Cargo, vamos migrar (DROP e CREATE por causa da PK)
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ValeAlimentacaoSettings' AND COLUMN_NAME='CodSin')
        BEGIN
            DROP TABLE ValeAlimentacaoSettings;
            CREATE TABLE ValeAlimentacaoSettings (
                NumEmp INT,
                CodFil INT,
                NomeFilial NVARCHAR(255),
                CodSin INT,
                NomeSindicato NVARCHAR(255),
                ValorDiario DECIMAL(18,2) DEFAULT 0,
                PRIMARY KEY (NumEmp, CodFil, CodSin)
            );
        END
    END", dbConn);
    cmdVASettings.ExecuteNonQuery();

    // --- Seed Inicial ValeAlimentacaoSettings ---
    // Removemos o check de se está vazio se preferirmos que ele popule novos cargos automaticamente
    // Mas para este caso, se já tiver dados, deixaremos quieto para não sobrescrever o que o usuário já fez.
    using (var cmdCheckEmpty = new SqlCommand("SELECT COUNT(*) FROM ValeAlimentacaoSettings", dbConn)) {
        int settingsCount = (int)cmdCheckEmpty.ExecuteScalar();
        if (settingsCount == 0)
        {
            Console.WriteLine("Populando ValeAlimentacaoSettings com dados do Senior...");
            try {
                // Usamos o connection string de produção do Senior
                using (var sConn = new SqlConnection("Server=portalrh.gruposetup.com,1433;Database=VETORH;User Id=upquery;Password=Upquery2022@;TrustServerCertificate=true;")) {
                    sConn.Open();
                    using var sCmd = new SqlCommand(@"
                    SELECT DISTINCT F.numemp, F.codfil, L.nomfil, COALESCE(HS.codsin, 0) AS codsin, ISNULL(S.nomsin, 'SINDICATO NÃO INFORMADO') AS sindicato
                    FROM R034FUN F 
                    JOIN R030FIL L ON F.numemp = L.numemp AND F.codfil = L.codfil
                    LEFT JOIN R038HSI HS ON F.numemp = HS.numemp AND F.tipcol = HS.tipcol AND F.numcad = HS.numcad 
                    AND HS.datalt = (SELECT MAX(datalt) FROM R038HSI WHERE numemp=F.numemp AND tipcol=F.tipcol AND numcad=F.numcad)
                    LEFT JOIN R014SIN S ON HS.codsin = S.codsin
                    WHERE F.sitafa <> '7' AND F.tipcol IN (1, 2)", sConn);
                    
                    using var reader = sCmd.ExecuteReader();
                    while (reader.Read()) {
                        using var iCmd = new SqlCommand(@"
                            INSERT INTO ValeAlimentacaoSettings (NumEmp, CodFil, NomeFilial, CodSin, NomeSindicato, ValorDiario)
                            VALUES (@e, @f, @nf, @cs, @ns, 47.50)", dbConn);
                        iCmd.Parameters.AddWithValue("@e", reader["numemp"]);
                        iCmd.Parameters.AddWithValue("@f", reader["codfil"]);
                        iCmd.Parameters.AddWithValue("@nf", reader["nomfil"]?.ToString()?.Trim());
                        iCmd.Parameters.AddWithValue("@cs", reader["codsin"]);
                        iCmd.Parameters.AddWithValue("@ns", reader["sindicato"]?.ToString()?.Trim());
                        iCmd.ExecuteNonQuery();
                    }
                    Console.WriteLine("ValeAlimentacaoSettings populado com sucesso.");
                }
            } catch (Exception ex) {
                Console.WriteLine("Erro ao popular ValeAlimentacaoSettings: " + ex.Message);
            }
        }
    }

    // --- Novas Tabelas de Gestão de Acessos ---
    using var cmdGroups = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Groups' and xtype='U') 
        BEGIN 
            CREATE TABLE Groups (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(50) UNIQUE NOT NULL);
            INSERT INTO Groups (Name) VALUES ('Administrador'), ('Padrao');
        END", dbConn);
    cmdGroups.ExecuteNonQuery();

    using var cmdGroupScreens = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GroupScreens' and xtype='U') 
        BEGIN 
            CREATE TABLE GroupScreens (GroupId INT, ScreenPath NVARCHAR(100), FOREIGN KEY (GroupId) REFERENCES Groups(Id) ON DELETE CASCADE);
            -- Permissão total para Administrador (Id=1)
            INSERT INTO GroupScreens (GroupId, ScreenPath) VALUES (1, 'home.html'), (1, 'dp.html'), (1, 'rotinas.html'), (1, 'usuarios.html'), (1, 'grupos.html'), (1, 'vale_alimentacao.html');
            -- Permissão básica para Padrao (Id=2)
            INSERT INTO GroupScreens (GroupId, ScreenPath) VALUES (2, 'home.html');
        END", dbConn);
    cmdGroupScreens.ExecuteNonQuery();



    using var cmdGroupVA = new SqlCommand(@"IF NOT EXISTS (SELECT 1 FROM GroupScreens WHERE GroupId = 1 AND ScreenPath = 'vale_alimentacao.html') 
        BEGIN 
            INSERT INTO GroupScreens (GroupId, ScreenPath) VALUES (1, 'vale_alimentacao.html');
        END", dbConn);
    cmdGroupVA.ExecuteNonQuery();

    using var cmdUserCC = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserCostCenters' and xtype='U') 
        BEGIN 
            CREATE TABLE UserCostCenters (UserId INT, CostCenterCode NVARCHAR(50), FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE);
        END", dbConn);
    cmdUserCC.ExecuteNonQuery();

    using var cmdAddGroupId = new SqlCommand(@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Users' AND COLUMN_NAME='GroupId') 
        BEGIN 
            ALTER TABLE Users ADD GroupId INT DEFAULT 2;
        END", dbConn);
    cmdAddGroupId.ExecuteNonQuery();

    using var cmdUpdateAdmin = new SqlCommand("UPDATE Users SET GroupId = 1 WHERE Username = 'admin' OR Email = 'eduardo.demetrio@gruposetup.com' OR Username = 'eduardo.demetrio@gruposetup.com'", dbConn);
    cmdUpdateAdmin.ExecuteNonQuery();
}

app.MapPost("/api/register", async (UserDTO dto) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    
    using var checkCmd = new SqlCommand("SELECT Id FROM Users WHERE Username=@u OR Email=@e", conn);
    checkCmd.Parameters.AddWithValue("@u", dto.Username);
    checkCmd.Parameters.AddWithValue("@e", dto.Email);
    if(await checkCmd.ExecuteScalarAsync() != null) return Results.BadRequest(new { message = "Username or Email already exists" });
    
    using var insertCmd = new SqlCommand("INSERT INTO Users (Username, PasswordHash, Email) VALUES (@u, @p, @e)", conn);
    insertCmd.Parameters.AddWithValue("@u", dto.Username);
    var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    insertCmd.Parameters.AddWithValue("@p", hash);
    insertCmd.Parameters.AddWithValue("@e", dto.Email);
    await insertCmd.ExecuteNonQueryAsync();
    
    return Results.Ok(new { message = "User registered successfully" });
});

app.MapPost("/api/login", async (UserDTO dto) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT Username, PasswordHash, ISNULL(IsActive, 1) FROM Users WHERE Username=@u OR Email=@u", conn);
    cmd.Parameters.AddWithValue("@u", dto.Username);
    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync()) {
        var dbUsername = reader.GetString(0);
        var hash = reader.GetString(1);
        if (!reader.GetBoolean(2)) return Results.BadRequest(new { message = "Seu usuário está desativado. Contate o TI." });
        
        if (BCrypt.Net.BCrypt.Verify(dto.Password, hash)) {
            // Buscar permissões
            var screens = new List<string>();
            int groupId = 2;
            using var conn2 = new SqlConnection(connStr);
            await conn2.OpenAsync();
            using var cmd2 = new SqlCommand("SELECT g.Id, gs.ScreenPath FROM Groups g LEFT JOIN GroupScreens gs ON g.Id = gs.GroupId WHERE g.Id = (SELECT GroupId FROM Users WHERE Username=@u OR Email=@u)", conn2);
            cmd2.Parameters.AddWithValue("@u", dto.Username);
            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync()) {
                groupId = reader2.GetInt32(0);
                if (!reader2.IsDBNull(1)) screens.Add(reader2.GetString(1));
            }
            
            return Results.Ok(new { 
                message = "Login successful", 
                username = dbUsername, 
                groupId = groupId,
                screens = screens
            });
        }
    }
    return Results.Unauthorized();
});

app.MapPost("/api/reset-password", async (UserDTO dto) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var checkCmd = new SqlCommand("SELECT Id FROM Users WHERE Username=@u AND Email=@e", conn);
    checkCmd.Parameters.AddWithValue("@u", dto.Username);
    checkCmd.Parameters.AddWithValue("@e", dto.Email);
    var id = await checkCmd.ExecuteScalarAsync();
    if(id == null) return Results.NotFound(new { message = "User not found" });
    var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
    using var updateCmd = new SqlCommand("UPDATE Users SET PasswordHash=@p WHERE Id=@id", conn);
    updateCmd.Parameters.AddWithValue("@p", hash);
    updateCmd.Parameters.AddWithValue("@id", id);
    await updateCmd.ExecuteNonQueryAsync();
    return Results.Ok(new { message = "Password reset successful" });
});

// --- FacilitiesJobs: leitura do cache local ---
app.MapGet("/api/facilities-jobs", async () => {
    var results = new List<object>();
    try {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT JobTitle, EstCar, CodCar, SalaryBase, UpdatedAt FROM FacilitiesJobs ORDER BY JobTitle", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            results.Add(new {
                JobTitle    = reader["JobTitle"].ToString(),
                EstCar      = Convert.ToInt32(reader["EstCar"]),
                CodCar      = reader["CodCar"].ToString(),
                SalaryBase  = Convert.ToDecimal(reader["SalaryBase"]),
                UpdatedAt   = Convert.ToDateTime(reader["UpdatedAt"]).ToString("dd/MM/yyyy HH:mm")
            });
        }
        return Results.Ok(results);
    } catch(Exception ex) {
        return Results.Problem(ex.Message);
    }
});

// --- Sync Facilities Jobs: consulta Senior → grava cache local ---
app.MapPost("/api/jobs/sync-facilities-jobs", async () => {
    var status = "Executado";
    var msg    = "Cargos Facilities sincronizados com sucesso.";
    try {
        using var seniorConn = new SqlConnection(seniorConnString);
        await seniorConn.OpenAsync();

        var query = @"
            SELECT CAR.TITCAR, CAR.ESTCAR, CAR.CODCAR, MAX(F.VALSAL) AS SalarioBase
            FROM R034FUN F
            INNER JOIN R024CAR CAR ON F.ESTCAR = CAR.ESTCAR AND F.CODCAR = CAR.CODCAR
            WHERE F.SITAFA <> 7
              AND F.VALSAL > 0
              AND (
                CAR.TITCAR LIKE '%FACILITIES%' OR CAR.TITCAR LIKE '%ASG%'
                OR CAR.TITCAR LIKE '%LIMPEZ%'   OR CAR.TITCAR LIKE '%SERVENTE%'
                OR CAR.TITCAR LIKE '%PORTARIA%' OR CAR.TITCAR LIKE '%CONSERV%'
                OR CAR.TITCAR LIKE '%RECEPCI%'  OR CAR.TITCAR LIKE '%VIGILANT%'
                OR CAR.TITCAR LIKE '%ZELADORIA%' OR CAR.TITCAR LIKE '%SUPERVISOR%'
              )
            GROUP BY CAR.TITCAR, CAR.ESTCAR, CAR.CODCAR
            ORDER BY CAR.TITCAR";

        using var sCmd = new SqlCommand(query, seniorConn);
        using var sReader = await sCmd.ExecuteReaderAsync();

        var items = new List<dynamic>();
        while(await sReader.ReadAsync()) {
            items.Add(new {
                Title    = sReader["TITCAR"].ToString(),
                EstCar   = Convert.ToInt32(sReader["ESTCAR"]),
                CodCar   = sReader["CODCAR"].ToString(),
                Salary   = Convert.ToDecimal(sReader["SalarioBase"])
            });
        }
        sReader.Close();

        using var localConn = new SqlConnection(connStr);
        await localConn.OpenAsync();
        using var clearCmd = new SqlCommand("DELETE FROM FacilitiesJobs", localConn);
        await clearCmd.ExecuteNonQueryAsync();

        foreach(var i in items) {
            using var insCmd = new SqlCommand(
                "INSERT INTO FacilitiesJobs (JobTitle, EstCar, CodCar, SalaryBase) VALUES (@t, @e, @c, @s)",
                localConn);
            insCmd.Parameters.AddWithValue("@t", (object)i.Title  ?? DBNull.Value);
            insCmd.Parameters.AddWithValue("@e", (object)i.EstCar ?? DBNull.Value);
            insCmd.Parameters.AddWithValue("@c", (object)i.CodCar ?? DBNull.Value);
            insCmd.Parameters.AddWithValue("@s", (object)i.Salary ?? DBNull.Value);
            await insCmd.ExecuteNonQueryAsync();
        }

        msg = $"{items.Count} cargo(s) sincronizados com sucesso.";
    } catch(Exception e) {
        status = "Erro";
        msg    = e.Message;
    }

    using var logConn = new SqlConnection(connStr);
    await logConn.OpenAsync();
    using var logCmd = new SqlCommand(
        "INSERT INTO JobLogs (SystemName, RoutineName, Status, Message) VALUES ('Senior', 'Cargos Facilities', @s, @m)",
        logConn);
    logCmd.Parameters.AddWithValue("@s", status);
    logCmd.Parameters.AddWithValue("@m", msg);
    await logCmd.ExecuteNonQueryAsync();

    if(status == "Erro") return Results.Problem(msg);
    return Results.Ok(new { message = msg });
});

app.MapGet("/api/birthdays", async () => {
    var results = new List<object>();
    try {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT Registration, Name, JobTitle, CostCenter, BirthDate, Age, UpdatedAt FROM SeniorBirthdays", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            results.Add(new {
                Registration = reader["Registration"].ToString(),
                Name = reader["Name"]?.ToString() ?? "",
                JobTitle = reader["JobTitle"]?.ToString() ?? "-",
                CostCenter = reader["CostCenter"]?.ToString() ?? "-",
                BirthDate = reader["BirthDate"]?.ToString() ?? "",
                Age = reader["Age"]
            });
        }
        return Results.Ok(results);
    } catch(Exception ex) {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/jobs/logs", async () => {
    var results = new List<object>();
    try {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT top 20 Id, SystemName, RoutineName, Status, ExecutedAt, Message FROM JobLogs ORDER BY ExecutedAt DESC", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            results.Add(new {
                System = reader["SystemName"].ToString(),
                Routine = reader["RoutineName"].ToString(),
                Status = reader["Status"].ToString(),
                ExecutedAt = Convert.ToDateTime(reader["ExecutedAt"]).ToString("dd/MM/yyyy HH:mm:ss"),
                Message = reader["Message"]?.ToString()
            });
        }
        return Results.Ok(results);
    } catch(Exception ex) {
        return Results.Problem(ex.Message);
    }
});

// --- Sync Birthdays: consulta Senior → grava cache local ---
app.MapPost("/api/jobs/sync-birthdays", async () => {
    var status = "Executado";
    var msg    = "Aniversariantes sincronizados com sucesso.";
    try {
        using var seniorConn = new SqlConnection(seniorConnString);
        await seniorConn.OpenAsync();
        
        var query = @"
            SELECT top 20
                CAST(F.NUMCAD AS VARCHAR) as Registration,
                F.NOMFUN as Name,
                F.DATNAS as BirthDate,
                CAR.TITCAR as JobTitle,
                C.NOMCCU as CostCenter
            FROM R034FUN F
            LEFT JOIN R018CCU C ON F.NUMEMP = C.NUMEMP AND F.CODCCU = C.CODCCU
            LEFT JOIN R024CAR CAR ON F.ESTCAR = CAR.ESTCAR AND F.CODCAR = CAR.CODCAR
            WHERE F.SITAFA <> 7 
              AND F.DATNAS IS NOT NULL
              AND MONTH(F.DATNAS) = MONTH(GETDATE())
              AND DAY(F.DATNAS) >= DAY(GETDATE())
            ORDER BY DAY(F.DATNAS) ASC
        ";
        using var sCmd = new SqlCommand(query, seniorConn);
        using var sReader = await sCmd.ExecuteReaderAsync();
        
        var items = new List<dynamic>();
        while(await sReader.ReadAsync()) {
            var n = sReader["Name"]?.ToString() ?? "";
            var bDateDT = Convert.ToDateTime(sReader["BirthDate"]);
            items.Add(new {
                Reg = sReader["Registration"].ToString(),
                Name = n,
                Job = sReader["JobTitle"]?.ToString() ?? "-",
                CC = sReader["CostCenter"]?.ToString() ?? "-",
                BDate = bDateDT.ToString("dd/MMM").ToUpper(),
                Age = DateTime.Today.Year - bDateDT.Year
            });
        }
        sReader.Close();
        
        using var localConn = new SqlConnection(connStr);
        await localConn.OpenAsync();
        using var clearCmd = new SqlCommand("DELETE FROM SeniorBirthdays", localConn);
        await clearCmd.ExecuteNonQueryAsync();
        
        foreach(var i in items) {
            using var insCmd = new SqlCommand("INSERT INTO SeniorBirthdays (Registration, Name, JobTitle, CostCenter, BirthDate, Age) VALUES (@r, @n, @j, @c, @b, @a)", localConn);
            insCmd.Parameters.AddWithValue("@r", i.Reg);
            insCmd.Parameters.AddWithValue("@n", i.Name);
            insCmd.Parameters.AddWithValue("@j", i.Job);
            insCmd.Parameters.AddWithValue("@c", i.CC);
            insCmd.Parameters.AddWithValue("@b", i.BDate);
            insCmd.Parameters.AddWithValue("@a", i.Age);
            await insCmd.ExecuteNonQueryAsync();
        }
    } catch(Exception e) {
        status = "Erro";
        msg = e.Message;
    }
    
    using var logConn = new SqlConnection(connStr);
    await logConn.OpenAsync();
    using var logCmd = new SqlCommand("INSERT INTO JobLogs (SystemName, RoutineName, Status, Message) VALUES ('Senior', 'Aniversariantes', @s, @m)", logConn);
    logCmd.Parameters.AddWithValue("@s", status);
    logCmd.Parameters.AddWithValue("@m", msg);
    await logCmd.ExecuteNonQueryAsync();
    
    if (status == "Erro") return Results.Problem(msg);
    return Results.Ok(new { message = msg });
});


app.MapPost("/api/users/{id}/toggle", async (int id) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("UPDATE Users SET IsActive = CASE WHEN ISNULL(IsActive, 1) = 1 THEN 0 ELSE 1 END WHERE Id=@id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { message = "Status invertido com sucesso" });
});

app.MapGet("/api/quotes", async () => {
    var results = new List<object>();
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT Id, ClientName, RoleName, Postos, Pessoas, TotalValue, CreatedBy, CreatedAt FROM Quotes ORDER BY CreatedAt DESC", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    while(await reader.ReadAsync()) {
         results.Add(new {
             Id = reader.GetInt32(0),
             ClientName = reader.GetString(1),
             RoleName = reader.GetString(2),
             Postos = reader.GetInt32(3),
             Pessoas = reader.GetInt32(4),
             TotalValue = reader.GetDecimal(5),
             CreatedBy = reader.GetString(6),
             CreatedAt = reader.GetDateTime(7).ToString("dd/MM/yy HH:mm")
         });
    }
    return Results.Ok(results);
});

app.MapPost("/api/quotes", async (QuoteDTO dto) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("INSERT INTO Quotes (ClientName, RoleName, Postos, Pessoas, TotalValue, CreatedBy) VALUES (@c, @r, @po, @pe, @t, @u)", conn);
    cmd.Parameters.AddWithValue("@c", dto.ClientName);
    cmd.Parameters.AddWithValue("@r", dto.RoleName);
    cmd.Parameters.AddWithValue("@po", dto.Postos);
    cmd.Parameters.AddWithValue("@pe", dto.Pessoas);
    cmd.Parameters.AddWithValue("@t", dto.TotalValue);
    cmd.Parameters.AddWithValue("@u", dto.CreatedBy);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { message = "Cotação salva" });
});

// ==========================================
// MÓDULO DP - DOSSIÊ DO COLABORADOR
// ==========================================

app.MapGet("/api/employees/search", async (string q) =>
{
    var employees = new List<object>();
    using (var conn = new SqlConnection(seniorConnString))
    {
        await conn.OpenAsync();
        string sql = @"SELECT TOP 20 NUMCAD, NOMFUN, SITAFA 
                       FROM R034FUN 
                       WHERE (NOMFUN LIKE @q OR CAST(NUMCAD AS VARCHAR) LIKE @q)
                       ORDER BY NOMFUN";
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@q", "%" + q + "%");
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    employees.Add(new { 
                        numcad = reader["NUMCAD"], 
                        nomfun = reader["NOMFUN"].ToString().Trim(),
                        status = Convert.ToInt32(reader["SITAFA"]) == 7 ? "Demitido" : "Ativo"
                    });
                }
            }
        }
    }
    return Results.Ok(employees);
});

app.MapGet("/api/employees/{numcad}/dossier", async (int numcad) =>
{
    try
    {
        using (var conn = new SqlConnection(seniorConnString))
        {
            await conn.OpenAsync();
            
            // 1. Dados Básicos, Pessoais e Endereço
            string sqlBasic = @"
                SELECT F.NUMCAD, F.NOMFUN, F.DATADM, F.DATAFA as DATDEM, F.VALSAL, F.SITAFA,
                       P.NUMCPF, P.NUMCID as NUMIDT, P.NUMPIS, P.DATNAS, P.NUMTEL, P.DDDTEL as DDDTEL,
                       E.ENDRUA, E.ENDNUM, E.ENDCPL, E.ENDCEP,
                       CAR.TITCAR, CC.NOMCCU, SIT.DESSIT
                FROM R034FUN F
                LEFT JOIN R033PES P ON F.CODPES = P.CODPES
                LEFT JOIN R033END E ON P.CODPES = E.CODPES AND E.TIPEND = 1
                LEFT JOIN R024CAR CAR ON F.ESTCAR = CAR.ESTCAR AND F.CODCAR = CAR.CODCAR
                LEFT JOIN R018CCU CC ON F.NUMEMP = CC.NUMEMP AND F.CODCCU = CC.CODCCU
                LEFT JOIN R010SIT SIT ON F.SITAFA = SIT.CODSIT
                WHERE F.NUMCAD = @numcad";

            object basicInfo = null;
            using (var cmd = new SqlCommand(sqlBasic, conn))
            {
                cmd.Parameters.AddWithValue("@numcad", numcad);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        basicInfo = new {
                            numcad = reader["NUMCAD"],
                            nomfun = reader["NOMFUN"].ToString().Trim(),
                            datadm = reader["DATADM"],
                            datdem = reader["DATDEM"] != DBNull.Value ? reader["DATDEM"] : null,
                            valsal = reader["VALSAL"],
                            sitafa = Convert.ToInt32(reader["SITAFA"]),
                            statusDesc = reader["DESSIT"].ToString().Trim(),
                            cpf = reader["NUMCPF"],
                            rg = reader["NUMIDT"],
                            pis = reader["NUMPIS"],
                            datnas = reader["DATNAS"],
                            telefone = reader["NUMTEL"],
                            endereco = $"{reader["ENDRUA"].ToString().Trim()}, {reader["ENDNUM"]} {reader["ENDCPL"]}".Trim(),
                            cep = reader["ENDCEP"],
                            cargo = reader["TITCAR"].ToString().Trim(),
                            centroCusto = reader["NOMCCU"].ToString().Trim()
                        };
                    }
                }
            }

            if (basicInfo == null) return Results.NotFound("Funcionário não encontrado.");

            // 2. Financeiro (Holerite - Últimos 6 meses)
            // Nota: Padronização Senior: CODEVE < 4000 costumam ser Proventos, >= 4000 Descontos
            var financial = new List<object>();
            string sqlFin = @"
                SELECT TOP 6 C.PERREF, 
                       SUM(CASE WHEN M.CODEVE < 4000 THEN M.VALEVE ELSE 0 END) as Proventos,
                       SUM(CASE WHEN M.CODEVE >= 4000 AND M.CODEVE < 9000 THEN M.VALEVE ELSE 0 END) as Descontos
                FROM R044MOV M
                JOIN R044CAL C ON M.CODCAL = C.CODCAL
                WHERE M.NUMCAD = @numcad
                GROUP BY C.PERREF
                ORDER BY C.PERREF DESC";
            
            using (var cmd = new SqlCommand(sqlFin, conn))
            {
                cmd.Parameters.AddWithValue("@numcad", numcad);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        decimal prov = Convert.ToDecimal(reader["Proventos"]);
                        decimal desc = Convert.ToDecimal(reader["Descontos"]);
                        financial.Add(new {
                            periodo = reader["PERREF"],
                            proventos = prov,
                            descontos = desc,
                            liquido = prov - desc
                        });
                    }
                }
            }
                            // 3. Histórico de Afastamentos
            var absences = new List<object>();
            try {
                string sqlAbs = @"SELECT TOP 5 H.DATAFA, H.DATTER, S.DESSIT 
                                 FROM R038AFA H 
                                 JOIN R010SIT S ON H.SITAFA = S.CODSIT 
                                 WHERE H.NUMCAD = @numcad 
                                 ORDER BY H.DATAFA DESC";
                using (var cmd = new SqlCommand(sqlAbs, conn))
                {
                    cmd.Parameters.AddWithValue("@numcad", numcad);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            absences.Add(new {
                                inicio = reader["DATAFA"],
                                termino = reader["DATTER"] != DBNull.Value ? reader["DATTER"] : null,
                                motivo = reader["DESSIT"].ToString().Trim()
                            });
                        }
                    }
                }
            } catch { /* Ignora erro na seção específica */ }

            // 4. Promoções (Histórico de Cargo)
            var promotions = new List<object>();
            try {
                string sqlProm = @"SELECT TOP 5 H.DATALT, C.TITCAR 
                                  FROM R038HCA H 
                                  JOIN R024CAR C ON H.ESTCAR = C.ESTCAR AND H.CODCAR = C.CODCAR 
                                  WHERE H.NUMCAD = @numcad 
                                  ORDER BY H.DATALT DESC";
                using (var cmd = new SqlCommand(sqlProm, conn))
                {
                    cmd.Parameters.AddWithValue("@numcad", numcad);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            promotions.Add(new {
                                data = reader["DATALT"],
                                cargo = reader["TITCAR"].ToString().Trim()
                            });
                        }
                    }
                }
            } catch { }

            // 5. Histórico de Centro de Custo
            var ccHistory = new List<object>();
            try {
                string sqlCC = @"SELECT TOP 5 H.DATALT, C.NOMCCU 
                                FROM R038HCC H 
                                JOIN R018CCU C ON H.NUMEMP = C.NUMEMP AND H.CODCCU = C.CODCCU 
                                WHERE H.NUMCAD = @numcad 
                                ORDER BY H.DATALT DESC";
                using (var cmd = new SqlCommand(sqlCC, conn))
                {
                    cmd.Parameters.AddWithValue("@numcad", numcad);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ccHistory.Add(new {
                                data = reader["DATALT"],
                                centroCusto = reader["NOMCCU"].ToString().Trim()
                            });
                        }
                    }
                }
            } catch { }

            // 6. Férias
            var vacations = new List<object>();
            try {
                string sqlVac = @"SELECT TOP 3 DATINI, DATFIM, SITFER 
                                 FROM R040FEV 
                                 WHERE NUMCAD = @numcad 
                                 ORDER BY DATINI DESC";
                using (var cmd = new SqlCommand(sqlVac, conn))
                {
                    cmd.Parameters.AddWithValue("@numcad", numcad);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string sit = reader["SITFER"].ToString();
                            string sitDesc = sit == "1" ? "Gozadas" : (sit == "2" ? "Pagas" : "Agendadas");
                            vacations.Add(new {
                                inicio = reader["DATINI"],
                                fim = reader["DATFIM"],
                                status = sitDesc
                            });
                        }
                    }
                }
            } catch { }
            
            return Results.Ok(new {
                basic = basicInfo,
                financial = financial,
                absences = absences,
                promotions = promotions,
                ccHistory = ccHistory,
                vacations = vacations
            });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem("Falha ao gerar dossiê: " + ex.ToString());
    }
});

// ==========================================
// MÓDULO DP - VALE ALIMENTAÇÃO (CACHE LOCAL)
// ==========================================

// Helper: calcula a competencia atual (26 do mês anterior ao 25 do mês atual)
static (DateTime inicio, DateTime fim) GetCompetencia() {
    var hoje = DateTime.Today;
    DateTime inicio, fim;
    if (hoje.Day >= 26) {
        inicio = new DateTime(hoje.Year, hoje.Month, 26);
        var proxMes = hoje.AddMonths(1);
        fim = new DateTime(proxMes.Year, proxMes.Month, 25);
    } else {
        var mesAnterior = hoje.AddMonths(-1);
        inicio = new DateTime(mesAnterior.Year, mesAnterior.Month, 26);
        fim = new DateTime(hoje.Year, hoje.Month, 25);
    }
    return (inicio, fim);
}

// Helper: calcula a competencia anterior (26 do mês retrasado a 25 do mês anterior ao atual)
static (DateTime inicio, DateTime fim) GetCompetenciaAnterior() {
    var (atualInicio, _) = GetCompetencia();
    var fim = atualInicio.AddDays(-1);
    var inicio = new DateTime(fim.Year, fim.Month, 26).AddMonths(-1);
    return (inicio, fim);
}

// Helper: gera lista de feriados nacionais (fixos e móveis) para um ano
static HashSet<DateTime> GetNationalHolidays(int ano) {
    var feriados = new HashSet<DateTime>();

    // Feriados móveis (Páscoa)
    int c = ano / 100;
    int n = ano - 19 * (ano / 19);
    int k = (c - 17) / 25;
    int i = c - c / 4 - (c - k) / 3 + 19 * n + 15;
    i = i - 30 * (i / 30);
    i = i - (i / 28) * (1 - (i / 28) * (29 / (i + 1)) * ((21 - n) / 11));
    int j = ano + ano / 4 + i + 2 - c + c / 4;
    j = j - 7 * (j / 7);
    int l = i - j;
    int m = 3 + (l + 40) / 44;
    int d = l + 28 - 31 * (m / 4);
    DateTime pascoa = new DateTime(ano, m, d);
    
    feriados.Add(pascoa.AddDays(-2)); // Sexta-feira Santa
    feriados.Add(pascoa.AddDays(-47)); // Terça de carnaval
    feriados.Add(pascoa.AddDays(60)); // Corpus Christi

    // Feriados Fixos
    feriados.Add(new DateTime(ano, 1, 1));   // Confraternização Universal
    feriados.Add(new DateTime(ano, 4, 21));  // Tiradentes
    feriados.Add(new DateTime(ano, 5, 1));   // Trabalhador
    feriados.Add(new DateTime(ano, 9, 7));   // Independência
    feriados.Add(new DateTime(ano, 10, 12)); // N. S. Aparecida
    feriados.Add(new DateTime(ano, 11, 2));  // Finados
    feriados.Add(new DateTime(ano, 11, 15)); // Proclamação da República
    feriados.Add(new DateTime(ano, 12, 25)); // Natal

    return feriados;
}

// Helper: calcula os dias úteis (Seg-Sex) de um mês específico descontando feriados nacionais
static int CalcularDiasUteisMes(int ano, int mes) {
    int dias = DateTime.DaysInMonth(ano, mes);
    int diasUteis = 0;
    var feriados = GetNationalHolidays(ano);

    for (int day = 1; day <= dias; day++) {
        DateTime dt = new DateTime(ano, mes, day);
        if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday) {
            if (!feriados.Contains(dt)) {
                diasUteis++;
            }
        }
    }
    return diasUteis;
}

// Helper: Obtém os dias de trabalho previstos por escala no mês (Projetado via R006HOR)
static async Task<Dictionary<int, List<DateTime>>> GetScaleWorkDays(string seniorConnString, IEnumerable<int> scaleIds, DateTime start, DateTime end) {
    var result = new Dictionary<int, List<DateTime>>();
    var distinctIds = scaleIds.Distinct().ToList();
    if (!distinctIds.Any()) return result;

    var scaleRules = new Dictionary<int, List<bool>>();
    using (var conn = new SqlConnection(seniorConnString)) {
        await conn.OpenAsync();
        
        int batchSize = 100;
        for (int i = 0; i < distinctIds.Count; i += batchSize) {
            var batch = distinctIds.Skip(i).Take(batchSize);
            var idsStr = string.Join(",", batch);
            var sql = $@"
                SELECT H.codesc, H.seqreg, T.deshor
                FROM R006HOR H
                JOIN R004HOR T ON H.codhor = T.codhor
                WHERE H.codesc IN ({idsStr})
                ORDER BY H.codesc, H.seqreg";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
                int esc = Convert.ToInt32(reader["codesc"]);
                string des = reader["deshor"]?.ToString()?.ToUpper() ?? "";
                
                // Detecção por palavra-chave: Se não tem Folga/DSR/Compensado, é Trabalho.
                bool isWork = !des.Contains("FOLGA") && 
                              !des.Contains("DSR") && 
                              !des.Contains("COMPENS") && 
                              !des.Contains("FERIADO") && 
                              !des.Contains("DESCANSO");

                if (!scaleRules.ContainsKey(esc)) scaleRules[esc] = new List<bool>();
                scaleRules[esc].Add(isWork);
            }
        }
    }

    // Projetamos o calendário para o mês
    foreach (var rule in scaleRules) {
        var workDates = new List<DateTime>();
        var codesc = rule.Key;
        var cycle = rule.Value;
        if (cycle.Count == 0) continue;

        for (DateTime dt = start; dt <= end; dt = dt.AddDays(1)) {
            int seq = 1;
            // Para escalas Tipo 'P' (Prorrogáveis/Semanais), Seq 1 = Segunda-feira
            if (cycle.Count == 7 || cycle.Count == 14 || cycle.Count == 21 || cycle.Count == 28) {
                int dayOfWeek = (int)dt.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Domingo
                seq = dayOfWeek;
            } else {
                // Ciclo Revezamento ou Outro (Simula a partir do dia 1 do mês)
                int daysRef = (dt - start).Days;
                seq = (daysRef % cycle.Count) + 1;
            }

            if (seq <= cycle.Count) {
                if (cycle[seq - 1]) {
                    workDates.Add(dt.Date);
                }
            }
        }
        result[codesc] = workDates;
    }
    Console.WriteLine($"[DEBUG_V3] GetScaleWorkDays finalizado. Escalas com regras: {scaleRules.Count}. Escalas com dias projetados: {result.Count}");
    return result;
}

// GET: lê do cache local
app.MapGet("/api/dp/vale_alimentacao", async () => {
    var results = new List<object>();
    try {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT Matricula, Nome, Nascimento, CPF, Situacao, Admissao, Demissao,
                   Funcao, CentroCusto, Escala, DiasPrevistos, DiasExtras, Ausencias,
                   ValorDiario, ValorVA,
                   CompetenciaInicio, CompetenciaFim, UpdatedAt,
                   WorkDatesJson, ExtraDatesJson, AbsenceDatesJson, DiasFeriados
            FROM ValeAlimentacaoCache
            ORDER BY Nome", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            results.Add(new {
                Matricula    = reader["Matricula"],
                Nome         = reader["Nome"]?.ToString()?.Trim(),
                Nascimento   = reader["Nascimento"]?.ToString(),
                CPF          = reader["CPF"]?.ToString()?.Trim(),
                Situacao     = reader["Situacao"]?.ToString()?.Trim(),
                Admissao     = reader["Admissao"]?.ToString(),
                Demissao     = reader["Demissao"]?.ToString(),
                Funcao       = reader["Funcao"]?.ToString()?.Trim(),
                CentroCusto  = reader["CentroCusto"]?.ToString()?.Trim(),
                Escala       = reader["Escala"]?.ToString()?.Trim(),
                DiasPrevistos= reader["DiasPrevistos"] != DBNull.Value ? Convert.ToDecimal(reader["DiasPrevistos"]) : (decimal?)null,
                DiasExtras   = reader["DiasExtras"]    != DBNull.Value ? Convert.ToDecimal(reader["DiasExtras"])    : 0,
                Ausencias    = reader["Ausencias"]     != DBNull.Value ? Convert.ToDecimal(reader["Ausencias"])     : 0,
                DiasFeriados = reader["DiasFeriados"]  != DBNull.Value ? Convert.ToDecimal(reader["DiasFeriados"])   : 0,
                ValorDiario  = reader["ValorDiario"]   != DBNull.Value ? Convert.ToDecimal(reader["ValorDiario"])   : (decimal?)null,
                ValorVA      = reader["ValorVA"]       != DBNull.Value ? Convert.ToDecimal(reader["ValorVA"])       : (decimal?)null,
                WorkDatesJson = reader["WorkDatesJson"]?.ToString(),
                ExtraDatesJson = reader["ExtraDatesJson"]?.ToString(),
                AbsenceDatesJson = reader["AbsenceDatesJson"]?.ToString(),
                Competencia  = reader["CompetenciaInicio"] != DBNull.Value
                    ? $"{Convert.ToDateTime(reader["CompetenciaInicio"]):dd/MM/yyyy} a {Convert.ToDateTime(reader["CompetenciaFim"]):dd/MM/yyyy}"
                    : "",
                UpdatedAt    = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]).ToString("dd/MM/yyyy HH:mm") : ""
            });
        }
        return Results.Ok(results);
    } catch(Exception ex) {
        return Results.Problem(ex.Message);
    }
});

// GET: busca configurações de valores por cargo
app.MapGet("/api/dp/vale_alimentacao/settings", async () => {
    var results = new List<object>();
    try {
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        // Busca todos as combines únicas de Filial e Sindicato
        using var cmd = new SqlCommand("SELECT NumEmp, CodFil, NomeFilial, CodSin, NomeSindicato, ValorDiario FROM ValeAlimentacaoSettings ORDER BY NomeFilial, NomeSindicato", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while(await reader.ReadAsync()) {
            results.Add(new {
                NumEmp = reader["NumEmp"],
                CodFil = reader["CodFil"],
                NomeFilial = reader["NomeFilial"]?.ToString()?.Trim(),
                CodSin = reader["CodSin"],
                NomeSindicato = reader["NomeSindicato"]?.ToString()?.Trim(),
                ValorDiario = Convert.ToDecimal(reader["ValorDiario"])
            });
        }
        return Results.Ok(results);
    } catch(Exception ex) {
        return Results.Problem(ex.Message);
    }
});

// POST: salva valor para um cargo
app.MapPost("/api/dp/vale_alimentacao/settings", async (JsonElement body) => {
    try {
        var numEmp = body.GetProperty("numEmp").GetInt32();
        var codFil = body.GetProperty("codFil").GetInt32();
        var codSin = body.GetProperty("codSin").GetInt32();
        var valor = body.GetProperty("valor").GetDecimal();
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM ValeAlimentacaoSettings WHERE NumEmp = @numEmp AND CodFil = @codFil AND CodSin = @codSin)
                UPDATE ValeAlimentacaoSettings SET ValorDiario = @valor WHERE NumEmp = @numEmp AND CodFil = @codFil AND CodSin = @codSin
            ELSE
                -- Somente atuará de forma robusta no update da seed inicial, caso precise inserir, nomeSindicato ficara nulo aqui mas a UI envia completo se necessario
                INSERT INTO ValeAlimentacaoSettings (NumEmp, CodFil, CodSin, NomeSindicato, ValorDiario) VALUES (@numEmp, @codFil, @codSin, '', @valor)", conn);
        cmd.Parameters.AddWithValue("@numEmp", numEmp);
        cmd.Parameters.AddWithValue("@codFil", codFil);
        cmd.Parameters.AddWithValue("@codSin", codSin);
        cmd.Parameters.AddWithValue("@valor", valor);
        await cmd.ExecuteNonQueryAsync();
        return Results.Ok(new { message = "Valor atualizado com sucesso." });
    } catch(Exception ex) {
        return Results.Problem(ex.Message);
    }
});

// POST: sincroniza VETORH → cache local (com lógica de competência 26-25 e valores manuais)
app.MapPost("/api/jobs/sync-vale-alimentacao", async () => {
    var status = "Executado";
    var msg = "VA sincronizado com sucesso.";
    try {
        var (compInicio, compFim) = GetCompetencia();
        var (prevInicio, prevFim) = GetCompetenciaAnterior();
        
        // Mês cheio de referência para a BASE (Dias Previstos)
        var baseStart = new DateTime(compFim.Year, compFim.Month, 1);
        var baseEnd = new DateTime(compFim.Year, compFim.Month, DateTime.DaysInMonth(compFim.Year, compFim.Month));
        var holidays = GetNationalHolidays(compFim.Year);

        // 1. Obter Configurações de Valores por Contrato e Sindicato (Local)
        var settingsByBranchRole = new Dictionary<string, decimal>();
        using (var lConn = new SqlConnection(connStr)) {
            await lConn.OpenAsync();
            using var cmd = new SqlCommand("SELECT NumEmp, CodFil, CodSin, ValorDiario FROM ValeAlimentacaoSettings", lConn);
            using var rLocal = await cmd.ExecuteReaderAsync();
            while (await rLocal.ReadAsync()) {
                var key = $"{rLocal["NumEmp"]}#{rLocal["CodFil"]}#{rLocal["CodSin"]}";
                settingsByBranchRole[key] = Convert.ToDecimal(rLocal["ValorDiario"]);
            }
        }

        // 2. Obter Dados de Extras e Ausências da Senior (R066SIT - Ponto) com Fallback para R044MOV (Folha)
        var nextiDataDetails = new Dictionary<string, (List<DateTime> extraDates, List<DateTime> absenceDates)>();
        try {
            var hoje = DateTime.Now;
            using (var sConn = new SqlConnection(seniorConnString)) {
                await sConn.OpenAsync();
                
                // 2.1. Busca Ponto Diário (R066SIT)
                using var cmdOcc = new SqlCommand(@"
                    SELECT S.numcad, S.datapu,
                           SUM(CASE WHEN T.tipsit = 16 THEN S.qtdhor ELSE 0 END) as ExtrasMin,
                           SUM(CASE WHEN T.tipsit = 15 THEN 1 ELSE 0 END) as TemFalta
                    FROM R066SIT S
                    JOIN R010SIT T ON S.codsit = T.codsit
                    WHERE S.datapu BETWEEN @start AND @end
                      AND T.tipsit IN (15, 16)
                    GROUP BY S.numcad, S.datapu", sConn);
                
                cmdOcc.Parameters.AddWithValue("@start", prevInicio);
                cmdOcc.Parameters.AddWithValue("@end", prevFim);

                using var rOcc = await cmdOcc.ExecuteReaderAsync();
                while (await rOcc.ReadAsync()) {
                    string mat = rOcc["numcad"].ToString();
                    DateTime data = Convert.ToDateTime(rOcc["datapu"]);
                    decimal extraMin = Convert.ToDecimal(rOcc["ExtrasMin"]);
                    bool temFalta = Convert.ToInt32(rOcc["TemFalta"]) > 0;

                    if (!nextiDataDetails.ContainsKey(mat)) nextiDataDetails[mat] = (new List<DateTime>(), new List<DateTime>());
                    
                    if (extraMin > 240) nextiDataDetails[mat].extraDates.Add(data);
                    if (temFalta) nextiDataDetails[mat].absenceDates.Add(data);
                }
                rOcc.Close();

                // 2.2. Fallback para Totais de Folha (R044MOV)
                using var cmdMov = new SqlCommand(@"
                    SELECT M.numcad, SUM(M.refeve) as TotalExtra, MIN(C.inicmp) as Referencia
                    FROM R044MOV M
                    JOIN R044CAL C ON M.codcal = C.codcal
                    JOIN R008EVC E ON M.tabeve = E.codtab AND M.codeve = E.codeve
                    WHERE C.perref = @mesRef
                      AND E.deseve LIKE '%EXTRA%'
                    GROUP BY M.numcad", sConn);
                
                cmdMov.Parameters.AddWithValue("@mesRef", new DateTime(hoje.Year, hoje.Month, 1));
                
                using var rMov = await cmdMov.ExecuteReaderAsync();
                while (await rMov.ReadAsync()) {
                    string mat = rMov["numcad"].ToString();
                    decimal totalExtra = Convert.ToDecimal(rMov["TotalExtra"]);
                    DateTime refDate = Convert.ToDateTime(rMov["Referencia"]);
                    
                    if (!nextiDataDetails.ContainsKey(mat)) nextiDataDetails[mat] = (new List<DateTime>(), new List<DateTime>());
                    if (nextiDataDetails[mat].extraDates.Count == 0 && totalExtra > 240) {
                        // André Style: se não tem detalhe, marca o primeiro dia do mes como representativo do extra
                        nextiDataDetails[mat].extraDates.Add(refDate);
                    }
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Erro Senior Point Sync: " + ex.Message);
        }

        // 3. Obter Colaboradores do Senior (VETORH)
        var employees = new List<dynamic>();
        using (var sConn = new SqlConnection(seniorConnString)) {
            await sConn.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT 
                    f.numemp AS NumEmp, f.codfil AS CodFil, COALESCE(hs.codsin, 0) AS CodSin, ISNULL(sin.nomsin, 'SINDICATO NÃO INFORMADO') AS NomeSindicato,
                    f.numcad AS Matricula, f.nomfun AS Nome, p.datnas AS Nascimento, p.numcpf AS CPF,
                    s.dessit AS Situacao, f.datadm AS Admissao, f.datafa AS Demissao,
                    c.titcar AS Funcao, r.nomccu AS CentroCusto, 
                    f.codesc AS CodEscala,
                    CONCAT(f.codesc, ' - ', e.nomesc) AS Escala,
                    0 AS DiasPrevistos 
                FROM R034FUN f
                JOIN R033PES p ON f.codpes = p.codpes
                JOIN R010SIT s ON f.sitafa = s.codsit
                JOIN R024CAR c ON f.estcar = c.estcar AND f.codcar = c.codcar
                JOIN R018CCU r ON f.numemp = r.numemp AND f.codccu = r.codccu
                JOIN R006ESC e ON f.codesc = e.codesc
                LEFT JOIN R038HSI hs ON f.numemp = hs.numemp AND f.tipcol = hs.tipcol AND f.numcad = hs.numcad AND hs.datalt = (SELECT MAX(datalt) FROM R038HSI WHERE numemp=f.numemp AND tipcol=f.tipcol AND numcad=f.numcad)
                LEFT JOIN R014SIN sin ON hs.codsin = sin.codsin
                WHERE f.tipcol <> 2
                  AND f.numcad NOT LIKE '1-2%'
                  AND (f.sitafa <> 7 OR (f.sitafa = 7 AND f.datafa >= @compInicio))
                ORDER BY f.nomfun", sConn);
            
            cmd.Parameters.AddWithValue("@compInicio", compInicio);

            using var rSenior = await cmd.ExecuteReaderAsync();
            while (await rSenior.ReadAsync()) {
                employees.Add(new {
                    NumEmp = Convert.ToInt32(rSenior["NumEmp"]),
                    CodFil = Convert.ToInt32(rSenior["CodFil"]),
                    CodSin = Convert.ToInt32(rSenior["CodSin"]),
                    CodEscala = Convert.ToInt32(rSenior["CodEscala"]),
                    NomeSindicato = rSenior["NomeSindicato"]?.ToString()?.Trim(),
                    Matricula = rSenior["Matricula"].ToString(),
                    Nome = rSenior["Nome"]?.ToString()?.Trim(),
                    Nascimento = rSenior["Nascimento"] != DBNull.Value ? Convert.ToDateTime(rSenior["Nascimento"]).ToString("dd/MM/yyyy") : "",
                    CPF = rSenior["CPF"]?.ToString()?.Trim(),
                    Situacao = rSenior["Situacao"]?.ToString()?.Trim(),
                    Admissao = rSenior["Admissao"] != DBNull.Value ? Convert.ToDateTime(rSenior["Admissao"]).ToString("dd/MM/yyyy") : "",
                    Demissao = rSenior["Demissao"] != DBNull.Value ? Convert.ToDateTime(rSenior["Demissao"]).ToString("dd/MM/yyyy") : "",
                    Funcao = rSenior["Funcao"]?.ToString()?.Trim(),
                    CentroCusto = rSenior["CentroCusto"]?.ToString()?.Trim(),
                    Escala = rSenior["Escala"]?.ToString()?.Trim(),
                    DiasPrevistos = 0m
                });
            }
        }

        // Fallback para 2026 se a R010SIT estiver vazia (comum em virada de ano)
        if (holidays.Count == 0 && baseStart.Year == 2026 && baseStart.Month == 4) {
            holidays.Add(new DateTime(2026, 4, 3));  // Sexta-feira Santa
            holidays.Add(new DateTime(2026, 4, 21)); // Tiradentes
        }

        // 3.1 Calcular Base por Escala Dinâmica
        var scaleWorkDaysMap = await GetScaleWorkDays(seniorConnString, employees.Select(e => (int)e.CodEscala), baseStart, baseEnd);
        var scaleFinalResults = new Dictionary<int, (decimal bruto, decimal feriados)>();

        foreach (var kvp in scaleWorkDaysMap) {
            int bruto = kvp.Value.Count;
            int feriadosCount = 0;
            foreach (var date in kvp.Value) {
                if (holidays.Contains(date)) {
                    feriadosCount++;
                }
            }
            scaleFinalResults[kvp.Key] = ((decimal)bruto, (decimal)feriadosCount);
        }

        // 4. Salvar no Cache Local e Calcular VA
        using (var lConn = new SqlConnection(connStr)) {
            await lConn.OpenAsync();
            using var tx = lConn.BeginTransaction();
            try {
                using var cmdClear = new SqlCommand("DELETE FROM ValeAlimentacaoCache", lConn, tx);
                await cmdClear.ExecuteNonQueryAsync();

                foreach (var emp in employees) {
                    var settingsKey = $"{emp.NumEmp}#{emp.CodFil}#{emp.CodSin}";
                    var vDiario = settingsByBranchRole.ContainsKey(settingsKey) ? settingsByBranchRole[settingsKey] : 0;
                    
                    int codEscala = (int)emp.CodEscala;
                    var baseInfo = scaleFinalResults.ContainsKey(codEscala) ? scaleFinalResults[codEscala] : (bruto: 0m, feriados: 0m);
                    decimal diasPrevistosBruto = baseInfo.bruto;
                    decimal diasFeriados = baseInfo.feriados;
                    
                    string matriculaStr = emp.Matricula.ToString();
                    
                    var details = nextiDataDetails.ContainsKey(matriculaStr) ? nextiDataDetails[matriculaStr] : (extraDates: new List<DateTime>(), absenceDates: new List<DateTime>());
                    var workDates = scaleWorkDaysMap.ContainsKey(codEscala) ? scaleWorkDaysMap[codEscala].Where(d => !holidays.Contains(d)).ToList() : new List<DateTime>();
                    
                    var workDatesJson = JsonSerializer.Serialize(workDates.Select(d => d.ToString("yyyy-MM-dd")));
                    var extraDatesJson = JsonSerializer.Serialize(details.extraDates.Select(d => d.ToString("yyyy-MM-dd")));
                    var absenceDatesJson = JsonSerializer.Serialize(details.absenceDates.Select(d => d.ToString("yyyy-MM-dd")));

                    decimal extras = details.extraDates.Count;
                    decimal ausencias = details.absenceDates.Count;
                    decimal totalVA = Math.Max(0, (diasPrevistosBruto - diasFeriados + extras - ausencias)) * vDiario;

                    using var cmdIns = new SqlCommand(@"
                        INSERT INTO ValeAlimentacaoCache 
                        (Matricula, Nome, Nascimento, CPF, Situacao, Admissao, Demissao, Funcao, CentroCusto, Escala, 
                         DiasPrevistos, DiasExtras, Ausencias, DiasFeriados, ValorDiario, ValorVA, CompetenciaInicio, CompetenciaFim, UpdatedAt,
                         WorkDatesJson, ExtraDatesJson, AbsenceDatesJson)
                        VALUES 
                        (@m, @nome, @nas, @cpf, @sit, @adm, @dem, @fun, @cc, @esc, @prev, @ext, @aus, @f, @vd, @vva, @ci, @cf, GETDATE(),
                         @wdj, @edj, @adj)", lConn, tx);
                    
                    cmdIns.Parameters.AddWithValue("@m", emp.Matricula);
                    cmdIns.Parameters.AddWithValue("@nome", emp.Nome);
                    cmdIns.Parameters.AddWithValue("@nas", emp.Nascimento);
                    cmdIns.Parameters.AddWithValue("@cpf", emp.CPF);
                    cmdIns.Parameters.AddWithValue("@sit", emp.Situacao);
                    cmdIns.Parameters.AddWithValue("@adm", emp.Admissao);
                    cmdIns.Parameters.AddWithValue("@dem", emp.Demissao);
                    cmdIns.Parameters.AddWithValue("@fun", emp.Funcao);
                    cmdIns.Parameters.AddWithValue("@cc", emp.CentroCusto);
                    cmdIns.Parameters.AddWithValue("@esc", emp.Escala);
                    cmdIns.Parameters.AddWithValue("@prev", diasPrevistosBruto);
                    cmdIns.Parameters.AddWithValue("@ext", extras);
                    cmdIns.Parameters.AddWithValue("@aus", ausencias);
                    cmdIns.Parameters.AddWithValue("@f", diasFeriados);
                    cmdIns.Parameters.AddWithValue("@vd", vDiario);
                    cmdIns.Parameters.AddWithValue("@vva", totalVA);
                    cmdIns.Parameters.AddWithValue("@ci", compInicio);
                    cmdIns.Parameters.AddWithValue("@cf", compFim);
                    cmdIns.Parameters.AddWithValue("@wdj", workDatesJson);
                    cmdIns.Parameters.AddWithValue("@edj", extraDatesJson);
                    cmdIns.Parameters.AddWithValue("@adj", absenceDatesJson);

                    await cmdIns.ExecuteNonQueryAsync();
                }
                tx.Commit();
            } catch {
                tx.Rollback();
                throw;
            }
        }

        msg = $"[V3] {employees.Count} colaboradores sincronizados com sucesso. Escalas Processadas: {scaleFinalResults.Count}";
        Console.WriteLine($"[DEBUG_V3] Sincronismo concluído: {msg}");
        return Results.Ok(new { status, message = msg });
    } catch(Exception ex) {
        return Results.Problem(ex.ToString());
    }
});

// ==========================================
// MÓDULO HR - INTEGRAÇÃO SENIOR HCM (REST)
// ==========================================

var seniorSettings = new {
    ApiKey = "4a549045-cac2-4290-b63e-fa944f4ed433",
    Tenant = "gruposetup.com",
    Username = "eduardo.macarini@gruposetup.com",
    Password = "setup4102",
    ApiUrl = "https://api.senior.com.br/hcm/vacancymanagement/", // Alterado de recruitment para vacancymanagement
    LoginUrl = "https://api.senior.com.br/platform/authentication/login"
};

async Task<string> GetSeniorToken() {
    try {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var loginData = new { 
            username = seniorSettings.Username, 
            password = seniorSettings.Password, 
            tenant = seniorSettings.Tenant 
        };
        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(seniorSettings.LoginUrl, content);
        if (!response.IsSuccessStatusCode) return "";
        
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("access_token", out var tokenProp)) {
            return tokenProp.GetString() ?? "";
        }
        return "";
    } catch {
        return "";
    }
}

app.MapGet("/api/hr/vacancies", async () => {
    try {
        string token = await GetSeniorToken();
        if (string.IsNullOrEmpty(token)) return Results.Problem("Falha na autenticação com a Senior");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("x-api-key", seniorSettings.ApiKey);

        // Endpoint genérico para listar vagas. Ajustado para o padrão Senior X.
        // No Senior X, consultas complexas costumam ser via queries/listVacancies
        var queryData = new { filter = "" }; 
        var content = new StringContent(JsonSerializer.Serialize(queryData), Encoding.UTF8, "application/json");
        
        // Tentamos o endpoint de listagem de vagas
        var response = await client.PostAsync(seniorSettings.ApiUrl + "queries/listVacancies", content);
        
        // Fallback para entities se queries falhar
        if (!response.IsSuccessStatusCode) {
            var errorBody = await response.Content.ReadAsStringAsync();
            return Results.Problem($"Erro ao buscar vagas (vacancymanagement/listVacancies): {response.ReasonPhrase}. Detalhe: {errorBody}");
        }

        var dataJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(dataJson);
        
        var vacancies = new List<object>();
        JsonElement root = doc.RootElement;
        
        // Senior X geralmente retorna uma lista em 'contents' ou na raiz
        var list = root.ValueKind == JsonValueKind.Array ? root : (root.TryGetProperty("contents", out var c) ? c : root);

        if (list.ValueKind == JsonValueKind.Array) {
            foreach (var item in list.EnumerateArray()) {
                DateTime openingDate = DateTime.Now;
                if (item.TryGetProperty("openingDate", out var d) && d.TryGetDateTime(out var dt)) openingDate = dt;
                else if (item.TryGetProperty("createdAt", out var cr) && cr.TryGetDateTime(out var ct)) openingDate = ct;

                int daysOpen = (DateTime.Now - openingDate).Days;
                
                vacancies.Add(new {
                    id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : "",
                    title = item.TryGetProperty("title", out var t) ? t.GetString() : (item.TryGetProperty("vacancyTitle", out var vt) ? vt.GetString() : "Vaga sem título"),
                    status = item.TryGetProperty("status", out var s) ? s.GetProperty("description").GetString() : "Aberta",
                    recruiter = item.TryGetProperty("responsible", out var r) ? r.GetProperty("name").GetString() : "N/A",
                    openingDate = openingDate.ToString("dd/MM/yyyy"),
                    daysOpen = daysOpen,
                    candidatesCount = item.TryGetProperty("candidatesCount", out var cc) ? cc.GetInt32() : 0
                });
            }
        }

        return Results.Ok(vacancies);
    } catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/users", async () => {
    var users = new List<object>();
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT u.Id, u.Username, u.Email, u.CreatedAt, u.IsActive, g.Name as GroupName, u.GroupId FROM Users u LEFT JOIN Groups g ON u.GroupId = g.Id", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync()) {
        users.Add(new {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2),
            CreatedAt = reader.GetDateTime(3).ToString("yyyy-MM-dd HH:mm"),
            IsActive = reader.GetBoolean(4),
            GroupName = reader.IsDBNull(5) ? "Sem Grupo" : reader.GetString(5),
            GroupId = reader.IsDBNull(6) ? 2 : reader.GetInt32(6)
        });
    }
    return Results.Ok(users);
});

// --- Gestão de Grupos ---
app.MapGet("/api/groups", async () => {
    var groups = new List<object>();
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT Id, Name FROM Groups", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync()) groups.Add(new { Id = reader.GetInt32(0), Name = reader.GetString(1) });
    return Results.Ok(groups);
});

app.MapGet("/api/groups/{id}/screens", async (int id) => {
    var screens = new List<string>();
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT ScreenPath FROM GroupScreens WHERE GroupId=@id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync()) screens.Add(reader.GetString(0));
    return Results.Ok(screens);
});

app.MapPost("/api/groups", async (GroupDTO dto) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();
    try {
        int groupId = dto.Id;
        if (groupId == 0) {
            using var cmd = new SqlCommand("INSERT INTO Groups (Name) OUTPUT INSERTED.Id VALUES (@n)", conn, transaction);
            cmd.Parameters.AddWithValue("@n", dto.Name);
            groupId = (int)await cmd.ExecuteScalarAsync();
        } else {
            using var cmd = new SqlCommand("UPDATE Groups SET Name=@n WHERE Id=@id", conn, transaction);
            cmd.Parameters.AddWithValue("@n", dto.Name);
            cmd.Parameters.AddWithValue("@id", groupId);
            await cmd.ExecuteNonQueryAsync();
            using var delCmd = new SqlCommand("DELETE FROM GroupScreens WHERE GroupId=@id", conn, transaction);
            delCmd.Parameters.AddWithValue("@id", groupId);
            await delCmd.ExecuteNonQueryAsync();
        }
        foreach (var s in dto.Screens) {
            using var sCmd = new SqlCommand("INSERT INTO GroupScreens (GroupId, ScreenPath) VALUES (@id, @s)", conn, transaction);
            sCmd.Parameters.AddWithValue("@id", groupId);
            sCmd.Parameters.AddWithValue("@s", s);
            await sCmd.ExecuteNonQueryAsync();
        }
        transaction.Commit();
        return Results.Ok(new { id = groupId });
    } catch (Exception ex) { transaction.Rollback(); return Results.Problem(ex.Message); }
});

app.MapDelete("/api/groups/{id}", async (int id) => {
    if (id <= 2) return Results.BadRequest("Grupos padrão não podem ser removidos.");
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("DELETE FROM Groups WHERE Id=@id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

app.MapPost("/api/users/{id}/assign-group", async (int id, [FromBody] int groupId) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("UPDATE Users SET GroupId=@g WHERE Id=@id", conn);
    cmd.Parameters.AddWithValue("@g", groupId);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

app.MapGet("/api/users/{id}/cost-centers", async (int id) => {
    var ccs = new List<string>();
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("SELECT CostCenterCode FROM UserCostCenters WHERE UserId=@id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync()) ccs.Add(reader.GetString(0));
    return Results.Ok(ccs);
});

app.MapPost("/api/users/{id}/cost-centers", async (int id, UserCCDTO dto) => {
    using var conn = new SqlConnection(connStr);
    await conn.OpenAsync();
    using var trans = conn.BeginTransaction();
    try {
        using var del = new SqlCommand("DELETE FROM UserCostCenters WHERE UserId=@id", conn, trans);
        del.Parameters.AddWithValue("@id", id);
        await del.ExecuteNonQueryAsync();
        foreach (var cc in dto.CostCenters) {
            using var ins = new SqlCommand("INSERT INTO UserCostCenters (UserId, CostCenterCode) VALUES (@id, @cc)", conn, trans);
            ins.Parameters.AddWithValue("@id", id);
            ins.Parameters.AddWithValue("@cc", cc);
            await ins.ExecuteNonQueryAsync();
        }
        trans.Commit(); return Results.Ok();
    } catch (Exception ex) { trans.Rollback(); return Results.Problem(ex.Message); }
});

app.MapGet("/api/hr/recruitment-insights", async () => {
    try {
        string token = await GetSeniorToken();
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("x-api-key", seniorSettings.ApiKey);

        var response = await client.GetAsync(seniorSettings.ApiUrl + "entities/vacancy");
        if (!response.IsSuccessStatusCode) return Results.Problem("Erro ao buscar dados para insights");

        var dataJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(dataJson);
        
        var allVacancies = new List<dynamic>();
        JsonElement root = doc.RootElement;
        var list = root.ValueKind == JsonValueKind.Array ? root : (root.TryGetProperty("contents", out var c) ? c : root);

        if (list.ValueKind == JsonValueKind.Array) {
            foreach (var item in list.EnumerateArray()) {
                allVacancies.Add(new {
                    Recruiter = item.TryGetProperty("responsible", out var r) ? r.GetProperty("name").GetString() : "Desconhecido",
                    DaysOpen = (DateTime.Now - (item.TryGetProperty("createdAt", out var d) && d.TryGetDateTime(out var dt) ? dt : DateTime.Now)).Days,
                    IsClosed = item.TryGetProperty("status", out var s) && s.GetProperty("name").GetString() == "CLOSED"
                });
            }
        }

        var recruiterStats = allVacancies
            .GroupBy(v => (string)v.Recruiter)
            .Select(g => new {
                name = g.Key,
                totalVacancies = g.Count(),
                avgDays = Math.Round(g.Average(v => (int)v.DaysOpen), 1)
            })
            .OrderByDescending(r => r.totalVacancies)
            .ToList();

        var topRecruiter = recruiterStats.FirstOrDefault();
        var slowestRecruiter = recruiterStats.OrderByDescending(r => r.avgDays).FirstOrDefault();

        return Results.Ok(new {
            stats = recruiterStats,
            insights = new {
                mostActive = topRecruiter?.name ?? "N/A",
                mostActiveCount = topRecruiter?.totalVacancies ?? 0,
                slowest = slowestRecruiter?.name ?? "N/A",
                slowestAvgDays = slowestRecruiter?.avgDays ?? 0
            }
        });
    } catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/connections/status", async () => {
    var results = new List<object>();

    // 1. Senior SQL (VETORH)
    var seniorSql = new { name = "Senior SQL (VETORH)", type = "SQL Server", status = "Offline", latency = 0L, error = "" };
    try {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var conn = new SqlConnection(seniorConnString);
        var task = conn.OpenAsync();
        if (await Task.WhenAny(task, Task.Delay(5000)) == task) {
            await task;
            using var cmd = new SqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            sw.Stop();
            seniorSql = new { name = seniorSql.name, type = seniorSql.type, status = "Online", latency = sw.ElapsedMilliseconds, error = "" };
        } else {
            seniorSql = new { name = seniorSql.name, type = seniorSql.type, status = "Offline", latency = 0L, error = "Timeout (5s)" };
        }
    } catch (Exception ex) { seniorSql = new { name = seniorSql.name, type = seniorSql.type, status = "Offline", latency = 0L, error = ex.Message }; }
    results.Add(seniorSql);

    // 2. Senior REST API
    var seniorRest = new { name = "Senior REST API", type = "REST Service", status = "Offline", latency = 0L, error = "" };
    try {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var tokenTask = GetSeniorToken();
        if (await Task.WhenAny(tokenTask, Task.Delay(5000)) == tokenTask) {
            var token = await tokenTask;
            sw.Stop();
            if (!string.IsNullOrEmpty(token)) {
                seniorRest = new { name = seniorRest.name, type = seniorRest.type, status = "Online", latency = sw.ElapsedMilliseconds, error = "" };
            } else {
                seniorRest = new { name = seniorRest.name, type = seniorRest.type, status = "Offline", latency = 0L, error = "Falha na autenticação" };
            }
        } else {
            seniorRest = new { name = seniorRest.name, type = seniorRest.type, status = "Offline", latency = 0L, error = "Timeout (5s)" };
        }
    } catch (Exception ex) { seniorRest = new { name = seniorRest.name, type = seniorRest.type, status = "Offline", latency = 0L, error = ex.Message }; }
    results.Add(seniorRest);

    // 3. SQL Local (LoginSystem)
    var localSql = new { name = "SQL Local (LoginSystem)", type = "SQL Server", status = "Offline", latency = 0L, error = "" };
    try {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var conn = new SqlConnection(connStr);
        var task = conn.OpenAsync();
        if (await Task.WhenAny(task, Task.Delay(3000)) == task) {
            await task;
            using var cmd = new SqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            sw.Stop();
            localSql = new { name = localSql.name, type = localSql.type, status = "Online", latency = sw.ElapsedMilliseconds, error = "" };
        } else {
            localSql = new { name = localSql.name, type = localSql.type, status = "Offline", latency = 0L, error = "Timeout (3s)" };
        }
    } catch (Exception ex) { localSql = new { name = localSql.name, type = localSql.type, status = "Offline", latency = 0L, error = ex.Message }; }
    results.Add(localSql);

    // 4. Nexti API
    var nextiApi = new { name = "Nexti API", type = "REST Service (OAuth2)", status = "Offline", latency = 0L, error = "" };
    try {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("gruposetup:611e03d43da3c23a1431027405c993e22b9643c4"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var nAuthUrl = $"https://api.nexti.com/security/oauth/token?cb={Guid.NewGuid()}";
        
        var nPayload = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });
        
        try {
            var nRes = await client.PostAsync(nAuthUrl, nPayload);
            sw.Stop();
            if (nRes.IsSuccessStatusCode) {
                nextiApi = new { name = nextiApi.name, type = nextiApi.type, status = "Online", latency = sw.ElapsedMilliseconds, error = "" };
            } else {
                var errContent = await nRes.Content.ReadAsStringAsync();
                nextiApi = new { name = nextiApi.name, type = nextiApi.type, status = "Offline", latency = 0L, error = $"HTTP {(int)nRes.StatusCode}: {errContent}" };
            }
        } catch (TaskCanceledException) {
            nextiApi = new { name = nextiApi.name, type = nextiApi.type, status = "Offline", latency = 0L, error = "Timeout (5s)" };
        } catch (HttpRequestException ex) {
             nextiApi = new { name = nextiApi.name, type = nextiApi.type, status = "Offline", latency = 0L, error = $"Erro de Rede: {ex.Message}" };
        }
    } catch (Exception ex) { nextiApi = new { name = nextiApi.name, type = nextiApi.type, status = "Offline", latency = 0L, error = ex.Message }; }
    results.Add(nextiApi);

    return Results.Ok(results);
});

app.Run();

public class QuoteDTO {
    public string ClientName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public int Postos { get; set; }
    public int Pessoas { get; set; }
    public decimal TotalValue { get; set; }
    public string CreatedBy { get; set; } = "";
}

public class UserDTO {
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Email { get; set; } = "";
}

public class GroupDTO {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> Screens { get; set; } = new();
}

public class UserCCDTO {
    public List<string> CostCenters { get; set; } = new();
}
