namespace ZapChat.Api.Common.Helpers;
using System.Collections.Concurrent;
public class ConnectionManager
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();
    private readonly object _lock = new();
    public void Add(Guid userId, string connectionId)
    {
        lock (_lock)
        {
            if(!_connections.ContainsKey(userId))
                _connections[userId]= new HashSet<string>();
            _connections[userId].Add(connectionId);
        }
    }

    public void Remove(Guid userId, string connectionId)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(userId, out var conns))
            {
                conns.Remove(connectionId);
                if (conns.Count == 0)
                    _connections.TryRemove(userId, out _);
            }
        }
    }

    public IEnumerable<string> GetConnections(Guid userId)
        => _connections.TryGetValue(userId, out var conns) ? conns : [];

    public bool IsOnline(Guid userId)
        => _connections.ContainsKey(userId) && _connections[userId].Count > 0;

    public IEnumerable<Guid> GetOnlineUserIds()
        => _connections.Keys;

}