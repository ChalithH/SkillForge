using SkillForge.Api.Services;

namespace SkillForge.Tests.Services;

public class UserPresenceServiceTests
{
    private readonly UserPresenceService _presenceService;

    public UserPresenceServiceTests()
    {
        _presenceService = new UserPresenceService();
    }

    [Fact]
    public async Task UserConnectedAsync_SingleConnection_UserBecomesOnline()
    {
        // Arrange
        const int userId = 1;
        const string connectionId = "conn1";

        // Act
        await _presenceService.UserConnectedAsync(userId, connectionId);

        // Assert
        var isOnline = await _presenceService.IsUserOnlineAsync(userId);
        Assert.True(isOnline);
    }

    [Fact]
    public async Task UserConnectedAsync_MultipleConnections_TracksBothConnections()
    {
        // Arrange
        const int userId = 1;
        const string connectionId1 = "conn1";
        const string connectionId2 = "conn2";

        // Act
        await _presenceService.UserConnectedAsync(userId, connectionId1);
        await _presenceService.UserConnectedAsync(userId, connectionId2);

        // Assert
        var connectionIds = await _presenceService.GetUserConnectionIdsAsync(userId);
        Assert.Equal(2, connectionIds.Count);
        Assert.Contains(connectionId1, connectionIds);
        Assert.Contains(connectionId2, connectionIds);
    }

    [Fact]
    public async Task UserDisconnectedAsync_LastConnection_UserGoesOffline()
    {
        // Arrange
        const int userId = 1;
        const string connectionId = "conn1";

        await _presenceService.UserConnectedAsync(userId, connectionId);

        // Act
        await _presenceService.UserDisconnectedAsync(connectionId);

        // Assert
        var isOnline = await _presenceService.IsUserOnlineAsync(userId);
        Assert.False(isOnline);
    }

    [Fact]
    public async Task UserDisconnectedAsync_OneOfMultipleConnections_UserStaysOnline()
    {
        // Arrange
        const int userId = 1;
        const string connectionId1 = "conn1";
        const string connectionId2 = "conn2";

        await _presenceService.UserConnectedAsync(userId, connectionId1);
        await _presenceService.UserConnectedAsync(userId, connectionId2);

        // Act
        await _presenceService.UserDisconnectedAsync(connectionId1);

        // Assert
        var isOnline = await _presenceService.IsUserOnlineAsync(userId);
        Assert.True(isOnline);

        var connectionIds = await _presenceService.GetUserConnectionIdsAsync(userId);
        Assert.Single(connectionIds);
        Assert.Contains(connectionId2, connectionIds);
        Assert.DoesNotContain(connectionId1, connectionIds);
    }

    [Fact]
    public async Task UserDisconnectedAsync_NonExistentConnection_DoesNotThrow()
    {
        // Arrange
        const string nonExistentConnectionId = "nonexistent";

        // Act & Assert - Should not throw
        await _presenceService.UserDisconnectedAsync(nonExistentConnectionId);

        // Additional verification - service state should be unaffected
        var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();
        Assert.Empty(onlineUsers);
    }

    [Fact]
    public async Task IsUserOnlineAsync_NoConnections_ReturnsFalse()
    {
        // Arrange
        const int userId = 1;

        // Act
        var isOnline = await _presenceService.IsUserOnlineAsync(userId);

        // Assert
        Assert.False(isOnline);
    }

    [Fact]
    public async Task IsUserOnlineAsync_WithConnections_ReturnsTrue()
    {
        // Arrange
        const int userId = 1;
        const string connectionId = "conn1";

        await _presenceService.UserConnectedAsync(userId, connectionId);

        // Act
        var isOnline = await _presenceService.IsUserOnlineAsync(userId);

        // Assert
        Assert.True(isOnline);
    }

    [Fact]
    public async Task GetOnlineUserIdsAsync_NoUsers_ReturnsEmptyList()
    {
        // Act
        var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();

        // Assert
        Assert.Empty(onlineUsers);
    }

    [Fact]
    public async Task GetOnlineUserIdsAsync_MultipleUsers_ReturnsAllOnlineUsers()
    {
        // Arrange
        const int userId1 = 1;
        const int userId2 = 2;
        const int userId3 = 3;
        
        await _presenceService.UserConnectedAsync(userId1, "conn1");
        await _presenceService.UserConnectedAsync(userId2, "conn2");
        await _presenceService.UserConnectedAsync(userId3, "conn3");

        // Act
        var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();

        // Assert
        Assert.Equal(3, onlineUsers.Count);
        Assert.Contains(userId1, onlineUsers);
        Assert.Contains(userId2, onlineUsers);
        Assert.Contains(userId3, onlineUsers);
    }

    [Fact]
    public async Task GetOnlineUserIdsAsync_SomeUsersDisconnected_ReturnsOnlyOnlineUsers()
    {
        // Arrange
        const int userId1 = 1;
        const int userId2 = 2;
        const int userId3 = 3;
        
        await _presenceService.UserConnectedAsync(userId1, "conn1");
        await _presenceService.UserConnectedAsync(userId2, "conn2");
        await _presenceService.UserConnectedAsync(userId3, "conn3");

        // Disconnect user 2
        await _presenceService.UserDisconnectedAsync("conn2");

        // Act
        var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();

        // Assert
        Assert.Equal(2, onlineUsers.Count);
        Assert.Contains(userId1, onlineUsers);
        Assert.DoesNotContain(userId2, onlineUsers);
        Assert.Contains(userId3, onlineUsers);
    }

    [Fact]
    public async Task GetUserConnectionIdsAsync_NoConnections_ReturnsEmptyList()
    {
        // Arrange
        const int userId = 1;

        // Act
        var connectionIds = await _presenceService.GetUserConnectionIdsAsync(userId);

        // Assert
        Assert.Empty(connectionIds);
    }

    [Fact]
    public async Task GetUserConnectionIdsAsync_WithConnections_ReturnsAllConnections()
    {
        // Arrange
        const int userId = 1;
        const string connectionId1 = "conn1";
        const string connectionId2 = "conn2";
        const string connectionId3 = "conn3";

        await _presenceService.UserConnectedAsync(userId, connectionId1);
        await _presenceService.UserConnectedAsync(userId, connectionId2);
        await _presenceService.UserConnectedAsync(userId, connectionId3);

        // Act
        var connectionIds = await _presenceService.GetUserConnectionIdsAsync(userId);

        // Assert
        Assert.Equal(3, connectionIds.Count);
        Assert.Contains(connectionId1, connectionIds);
        Assert.Contains(connectionId2, connectionIds);
        Assert.Contains(connectionId3, connectionIds);
    }

    [Fact]
    public async Task GetUserIdByConnectionIdAsync_ExistingConnection_ReturnsUserId()
    {
        // Arrange
        const int userId = 1;
        const string connectionId = "conn1";

        await _presenceService.UserConnectedAsync(userId, connectionId);

        // Act
        var result = await _presenceService.GetUserIdByConnectionIdAsync(connectionId);

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetUserIdByConnectionIdAsync_NonExistentConnection_ReturnsNull()
    {
        // Arrange
        const string nonExistentConnectionId = "nonexistent";

        // Act
        var result = await _presenceService.GetUserIdByConnectionIdAsync(nonExistentConnectionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ConcurrentOperations_MultipleUsersAndConnections_HandlesCorrectly()
    {
        // Arrange - Simulate concurrent operations
        var tasks = new List<Task>();

        // Add multiple users with multiple connections
        for (int userId = 1; userId <= 10; userId++)
        {
            for (int connIndex = 1; connIndex <= 3; connIndex++)
            {
                var connectionId = $"user{userId}_conn{connIndex}";
                tasks.Add(_presenceService.UserConnectedAsync(userId, connectionId));
            }
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();
        Assert.Equal(10, onlineUsers.Count);

        // Verify each user has 3 connections
        for (int userId = 1; userId <= 10; userId++)
        {
            var connectionIds = await _presenceService.GetUserConnectionIdsAsync(userId);
            Assert.Equal(3, connectionIds.Count);
            Assert.True(await _presenceService.IsUserOnlineAsync(userId));
        }
    }

    [Fact]
    public async Task MixedOperations_ConnectAndDisconnect_MaintainsCorrectState()
    {
        // Arrange
        const int userId1 = 1;
        const int userId2 = 2;
        
        // User 1 connects with 2 connections
        await _presenceService.UserConnectedAsync(userId1, "user1_conn1");
        await _presenceService.UserConnectedAsync(userId1, "user1_conn2");
        
        // User 2 connects with 1 connection
        await _presenceService.UserConnectedAsync(userId2, "user2_conn1");

        // Act - Disconnect one of user 1's connections
        await _presenceService.UserDisconnectedAsync("user1_conn1");

        // Assert
        // User 1 should still be online (has 1 remaining connection)
        Assert.True(await _presenceService.IsUserOnlineAsync(userId1));
        var user1Connections = await _presenceService.GetUserConnectionIdsAsync(userId1);
        Assert.Single(user1Connections);
        Assert.Contains("user1_conn2", user1Connections);

        // User 2 should still be online
        Assert.True(await _presenceService.IsUserOnlineAsync(userId2));

        // Total online users should be 2
        var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();
        Assert.Equal(2, onlineUsers.Count);
    }

    [Fact]
    public async Task SameConnectionId_DifferentUsers_DoesNotInterfere()
    {
        // Arrange - Two different users with same connection ID (shouldn't happen in practice but test robustness)
        const int userId1 = 1;
        const int userId2 = 2;
        const string connectionId = "same_conn_id";

        // Act - Connect user 1
        await _presenceService.UserConnectedAsync(userId1, connectionId);
        
        // Verify user 1 is online
        Assert.True(await _presenceService.IsUserOnlineAsync(userId1));
        Assert.Equal(userId1, await _presenceService.GetUserIdByConnectionIdAsync(connectionId));

        // Connect user 2 with same connection ID (this will overwrite the mapping)
        await _presenceService.UserConnectedAsync(userId2, connectionId);

        // Assert - The connection should now map to user 2
        Assert.Equal(userId2, await _presenceService.GetUserIdByConnectionIdAsync(connectionId));
        
        // Both users should appear online (they each have the connection in their sets)
        Assert.True(await _presenceService.IsUserOnlineAsync(userId1));
        Assert.True(await _presenceService.IsUserOnlineAsync(userId2));
    }
}