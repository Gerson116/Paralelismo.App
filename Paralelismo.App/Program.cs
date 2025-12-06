using Paralelismo.App.DTOs;
using Paralelismo.App.Entities;
using Paralelismo.App.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        string relativePath = "D:\\repos\\Paralelismo.App\\Paralelismo.App\\Data\\";

        Console.WriteLine("Hello, World!");

        // Crear el DbContext (no se llama SaveChanges hasta que terminen las cargas)
        using var db = new AppDbContext();
        //await db.Database.MigrateAsync();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM ArticulosVendidos; DELETE FROM Facturas; DELETE FROM Marcas; DELETE FROM Clientes;");

        // Verificar que las tablas quedaron vacías
        var clientesCountAfterDelete = await db.Clientes.CountAsync();
        var facturasCountAfterDelete = await db.Facturas.CountAsync();
        var marcasCountAfterDelete = await db.Marcas.CountAsync();
        var articulosCountAfterDelete = await db.ArticulosVendidos.CountAsync();

        Console.WriteLine("Resultados después del DELETE:");
        Console.WriteLine($"  Clientes: {clientesCountAfterDelete}");
        Console.WriteLine($"  Facturas: {facturasCountAfterDelete}");
        Console.WriteLine($"  Marcas: {marcasCountAfterDelete}");
        Console.WriteLine($"  ArticulosVendidos: {articulosCountAfterDelete}");

        // Informar si la eliminación dejó tablas vacías
        if (clientesCountAfterDelete == 0 && facturasCountAfterDelete == 0 && marcasCountAfterDelete == 0 && articulosCountAfterDelete == 0)
        {
            Console.WriteLine("Eliminación realizada: todas las tablas están vacías.");
        }
        else
        {
            Console.WriteLine("Eliminación parcial: revisar los conteos anteriores para más detalles.");
        }

        // Iniciar cargas en paralelo
        Task<List<Cliente>> tClientes = CargarClientes(relativePath);
        Task<List<Factura>> tFacturas = CargarFacturas(relativePath);
        Task<List<Marca>> tMarcas = CargarMarcas(relativePath);
        Task<List<ArticuloVendido>> tArticulosVendidos = CargarArticulosVendidos(relativePath);

        // Esperar a que todas las cargas finalicen
        await Task.WhenAll(tClientes, tFacturas, tMarcas, tArticulosVendidos);

        // Mostrar cantidad de elementos cargados antes de guardar
        Console.WriteLine($"Resumen de carga: Clientes={tClientes.Result.Count}, Facturas={tFacturas.Result.Count}, Marcas={tMarcas.Result.Count}, ArticulosVendidos={tArticulosVendidos.Result.Count}");

        // Mapear resultados y guardar en la base de datos
        var clientes = tClientes.Result;
        if (clientes.Any())
        {
            Console.WriteLine($"Agregando {clientes.Count} clientes a la base...");
            db.Clientes.AddRange(clientes);
        }

        var facturas = tFacturas.Result;
        if (facturas.Any())
        {
            Console.WriteLine($"Agregando {facturas.Count} facturas a la base...");
            db.Facturas.AddRange(facturas);
        }

        var marcas = tMarcas.Result;
        if (marcas.Any())
        {
            Console.WriteLine($"Agregando {marcas.Count} marcas a la base...");
            db.Marcas.AddRange(marcas);
        }

        var articulos = tArticulosVendidos.Result;
        if (articulos.Any())
        {
            Console.WriteLine($"Agregando {articulos.Count} artículos vendidos a la base...");
            db.ArticulosVendidos.AddRange(articulos);
        }

        try
        {
            // Abrir conexión y usar transacción para permitir SET IDENTITY_INSERT
            await db.Database.OpenConnectionAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                await db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine("DbUpdateException: " + ex.Message);
            if (ex.InnerException != null) Console.WriteLine("Inner: " + ex.InnerException.Message);

            foreach (var entry in ex.Entries)
            {
                Console.WriteLine($"Entity type: {entry.Entity.GetType().FullName}, State: {entry.State}");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(entry.CurrentValues.ToObject()));
            }

            throw;
        }

        Console.WriteLine("Datos guardados en la base.");

        Console.WriteLine("Todos los datos han sido cargados.");
    }

    static async Task<List<Cliente>> CargarClientes(string relativePath)
    {
        var sw = Stopwatch.StartNew();

        relativePath += "clientes.csv";

        if (!File.Exists(relativePath))
            throw new FileNotFoundException("El archivo de clientes no fue encontrado.");

        Console.WriteLine("Cargando clientes Hilo 1");

        #region Leyendo archivos
        var linea = await File.ReadAllLinesAsync(relativePath);

        var clientesDTO = new List<ClienteDTO>();
        var clientes = new List<Cliente>();

        if (linea.Length == 0)
            throw new Exception("El archivo de clientes está vacío.");

        // Empezar en 1 para omitir el encabezado (línea 0)
        int filasProcesadas = 0;
        for (int i = 1; i < linea.Length; i++)
        {
            //  Nombre,Apellido,Email,Cedula
            var element = linea[i];
            if (string.IsNullOrWhiteSpace(element)) continue;

            var parts = ConvertirLineasCsv(element);
            var nombre = parts.ElementAtOrDefault(0) ?? string.Empty;
            var apellidos = parts.ElementAtOrDefault(1) ?? string.Empty;
            var email = parts.ElementAtOrDefault(2) ?? string.Empty;
            var cedula = parts.ElementAtOrDefault(3) ?? string.Empty;

            var cliente = new Cliente
            {
                Nombre = nombre,
                Apellido = apellidos,
                Email = email,
                Cedula = cedula
            };

            clientes.Add(cliente);
            filasProcesadas++;
        }
        #endregion

        await Task.Delay(2000);
        sw.Stop();
        Console.WriteLine($"Clientes cargados en {sw.Elapsed.TotalSeconds:F2}s. Filas procesadas: {filasProcesadas}, entidades creadas: {clientes.Count}");
        return clientes;
    }

    static int HashStringToInt(string s)
    {
        unchecked
        {
            int hash = 23;
            foreach (var c in s)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }

    static async Task<List<Factura>> CargarFacturas(string relativePath)
    {
        var sw = Stopwatch.StartNew();

        relativePath += "facturas.csv";

        if (!File.Exists(relativePath))
            throw new FileNotFoundException("El archivo de facturas no fue encontrado.");

        Console.WriteLine("Cargando facturas Hilo 2");

        #region Leyendo archivos
        var linea = await File.ReadAllLinesAsync(relativePath);

        var facturasDTO = new List<FacturaDTO>();
        var facturas = new List<Factura>();

        if (linea.Length == 0)
            throw new Exception("El archivo de facturas está vacío.");

        int filasProcesadas = 0;
        // Empezar en 1 para omitir encabezado
        for (int i = 1; i < linea.Length; i++)
        {
            //  Monto,Cantidad,ITBIS,Descuento
            var element = linea[i];
            if (string.IsNullOrWhiteSpace(element)) continue;
            var parts = ConvertirLineasCsv(element);
            var monto = parts.ElementAtOrDefault(0) ?? string.Empty;
            var cantidad = parts.ElementAtOrDefault(1) ?? string.Empty;
            var itbis = parts.ElementAtOrDefault(2) ?? string.Empty;
            var descuento = parts.ElementAtOrDefault(3) ?? string.Empty;

            var factura = new Factura
            {
                Monto = monto != string.Empty ? Decimal.Parse(monto) : 0m,
                Cantidad = cantidad != string.Empty ? int.Parse(cantidad) : 0,
                ITBIS = itbis != string.Empty ? Decimal.Parse(itbis) : 0m,
                Descuento = descuento != string.Empty ? Decimal.Parse(descuento) : 0m
            };

            facturas.Add(factura);
            filasProcesadas++;
        }
        #endregion

        await Task.Delay(2000);
        sw.Stop();
        Console.WriteLine($"Facturas cargadas en {sw.Elapsed.TotalSeconds:F2}s. Filas procesadas: {filasProcesadas}, entidades creadas: {facturas.Count}");
        return facturas;
    }

    static async Task<List<Marca>> CargarMarcas(string relativePath)
    {
        var sw = Stopwatch.StartNew();

        relativePath += "marcas.csv";

        if (!File.Exists(relativePath))
            throw new FileNotFoundException("El archivo de marcas no fue encontrado.");

        Console.WriteLine("Cargando marcas Hilo 3");

        #region Leyendo archivos
        var linea = await File.ReadAllLinesAsync(relativePath);

        var marcasDTO = new List<MarcaDTO>();
        var marcas = new List<Marca>();

        if (linea.Length == 0)
            throw new Exception("El archivo de marcas está vacío.");

        int filasProcesadas = 0;
        // Empezar en 1 para omitir encabezado
        for (int i = 1; i < linea.Length; i++)
        {
            //  Nombre,Marca,Origen
            var element = linea[i];
            if (string.IsNullOrWhiteSpace(element)) continue;

            var parts = ConvertirLineasCsv(element);
            var nombre = parts.ElementAtOrDefault(0) ?? string.Empty;
            var marca = parts.ElementAtOrDefault(1) ?? string.Empty;
            var origen = parts.ElementAtOrDefault(2) ?? string.Empty;

            var marcaEntity = new Marca
            {
                Nombre = nombre,
                NombreMarca = marca,
                Origen = origen
            };

            marcas.Add(marcaEntity);
            filasProcesadas++;
        }
        #endregion

        await Task.Delay(2000);
        sw.Stop();
        Console.WriteLine($"Marcas cargadas en {sw.Elapsed.TotalSeconds:F2}s. Filas procesadas: {filasProcesadas}, entidades creadas: {marcas.Count}");
        return marcas;
    }

    static async Task<List<ArticuloVendido>> CargarArticulosVendidos(string relativePath)
    {
        var sw = Stopwatch.StartNew();

        relativePath += "articulos_vencidos.csv";

        if (!File.Exists(relativePath))
            throw new FileNotFoundException("El archivo de artículos vencidos no fue encontrado.");

        #region Leyendo archivos
        var linea = await File.ReadAllLinesAsync(relativePath);

        var articulosDTO = new List<ArticuloVendidoDTO>();
        var articulos = new List<ArticuloVendido>();

        if (linea.Length == 0)
            throw new Exception("El archivo de artículos vendidos está vacío.");

        int filasProcesadas = 0;
        // Empezar en 1 para omitir encabezado
        for (int i = 1; i < linea.Length; i++)
        {
            //  MarcaId,    FechaVencimiento,   NumeroDeLote
            var element = linea[i];
            if (string.IsNullOrWhiteSpace(element)) continue;

            var parts = ConvertirLineasCsv(element);
            var marcaId = parts.ElementAtOrDefault(0) ?? string.Empty;
            var fechaVencimiento = parts.ElementAtOrDefault(1) ?? string.Empty;
            var numeroDeLote = parts.ElementAtOrDefault(2) ?? string.Empty;

            if (marcaId != string.Empty)
            {
                articulos.Add(new ArticuloVendido
                {
                    MarcaId = int.Parse(marcaId),
                    FechaVencimiento = fechaVencimiento,
                    NumeroDeLote = numeroDeLote
                });

                filasProcesadas++;
            }
        }
        #endregion

        Console.WriteLine("Cargando artículos vencidos Hilo 4");

        await Task.Delay(2000);
        sw.Stop();
        Console.WriteLine($"Artículos vencidos cargados en {sw.Elapsed.TotalSeconds:F2}s. Filas procesadas: {filasProcesadas}, entidades creadas: {articulos.Count}");
        return articulos;
    }

    /// <summary>
    /// Convierte una línea de texto en formato CSV en una lista de valores.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    static List<string> ConvertirLineasCsv(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}