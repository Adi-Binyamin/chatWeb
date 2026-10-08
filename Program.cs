using chatWeb.Data;
using chatWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// SignalR
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();


// Register
app.MapPost("/register", async (User user, AppDbContext db) =>
{
    var existingUser = await db.Users.FindAsync(user.Username);
    if (existingUser != null)
    {
        return Results.BadRequest(new
        {
            message = "Username already exists"
        });
    }
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new
    {
        message = "User registered successfully"
    });
});


// Login
app.MapPost("/login", async (User user, AppDbContext db) =>
{
    var existingUser = await db.Users.FindAsync(user.Username);
    if (existingUser == null)
    {
        return Results.BadRequest(new
        {
            message = "Username or password is correct"
        });
    }
    if (existingUser.Password != user.Password)
    {
        return Results.BadRequest(new
        {
            message = "Username or password is correct"
        });
    }
    return Results.Ok(new
    {
        message = "Login successful"
    });
});

// Get user's chats
app.MapGet("/chats/{username}", async (string username, AppDbContext db) =>
{
    var userChats = await db.ChatParticipants
        .Where(cp => cp.Username == username)
        .Include(cp => cp.Chat)
            .ThenInclude(c => c.Participants)
        .Include(cp => cp.Chat)
            .ThenInclude(c => c.Messages)
        .ToListAsync();

    var chats = userChats.Select(cp => new
    {
        ChatId = cp.ChatId,
        OtherUser = cp.Chat.Participants
            .Where(p => p.Username != username)
            .Select(p => p.Username)
            .FirstOrDefault(),
        LastMessage = cp.Chat.Messages
            .OrderByDescending(m => m.SentAt)
            .Select(m => new
            {
                m.MessageId,
                m.Username,
                m.Text,
                m.SentAt
            })
            .FirstOrDefault()
    })
    .OrderByDescending(chat =>
        chat.LastMessage != null
            ? chat.LastMessage.SentAt
            : DateTime.MinValue)
    .ToList();
    return Results.Ok(chats);
});

// Create private chat
app.MapPost("/chats", async (string username1, string username2, AppDbContext db) =>
{
    var user1 = await db.Users.FindAsync(username1);
    var user2 = await db.Users.FindAsync(username2);

    if (user1 == null || user2 == null)
    {
        return Results.BadRequest(new
        {
            message = "One or both users do not exist"
        });
    }

    var chat = new Chat();

    db.Chats.Add(chat);
    await db.SaveChangesAsync();

    var participant1 = new ChatParticipant
    {
        ChatId = chat.ChatId,
        Username = username1
    };

    var participant2 = new ChatParticipant
    {
        ChatId = chat.ChatId,
        Username = username2
    };

    db.ChatParticipants.Add(participant1);
    db.ChatParticipants.Add(participant2);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        chatId = chat.ChatId,
        otherUser = username2
    });
});

// Get messages of a chat
app.MapGet("/chats/{chatId}/messages", async (int chatId, AppDbContext db) =>
{
    var messages = await db.Messages
        .Where(m => m.ChatId == chatId)
        .OrderBy(m => m.SentAt)
        .Select(m => new
        {
            m.MessageId,
            m.Username,
            m.Text,
            m.SentAt
        })
        .ToListAsync();
    return Results.Ok(messages);
});

// Send message 
app.MapPost("/messages", async (Message message, AppDbContext db, IHubContext<ChatHub> hubContext) =>
{
    var chat = await db.Chats.FindAsync(message.ChatId);
    var user = await db.Users.FindAsync(message.Username);

    if (chat == null || user == null)
    {
        return Results.BadRequest(new
        {
            message = "Chat or user does not exist"
        });
    }

    var participant = await db.ChatParticipants
        .FirstOrDefaultAsync(cp =>
            cp.ChatId == message.ChatId && cp.Username == message.Username);

    if (participant == null)
    {
        return Results.BadRequest(new
        {
            message = "User is not a participant in this chat"
        });
    }

    message.SentAt = DateTime.Now;
    db.Messages.Add(message);
    await db.SaveChangesAsync();

    // יצירת אובייקט הודעה שטוח ונקי ללא מעגלים לשידור ב-SignalR
    var messageDto = new
    {
        messageId = message.MessageId,
        chatId = message.ChatId,
        username = message.Username,
        text = message.Text,
        sentAt = message.SentAt
    };

    await hubContext.Clients.All.SendAsync("ReceiveMessage", messageDto);

    return Results.Ok(messageDto);
});

// Search user and check if chat already exists
app.MapGet("/users/search", async (string query, string currentUsername, AppDbContext db) =>
{
    var targetUser = await db.Users.FindAsync(query);
    if (targetUser == null || targetUser.Username == currentUsername)
    {
        return Results.NotFound(new { message = "User not found" });
    }

    var existingChat = await db.Chats
        .Where(c => c.Participants.Any(p => p.Username == currentUsername) &&
                    c.Participants.Any(p => p.Username == query) &&
                    c.Participants.Count == 2)
        .Select(c => (int?)c.ChatId)
        .FirstOrDefaultAsync();


    return Results.Ok(new
    {
        username = targetUser.Username,
        isAlreadyConnected = existingChat != null,
        chatId = existingChat
    });
});

app.MapHub<ChatHub>("/chathub");

app.Run();

public class ChatHub : Hub
{
}