using System;
using System.Data;

namespace ResourceManagement.Examples;

/// <summary>
/// Demonstrates database connection management patterns
/// Shows proper disposal with connection pooling and transactions
/// </summary>

// Simulated database connection for demonstration
public class DatabaseConnection : IDisposable
{
    private bool _disposed = false;
    private readonly string _connectionString;

    public DatabaseConnection(string connectionString)
    {
        _connectionString = connectionString;
        Console.WriteLine($"Opening connection: {_connectionString}");
    }

    public void ExecuteQuery(string sql)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DatabaseConnection));
        Console.WriteLine($"Executing: {sql}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Console.WriteLine($"Closing connection: {_connectionString}");
        _disposed = true;
    }
}

/// <summary>
/// Database repository pattern with proper disposal
/// </summary>
public class UserRepository : IDisposable
{
    private readonly DatabaseConnection _connection;

    public UserRepository(string connectionString)
    {
        _connection = new DatabaseConnection(connectionString);
    }

    public void GetUser(int id)
    {
        _connection.ExecuteQuery($"SELECT * FROM Users WHERE Id = {id}");
    }

    public void UpdateUser(int id, string name)
    {
        _connection.ExecuteQuery($"UPDATE Users SET Name = '{name}' WHERE Id = {id}");
    }

    public void Dispose()
    {
        _connection?.Dispose(); // ✓ Good - dispose connection
    }
}

/// <summary>
/// Unit of Work pattern with transaction management
/// </summary>
public class UnitOfWork : IDisposable
{
    private readonly DatabaseConnection _connection;
    private bool _committed = false;

    public UnitOfWork(string connectionString)
    {
        _connection = new DatabaseConnection(connectionString);
        Console.WriteLine("Beginning transaction");
    }

    public void RegisterUser(string name)
    {
        _connection.ExecuteQuery($"INSERT INTO Users (Name) VALUES ('{name}')");
    }

    public void Commit()
    {
        _connection.ExecuteQuery("COMMIT");
        _committed = true;
    }

    public void Dispose()
    {
        if (!_committed)
        {
            Console.WriteLine("Rolling back transaction");
            _connection.ExecuteQuery("ROLLBACK");
        }
        _connection?.Dispose();
    }
}

/// <summary>
/// Connection factory pattern
/// Demonstrates proper disposal responsibility with factory methods
/// </summary>
public class DatabaseConnectionFactory
{
    private readonly string _connectionString;

    public DatabaseConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ✓ Good: "Create" indicates caller owns the connection
    public DatabaseConnection CreateConnection()
    {
        return new DatabaseConnection(_connectionString);
    }

    // ✓ Good: Returns repository that manages its own connection
    public UserRepository CreateUserRepository()
    {
        return new UserRepository(_connectionString);
    }

    // Example usage with proper disposal
    public void ExecuteWithConnection(Action<DatabaseConnection> action)
    {
        using var connection = CreateConnection();
        action(connection);
    }
}

/// <summary>
/// Connection pool pattern (simplified)
/// Shows managing collections of disposables
/// </summary>
public class ConnectionPool : IDisposable
{
    private readonly List<DatabaseConnection> _availableConnections = new();
    private readonly List<DatabaseConnection> _inUseConnections = new();
    private readonly string _connectionString;
    private readonly int _maxPoolSize;

    public ConnectionPool(string connectionString, int maxPoolSize = 10)
    {
        _connectionString = connectionString;
        _maxPoolSize = maxPoolSize;
    }

    public DatabaseConnection GetConnection()
    {
        lock (_availableConnections)
        {
            if (_availableConnections.Count > 0)
            {
                var connection = _availableConnections[0];
                _availableConnections.RemoveAt(0);
                _inUseConnections.Add(connection);
                return connection;
            }

            if (_inUseConnections.Count < _maxPoolSize)
            {
                var newConnection = new DatabaseConnection(_connectionString);
                _inUseConnections.Add(newConnection);
                return newConnection;
            }

            throw new InvalidOperationException("Connection pool exhausted");
        }
    }

    public void ReturnConnection(DatabaseConnection connection)
    {
        lock (_availableConnections)
        {
            if (_inUseConnections.Remove(connection))
            {
                _availableConnections.Add(connection);
            }
        }
    }

    public void Dispose()
    {
        // ✓ Good: Dispose all pooled connections
        foreach (var connection in _availableConnections)
        {
            connection?.Dispose();
        }
        _availableConnections.Clear();

        foreach (var connection in _inUseConnections)
        {
            connection?.Dispose();
        }
        _inUseConnections.Clear();
    }
}

/// <summary>
/// Demonstrates proper database usage patterns
/// </summary>
public class DatabaseExamples
{
    public void SimpleQuery()
    {
        // ✓ Good: Using statement ensures disposal
        using var connection = new DatabaseConnection("Server=localhost");
        connection.ExecuteQuery("SELECT * FROM Users");
    }

    public void RepositoryPattern()
    {
        // ✓ Good: Repository manages its own connection
        using var repository = new UserRepository("Server=localhost");
        repository.GetUser(1);
        repository.UpdateUser(1, "John");
    }

    public void TransactionPattern()
    {
        // ✓ Good: Transaction rolled back if not committed
        using var unitOfWork = new UnitOfWork("Server=localhost");
        unitOfWork.RegisterUser("Alice");
        unitOfWork.RegisterUser("Bob");
        unitOfWork.Commit();
    }

    public void FactoryPattern()
    {
        var factory = new DatabaseConnectionFactory("Server=localhost");

        // ✓ Good: Factory method with callback handles disposal
        factory.ExecuteWithConnection(conn =>
        {
            conn.ExecuteQuery("SELECT COUNT(*) FROM Users");
        });
    }

    public void ConnectionPoolPattern()
    {
        using var pool = new ConnectionPool("Server=localhost", maxPoolSize: 5);

        // Get connection from pool
        var connection = pool.GetConnection();
        try
        {
            connection.ExecuteQuery("SELECT * FROM Users");
        }
        finally
        {
            // Return to pool (doesn't dispose)
            pool.ReturnConnection(connection);
        }
    }
}
